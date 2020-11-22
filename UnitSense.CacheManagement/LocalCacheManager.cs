using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace UnitSense.CacheManagement
{
    /// <summary>
    /// Utilise le cache local ASP.NET
    /// </summary>
    public class LocalCacheManager : ICacheManager
    {
        private MemoryCache myCache = new MemoryCache(new MemoryCacheOptions());
        private string callStackTrace;
        private ReaderWriterLockSlim _readerWriterLockSlim;


        public LocalCacheManager()
        {
#if DEBUG
            this.callStackTrace = Assembly.GetCallingAssembly().FullName;
#endif
            this._readerWriterLockSlim = new ReaderWriterLockSlim();
        }

        public bool CacheEnabled { get; set; }

        public void Clear()
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                myCache.Dispose();
                myCache = new MemoryCache(new MemoryCacheOptions());
            }
            catch (Exception)
            {
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public void Delete(string key)
        {
            myCache.Remove(key);
        }

        public Task<bool> DeleteAsync(string key)
        {
            return Task.Factory.StartNew<bool>(() =>
            {
                Delete(key);
                return true;
            });
        }

        public bool GetByKey(string key, out object value)
        {
            return myCache.TryGetValue(key, out value);
        }

        public bool GetByKey<T>(string key, out T value)
        {
            return myCache.TryGetValue(key, out value);
        }

        public T GetOrStore<T>(string key, Func<T> DataRetrievalMethod, TimeSpan? TimeToLive)
        {
            T result = myCache.Get<T>(key);
            if (result == null)
            {
                result = DataRetrievalMethod.Invoke();
                SetValue(key, result, TimeToLive);
            }

            return result;
        }

        public T GetOrStore<T>(string hashsetKey, string hashfieldKey, Func<T> DataRetrievalMethod, TimeSpan? ttl)
        {
            var internalKey = MorphToInternalHashetKey(hashsetKey, hashfieldKey);
            return GetOrStore<T>(internalKey, DataRetrievalMethod, ttl);
       
        }

        public Task<T> GetOrStoreAsync<T>(string key, Func<Task<T>> dataRetrievalMethodAsync, TimeSpan? timeToLive)
        {
            return Task.Run<T>(async () =>
            {
                T result = myCache.Get<T>(key);
                if (result == null)
                {
                    result = await dataRetrievalMethodAsync.Invoke();
                    SetValue(key, result, timeToLive);
                }

                return result;
            });
        }

        public T HashGetByKey<T>(string hashsetKey, string itemKey)
        {
           

            try
            {
                string internalKey = MorphToInternalHashetKey(hashsetKey, itemKey);
                GetByKey<T>(internalKey, out T item);
                return item;
            }
            finally
            {
               
            }
        }

        public void HashSetByKey<T>(string hashsetKey, string itemKey, T item)
        {
      
            try
            {
                var internalKey = MorphToInternalHashetKey(hashsetKey, itemKey);
                SetValue(internalKey, item, TimeSpan.FromMinutes(1));
            }
            finally
            {
         
            }
        }

        public void DeleteHashSet(string hashsetKey)
        {
            var keys = myCache.GetKeys<string>().Where(x => x.StartsWith(HsToken)).ToList();
            foreach (string key in keys)
            {
                myCache.Remove(key);
            }

        }


        public bool KeyExists(string key)
        {
            bool ex = GetByKey(key, out object val);
            return ex;
        }

        public void SetValue(string key, object value, TimeSpan? ttl)
        {
            myCache.Set(key, value, ttl.GetValueOrDefault(TimeSpan.FromMinutes(5)));
        }

        private string MorphToInternalHashetKey(string hsKey, string itemKey)
        {
            return $"{HsToken}{hsKey}-{itemKey}";
        }

        private string HsToken => "__UnitSense.CacheManagement.HashSet-";
    }


}