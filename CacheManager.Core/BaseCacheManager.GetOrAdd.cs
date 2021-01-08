using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public partial class BaseCacheManager<TKey, TValue>
    {
        /// <inheritdoc />
        public TValue GetOrAdd(TKey key, TValue value)
        {
            NotNull(key, nameof(key));
            if (TryGetOrAddInternal(key, value, out ICacheItem<TKey, TValue> ritemsult))
            {
                return ritemsult.Value;
            }
            throw new InvalidOperationException($"Could not get nor add the item {key}");
        }

        /// <inheritdoc />
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            NotNull(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));
            if (TryGetOrAddInternal(key, k => new CacheItem<TKey, TValue>(k, valueFactory(k)), out ICacheItem<TKey, TValue> item))
            {
                return item.Value;
            }
            throw new InvalidOperationException($"Could not get nor add the item {key}");
        }

        /// <inheritdoc />
        public ICacheItem<TKey, TValue> GetOrAdd(TKey key, Func<TKey, ICacheItem<TKey, TValue>> valueFactory)
        {
            NotNull(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            if (TryGetOrAddInternal(key, valueFactory, out ICacheItem<TKey, TValue> item))
            {
                return item;
            }
            throw new InvalidOperationException($"Could not get nor add the item {key}");
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(TKey key, Func<TKey, TValue> valueFactory, out TValue value)
        {
            NotNull(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            if (TryGetOrAddInternal(key, k => new CacheItem<TKey, TValue>(k, valueFactory(k)), out var item))
            {
                value = item.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(TKey key, Func<TKey, ICacheItem<TKey, TValue>> valueFactory, out ICacheItem<TKey, TValue> item)
        {
            NotNull(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return TryGetOrAddInternal(key, valueFactory, out item);
        }

        private bool TryGetOrAddInternal(TKey key, TValue value, out ICacheItem<TKey, TValue> item)
        {
            ICacheItem<TKey, TValue> newItem = null;
            var tries = 0;
            do
            {
                tries++;
                item = GetCacheItemInternal(key);
                if (item != null)
                {
                    return true;
                }

                // changed logic to invoke the factory only once in case of retries
                if (newItem == null)
                {
                    newItem = CreateCacheItem(key, value);
                }

                if (AddInternal(newItem))
                {
                    item = newItem;
                    return true;
                }
            }
            while (tries <= Configuration.MaxRetries);
            return false;
        }

        private bool TryGetOrAddInternal(TKey key, Func<TKey, ICacheItem<TKey, TValue>> valueFactory, out ICacheItem<TKey, TValue> item)
        {
            ICacheItem<TKey, TValue> newItem = null;
            var tries = 0;
            do
            {
                tries++;
                item = GetCacheItemInternal(key);
                if (item != null)
                {
                    return true;
                }

                // changed logic to invoke the factory only once in case of retries
                if (newItem == null)
                {
                    newItem = valueFactory(key);
                    if (newItem == null)
                    {
                        return false;
                    }
                }

                if (AddInternal(newItem))
                {
                    item = newItem;
                    return true;
                }
            }
            while (tries <= Configuration.MaxRetries);
            if (newItem.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
            return false;
        }
    }
}
