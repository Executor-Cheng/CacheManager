using System;
using CacheManager.Core.Internal;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// The item which will be stored in the cache holding the cache value and additional
    /// information needed by the cache handles and manager.
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public class CacheItem<TValue> : ICacheItemProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <exception cref="System.ArgumentNullException">If key or value are null.</exception>
        public CacheItem(string key, TValue value)
            : this(key, null, value, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If key, value or region are null.</exception>
        public CacheItem(string key, string region, TValue value)
            : this(key, region, value, null, null, null)
        {
            NotNullOrWhiteSpace(region, nameof(region));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="expiration">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <exception cref="System.ArgumentNullException">If key or value are null.</exception>
        public CacheItem(string key, TValue value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, null, value, expiration, timeout, null, null, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="expiration">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <exception cref="System.ArgumentNullException">If key, value or region are null.</exception>
        public CacheItem(string key, string region, TValue value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, region, value, expiration, timeout, null, null, false)
        {
            NotNullOrWhiteSpace(region, nameof(region));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        protected CacheItem()
        {
        }

        private CacheItem(string key, string region, TValue value, ExpirationMode? expiration, TimeSpan? timeout, DateTime? created, DateTime? lastAccessed = null, bool expirationDefaults = true)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(value, nameof(value));

            Key = key;
            Region = region;
            Value = value;
            ValueType = value.GetType();
            ExpirationMode = expiration ?? ExpirationMode.Default;
            ExpirationTimeout = (ExpirationMode == ExpirationMode.None || ExpirationMode == ExpirationMode.Default) ? TimeSpan.Zero : timeout ?? TimeSpan.Zero;
            UsesExpirationDefaults = expirationDefaults;

            // validation check for very high expiration time.
            // Otherwise this will lead to all kinds of errors (e.g. adding time to sliding while using a TimeSpan with long.MaxValue ticks)
            if (ExpirationTimeout.TotalDays > 365)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be between 00:00:00 and 365:00:00:00.");
            }

            if (ExpirationMode != ExpirationMode.Default && ExpirationMode != ExpirationMode.None && ExpirationTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be greater than zero if expiration mode is defined.");
            }

            if (created.HasValue && created.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Created date kind must be {DateTimeKind.Utc}.", nameof(created));
            }

            if (lastAccessed.HasValue && lastAccessed.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Last accessed date kind must be {DateTimeKind.Utc}.", nameof(lastAccessed));
            }

            CreatedUtc = created ?? DateTime.UtcNow;
            LastAccessedUtc = lastAccessed ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a value indicating whether the item is logically expired or not.
        /// Depending on the cache vendor, the item might still live in the cache although
        /// according to the expiration mode and timeout, the item is already expired.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                var now = DateTime.UtcNow;
                if (ExpirationMode == ExpirationMode.Absolute
                    && CreatedUtc.Add(ExpirationTimeout) < now)
                {
                    return true;
                }
                else if (ExpirationMode == ExpirationMode.Sliding
                    && LastAccessedUtc.Add(ExpirationTimeout) < now)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the creation date of the cache item.
        /// </summary>
        /// <value>The creation date.</value>
        public DateTime CreatedUtc { get; }

        /// <summary>
        /// Gets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        public ExpirationMode ExpirationMode { get; }

        /// <summary>
        /// Gets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        public TimeSpan ExpirationTimeout { get; }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets or sets the last accessed date of the cache item.
        /// </summary>
        /// <value>The last accessed date.</value>
        public DateTime LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets the cache region.
        /// </summary>
        /// <value>The cache region.</value>
        public string Region { get; }

        /// <summary>
        /// Gets the cache value.
        /// </summary>
        /// <value>The cache value.</value>
        public TValue Value { get; }

        /// <summary>
        /// Gets the type of the cache value.
        /// <para>This might be used for serialization and deserialization.</para>
        /// </summary>
        /// <value>The type of the cache value.</value>
        public Type ValueType { get; }

        /// <summary>
        /// Gets a value indicating whether the cache item uses the cache handle's configured expiration.
        /// </summary>
        public bool UsesExpirationDefaults { get; } = true;

        /// <inheritdoc />
        public override string ToString()
        {
            return !string.IsNullOrWhiteSpace(Region) ?
                $"'{Region}:{Key}', exp:{ExpirationMode} {ExpirationTimeout}, lastAccess:{LastAccessedUtc}"
                : $"'{Key}', exp:{ExpirationMode} {ExpirationTimeout}, lastAccess:{LastAccessedUtc}";
        }

        internal CacheItem<TValue> WithExpiration(ExpirationMode mode, TimeSpan timeout, bool usesHandleDefault = true) =>
            new CacheItem<TValue>(Key, Region, Value, mode, timeout, mode == ExpirationMode.Absolute ? DateTime.UtcNow : CreatedUtc, LastAccessedUtc, usesHandleDefault);

        /// <summary>
        /// Creates a copy of the current cache item and sets a new absolute expiration date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="absoluteExpiration">The absolute expiration date.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration)
        {
            var timeout = absoluteExpiration - DateTimeOffset.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return WithExpiration(ExpirationMode.Absolute, timeout, false);
        }

        /// <summary>
        /// Creates a copy of the current cache item and sets a new absolute expiration date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="absoluteExpiration">The absolute expiration date.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithAbsoluteExpiration(TimeSpan absoluteExpiration)
        {
            if (absoluteExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return WithExpiration(ExpirationMode.Absolute, absoluteExpiration, false);
        }

        /// <summary>
        /// Creates a copy of the current cache item and sets a new sliding expiration value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="slidingExpiration">The sliding expiration value.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithSlidingExpiration(TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            return WithExpiration(ExpirationMode.Sliding, slidingExpiration, false);
        }

        /// <summary>
        /// Creates a copy of the current cache item without expiration. Can be used to update the cache
        /// and remove any previously configured expiration of the item.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithNoExpiration() =>
            new CacheItem<TValue>(Key, Region, Value, ExpirationMode.None, TimeSpan.Zero, CreatedUtc, LastAccessedUtc, false);

        /// <summary>
        /// Creates a copy of the current cache item with no explicit expiration, instructing the cache to use the default defined in the cache handle configuration.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithDefaultExpiration() =>
            new CacheItem<TValue>(Key, Region, Value, ExpirationMode.Default, TimeSpan.Zero, CreatedUtc, LastAccessedUtc, true);

        /// <summary>
        /// Creates a copy of the current cache item with new value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="value">The new value.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithValue(TValue value) =>
            new CacheItem<TValue>(Key, Region, value, ExpirationMode, ExpirationTimeout, CreatedUtc, LastAccessedUtc, UsesExpirationDefaults);

        /// <summary>
        /// Creates a copy of the current cache item with a given created date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="created">The new created date.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<TValue> WithCreated(DateTime created) =>
            new CacheItem<TValue>(Key, Region, Value, ExpirationMode, ExpirationTimeout, created, LastAccessedUtc, UsesExpirationDefaults);
    }
}
