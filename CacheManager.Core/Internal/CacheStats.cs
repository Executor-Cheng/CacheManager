using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// <para>Stores statistical information for a <see cref="BaseCacheHandle{TCacheValue}"/>.</para>
    /// <para>
    /// Statistical counters are stored globally for the <see cref="BaseCacheHandle{TCacheValue}"/>
    /// and for each cache region!
    /// </para>
    /// <para>
    /// To retrieve a counter for a region only, specify the optional region attribute of GetStatistics.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The class is primarily used internally. Only the GetStatistics is visible. Therefore the
    /// class is sealed.
    /// </remarks>
    /// <typeparam name="TValue">Inherited object type of the owning cache handle.</typeparam>
    public sealed class CacheStats<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly CacheStatsCounter _counter;
        private readonly bool _isPerformanceCounterEnabled;
        private readonly bool _isStatsEnabled;
        private readonly CachePerformanceCounters<TKey, TValue> _performanceCounters;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheStats{TCacheValue}"/> class.
        /// </summary>
        /// <param name="cacheName">Name of the cache.</param>
        /// <param name="handleName">Name of the handle.</param>
        /// <param name="enabled">
        /// If set to <c>true</c> the stats are enabled. Otherwise any statistics and performance
        /// counters will be disabled.
        /// </param>
        /// <param name="enablePerformanceCounters">
        /// If set to <c>true</c> performance counters and statistics will be enabled.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// If cacheName or handleName are null.
        /// </exception>
        public CacheStats(string cacheName, string handleName, bool enabled = true, bool enablePerformanceCounters = false)
        {
            NotNullOrWhiteSpace(cacheName, nameof(cacheName));
            NotNullOrWhiteSpace(handleName, nameof(handleName));

            // if performance counters are enabled, stats must be enabled, too.
            _isStatsEnabled = enablePerformanceCounters || enabled;
            _isPerformanceCounterEnabled = enablePerformanceCounters;
            _counter = new CacheStatsCounter();

            if (_isPerformanceCounterEnabled)
            {
                _performanceCounters = new CachePerformanceCounters<TKey, TValue>(cacheName, handleName, this);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CacheStats{TCacheValue}"/> class.
        /// </summary>
        ~CacheStats()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <para>
        /// Returns the corresponding statistical information of the
        /// <see cref="CacheStatsCounterType"/> type.
        /// </para>
        /// <para>
        /// If the cache handles is configured to disable statistics, the method will always return zero.
        /// </para>
        /// </summary>
        /// <remarks>
        /// In multithreaded environments the counters can be changed while reading. Do not rely on
        /// those counters as they might not be 100% accurate.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var cache = CacheFactory.FromConfiguration("myCache");
        ///
        /// foreach (var handle in cache.CacheHandles)
        /// {
        ///    var stats = handle.Stats;
        ///    Console.WriteLine(string.Format(
        ///            "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
        ///                stats.GetStatistic(CacheStatsCounterType.Items),
        ///                stats.GetStatistic(CacheStatsCounterType.Hits),
        ///                stats.GetStatistic(CacheStatsCounterType.Misses),
        ///                stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.ClearCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.AddCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.PutCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.GetCalls)
        ///            ));
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="type">The stats type to retrieve the number for.</param>
        /// <returns>A number representing the counts for the specified <see cref="CacheStatsCounterType"/>.</returns>
        public long GetStatistic(CacheStatsCounterType type) 
        {
            if (!_isStatsEnabled)
            {
                return 0L;
            }
            return _counter.Get(type);
        }

        /// <summary>
        /// Called when an item gets added to the cache.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        public void OnAdd(CacheItem<TKey, TValue> item)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            NotNull(item, nameof(item));

            _counter.Increment(CacheStatsCounterType.AddCalls);
            _counter.Increment(CacheStatsCounterType.Items);
        }

        /// <summary>
        /// Called when the cache got cleared.
        /// </summary>
        public void OnClear()
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            // clear needs a lock, otherwise we might mess up the overall counts
            _counter.Set(CacheStatsCounterType.Items, 0L);
            _counter.Increment(CacheStatsCounterType.ClearCalls);
        }

        /// <summary>
        /// Called when cache Get got invoked.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnGet()
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            _counter.Increment(CacheStatsCounterType.GetCalls);
        }

        /// <summary>
        /// Called when a Get was successful.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnHit()
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            _counter.Increment(CacheStatsCounterType.Hits);
        }

        /// <summary>
        /// Called when a Get was not successful.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnMiss()
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            _counter.Increment(CacheStatsCounterType.Misses);
        }

        /// <summary>
        /// Called when an item got updated.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemAdded">If <c>true</c> the item didn't exist and has been added.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        public void OnPut(CacheItem<TKey, TValue> item, bool itemAdded)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            NotNull(item, nameof(item));

            _counter.Increment(CacheStatsCounterType.PutCalls);

            if (itemAdded)
            {
                _counter.Increment(CacheStatsCounterType.Items);
            }
        }

        /// <summary>
        /// Called when an item has been removed from the cache.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnRemove()
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            _counter.Increment(CacheStatsCounterType.RemoveCalls);
            _counter.Decrement(CacheStatsCounterType.Items);
        }

        /// <summary>
        /// Called when an item has been updated.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="result">The result.</param>
        /// <exception cref="System.ArgumentNullException">If key or result are null.</exception>
        public void OnUpdate(TKey key, UpdateItemResult<TKey, TValue> result)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            NotNull(key, nameof(key));
            NotNull(result, nameof(result));

            _counter.Add(CacheStatsCounterType.GetCalls, result.NumberOfTriesNeeded);
            _counter.Add(CacheStatsCounterType.Hits, result.NumberOfTriesNeeded);
            _counter.Increment(CacheStatsCounterType.PutCalls);
        }

        private void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (_isPerformanceCounterEnabled)
                {
                    _performanceCounters.Dispose();
                }
            }
        }
    }
}
