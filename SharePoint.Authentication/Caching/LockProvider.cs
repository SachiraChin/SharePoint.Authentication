using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    internal class LockProvider<T> : BaseCacheExtension, ILockProvider<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        // This locked shared by all instances of this class. Staticness and concurrency is expected and handled
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> StaticKeyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public LockProvider(string memoryGroup) : base(memoryGroup)
        {
        }

        public ConcurrentDictionary<string, SemaphoreSlim> KeyLocks => StaticKeyLocks;

        public async Task<T> PerformActionLockedAsync(string key, Func<Task<T>> action)
        {
            var keyLock = KeyLocks.GetOrAdd(GetKey(key), x => new SemaphoreSlim(1, 1));
            await keyLock.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                keyLock.Release();
            }
        }

        public T PerformActionLocked(string key, Func<T> action)
        {
            var keyLock = KeyLocks.GetOrAdd(GetKey(key), x => new SemaphoreSlim(1, 1));
            keyLock.Wait();
            try
            {
                return action();
            }
            finally
            {
                keyLock.Release();
            }
        }
    }
}
