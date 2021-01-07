using CacheManager.Core.Utility;
using System;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Base implementation for cache serializers.
    /// </summary>
    public abstract class CacheSerializer<TKey, TValue> : ICacheSerializer<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Returns the open generic type of this class.
        /// </summary>
        /// <returns>The type.</returns>
        protected abstract Type GetOpenGeneric();

        /// <summary>
        /// Creates a new instance of the serializer specific cache item.
        /// Items should implement <see cref="SerializerCacheItem{T}"/> and the implementation should call
        /// the second constructor taking exactly these two arguments.
        /// </summary>
        /// <param name="properties">The item properties to copy from.</param>
        /// <param name="value">The actual cache item value.</param>
        /// <returns>The serializer specific cache item instance.</returns>
        /// <typeparam name="TCacheValue">The cache value type.</typeparam>
        protected abstract TValue CreateNewItem(ICacheItemProperties<TKey> properties, TValue value);

        /// <inheritdoc/>
        public abstract byte[] Serialize(TValue value);

        /// <inheritdoc/>
        public abstract TValue Deserialize(byte[] data);

        /// <inheritdoc/>
        public virtual byte[] SerializeCacheItem(CacheItem<TKey, TValue> value)
        {
            Guard.NotNull(value, nameof(value));
            var item = CreateFromCacheItem(value);
            return Serialize(item);
        }

        /// <inheritdoc/>
        public virtual CacheItem<TKey, TValue> DeserializeCacheItem(byte[] value)
        {
            var item = (ICacheItemConverter<TKey, TValue>)Deserialize(value);
            return item?.ToCacheItem();
        }

        private TValue CreateFromCacheItem(CacheItem<TKey, TValue> source)
        {
            return source.Value;
        }
    }
}
