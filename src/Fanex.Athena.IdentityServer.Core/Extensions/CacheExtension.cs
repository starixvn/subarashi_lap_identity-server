using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Extensions
{
    public static class CacheExtension
    {
        public static async Task<T> TryGetOrAddFromCache<T>(this IMemoryCache cache, string cacheKey, TimeSpan cacheTime, Func<Task<T>> func)
        {
            if (!cache.TryGetValue(cacheKey, out T data))
            {
                data = await func.Invoke();
                if (data is not null)
                {
                    cache.Set(cacheKey, data, cacheTime);
                }
            }
            return data;
        }
    }
}
