﻿using CacheManager.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    public interface IBaseCacheHandle<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
    {
        CacheHandleConfiguration Configuration { get; }

        int Count { get; }

        bool IsDistributedCache { get; }

        CacheStats<TKey, TValue> Stats { get; }

        event EventHandler<CacheItemRemovedEventArgs<TKey>> OnCacheSpecificRemove;

        UpdateItemResult<TKey, TValue> Update(TKey key, Func<TValue, TValue> updateValue, int maxRetries);
    }

    /// <summary>
    /// The <c>BaseCacheHandle</c> implements all the logic which might be common for all the cache
    /// handles. It abstracts the <see cref="ICache{T}"/> interface and defines new properties and
    /// methods the implementer must use.
    /// <para>Actually it is not advisable to not use <see cref="BaseCacheHandle{T}"/>.</para>
    /// </summary>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public abstract class BaseCacheHandle<TKey, TValue> : BaseCache<TKey, TValue>, IBaseCacheHandle<TKey, TValue> where TKey : notnull
    {
        private readonly object _updateLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager's configuration.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="managerConfiguration"/> or <paramref name="configuration"/> are null.
        /// </exception>
        /// <exception cref="System.ArgumentException">If <paramref name="configuration"/> name is empty.</exception>
        protected BaseCacheHandle(IOptions<CacheManagerConfiguration<TKey, TValue>> managerConfiguration,
                                  IOptions<CacheHandleConfiguration> configuration)
        {
            NotNullOrWhiteSpace(configuration.Value.Name, nameof(configuration.Value.Name));

            Configuration = configuration.Value;

            Stats = new CacheStats<TKey, TValue>(
                managerConfiguration.Value.Name,
                Configuration.Name,
                Configuration.EnableStatistics,
                Configuration.EnablePerformanceCounters);
        }

        /// <summary>
        /// Indicates if this cache handle is a distributed cache.
        /// </summary>
        /// <remarks>
        /// The value will be evaluated by the backplane logic to figure out what to do if remote events are received.
        /// <para>
        /// If the cache handle is distributed, a remote remove event for example does not cause another <c>Remove</c> call. 
        /// For in-memory cache handles which are backplane source though, it would trigger a <c>Remove</c>.
        /// </para>
        /// </remarks>
        public virtual bool IsDistributedCache { get { return false; } }

        public event EventHandler<CacheItemRemovedEventArgs<TKey>> OnCacheSpecificRemove;

        /// <summary>
        /// Gets the cache handle configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public virtual CacheHandleConfiguration Configuration { get; }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public abstract int Count { get; }

        /// <summary>
        /// Gets the cache stats object.
        /// </summary>
        /// <value>The stats.</value>
        public virtual CacheStats<TKey, TValue> Stats { get; }

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same
        /// key, and during the update process, someone else changed the value for the key, the
        /// cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the most
        /// recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="maxRetries">The number of tries.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public virtual UpdateItemResult<TKey, TValue> Update(TKey key, Func<TValue, TValue> updateValue, int maxRetries)
        {
            NotNull(updateValue, nameof(updateValue));
            CheckDisposed();

            lock (_updateLock)
            {
                var original = GetCacheItem(key);
                if (original == null)
                {
                    return UpdateItemResult.ForItemDidNotExist<TKey, TValue>();
                }

                var newValue = updateValue(original.Value);

                if (newValue == null)
                {
                    return UpdateItemResult.ForFactoryReturnedNull<TKey, TValue>();
                }

                var newItem = original.WithValue(newValue);
                newItem.LastAccessedUtc = DateTime.UtcNow;
                Put(newItem);
                return UpdateItemResult.ForSuccess(newItem);
            }
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected internal override bool AddInternal(CacheItem<TKey, TValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            return AddInternalPrepared(item);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal override void PutInternal(CacheItem<TKey, TValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            PutInternalPrepared(item);
        }

        /// <summary>
        /// Can be used to signal a remove event to the <see cref="ICacheManager{TCacheValue}"/> in case the underlying cache supports this and the implementation
        /// can react on evictions and expiration of cache items.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="region">The cache region. Can be null.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="value">The original cache value. The value might be null if the underlying cache system doesn't support returning the value on eviction.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="key"/> is null.</exception>
        protected void TriggerCacheSpecificRemove(TKey key, CacheItemRemovedReason reason, object value)
        {
            NotNull(key, nameof(key));

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"'{ Configuration.Name}' triggered remove '{key}' because '{reason}'.");
            }

            // internal remove event, we don't know the level at this point => emit 0
            OnCacheSpecificRemove?.Invoke(this, new CacheItemRemovedEventArgs<TKey>(key, reason, value, 0));
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract bool AddInternalPrepared(CacheItem<TKey, TValue> item);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicator if managed resources should be released.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                Stats.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets the item expiration.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Returns the updated cache item.</returns>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// If expiration mode is defined without timeout.
        /// </exception>
        protected virtual CacheItem<TKey, TValue> GetItemExpiration(CacheItem<TKey, TValue> item)
        {
            NotNull(item, nameof(item));

            // logic should be that the item setting overrules the handle setting if the item
            // doesn't define a mode (value is Default) it should use the handle's setting. if the
            // handle also doesn't define a mode (value is None|Default), we use None.
            var expirationMode = ExpirationMode.Default;
            var expirationTimeout = TimeSpan.Zero;
            var useItemExpiration = item.ExpirationMode != ExpirationMode.Default && !item.UsesExpirationDefaults;

            if (useItemExpiration)
            {
                expirationMode = item.ExpirationMode;
                expirationTimeout = item.ExpirationTimeout;
            }
            else if (Configuration.ExpirationMode != ExpirationMode.Default)
            {
                expirationMode = Configuration.ExpirationMode;
                expirationTimeout = Configuration.ExpirationTimeout;
            }

            if (expirationMode == ExpirationMode.Default || expirationMode == ExpirationMode.None)
            {
                expirationMode = ExpirationMode.None;
                expirationTimeout = TimeSpan.Zero;
            }
            else if (expirationTimeout == TimeSpan.Zero)
            {
                throw new InvalidOperationException("Expiration mode is defined without timeout.");
            }

            return item.WithExpiration(expirationMode, expirationTimeout, !useItemExpiration);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected abstract void PutInternalPrepared(CacheItem<TKey, TValue> item);
    }
}