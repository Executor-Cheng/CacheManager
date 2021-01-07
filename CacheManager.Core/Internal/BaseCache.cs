using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// The BaseCache class implements the overall logic of this cache library and delegates the
    /// concrete implementation of how e.g. add, get or remove should work to a derived class.
    /// <para>
    /// To use this base class simply override the abstract methods for Add, Get, Put and Remove.
    /// <br/> All other methods defined by <c>ICache</c> will be delegated to those methods.
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public abstract class BaseCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCache{TCacheValue}"/> class.
        /// </summary>
        protected internal BaseCache()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseCache{TCacheValue}"/> class.
        /// </summary>
        ~BaseCache()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger instance.</value>
        protected abstract ILogger Logger { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseCache{TCacheValue}"/> is disposed.
        /// </summary>
        /// <value><c>true</c> if disposed; otherwise, <c>false</c>.</value>
        protected bool Disposed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseCache{TCacheValue}"/> is disposing.
        /// </summary>
        /// <value><c>true</c> if disposing; otherwise, <c>false</c>.</value>
        protected bool Disposing { get; set; }

        /// <summary>
        /// Gets or sets a value for the specified key. The indexer is identical to the
        /// corresponding <see cref="Put(string, TValue)"/> and <see cref="Get(string)"/> calls.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual TValue this[TKey key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                Put(key, value);
            }
        }

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
        public virtual bool Add(TKey key, TValue value)
        {
            // null checks are done within ctor of the item
            var item = new CacheItem<TKey, TValue>(key, value);
            return Add(item);
        }

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
        public virtual bool Add(CacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            return AddInternal(item);
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public abstract bool Exists(TKey key);

        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        public virtual TValue Get(TKey key)
        {
            var item = GetCacheItem(key);

            if (item != null)
            {
                return item.Value;
            }

            throw new KeyNotFoundException("The given key was not present in the cache.");
        }

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual CacheItem<TKey, TValue> GetCacheItem(TKey key)
        {
            NotNull(key, nameof(key));

            return GetCacheItemInternal(key);
        }

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
        public virtual void Put(TKey key, TValue value)
        {
            var item = new CacheItem<TKey, TValue>(key, value);
            Put(item);
        }

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
        public virtual void Put(CacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            PutInternal(item);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual bool Remove(TKey key)
        {
            NotNull(key, nameof(key));

            return RemoveInternal(key);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected internal abstract bool AddInternal(CacheItem<TKey, TValue> item);

        /// <summary>
        /// Puts a value into the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal abstract void PutInternal(CacheItem<TKey, TValue> item);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeManaged">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposeManaged)
        {
            Disposing = true;
            if (!Disposed)
            {
                Disposed = true;
            }
            Disposing = false;
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected abstract CacheItem<TKey, TValue> GetCacheItemInternal(TKey key);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract bool RemoveInternal(TKey key);

        /// <summary>
        /// Checks if the instance is disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the instance is disposed.</exception>
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}