using CacheManager.Core.Internal;
using System;

namespace CacheManager.Core.Configuration
{
    /// <summary>
    /// The basic cache manager configuration class.
    /// </summary>
    public sealed class CacheManagerConfiguration<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerConfiguration"/> class.
        /// </summary>
        public CacheManagerConfiguration()
        {
        }

        /// <summary>
        /// Gets or sets the name of the cache.
        /// </summary>
        /// <value>The name of the cache.</value>
        public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the <see cref="UpdateMode"/> for the cache manager instance.
        /// <para>
        /// Drives the behavior of the cache manager how it should update the different cache
        /// handles it manages.
        /// </para>
        /// </summary>
        /// <value>The cache update mode.</value>
        /// <see cref="UpdateMode"/>
        public CacheUpdateMode UpdateMode { get; set; } = CacheUpdateMode.Up;

        /// <summary>
        /// Gets or sets the limit of the number of retry operations per action.
        /// <para>Default is 50.</para>
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaxRetries { get; set; } = 50;

        /// <summary>
        /// Gets or sets the number of milliseconds the cache should wait before it will retry an action.
        /// <para>Default is 100.</para>
        /// </summary>
        /// <value>The retry timeout.</value>
        public int RetryTimeout { get; set; } = 100;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name}: {UpdateMode} with {MaxRetries} {(MaxRetries > 1 ? "retries" : "retry")}, timeout={RetryTimeout}s";
        }
    }

    public class CacheHandleConfiguration<TKey, TValue, THandle> : CacheHandleConfiguration where TKey : notnull 
                                                                                            where THandle : IBaseCacheHandle<TKey, TValue>
    {

    }

    public abstract class CacheHandleConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandleConfiguration"/> class.
        /// </summary>
        public CacheHandleConfiguration()
        {
            Name = Key = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets or sets a value indicating whether performance counters should be enabled or not.
        /// <para>
        /// If enabled, and the initialization of performance counters doesn't work, for example
        /// because of security reasons. The counters will get disabled silently.
        /// </para>
        /// </summary>
        /// <value><c>true</c> if performance counters should be enable; otherwise, <c>false</c>.</value>
        public bool EnablePerformanceCounters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether statistics should be enabled.
        /// </summary>
        /// <value><c>true</c> if statistics should be enabled; otherwise, <c>false</c>.</value>
        public bool EnableStatistics { get; set; }

        /// <summary>
        /// Gets or sets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        public ExpirationMode ExpirationMode { get; set; }

        /// <summary>
        /// Gets or sets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        public TimeSpan ExpirationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the name for the cache handle which is also the identifier of the configuration.
        /// </summary>
        /// <value>The name of the handle.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the configuration key.
        /// Some cache handles require to reference another part of the configuration by name.
        /// If not specified, the <see cref="Name"/> will be used instead.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is backplane source.
        /// <para>
        /// Only one cache handle inside one cache manager can be backplane source. Usually this is
        /// a distributed cache. It might not make any sense to define an in process cache as backplane source.
        /// </para>
        /// <para>If no backplane is configured for the cache, this setting will have no effect.</para>
        /// </summary>
        /// <value><c>true</c> if this instance should be backplane source; otherwise, <c>false</c>.</value>
        public bool IsBackplaneSource { get; set; }
    }
}