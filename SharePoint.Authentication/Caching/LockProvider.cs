using AsyncKeyedLock;
using System;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public class LockProvider : ILockProvider
    {
        // ReSharper disable once StaticMemberInGenericType
        // This locked shared by all instances of this class. Staticness and concurrency is expected and handled
        private static readonly AsyncKeyedLocker<string> StaticKeyLocks = new AsyncKeyedLocker<string>(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        public AsyncKeyedLocker<string> KeyLocks => StaticKeyLocks;

        public async Task<T> PerformActionLockedAsync<T>(string key, Func<Task<T>> action)
        {
            using (await KeyLocks.LockAsync(key).ConfigureAwait(false))
            {
                return await action();
            }
        }

        public T PerformActionLocked<T>(string key, Func<T> action)
        {
            using (KeyLocks.Lock(key))
            {
                return action();
            }
        }
    }
}
