﻿using CacheManager.Core.Configuration;
using CacheManager.Core.Internal;
using System;
using System.Collections.Generic;

namespace CacheManager.Core
{
    /// <summary>
    /// This interface extends the <c>ICache</c> interface by some cache manager specific methods and events.
    /// </summary>
    /// <typeparam name="TValue">The type of the cache item value.</typeparam>
    public interface ICacheManager<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Occurs when an item was successfully added to the cache.
        /// <para>The event will not get triggered if <c>Add</c> would return false.</para>
        /// </summary>
        event EventHandler<CacheActionEventArgs<TKey>> OnAdd;

        /// <summary>
        /// Occurs when <c>Clear</c> gets called, after the cache has been cleared.
        /// </summary>
        event EventHandler<CacheClearEventArgs> OnClear;

        /// <summary>
        /// Occurs when an item was retrieved from the cache.
        /// <para>The event will only get triggered on cache hit. Misses do not trigger!</para>
        /// </summary>
        event EventHandler<CacheActionEventArgs<TKey>> OnGet;

        /// <summary>
        /// Occurs when an item was put into the cache.
        /// </summary>
        event EventHandler<CacheActionEventArgs<TKey>> OnPut;

        /// <summary>
        /// Occurs when an item was successfully removed from the cache.
        /// </summary>
        event EventHandler<CacheActionEventArgs<TKey>> OnRemove;

        /// <summary>
        /// Occurs when an item was removed by the cache handle due to expiration or e.g. memory pressure eviction.
        /// The <see cref="CacheItemRemovedEventArgs.Reason"/> property indicates the reason while the <see cref="CacheItemRemovedEventArgs.Level"/> indicates
        /// which handle triggered the event.
        /// </summary>
        event EventHandler<CacheItemRemovedEventArgs<TKey>> OnRemoveByHandle;

        /// <summary>
        /// Occurs when an item was successfully updated.
        /// </summary>
        event EventHandler<CacheActionEventArgs<TKey>> OnUpdate;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        CacheManagerConfiguration<TKey, TValue> Configuration { get; }

        /// <summary>
        /// Gets the cache name.
        /// </summary>
        /// <value>The cache name.</value>
        string Name { get; }

        /// <summary>
        /// Gets a list of cache handles currently registered within the cache manager.
        /// </summary>
        /// <value>The cache handles.</value>
        /// <remarks>
        /// This list is read only, any changes to the returned list instance will not affect the
        /// state of the cache manager instance.
        /// </remarks>
        IEnumerable<IBaseCacheHandle<TKey, TValue>> CacheHandles { get; }

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="addValue">
        /// The value which should be added in case the item doesn't already exist.
        /// </param>
        /// <param name="updateValue">
        /// The function to perform the update in case the item does already exist.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        TValue AddOrUpdate(TKey key, TValue addValue, Func<TValue, TValue> updateValue);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="addValue">
        /// The value which should be added in case the item doesn't already exist.
        /// </param>
        /// <param name="updateValue">
        /// The function to perform the update in case the item does already exist.
        /// </param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        TValue AddOrUpdate(TKey key, TValue addValue, Func<TValue, TValue> updateValue, int maxRetries);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="addItem">The item which should be added or updated.</param>
        /// <param name="updateValue">The function to perform the update, if the item does exist.</param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="addItem"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        TValue AddOrUpdate(CacheItem<TKey, TValue> addItem, Func<TValue, TValue> updateValue);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="addItem">The item which should be added or updated.</param>
        /// <param name="updateValue">The function to perform the update, if the item does exist.</param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="addItem"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        TValue AddOrUpdate(CacheItem<TKey, TValue> addItem, Func<TValue, TValue> updateValue, int maxRetries);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The method returns either the existing item's value or the newly added <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        TValue GetOrAdd(TKey key, TValue value);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        CacheItem<TKey, TValue> GetOrAdd(TKey key, Func<TKey, CacheItem<TKey, TValue>> valueFactory);

