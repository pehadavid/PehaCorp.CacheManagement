using System;
using System.Threading.Tasks;

namespace UnitSense.CacheManagement
{
    namespace IMG.CacheManagement
    {
        public class DummyCacheManager : ICacheManager
        {
            private ICacheManager _cacheManagerImplementation;

            public bool GetByKey(string key, out object value)
            {
                throw new NotImplementedException();
            }

            public void SetValue(string key, object value, TimeSpan? ttl)
            {
                throw new NotImplementedException();
            }

            public void Delete(string key)
            {
                throw new NotImplementedException();
            }

            public Task<bool> DeleteAsync(string key)
            {
                throw new NotImplementedException();
            }

            public bool KeyExists(string key)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool CacheEnabled { get; set; }
            public T GetOrStore<T>(string key, Func<T> DataRetrievalMethod, TimeSpan? TimeToLive)
            {
                return DataRetrievalMethod.Invoke();
            }

            public T GetOrStore<T>(string hashsetKey, string hashfieldKey, Func<T> DataRetrievalMethod, TimeSpan? ttl)
            {
                throw new NotImplementedException();
            }

            public Task<T> GetOrStoreAsync<T>(string key, Func<Task<T>> dataRetrievalMethodAsync, TimeSpan? timeToLive)
            {
                throw new NotImplementedException();
            }

            public T HashGetByKey<T>(string hashsetKey, string itemKey)
            {
                throw new NotImplementedException();
            }

            public void HashSetByKey<T>(string hashsetKey, string itemKey, T item)
            {
                throw new NotImplementedException();
            }

            public void DeleteHashSet(string hashsetKey)
            {
                throw new NotImplementedException();
            }
        }
    }

}
