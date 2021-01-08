namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Defines the contract for serialization of the cache value and cache items.
    /// The cache item serialization should be separated in case the serialization
    /// technology does not support immutable objects; in that case <see cref="CacheItem{T}"/> might not
    /// be serializable directly and the implementation has to wrap the cache item.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key value.</typeparam>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public interface ICacheSerializer<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Serializes the given <paramref name="value"/> and returns the serialized data as byte array.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialization result</returns>
        byte[] Serialize(TValue value);

        /// <summary>
        /// Deserializes the <paramref name="data"/> into the given <paramref name="target"/> <c>Type</c>.
        /// </summary>
        /// <param name="data">The data which should be deserialized.</param>
        /// <returns>The deserialized object.</returns>
        TValue Deserialize(byte[] data);

        /// <summary>
        /// Serializes the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized result.</returns>
        byte[] SerializeCacheItem(ICacheItem<TKey, TValue> value);

        /// <summary>
        /// Deserializes the <paramref name="value"/> into a <see cref="CacheItem{T}"/>.
        /// </summary>
        /// <param name="value">The data to deserialize from.</param>
        /// <returns>The deserialized cache item.</returns>
        ICacheItem<TKey, TValue> DeserializeCacheItem(byte[] value);
    }
}