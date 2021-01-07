using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// The origin enum indicates if the cache event was triggered locally or through the backplane.
    /// </summary>
    public enum CacheActionEventArgOrigin
    {
        /// <summary>
        /// Locally triggered action.
        /// </summary>
        Local,

        /// <summary>
        /// Remote, through the backplane triggered action.
        /// </summary>
        Remote
    }

    /// <summary>
    /// A flag indicating the reason when an item got removed from the cache.
    /// </summary>
    public enum CacheItemRemovedReason
    {
        /// <summary>
        /// A <see cref="CacheItem{T}"/> was removed because it expired.
        /// </summary>
        Expired = 0,

        /// <summary>
        /// A <see cref="CacheItem{T}"/> was removed because the underlying cache decided to remove it.
        /// This can happen if cache-specific memory limits are reached for example.
        /// </summary>
        Evicted = 1,

        /// <summary>
        /// A <see cref="CacheItem{T}"/> was removed manually, without using CacheManager APIs (like using del via redis-cli).
        /// </summary>
        /// <remarks>
        /// This will eventually trigger a <see cref="ICacheManager{TCacheValue}.OnRemoveByHandle"/> for the responsible cache layer and 
        /// <see cref="ICacheManager{TCacheValue}.OnRemove"/> as the item has been removed.
        /// </remarks>
        ExternalDelete = 99
    }

    /// <summary>
    /// Event arguments for cache actions.
    /// </summary>
    public sealed class CacheItemRemovedEventArgs<TKey> : EventArgs where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItemRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="value">The original cached value which got removed. Might be null depending on the cache sub system.</param>
        /// <param name="level">The cache level the event got triggered by.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public CacheItemRemovedEventArgs(TKey key, CacheItemRemovedReason reason, object value, int level = 0)
        {
            NotNull(key, nameof(key));

            Reason = reason;
            Key = key;
            Level = level;
            Value = value;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public TKey Key { get; }

        /// <summary>
        /// Gets the reason flag indicating details why the <see cref="CacheItem{T}"/> has been removed.
        /// </summary>
        public CacheItemRemovedReason Reason { get; }

        /// <summary>
        /// Gets a value indicating the cache level the event got triggered by.
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// Gets the original cached value which was removed by this event.
        /// <para>
        /// The property might return <c>Null</c> if the underlying cache system doesn't 
        /// support returning the value on eviction (for example Redis).
        /// </para>
        /// </summary>
        public object Value { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheItemRemovedEventArgs {Key} - {Reason} {Level}";
        }
    }

    /// <summary>
    /// Event arguments for cache actions.
    /// </summary>
    public sealed class CacheActionEventArgs<TKey> : EventArgs where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheActionEventArgs"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public CacheActionEventArgs(TKey key)
        {
            NotNull(key, nameof(key));

            Key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheActionEventArgs"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="origin">The origin the event ocured. If remote, the event got triggered by the backplane and was not actually excecuted locally.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public CacheActionEventArgs(TKey key, CacheActionEventArgOrigin origin)
            : this(key)
        {
            Origin = origin;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public TKey Key { get; }

        /// <summary>
        /// Gets the event origin indicating if the event was triggered by a local action or remotly, through the backplane.
        /// </summary>
        public CacheActionEventArgOrigin Origin { get; } = CacheActionEventArgOrigin.Local;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheActionEventArgs {Key} - {Origin}";
        }
    }

    /// <summary>
    /// Event arguments for cache clear events.
    /// </summary>
    public sealed class CacheClearEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheClearEventArgs"/> class.
        /// </summary>
        /// <param name="origin">The origin the event ocured. If remote, the event got triggered by the backplane and was not actually excecuted locally.</param>
        public CacheClearEventArgs(CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            Origin = origin;
        }

        /// <summary>
        /// Gets the event origin indicating if the event was triggered by a local action or remotly, through the backplane.
        /// </summary>
        public CacheActionEventArgOrigin Origin { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheClearEventArgs {Origin}";
        }
    }
}