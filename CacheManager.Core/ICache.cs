using System;
using System.Collections.Generic;

namespace CacheManager.Core
{
    /// <summary>
    /// This interface is the base contract for the main stack of this library.
    /// <para>
    /// The <c>ICacheHandle</c> and <c>ICacheManager</c> interfaces are derived from <c>ICache</c>,
    /// meaning the method call signature throughout the stack is very similar.
    /// </para>
    /// <para>
    /// We want the flexibility of having a simple get/put/delete cache up to multiple caches
    /// layered on top of each other, still using the same simple and easy to understand interface.
    /// </para>
    /// <para>
    /// The <c>TCacheValue</c> can, but most not be used in the sense of strongly typing. This
    /// means, you can define and configure a cache for certain object types within your domain. But
    /// you can also use <c>object</c> and store anything you want within the cache. All underlying
    /// cache technologies usually do not care about types of the cache items.
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public interface ICache<TKey, TValue> : IDisposable where TKey : notnull
    {
        /// <summary>
        /// Gets or sets a value for the specified key. The indexer is identical to the
        /// corresponding <see cref="Put(string, TValue)"/> and <see cref="Get(string)"/> calls.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        TValue this[TKey key] { get; set; }

        /// <summary>
        /// Adds a value for the specified key to the cache.
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="key"/> already exists within the cache!
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        bool Add(TKey key, TValue value);

        /// <summary>
        /// Adds the specified <c>CacheItem</c> to the cache.
        /// <para>
        /// Use this overload to overrule the configured expiration settings of the cache and to
        /// define a custom expiration for this <paramref name="item"/> only.
        /// </para>
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="item"/> already exists within the cache!
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="item"/> or the item's key or value is null.
        /// </exception>
        bool Add(ICacheItem<TKey, TValue> item);

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns a value indicating if the <paramref name="key"/> exists in at least one cache layer
        /// configured in CacheManger, without actually retrieving it from the cache.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns><c>True</c> if the <paramref name="key"/> exists, <c>False</c> otherwise.</returns>
        bool Exists(TKey key);

        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">If the <paramref name="key"/> was not presented in the cache.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        TValue Get(TKey key);

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        ICacheItem<TKey, TValue> GetCacheItem(TKey key);

        /// <summary>
        /// Puts a value for the specified key into the cache.
        /// <para>
        /// If the <paramref name="key"/> already exists within the cache, the existing value will
        /// be replaced with the new <paramref name="value"/>.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        void Put(TKey key, TValue value);

        /// <summary>
        /// Puts the specified <c>CacheItem</c> into the cache.
        /// <para>
        /// If the <paramref name="item"/> already exists within the cache, the existing item will
        /// be replaced with the new <paramref name="item"/>.
        /// </para>
        /// <para>
        /// Use this overload to overrule the configured expiration settings of the cache and to
        /// define a custom expiration for this <paramref name="item"/> only.
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be cached.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="item"/> or the item's key or value is null.
        /// </exception>
        void Put(ICacheItem<TKey, TValue> item);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        bool Remove(TKey key);
    }
}