using System.Threading.Tasks;

namespace SharePoint.Authentication
{
    public interface ISessionProvider<T>
    {
        Task SetAsync(string key, T entity);
        Task<T> GetAsync(string key);
        void Set(string key, T entity);
        T Get(string key);
    }
}