using System;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Contract which exposes only the properties of the <see cref="CacheItem{T}"/> without T value.
    /// </summary>
    public interface ICacheItemProperties<TKey> where TKey : notnull
    {
        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        TKey Key { get; }

        /// <summary>
        /// Gets a value indicating whether the item is logically expired or not.
        /// Depending on the cache vendor, the item might still live in the cache although
        /// according to the expiration mode and timeout, the item is already expired.
        /// </summary>
        bool IsExpired { get; }
        
        /// <summary>
        /// Gets the creation date of the cache item.
        /// </summary>
        /// <value>The creation date.</value>
        DateTime CreatedUtc { get; }

        /// <summary>
        /// Gets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        ExpirationMode ExpirationMode { get; }

        /// <summary>
        /// Gets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        TimeSpan ExpirationTimeout { get; }

        /// <summary>
        /// Gets or sets the last accessed date of the cache item.
        /// </summary>
        /// <value>The last accessed date.</value>
        DateTime LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets a value indicating whether the cache item uses the cache handle's configured expiration.
        /// </summary>
        bool UsesExpirationDefaults { get; }
    }
}