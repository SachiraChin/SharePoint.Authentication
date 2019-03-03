using System.Threading.Tasks;

namespace SharePoint.Authentication
{
    public interface ISessionProvider<T>
    {
        Task Set(string key, T entity);
        Task<T> Get(string key);
    }
}