        /// <summary>
        /// Tries to either retrieve an existing item or add the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <param name="value">The cache value.</param>
        /// <returns><c>True</c> if the operation succeeds, <c>False</c> in case there are too many retries or the <paramref name="valueFactory"/> returns null.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        bool TryGetOrAdd(TKey key, Func<TKey, TValue> valueFactory, out TValue value);

        /// <summary>
        /// Tries to either retrieve an existing item or add the item to the cache if it does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <param name="item">The cache item.</param>
        /// <returns><c>True</c> if the operation succeeds, <c>False</c> in case there are too many retries or the <paramref name="valueFactory"/> returns null.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        bool TryGetOrAdd(TKey key, Func<TKey, CacheItem<TKey, TValue>> valueFactory, out CacheItem<TKey, TValue> item);

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
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <returns>The updated value, or null, if the update was not successful.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        TValue Update(TKey key, Func<TValue, TValue> updateValue);

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
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>The updated value, or null, if the update was not successful.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        TValue Update(TKey key, Func<TValue, TValue> updateValue, int maxRetries);

        /// <summary>
        /// Tries to update an existing key in the cache.
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
        /// <param name="value">The updated value, or null, if the update was not successful.</param>
        /// <returns><c>True</c> if the update operation was successful, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        bool TryUpdate(TKey key, Func<TValue, TValue> updateValue, out TValue value);

        /// <summary>
        /// Tries to update an existing key in the cache.
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
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>False</c>.
        /// </param>
        /// <param name="value">The updated value, or null, if the update was not successful.</param>
        /// <returns><c>True</c> if the update operation was successful, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        bool TryUpdate(TKey key, Func<TValue, TValue> updateValue, int maxRetries, out TValue value);

        /// <summary>
        /// Explicitly sets the expiration <paramref name="mode"/> and <paramref name="timeout"/> for the
        /// <paramref name="key"/> in all cache layers.
        /// This operation overrides any configured expiration per cache handle for this particular item.
        /// </summary>
        /// <remarks>
        /// Don't use this in concurrency critical scenarios if you are using distributed caches as <code>Expire</code> is not atomic;
        /// <code>Expire</code> uses <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> to store the item with the new expiration.
        /// </remarks>
        /// <param name="key">The cache key.</param>
        /// <param name="mode">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        void Expire(TKey key, ExpirationMode mode, TimeSpan timeout);

        /// <summary>
        /// Explicitly sets an absolute expiration date for the <paramref name="key"/> in all cache layers.
        /// This operation overrides any configured expiration per cache handle for this particular item.
        /// </summary>
        /// <remarks>
        /// Don't use this in concurrency critical scenarios if you are using distributed caches as <code>Expire</code> is not atomic;
        /// <code>Expire</code> uses <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> to store the item with the new expiration.
        /// </remarks>
        /// <param name="key">The cache key.</param>
        /// <param name="absoluteExpiration">
        /// The expiration date. The value must be greater than zero.
        /// </param>
        void Expire(TKey key, DateTimeOffset absoluteExpiration);

        /// <summary>
        /// Explicitly sets a sliding expiration date for the <paramref name="key"/> in all cache layers.
        /// This operation overrides any configured expiration per cache handle for this particular item.
        /// </summary>
        /// <remarks>
        /// Don't use this in concurrency critical scenarios if you are using distributed caches as <code>Expire</code> is not atomic;
        /// <code>Expire</code> uses <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> to store the item with the new expiration.
        /// </remarks>
        /// <param name="key">The cache key.</param>
        /// <param name="slidingExpiration">
        /// The expiration timeout. The value must be greater than zero.
        /// </param>
        void Expire(TKey key, TimeSpan slidingExpiration);

        /// <summary>
        /// Explicitly removes any expiration settings previously defined for the <paramref name="key"/>
        /// in all cache layers and sets the expiration mode to <see cref="ExpirationMode.None"/>.
        /// This operation overrides any configured expiration per cache handle for this particular item.
        /// </summary>
        /// <remarks>
        /// Don't use this in concurrency critical scenarios if you are using distributed caches as <code>Expire</code> is not atomic;
        /// <code>Expire</code> uses <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> to store the item with the new expiration.
        /// </remarks>
        /// <param name="key">The cache key.</param>
        void RemoveExpiration(TKey key);
    }
}