using System;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Caching
{
    public class MemoryCacheProvider : BaseCacheExtension, ICacheProvider
    {
        public MemoryCacheProvider(string memoryGroup, int cacheExpireInMinutes, bool shouldThrowExceptionOnError) : base(memoryGroup)
        {
            CacheExpireInMinutes = cacheExpireInMinutes;
            ShouldThrowExceptionOnError = shouldThrowExceptionOnError;
        }

        public int CacheExpireInMinutes { get; }
        public bool ShouldThrowExceptionOnError { get; }

        public virtual async Task<T> GetAsync<T>(string key, Func<Task<T>> getNewInstance, bool force = false)
        {
            try
            {
                T newValue;
                if (force)
                {
                    newValue = await getNewInstance();
                    await SetAsync(key, newValue);
                    return newValue;
                }

                var cachedData = MemoryCache.Default.Get( GetKey(key));
                if (cachedData != null)
                {
                    switch (cachedData)
                    {
                        case NullClass _:
                            return default;
                        case T returnData:
                            return returnData;
                    }
                }

                if (getNewInstance == null) return default;

                newValue = await getNewInstance();
                await SetAsync(key, newValue);
                return newValue;
            }
            catch (Exception)
            {
                if (ShouldThrowExceptionOnError)
                    throw;

                return default;
            }
        }

        public T Get<T>(string key, Func<T> getNewInstance, bool force = false)
        {
            try
            {
                T newValue;
                if (force)
                {
                    newValue = getNewInstance();
                    Set(key, newValue);
                    return newValue;
                }

                var cachedData = MemoryCache.Default.Get(GetKey(key));
                if (cachedData != null)
                {
                    switch (cachedData)
                    {
                        case NullClass _:
                            return default;
                        case T returnData:
                            return returnData;
                    }
                }

                if (getNewInstance == null) return default;

                newValue = getNewInstance();
                Set(key, newValue);
                return newValue;
            }
            catch (Exception)
            {
                if (ShouldThrowExceptionOnError)
                    throw;

                return default;
            }
        }

        public virtual void Remove(string key)
        {
            try
            {
                var internalKey = GetKey(key);
                MemoryCache.Default.Remove(internalKey);
            }
            catch (Exception)
            {
                if (ShouldThrowExceptionOnError)
                    throw;

                // ignored   
            }
        }

        public Task SetAsync<T>(string key, T value)
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
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(CacheExpireInMinutes),
                };
                MemoryCache.Default.Set(GetKey(key), setValue, cip);

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                if (ShouldThrowExceptionOnError)
                    throw;

                // ignored
                return Task.FromResult(true);
            }
        }

        public void Set<T>(string key, T value)
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
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(CacheExpireInMinutes),
                };
                MemoryCache.Default.Set(GetKey(key), setValue, cip);
            }
            catch (Exception)
            {
                if (ShouldThrowExceptionOnError)
                    throw;

                // ignored
            }
        }

        internal class NullClass
        {

        }
    }
}
