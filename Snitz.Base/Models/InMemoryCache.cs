using System;
using System.Runtime.Caching;

namespace Snitz.Base
{

    public class InMemoryCache : ICacheService
    {
        private int _expireIn = 10;
        public bool DoNotExpire { get; set; }

        public InMemoryCache()
        {

        }

        /// <summary>
        /// Create new cache object
        /// </summary>
        /// <param name="expires">minutes to keep cache</param>
        public InMemoryCache(int expires)
        {
            _expireIn = expires;
        }

        public T GetOrSet<T>(string cacheKey, Func<T> getItemCallback) where T : class
        {
            T item = MemoryCache.Default.Get(cacheKey) as T;
            if (item == null)
            {
                item = getItemCallback();
                if (DoNotExpire)
                    MemoryCache.Default.Add(cacheKey, item, null);
                else
                    MemoryCache.Default.Add(cacheKey, item, DateTimeOffset.Now.AddMinutes(_expireIn));
            }
            return item;
        }

        public T GetOrSet<T>(string cacheKey, Func<string, string, T> getItemCallback, string start, string end) where T : class
        {
            T item = MemoryCache.Default.Get(cacheKey) as T;
            if (item == null)
            {
                item = getItemCallback(start, end);
                MemoryCache.Default.Add(cacheKey, item, DateTimeOffset.Now.AddMinutes(_expireIn));
            }
            return item;
        }
        public void Remove(string cacheKey)
        {
            MemoryCache.Default.Remove(cacheKey);
        }

    }

    interface ICacheService
    {
        T GetOrSet<T>(string cacheKey, Func<T> getItemCallback) where T : class;
    }
}