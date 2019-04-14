using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public interface ILockProvider
    {
        Task<T> PerformActionLockedAsync<T>(string key, Func<Task<T>> action);
        T PerformActionLocked<T>(string key, Func<T> action);
        ConcurrentDictionary<string, SemaphoreSlim> KeyLocks { get; }
    }
}
