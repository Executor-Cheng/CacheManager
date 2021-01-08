using CacheManager.Core.Internal;
using Microsoft.Extensions.Logging;
using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public partial class BaseCacheManager<TKey, TValue>
    {
        /// <inheritdoc />
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TValue, TValue> updateValue) =>
            AddOrUpdate(key, addValue, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TValue, TValue> updateValue, int maxRetries) =>
            AddOrUpdate(CreateCacheItem(key, addValue), updateValue, maxRetries);

        /// <inheritdoc />
        public TValue AddOrUpdate(ICacheItem<TKey, TValue> addItem, Func<TValue, TValue> updateValue) =>
            AddOrUpdate(addItem, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TValue AddOrUpdate(ICacheItem<TKey, TValue> addItem, Func<TValue, TValue> updateValue, int maxRetries)
        {
            NotNull(addItem, nameof(addItem));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be non-negative.");

            return AddOrUpdateInternal(addItem, updateValue, maxRetries);
        }

        private TValue AddOrUpdateInternal(ICacheItem<TKey, TValue> item, Func<TValue, TValue> updateValue, int maxRetries)
        {
            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace($"Add or update: {item.Key}.");
            }

            var tries = 0;
            do
            {
                tries++;

                if (AddInternal(item))
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Add or update: {item.Key}: successfully added the item.");
                    }

                    return item.Value;
                }

                if (_logTrace)
                {
                    Logger.LogTrace($"Add or update: {item.Key}: add failed, trying to update...");
                }

                if (TryUpdate(item.Key, updateValue, maxRetries, out TValue returnValue))
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace($"Add or update: {item.Key}: successfully updated.");
                    }

                    return returnValue;
                }

                if (_logTrace)
                {
                    Logger.LogTrace($"Add or update: {item.Key}: update FAILED, retrying [{tries}/{Configuration.MaxRetries}].");
                }
            }
            while (tries <= maxRetries);

            // exceeded max retries, failing the operation... (should not happen in 99,99% of the cases though.)
            throw new InvalidOperationException($"Could not add nor update the item {item.Key}");
        }

        /// <inheritdoc />
        public bool TryUpdate(TKey key, Func<TValue, TValue> updateValue, out TValue value) =>
            TryUpdate(key, updateValue, Configuration.MaxRetries, out value);

        /// <inheritdoc />
        public bool TryUpdate(TKey key, Func<TValue, TValue> updateValue, int maxRetries, out TValue value)
        {
            NotNull(key, nameof(key));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            return UpdateInternal(_cacheHandles, key, updateValue, maxRetries, false, out value);
        }

        /// <inheritdoc />
        public TValue Update(TKey key, Func<TValue, TValue> updateValue) =>
            Update(key, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TValue Update(TKey key, Func<TValue, TValue> updateValue, int maxRetries)
        {
            NotNull(key, nameof(key));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            UpdateInternal(_cacheHandles, key, updateValue, maxRetries, true, out TValue value);

            return value;
        }

        private bool UpdateInternal(
            IBaseCacheHandle<TKey, TValue>[] handles,
            TKey key,
            Func<TValue, TValue> updateFactory,
            int maxRetries,
            bool throwOnFailure,
            out TValue value)
        {
            CheckDisposed();

            // assign null
            value = default;

            if (handles.Length == 0)
            {
                return false;
            }

            if (_logTrace)
            {
                Logger.LogTrace($"Update: {key}.");
            }

            // lowest level
            // todo: maybe check for only run on the backplate if configured (could potentially be not the last one).
            var handleIndex = handles.Length - 1;
            var handle = handles[handleIndex];

            var result = handle.Update(key, updateFactory, maxRetries);

            if (_logTrace)
            {
                Logger.LogTrace($"Update: {key}: tried on handle {handle.Configuration.Name}: result: {result.UpdateState}.");
            }

            if (result.UpdateState == UpdateItemResultState.Success)
            {
                // only on success, the returned value will not be null
                value = result.Value.Value;
                handle.Stats.OnUpdate(key, result);

                // evict others, we don't know if the update on other handles could actually
                // succeed... There is a risk the update on other handles could create a
                // different version than we created with the first successful update... we can
                // safely add the item to handles below us though.
                EvictFromHandlesAbove(key, handleIndex);

                // optimizing - not getting the item again from cache. We already have it
                // var item = string.IsNullOrWhiteSpace(region) ? handle.GetCacheItem(key) : handle.GetCacheItem(key, region);
                AddToHandlesBelow(result.Value, handleIndex);
                TriggerOnUpdate(key);
            }
            else if (result.UpdateState == UpdateItemResultState.FactoryReturnedNull)
            {
                Logger.LogWarning($"Update failed on '{key}' because value factory returned null.");

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{key}' because value factory returned null.");
                }
            }
            else if (result.UpdateState == UpdateItemResultState.TooManyRetries)
            {
                // if we had too many retries, this basically indicates an
                // invalid state of the cache: The item is there, but we couldn't update it and
                // it most likely has a different version
                Logger.LogWarning($"Update failed on '{key}' because of too many retries.");

                EvictFromOtherHandles(key, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{key}' because of too many retries: {result.NumberOfTriesNeeded}.");
                }
            }
            else if (result.UpdateState == UpdateItemResultState.ItemDidNotExist)
            {
                // If update fails because item doesn't exist AND the current handle is backplane source or the lowest cache handle level,
                // remove the item from other handles (if exists).
                // Otherwise, if we do not exit here, calling update on the next handle might succeed and would return a misleading result.
                Logger.LogInformation($"Update failed on '{key}' because the key did not exist.");

                EvictFromOtherHandles(key, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{key}' because the key did not exist.");
                }
            }

            // update backplane
            if (result.UpdateState == UpdateItemResultState.Success && _cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace($"Update: {key}: notifies backplane [change].");
                }

                _cacheBackplane.NotifyChange(key, CacheItemChangedEventAction.Update);
            }

            return result.UpdateState == UpdateItemResultState.Success;
        }
    }
}
