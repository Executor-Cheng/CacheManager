using CacheManager.Core.Configuration;
using Microsoft.Extensions.Options;
using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    public interface ICacheBackplane<TKey, TValue> : IDisposable where TKey : notnull
    {
        CacheManagerConfiguration<TKey, TValue> CacheConfiguration { get; }

        event EventHandler<CacheItemChangedEventArgs<TKey>> Changed;
        event EventHandler<EventArgs> Cleared;
        event EventHandler<CacheItemEventArgs<TKey>> Removed;

        void NotifyChange(TKey key, CacheItemChangedEventAction action);
        void NotifyClear();
        void NotifyRemove(TKey key);
    }

    /// <summary>
    /// In CacheManager, a cache backplane is used to keep in process and distributed caches in
    /// sync. <br/> If the cache manager runs inside multiple nodes or applications accessing the
    /// same distributed cache, and an in process cache is configured to be in front of the
    /// distributed cache handle. All Get calls will hit the in process cache. <br/> Now when an
    /// item gets removed for example by one client, all other clients still have that cache item
    /// available in the in process cache. <br/> This could lead to errors and unexpected behavior,
    /// therefore a cache backplane will send a message to all other cache clients to also remove
    /// that item.
    /// <para>
    /// The same mechanism will apply to any Update, Put, Remove, Clear or ClearRegion call of the cache.
    /// </para>
    /// </summary>
    public abstract class CacheBackplane<TKey, TValue> : ICacheBackplane<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Number of messages sent.
        /// </summary>
        public static long MessagesSent { get; set; }

        /// <summary>
        /// Number of messages received.
        /// </summary>
        public static long MessagesReceived { get; set; }

        /// <summary>
        /// Number of message chunks sent.
        /// Messages are sent in chunks to improve performance and decrease network traffic.
        /// </summary>
        public static long SentChunks { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheBackplane" /> class.
        /// </summary>
        /// <param name="configuration">The cache manager configuration.</param>
        /// <exception cref="System.ArgumentNullException">If configuration is null.</exception>
        protected CacheBackplane(IOptions<CacheManagerConfiguration<TKey, TValue>> configuration)
        {
            NotNull(configuration, nameof(configuration));
            CacheConfiguration = configuration.Value;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CacheBackplane"/> class.
        /// </summary>
        ~CacheBackplane()
        {
            Dispose(false);
        }

        /// <summary>
        /// The event gets fired whenever a change message for a key comes in,
        /// which means, another client changed a key.
        /// </summary>
        public event EventHandler<CacheItemChangedEventArgs<TKey>> Changed;

        /// <summary>
        /// The event gets fired whenever a cache clear message comes in.
        /// </summary>
        public event EventHandler<EventArgs> Cleared;

        /// <summary>
        /// The event gets fired whenever a removed message for a key comes in.
        /// </summary>
        public event EventHandler<CacheItemEventArgs<TKey>> Removed;

        /// <summary>
        /// Gets the cache configuration.
        /// </summary>
        /// <value>
        /// The cache configuration.
        /// </value>
        public CacheManagerConfiguration<TKey, TValue> CacheConfiguration { get; }

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
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The action.</param>
        public abstract void NotifyChange(TKey key, CacheItemChangedEventAction action);

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public abstract void NotifyClear();

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public abstract void NotifyRemove(TKey key);

        /// <summary>
        /// Sends a changed message for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The action.</param>
        protected internal void TriggerChanged(TKey key, CacheItemChangedEventAction action)
        {
            Changed?.Invoke(this, new CacheItemChangedEventArgs<TKey>(key, action));
        }

        /// <summary>
        /// Sends a cache cleared message.
        /// </summary>
        protected internal void TriggerCleared()
        {
            Cleared?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Sends a removed message for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key</param>
        protected internal void TriggerRemoved(TKey key)
        {
            Removed?.Invoke(this, new CacheItemEventArgs<TKey>(key));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool managed)
        {
        }
    }

    /// <summary>
    /// Base cache events arguments.
    /// </summary>
    public class CacheItemEventArgs<TKey> : EventArgs where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItemEventArgs{TKey}" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public CacheItemEventArgs(TKey key)
        {
            NotNull(key, nameof(key));
            Key = key;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        public TKey Key { get; }
    }

    /// <summary>
    /// Arguments for cache change events.
    /// </summary>
    public class CacheItemChangedEventArgs<TKey> : CacheItemEventArgs<TKey> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItemChangedEventArgs{TKey}" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The cache action.</param>
        public CacheItemChangedEventArgs(TKey key, CacheItemChangedEventAction action)
            : base(key)
        {
            Action = action;
        }

        /// <summary>
        /// Gets the action used to change a key in the cache.
        /// </summary>
        public CacheItemChangedEventAction Action { get; }
    }
}