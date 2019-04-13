using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public interface ILockProvider<T>
    {
        Task<T> PerformActionLockedAsync(string key, Func<Task<T>> action);
        T PerformActionLocked(string key, Func<T> action);
        ConcurrentDictionary<string, SemaphoreSlim> KeyLocks { get; }
    }
}
