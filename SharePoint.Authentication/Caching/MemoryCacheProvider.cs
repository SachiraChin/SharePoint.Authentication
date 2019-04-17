using System;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public class MemoryCacheProvider : ICacheProvider
    {
        public virtual async Task<T> GetAsync<T>(string key, Func<Task<T>> getNewInstance, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true, bool force = false)
        {
            try
            {
                T newValue;
                if (force && getNewInstance != null)
                {
                    newValue = await getNewInstance();
                    await SetAsync(key, newValue, cacheExpireInMinutes, shouldThrowExceptionOnError);
                    return newValue;
                }

                var cachedData = MemoryCache.Default.Get(key);
                if (cachedData != null)
                {
                    switch (cachedData)
                    {
                        case NullClass _:
                            return default(T);
                        case T returnData:
                            return returnData;
                    }
                }

                if (getNewInstance == null) return default(T);

                newValue = await getNewInstance();
                await SetAsync(key, newValue, cacheExpireInMinutes, shouldThrowExceptionOnError);
                return newValue;
            }
            catch (Exception)
            {
                if (shouldThrowExceptionOnError)
                    throw;

                return default(T);
            }
        }

        public T Get<T>(string key, Func<T> getNewInstance, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true, bool force = false)
        {
            try
            {
                T newValue;
                if (force)
                {
                    newValue = getNewInstance();
                    Set(key, newValue, cacheExpireInMinutes, shouldThrowExceptionOnError);
                    return newValue;
                }

                var cachedData = MemoryCache.Default.Get(key);
                if (cachedData != null)
                {
                    switch (cachedData)
                    {
                        case NullClass _:
                            return default(T);
                        case T returnData:
                            return returnData;
                    }
                }

                if (getNewInstance == null) return default(T);

                newValue = getNewInstance();
                Set(key, newValue, cacheExpireInMinutes, shouldThrowExceptionOnError);
                return newValue;
            }
            catch (Exception)
            {
                if (shouldThrowExceptionOnError)
                    throw;

                return default(T);
            }
        }

        public virtual void Remove(string key, bool shouldThrowExceptionOnError = true)
        {
            try
            {
                MemoryCache.Default.Remove(key);
            }
            catch (Exception)
            {
                if (shouldThrowExceptionOnError)
                    throw;

                // ignored   
            }
        }

        public Task SetAsync<T>(string key, T value, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true)
        {
            try
            {
                object setValue;
                if (value == null)
                    setValue = new NullClass();
                else
                    setValue = value;

                var cip = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(cacheExpireInMinutes),
                };
                MemoryCache.Default.Set(key, setValue, cip);

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                if (shouldThrowExceptionOnError)
                    throw;

                // ignored
                return Task.FromResult(true);
            }
        }

        public void Set<T>(string key, T value, int cacheExpireInMinutes, bool shouldThrowExceptionOnError = true)
        {
            try
            {
                object setValue;
                if (value == null)
                    setValue = new NullClass();
                else
                    setValue = value;

                var cip = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(cacheExpireInMinutes),
                };
                MemoryCache.Default.Set(key, setValue, cip);
            }
            catch (Exception)
            {
                if (shouldThrowExceptionOnError)
                    throw;

                // ignored
            }
        }

        internal class NullClass
        {

        }
    }
}
