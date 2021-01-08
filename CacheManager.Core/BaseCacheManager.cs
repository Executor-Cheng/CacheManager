using CacheManager.Core.Configuration;
using CacheManager.Core.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// The <see cref="BaseCacheManager{TCacheValue}"/> implements <see cref="ICacheManager{TCacheValue}"/> and is the main class
    /// of this library.
    /// The cache manager delegates all cache operations to the list of <see cref="BaseCacheHandle{T}"/>'s which have been
    /// added. It will keep them in sync according to rules and depending on the configuration.
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public partial class BaseCacheManager<TKey, TValue> : BaseCache<TKey, TValue>, ICacheManager<TKey, TValue>, IDisposable where TKey : notnull
    {
        private readonly bool _logTrace = false;
        private readonly IBaseCacheHandle<TKey, TValue>[] _cacheHandles;
        private readonly ICacheBackplane<TKey, TValue> _cacheBackplane;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheManager{TCacheValue}"/> class
        /// using the specified <paramref name="configuration"/>.
        /// If the name of the <paramref name="configuration"/> is defined, the cache manager will
        /// use it. Otherwise a random string will be generated.
        /// </summary>
        /// <param name="configuration">
        /// The configuration which defines the structure and complexity of the cache manager.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// When <paramref name="configuration"/> is null.
        /// </exception>
        /// <see cref="CacheFactory"/>
        /// <see cref="ConfigurationBuilder"/>
        /// <see cref="BaseCacheHandle{TCacheValue}"/>
        public BaseCacheManager(IServiceProvider sp,
                                ILogger<BaseCacheManager<TKey, TValue>> logger,
                                IOptions<CacheManagerConfiguration<TKey, TValue>> configuration,
                                IEnumerable<IBaseCacheHandle<TKey, TValue>> handles)
        {
            CacheManagerConfiguration<TKey, TValue> config = configuration.Value;
            NotNullOrWhiteSpace(config.Name, nameof(config.Name));

            Configuration = configuration.Value;

            //var serializer = CacheReflectionHelper.CreateSerializer(configuration, loggerFactory);

            Logger = logger;

            _logTrace = Logger.IsEnabled(LogLevel.Trace);

            Logger.LogInformation("Cache manager: adding cache handles...");

            try
            {
                _cacheHandles = handles.ToArray();//CacheReflectionHelper.CreateCacheHandles(this, loggerFactory, serializer).ToArray();

                var index = 0;
                foreach (var handle in _cacheHandles)
                {
                    var handleIndex = index;
                    handle.OnCacheSpecificRemove += (sender, args) =>
                    {
                        // added sync for using backplane with in-memory caches on cache specific removal
                        // but commented for now, this is not really needed if all instances use the same expiration etc, would just cause dublicated events
                        ////if (_cacheBackplane != null && handle.Configuration.IsBackplaneSource && !handle.IsDistributedCache)
                        ////{
                        ////    if (string.IsNullOrEmpty(args.Region))
                        ////    {
                        ////        _cacheBackplane.NotifyRemove(args.Key);
                        ////    }
                        ////    else
                        ////    {
                        ////        _cacheBackplane.NotifyRemove(args.Key, args.Region);
                        ////    }
                        ////}

                        // base cache handle does logging for this

                        if (Configuration.UpdateMode == CacheUpdateMode.Up)
                        {
                            if (_logTrace)
                            {
                                Logger.LogTrace($"Cleaning handles above '{handleIndex}' because of remove event.");
                            }

                            EvictFromHandlesAbove(args.Key, handleIndex);
                        }

                        // moving down below cleanup, optherwise the item could still be in memory
                        TriggerOnRemoveByHandle(args.Key, args.Reason, handleIndex + 1, args.Value);
                    };

                    index++;
                }

                _cacheBackplane = sp.GetService<ICacheBackplane<TKey, TValue>>();
                if (_cacheBackplane != null)
                {
                    RegisterCacheBackplane(_cacheBackplane);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error occurred while creating the cache manager.");
                throw;
            }
        }

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs<TKey>> OnAdd;

        /// <inheritdoc />
        public event EventHandler<CacheClearEventArgs> OnClear;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs<TKey>> OnGet;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs<TKey>> OnPut;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs<TKey>> OnRemove;

        /// <inheritdoc />
        public event EventHandler<CacheItemRemovedEventArgs<TKey>> OnRemoveByHandle;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs<TKey>> OnUpdate;

        /// <inheritdoc />
        public CacheManagerConfiguration<TKey, TValue> Configuration { get; }

        /// <inheritdoc />
        public IEnumerable<IBaseCacheHandle<TKey, TValue>> CacheHandles => new ReadOnlyCollection<IBaseCacheHandle<TKey, TValue>>(_cacheHandles);

        /// <summary>
        /// Gets the configured cache backplane.
        /// </summary>
        /// <value>The backplane.</value>
        public ICacheBackplane<TKey, TValue> Backplane => _cacheBackplane;

        /// <summary>
        /// Gets the cache name.
        /// </summary>
        /// <value>The name of the cache.</value>
        public string Name => Configuration.Name;

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <inheritdoc />
        public override void Clear()
        {
            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace("Clear: flushing cache...");
            }

            foreach (var handle in _cacheHandles)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Clear: clearing handle {0}.", handle.Configuration.Name);
                }

                handle.Clear();
                handle.Stats.OnClear();
            }

            if (_cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Clear: notifies backplane.");
                }

                _cacheBackplane.NotifyClear();
            }

            TriggerOnClear();
        }

        /// <inheritdoc />
        public override bool Exists(TKey key)
        {
            foreach (var handle in _cacheHandles)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Checking if [{0}] exists on handle '{1}'.", key, handle.Configuration.Name);
                }

                if (handle.Exists(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"Name: {Name}, Handles: [{string.Join(", ", _cacheHandles.Select(p => p.GetType().Name))}]";

        /// <inheritdoc />
        protected internal override bool AddInternal(ICacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace($"Add [{item}] started.");
            }

            var handleIndex = _cacheHandles.Length - 1;

            var result = AddItemToHandle(item, _cacheHandles[handleIndex]);

            // evict from other handles in any case because if it exists, it might be a different version
            // if not exist, its just a sanity check to invalidate other versions in upper layers.
            EvictFromOtherHandles(item.Key, handleIndex);

            if (result)
            {
                // update backplane
                if (_cacheBackplane != null)
                {
                    _cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Add);

                    if (_logTrace)
                    {
                        Logger.LogTrace($"Notified backplane 'change' because [{item}] was added.");
                    }
                }

                // trigger only once and not per handle and only if the item was added!
                TriggerOnAdd(item.Key);
            }

            return result;
        }

        /// <inheritdoc />
        protected internal override void PutInternal(ICacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace($"Put [{item}] started.");
            }

            foreach (var handle in _cacheHandles)
            {
                if (handle.Configuration.EnableStatistics)
                {
                    // check if it is really a new item otherwise the items count is crap because we
                    // count it every time, but use only the current handle to retrieve the item,
                    // otherwise we would trigger gets and find it in another handle maybe
                    var oldItem = handle.GetCacheItem(item.Key);
                    
                    handle.Stats.OnPut(item, oldItem == null);
                }

                if (_logTrace)
                {
                    Logger.LogTrace($"Put [{item.Key}] successfully to handle '{handle.Configuration.Name}'.");
                }

                handle.Put(item);
            }

            // update backplane
            if (_cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace($"Put [{item.Key}] was scuccessful. Notifying backplane [change].");
                }
                _cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Put);
            }

            TriggerOnPut(item.Key);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (var handle in _cacheHandles)
                {
                    handle.Dispose();
                }

                if (_cacheBackplane != null)
                {
                    _cacheBackplane.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        /// <inheritdoc />
        protected override ICacheItem<TKey, TValue> GetCacheItemInternal(TKey key)
        {
            CheckDisposed();

            ICacheItem<TKey, TValue> cacheItem = null;

            if (_logTrace)
            {
                Logger.LogTrace($"Get [{key}] started.");
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                var handle = _cacheHandles[handleIndex];
                cacheItem = handle.GetCacheItem(key);

                handle.Stats.OnGet();

                if (cacheItem != null)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Get [{key}], found in handle[{handleIndex}] '{handle.Configuration.Name}'.");
                    }

                    // update last accessed, might be used for custom sliding implementations
                    cacheItem.LastAccessedUtc = DateTime.UtcNow;

                    // update other handles if needed
                    AddToHandles(cacheItem, handleIndex);
                    handle.Stats.OnHit();
                    TriggerOnGet(key);
                    break;
                }
                else
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Get [{key}], item NOT found in handle[{handleIndex}] '{handle.Configuration.Name}'.");
                    }

                    handle.Stats.OnMiss();
                }
            }

            return cacheItem;
        }

        /// <inheritdoc />
        protected override bool RemoveInternal(TKey key)
        {
            CheckDisposed();

            var result = false;

            if (_logTrace)
            {
                Logger.LogTrace($"Removing [{key}].");
            }

            foreach (var handle in _cacheHandles)
            {
                var handleResult = handle.Remove(key);

                if (handleResult)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Remove [{key}], successfully removed from handle '{handle.Configuration.Name}'.");
                    }

                    result = true;
                    handle.Stats.OnRemove();
                }
            }

            if (result)
            {
                // update backplane
                if (_cacheBackplane != null)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Removed [{key}], notifying backplane [remove].");
                    }

                    _cacheBackplane.NotifyRemove(key);
                }

                // trigger only once and not per handle
                TriggerOnRemove(key);
            }

            return result;
        }

        private static bool AddItemToHandle(ICacheItem<TKey, TValue> item, IBaseCacheHandle<TKey, TValue> handle)
        {
            if (handle.Add(item))
            {
                handle.Stats.OnAdd(item);
                return true;
            }

            return false;
        }

        private static void ClearHandles(IBaseCacheHandle<TKey, TValue>[] handles)
        {
            foreach (var handle in handles)
            {
                handle.Clear();
                handle.Stats.OnClear();
            }

            ////this.TriggerOnClear();
        }

        private void EvictFromHandles(TKey key, IBaseCacheHandle<TKey, TValue>[] handles)
        {
            foreach (var handle in handles)
            {
                EvictFromHandle(key, handle);
            }
        }

        private void EvictFromHandle(TKey key, IBaseCacheHandle<TKey, TValue> handle)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"Evicting '{key}' from handle '{handle.Configuration.Name}'.");
            }

            bool result = handle.Remove(key);
            if (result)
            {
                handle.Stats.OnRemove();
            }
        }

        private void AddToHandles(ICacheItem<TKey, TValue> item, int foundIndex)
        {
            if (_logTrace)
            {
                Logger.LogTrace($"Start updating handles with [{item}].");
            }

            if (foundIndex == 0)
            {
                return;
            }

            // update all cache handles with lower order, up the list
            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                if (handleIndex < foundIndex)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Updating handles, added [{0}] to handle '{1}'.", item, _cacheHandles[handleIndex].Configuration.Name);
                    }

                    _cacheHandles[handleIndex].Add(item);
                }
            }
        }

        private void AddToHandlesBelow(ICacheItem<TKey, TValue> item, int foundIndex)
        {
            if (item == null)
            {
                return;
            }

            if (_logTrace)
            {
                Logger.LogTrace("Add [{0}] to handles below handle '{1}'.", item, foundIndex);
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                if (handleIndex > foundIndex)
                {
                    if (_cacheHandles[handleIndex].Add(item))
                    {
                        _cacheHandles[handleIndex].Stats.OnAdd(item);
                    }
                }
            }
        }

        private void EvictFromOtherHandles(TKey key, int excludeIndex)
        {
            if (excludeIndex < 0 || excludeIndex >= _cacheHandles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(excludeIndex));
            }

            if (_logTrace)
            {
                Logger.LogTrace($"Evict [{key}] from other handles excluding handle '{excludeIndex}'.");
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                if (handleIndex != excludeIndex)
                {
                    EvictFromHandle(key, _cacheHandles[handleIndex]);
                }
            }
        }

        private void EvictFromHandlesAbove(TKey key, int excludeIndex)
        {
            if (_logTrace)
            {
                Logger.LogTrace("Evict from handles above: {0}: above handle {1}.", key, excludeIndex);
            }

            if (excludeIndex < 0 || excludeIndex >= _cacheHandles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(excludeIndex));
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                if (handleIndex < excludeIndex)
                {
                    EvictFromHandle(key, _cacheHandles[handleIndex]);
                }
            }
        }

        private void RegisterCacheBackplane(ICacheBackplane<TKey, TValue> backplane)
        {
            // this should have been checked during activation already, just to be totally sure...
            if (_cacheHandles.Any(p => p.Configuration.IsBackplaneSource))
            {
                // added includeSource param to get the handles which need to be synced.
                // in case the backplane source is non-distributed (in-memory), only remotly triggered remove and clear should also
                // trigger a sync locally. For distribtued caches, we expect that the distributed cache is already the source and in sync
                // as that's the layer which triggered the event. In this case, only other in-memory handles above the distribtued, would be synced.
                var handles = new Func<bool, IBaseCacheHandle<TKey, TValue>[]>((includSource) =>
                {
                    var handleList = new List<IBaseCacheHandle<TKey, TValue>>();
                    foreach (var handle in _cacheHandles)
                    {
                        if (!handle.Configuration.IsBackplaneSource ||
                            (includSource && handle.Configuration.IsBackplaneSource && !handle.IsDistributedCache))
                        {
                            handleList.Add(handle);
                        }
                    }
                    return handleList.ToArray();
                });

                backplane.Changed += (sender, args) =>
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug($"Backplane event: [Changed] for '{args.Key}'.");
                    }

                    EvictFromHandles(args.Key, handles(false));
                    switch (args.Action)
                    {
                        case CacheItemChangedEventAction.Add:
                            TriggerOnAdd(args.Key, CacheActionEventArgOrigin.Remote);
                            break;

                        case CacheItemChangedEventAction.Put:
                            TriggerOnPut(args.Key, CacheActionEventArgOrigin.Remote);
                            break;

                        case CacheItemChangedEventAction.Update:
                            TriggerOnUpdate(args.Key, CacheActionEventArgOrigin.Remote);
                            break;
                    }
                };

                backplane.Removed += (sender, args) =>
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Backplane event: [Remove] of {args.Key}.");
                    }

                    EvictFromHandles(args.Key, handles(true));
                    TriggerOnRemove(args.Key, CacheActionEventArgOrigin.Remote);
                };

                backplane.Cleared += (sender, args) =>
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Backplane event: [Clear].");
                    }

                    ClearHandles(handles(true));
                    TriggerOnClear(CacheActionEventArgOrigin.Remote);
                };
            }
        }

        private void TriggerOnAdd(TKey key, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            OnAdd?.Invoke(this, new CacheActionEventArgs<TKey>(key, origin));
        }

        private void TriggerOnClear(CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            OnClear?.Invoke(this, new CacheClearEventArgs(origin));
        }

        private void TriggerOnGet(TKey key, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            OnGet?.Invoke(this, new CacheActionEventArgs<TKey>(key, origin));
        }

        private void TriggerOnPut(TKey key, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            OnPut?.Invoke(this, new CacheActionEventArgs<TKey>(key, origin));
        }

        private void TriggerOnRemove(TKey key, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            NotNull(key, nameof(key));
            OnRemove?.Invoke(this, new CacheActionEventArgs<TKey>(key, origin));
        }

        private void TriggerOnRemoveByHandle(TKey key, CacheItemRemovedReason reason, int level, object value)
        {
            NotNull(key, nameof(key));
            OnRemoveByHandle?.Invoke(this, new CacheItemRemovedEventArgs<TKey>(key, reason, value, level));
        }

        private void TriggerOnUpdate(TKey key, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            OnUpdate?.Invoke(this, new CacheActionEventArgs<TKey>(key, origin));
        }
    }
}
