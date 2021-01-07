using CacheManager.Core.Utility;
using System;
using System.Runtime.Serialization;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Simple converter contract used by the serializer cache item. Serializers will use that to convert back to
    /// The <see cref="CacheItem{T}"/>.
    /// </summary>
    public interface ICacheItemConverter<TKey, TValue>
    {
        /// <summary>
        /// Converts the current instance to a <see cref="CacheItem{T}"/>.
        /// The returned item must return the orignial created and last accessed date!
        /// </summary>
        /// <typeparam name="TValue">The type.</typeparam>
        /// <returns>The cache item.</returns>
        CacheItem<TKey, TValue> ToCacheItem();
    }

    /// <summary>
    /// Basic abstraction for serializers to work with cache items.
    /// </summary>
    /// <typeparam name="TValue">The type.</typeparam>

    [Serializable]
    [DataContract]
    public abstract class SerializerCacheItem<TKey, TValue> : ICacheItemConverter<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerCacheItem{T}"/> class.
        /// </summary>
        public SerializerCacheItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerCacheItem{T}"/> class.
        /// </summary>
        /// <param name="properties">The actual properties.</param>
        /// <param name="value">The cache value.</param>
        public SerializerCacheItem(ICacheItemProperties<TKey> properties, object value)
            : this()
        {
            Guard.NotNull(properties, nameof(properties));
            Guard.NotNull(value, nameof(value));

            CreatedUtc = properties.CreatedUtc.Ticks;
            ExpirationMode = properties.ExpirationMode;
            ExpirationTimeout = properties.ExpirationTimeout.TotalMilliseconds;
            Key = properties.Key;
            LastAccessedUtc = properties.LastAccessedUtc.Ticks;
            UsesExpirationDefaults = properties.UsesExpirationDefaults;
            ValueType = properties.ValueType.AssemblyQualifiedName;
            Value = (TValue)value;
        }

        /// <summary>
        /// Gets or sets the created utc date in ticks.
        /// Can be converted from and to <see cref="DateTime"/>.
        /// </summary>
        [DataMember]
        public abstract long CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the expiration mode.
        /// </summary>
        [DataMember]
        public abstract ExpirationMode ExpirationMode { get; set; }

        /// <summary>
        /// Gets or sets the expiration timeout in milliseconds.
        /// Can be coverted from and to <see cref="TimeSpan"/>.
        /// </summary>
        [DataMember]
        public abstract double ExpirationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        [DataMember]
        public abstract TKey Key { get; set; }

        /// <summary>
        /// Gets or sets the last accessed utc date in ticks.
        /// Can be converted from and to <see cref="DateTime"/>.
        /// </summary>
        [DataMember]
        public abstract long LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the default expiration should be used.
        /// </summary>
        [DataMember]
        public abstract bool UsesExpirationDefaults { get; set; }

        /// <summary>
        /// Gets or sets the value type.
        /// </summary>
        [DataMember]
        public abstract string ValueType { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [DataMember]
        public abstract TValue Value { get; set; }

        /// <inheritdoc/>
        public CacheItem<TKey, TValue> ToCacheItem()
        {
            var item = new CacheItem<TKey, TValue>(Key, (TValue)(object)Value);

            // resetting expiration in case the serializer actually stores serialization properties (Redis does for example).
            if (!UsesExpirationDefaults)
            {
                if (ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(TimeSpan.FromMilliseconds(ExpirationTimeout));
                }
                else if (ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(ExpirationTimeout));
                }
                else if (ExpirationMode == ExpirationMode.None)
                {
                    item = item.WithNoExpiration();
                }
            }
            else
            {
                item = item.WithDefaultExpiration();
            }

            item.LastAccessedUtc = new DateTime(LastAccessedUtc, DateTimeKind.Utc);

            return item.WithCreated(new DateTime(CreatedUtc, DateTimeKind.Utc));
        }
    }
}
