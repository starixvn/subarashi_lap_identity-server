using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Helpers
{
    public static class LockHelper
    {
        private static readonly ConcurrentDictionary<string, object> _lockObjects = new();

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _lockAsyncObjects = new();

        public static TResult Lock<TResult>(string key, Func<TResult> doing)
            where TResult : class
        {
            lock (_lockObjects.GetOrAdd(key, _ => new object()))
            {
                return doing?.Invoke();
            }
        }

        public static async Task<TResult> LockAsync<TResult>(string key, Func<Task<TResult>> doing)
           where TResult : class
        {
            var lockAsyncObject = _lockAsyncObjects.GetOrAdd(key, key => new SemaphoreSlim(1, 1));
            await lockAsyncObject.WaitAsync();

            try
            {
                if (doing is not null)
                {
                    return await doing.Invoke();
                }

                return default;
            }
            finally
            {
                lockAsyncObject.Release();
            }
        }
    }
}