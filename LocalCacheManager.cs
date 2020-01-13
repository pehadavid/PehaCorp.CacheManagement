using System;
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
            Dictionary<string, MemoryHashSetItem<T>> hashSetDictionary =
                myCache.Get<Dictionary<string, MemoryHashSetItem<T>>>(hashsetKey) ??
                new Dictionary<string, MemoryHashSetItem<T>>();
            MemoryHashSetItem<T> item = hashSetDictionary.Where(x => x.Key == hashfieldKey).Select(x => x.Value)
                .FirstOrDefault();
            if (item == null || item.IsExpired())
            {
                var data = DataRetrievalMethod.Invoke();
                item = new MemoryHashSetItem<T>()
                {
                    DateCreated = DateTime.UtcNow,
                    Item = data,
                    TimeToLive = ttl.GetValueOrDefault(TimeSpan.FromMinutes(1))
                };
                hashSetDictionary.Add(hashfieldKey, item);
                myCache.Set(hashsetKey, hashSetDictionary, TimeSpan.FromHours(1));
            }

            return item.Item;
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
                var dic = myCache.Get<Dictionary<string, MemoryHashSetItem<T>>>(hashsetKey);
                if (dic == null)
                    return default(T);
                var item = dic.FirstOrDefault(x => x.Key == itemKey);
                if (item.Value == null)
                {
                    return default(T);
                }

                if (!item.Value.IsExpired()) return item.Value.Item;
                
                dic.Remove(itemKey, out _);
                return default(T);
            }
            finally
            {
               
            }
        }

        public void HashSetByKey<T>(string hashsetKey, string itemKey, T item)
        {
           // _readerWriterLockSlim.EnterWriteLock();
            try
            {
                var dic = myCache.Get<Dictionary<string, MemoryHashSetItem<T>>>(hashsetKey) ??
                          new Dictionary<string, MemoryHashSetItem<T>>();

                if (!dic.ContainsKey(itemKey))
                    dic.TryAdd(itemKey,
                        new MemoryHashSetItem<T>()
                            {DateCreated = DateTime.UtcNow, TimeToLive = TimeSpan.FromMinutes(1), Item = item});
                myCache.Set(hashsetKey, dic);
            }
            finally
            {
             //   _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public void DeleteHashSet(string hashsetKey)
        {
            myCache.Remove(hashsetKey);

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
    }

    public class MemoryHashSetItem<T>
    {
        public T Item { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public DateTime DateCreated { get; set; }

        public bool IsExpired()
        {
            var ceilData = DateCreated.Add(TimeToLive);
            return ceilData < DateTime.UtcNow;
        }
    }
}