using System;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public interface ICacheProvider<T>
    {
        string MemoryGroup { get; }
        int CacheExpireInMinutes { get; }
        bool ShouldThrowExceptionOnError { get; }

        Task<T> GetAsync(string key, Func<Task<T>> getNewInstance, bool force = false);
        T Get(string key, Func<T> getNewInstance, bool force = false);
        
        T Get(string key);

        void Set(string key, T value);

        void Remove(string key);
        Task SetAsync(string key, T value);
    }
}
