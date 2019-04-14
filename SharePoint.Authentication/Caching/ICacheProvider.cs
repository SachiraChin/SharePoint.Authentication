using System;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public interface ICacheProvider
    {
        string MemoryGroup { get; }
        int CacheExpireInMinutes { get; }
        bool ShouldThrowExceptionOnError { get; }

        Task<T> GetAsync<T>(string key, Func<Task<T>> getNewInstance, bool force = false);
        T Get<T>(string key, Func<T> getNewInstance, bool force = false);
        void Remove(string key);
        Task SetAsync<T>(string key, T value);
        void Set<T>(string key, T value);
    }
}
