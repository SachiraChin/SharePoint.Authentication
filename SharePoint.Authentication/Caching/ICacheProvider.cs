using System;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public interface ICacheProvider
    {
        Task<T> GetAsync<T>(string key, Func<Task<T>> getNewInstance, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true, bool force = false);
        Task RemoveAsync(string key, bool shouldThrowExceptionOnError = true);
        Task SetAsync<T>(string key, T value, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true);
    }
}
