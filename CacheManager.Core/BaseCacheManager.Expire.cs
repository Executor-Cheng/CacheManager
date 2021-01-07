using Microsoft.Extensions.Logging;
using System;

namespace CacheManager.Core
{
    public partial class BaseCacheManager<TKey, TValue>
    {
        /// <inheritdoc />
        public void Expire(TKey key, ExpirationMode mode, TimeSpan timeout)
        {
            CheckDisposed();

            var item = GetCacheItemInternal(key);
            if (item == null)
            {
                Logger.LogTrace($"Expire: item not found for key {key}");
                return;
            }

            if (_logTrace)
            {
                Logger.LogTrace($"Expire [{item}] started.");
            }

            if (mode == ExpirationMode.Absolute)
            {
                item = item.WithAbsoluteExpiration(timeout);
            }
            else if (mode == ExpirationMode.Sliding)
            {
                item = item.WithSlidingExpiration(timeout);
            }
            else if (mode == ExpirationMode.None)
            {
                item = item.WithNoExpiration();
            }
            else if (mode == ExpirationMode.Default)
            {
                item = item.WithDefaultExpiration();
            }

            if (_logTrace)
            {
                Logger.LogTrace($"Expire - Expiration of [{item}] has been modified. Using put to store the item...");
            }

            PutInternal(item);
        }

        /// <inheritdoc />
        public void Expire(TKey key, DateTimeOffset absoluteExpiration)
        {
            var timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            Expire(key, ExpirationMode.Absolute, timeout);
        }

        /// <inheritdoc />
        public void Expire(TKey key, TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            Expire(key, ExpirationMode.Sliding, slidingExpiration);
        }

        /// <inheritdoc />
        public void RemoveExpiration(TKey key)
        {
            Expire(key, ExpirationMode.None, default);
        }
    }
}
