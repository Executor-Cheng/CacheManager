using System;
using CacheManager.Core.Internal;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public interface ICacheItem<TKey, TValue> : ICacheItemProperties<TKey> where TKey : notnull
    {
        /// <summary>
        /// Gets the cache value.
        /// </summary>
        /// <value>The cache value.</value>
        TValue Value { get; }

        ICacheItem<TKey, TValue> WithExpiration(ExpirationMode mode, TimeSpan timeout, bool usesHandleDefault = true);

        /// <summary>
        /// Creates a copy of the current cache item and sets a new absolute expiration date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="absoluteExpiration">The absolute expiration date.</param>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration);

        /// <summary>
        /// Creates a copy of the current cache item and sets a new absolute expiration date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="absoluteExpiration">The absolute expiration date.</param>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithAbsoluteExpiration(TimeSpan absoluteExpiration);

        /// <summary>
        /// Creates a copy of the current cache item with a given created date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="created">The new created date.</param>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithCreated(DateTime created);

        /// <summary>
        /// Creates a copy of the current cache item with no explicit expiration, instructing the cache to use the default defined in the cache handle configuration.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithDefaultExpiration();

        /// <summary>
        /// Creates a copy of the current cache item without expiration. Can be used to update the cache
        /// and remove any previously configured expiration of the item.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithNoExpiration();

        /// <summary>
        /// Creates a copy of the current cache item and sets a new sliding expiration value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="slidingExpiration">The sliding expiration value.</param>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpiration);

        /// <summary>
        /// Creates a copy of the current cache item with new value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="value">The new value.</param>
        /// <returns>The new instance of the cache item.</returns>
        ICacheItem<TKey, TValue> WithValue(TValue value);
    }

    /// <summary>
    /// The item which will be stored in the cache holding the cache value and additional
    /// information needed by the cache handles and manager.
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public class CacheItem<TKey, TValue> : ICacheItem<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <exception cref="System.ArgumentNullException">If key or value are null.</exception>
        public CacheItem(TKey key, TValue value)
            : this(key, value, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="expiration">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <exception cref="System.ArgumentNullException">If key or value are null.</exception>
        public CacheItem(TKey key, TValue value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, value, expiration, timeout, null, null, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        protected CacheItem()
        {
        }

        private CacheItem(TKey key, TValue value, ExpirationMode? expiration, TimeSpan? timeout, DateTime? created, DateTime? lastAccessed = null, bool expirationDefaults = true)
        {
            NotNull(key, nameof(key));
            NotNull(value, nameof(value));

            Key = key;
            Value = value;
            ExpirationMode = expiration ?? ExpirationMode.Default;
            ExpirationTimeout = (ExpirationMode == ExpirationMode.None || ExpirationMode == ExpirationMode.Default) ? TimeSpan.Zero : timeout ?? TimeSpan.Zero;
            UsesExpirationDefaults = expirationDefaults;

            // validation check for very high expiration time.
            // Otherwise this will lead to all kinds of errors (e.g. adding time to sliding while using a TimeSpan with long.MaxValue ticks)
            if (ExpirationTimeout.TotalDays > 365)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be between 00:00:00 and 365.00:00:00.");
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

        /// <inheritdoc/>
        public TKey Key { get; }

        /// <inheritdoc/>
        public TValue Value { get; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public DateTime CreatedUtc { get; }

        /// <inheritdoc/>
        public ExpirationMode ExpirationMode { get; }

        /// <inheritdoc/>
        public TimeSpan ExpirationTimeout { get; }

        /// <inheritdoc/>
        public DateTime LastAccessedUtc { get; set; }

        /// <inheritdoc/>
        public bool UsesExpirationDefaults { get; } = true;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Key}, exp:{ExpirationMode} {ExpirationTimeout}, lastAccess:{LastAccessedUtc}";
        }

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithExpiration(ExpirationMode mode, TimeSpan timeout, bool usesHandleDefault = true) =>
            new CacheItem<TKey, TValue>(Key, Value, mode, timeout, mode == ExpirationMode.Absolute ? DateTime.UtcNow : CreatedUtc, LastAccessedUtc, usesHandleDefault);

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration)
        {
            var timeout = absoluteExpiration - DateTimeOffset.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return WithExpiration(ExpirationMode.Absolute, timeout, false);
        }

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithAbsoluteExpiration(TimeSpan absoluteExpiration)
        {
            if (absoluteExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return WithExpiration(ExpirationMode.Absolute, absoluteExpiration, false);
        }

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            return WithExpiration(ExpirationMode.Sliding, slidingExpiration, false);
        }

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithNoExpiration() =>
            new CacheItem<TKey, TValue>(Key, Value, ExpirationMode.None, TimeSpan.Zero, CreatedUtc, LastAccessedUtc, false);

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithDefaultExpiration() =>
            new CacheItem<TKey, TValue>(Key, Value, ExpirationMode.Default, TimeSpan.Zero, CreatedUtc, LastAccessedUtc, true);

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithValue(TValue value) =>
            new CacheItem<TKey, TValue>(Key, value, ExpirationMode, ExpirationTimeout, CreatedUtc, LastAccessedUtc, UsesExpirationDefaults);

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> WithCreated(DateTime created) =>
            new CacheItem<TKey, TValue>(Key, Value, ExpirationMode, ExpirationTimeout, created, LastAccessedUtc, UsesExpirationDefaults);
    }
}
