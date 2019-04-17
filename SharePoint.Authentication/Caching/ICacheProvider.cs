using System;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public interface ICacheProvider
    {
        Task<T> GetAsync<T>(string key, Func<Task<T>> getNewInstance, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true, bool force = false);
        T Get<T>(string key, Func<T> getNewInstance, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true, bool force = false);
        void Remove(string key, bool shouldThrowExceptionOnError = true);
        Task SetAsync<T>(string key, T value, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true);
        void Set<T>(string key, T value, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true);
    }
}
