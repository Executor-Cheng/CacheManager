using CacheManager.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// This handle is for internal use and testing. It does not implement any expiration.
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public class DictionaryCacheHandle<TKey, TValue> : BaseCacheHandle<TKey, TValue> where TKey : notnull
    {
        private const int ScanInterval = 5000;
        private readonly static Random _random = new Random();
        private readonly ConcurrentDictionary<TKey, CacheItem<TKey, TValue>> _cache;
        private readonly Timer _timer;

        private int _scanRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public DictionaryCacheHandle(ILogger<DictionaryCacheHandle<TKey, TValue>> logger,
                                     IOptions<CacheManagerConfiguration<TKey, TValue>> managerConfiguration, 
                                     IOptions<CacheHandleConfiguration<TKey, TValue, DictionaryCacheHandle<TKey, TValue>>> configuration)
            : base(managerConfiguration, configuration)
        {
            Logger = logger;
            _cache = new ConcurrentDictionary<TKey, CacheItem<TKey, TValue>>();
            _timer = new Timer(TimerLoop, null, _random.Next(1000, ScanInterval), ScanInterval);
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => _cache.Count;

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear() => _cache.Clear();

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                _timer.Dispose();
            }
            base.Dispose(disposeManaged);
        }

        /// <inheritdoc />
        public override bool Exists(TKey key)
        {
            NotNull(key, nameof(key));
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected override bool AddInternalPrepared(CacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            return _cache.TryAdd(item.Key, item);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TKey, TValue> GetCacheItemInternal(TKey key)
        {
            if (_cache.TryGetValue(key, out CacheItem<TKey, TValue> result))
            {
                if (result.ExpirationMode != ExpirationMode.None && IsExpired(result, DateTime.UtcNow))
                {
                    _cache.TryRemove(key, out CacheItem<TKey, TValue> _);
                    TriggerCacheSpecificRemove(key, CacheItemRemovedReason.Expired, result.Value);
                    return null;
                }
            }
            return result;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected override void PutInternalPrepared(CacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            _cache[item.Key] = item;
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(TKey key)
        {
            return _cache.TryRemove(key, out CacheItem<TKey, TValue> _);
        }

        private static bool IsExpired(CacheItem<TKey, TValue> item, DateTime now)
        {
            return (item.ExpirationMode == ExpirationMode.Absolute && item.CreatedUtc.Add(item.ExpirationTimeout) < now) ||
                   (item.ExpirationMode == ExpirationMode.Sliding && item.LastAccessedUtc.Add(item.ExpirationTimeout) < now);
        }

        private void TimerLoop(object state)
        {
            if (_scanRunning > 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _scanRunning, 1, 0) == 0)
            {
                try
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("'{0}' starting eviction scan.", Configuration.Name);
                    }

                    ScanForExpiredItems();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred during eviction scan.");
                }
                finally
                {
                    Interlocked.Exchange(ref _scanRunning, 0);
                }
            }
        }

        private int ScanForExpiredItems()
        {
            var removed = 0;
            var now = DateTime.UtcNow;
            foreach (var item in _cache.Values)
            {
                if (IsExpired(item, now))
                {
                    RemoveInternal(item.Key);

                    // trigger global eviction event
                    TriggerCacheSpecificRemove(item.Key, CacheItemRemovedReason.Expired, item.Value);

                    // fix stats
                    Stats.OnRemove();
                    removed++;
                }
            }

            if (removed > 0 && Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation("'{0}' removed '{1}' expired items during eviction run.", Configuration.Name, removed);
            }

            return removed;
        }
    }
}