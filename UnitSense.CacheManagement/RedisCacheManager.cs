using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace UnitSense.CacheManagement
{
    public class RedisCacheManager : ICacheManager
    {
        protected ConnectionMultiplexer clientManager;
        protected string envName;
        public string EnvName => envName;
        public ConnectionMultiplexer GetMultiplexer()
        {
            return this.clientManager;
        }
        public RedisCacheManager(ConnectionMultiplexer m, string envName = "")
        {
            clientManager = m;
            this.envName = envName;
        }

        public bool GetByKey(string key, out object value)
        {
            var db = clientManager.GetDatabase();
            var redisValue = db.StringGet(key);
            if (!redisValue.HasValue)
            {
                value = null;
                return false;
            }
            value = JsonConvert.DeserializeObject(redisValue, GetJsonSerializerSettings());
            return true;


        }

        public bool GetByKey<T>(string key, out T value)
        {
            var db = clientManager.GetDatabase();
            var redisValue = db.StringGet(key);
            if (redisValue.HasValue)
            {
                value = JsonConvert.DeserializeObject<T>(redisValue);
                return true;
            }

            value = default(T);
            return false;
        }

        public void SetValue(string key, object value, TimeSpan? ttl)
        {
            var db = clientManager.GetDatabase();
            var strValue = JsonConvert.SerializeObject(value, GetJsonSerializerSettings());
            db.StringSet(key, strValue, ttl, When.Always, CommandFlags.FireAndForget);


            Debug.WriteLine($"Redis Set Value : {key}");
        }

        public void Delete(string key)
        {
            var db = clientManager.GetDatabase();
            db.KeyDeleteAsync(key);
        }

        public Task<bool> DeleteAsync(string key)
        {
            var db = clientManager.GetDatabase();
            return db.KeyDeleteAsync(key, CommandFlags.FireAndForget);
        }

        public bool KeyExists(string key)
        {
            var db = clientManager.GetDatabase();
            return db.KeyExists(key, CommandFlags.HighPriority);
        }

        public void Clear()
        {
            var db = clientManager.GetDatabase();
            db.Execute("FLUSHDB");

        }

        public bool CacheEnabled { get; set; }
        public T GetOrStore<T>(string key, Func<T> DataRetrievalMethod, TimeSpan? TimeToLive)
        {
            //#if DEBUG
            //            return DataRetrievalMethod.Invoke();
            //#endif

            try
            {
                var db = clientManager.GetDatabase();

                if (db.KeyExists(key))
                {
                    Debug.WriteLine($"Redis cache HIT : {key}");
                    var strGet = db.StringGet(key);
                    return JsonConvert.DeserializeObject<T>(strGet, GetJsonSerializerSettings());
                }
                else
                {
                    var data = DataRetrievalMethod.Invoke();
                    var strSet = JsonConvert.SerializeObject(data, GetJsonSerializerSettings());
                    db.StringSetAsync(key, strSet, TimeToLive, When.Always, CommandFlags.FireAndForget);
                    return data;
                }
            }
            catch (Exception ex)
            {

                return DataRetrievalMethod.Invoke();
            }



        }

        public T GetOrStore<T>(string hashsetKey, string hashfieldKey, Func<T> DataRetrievalMethod, TimeSpan? ttl)
        {
            try
            {
                var db = clientManager.GetDatabase();
                var hashData = db.HashGet(hashsetKey, hashfieldKey);
                if (hashData.HasValue)
                {
                    Debug.WriteLine($"Redis Hash cache HIT : {hashsetKey}/{hashfieldKey}");

                    return JsonConvert.DeserializeObject<T>(hashData, GetJsonSerializerSettings());
                }
                else
                {
                    var data = DataRetrievalMethod.Invoke();
                    var strSet = JsonConvert.SerializeObject(data, GetJsonSerializerSettings());
                    db.HashSet(hashsetKey, hashfieldKey, strSet, When.Always, CommandFlags.FireAndForget);

                    return data;
                }
            }
            catch (Exception ex)
            {

                return DataRetrievalMethod.Invoke();
            }
        }


        public Task<T> GetOrStoreAsync<T>(string key, Func<Task<T>> dataRetrievalMethodAsync, TimeSpan? timeToLive)
        {
            return Task.Run<T>(async () =>
           {
               //#if DEBUG
               //                return dataRetrievalMethodAsync.Invoke();
               //#endif
               try
               {
                   var db = clientManager.GetDatabase();
                   var dbResults = await db.StringGetAsync(key);
                   if (dbResults.HasValue)
                   {
                       Debug.WriteLine($"Redis cache HIT : {key}");
                       return JsonConvert.DeserializeObject<T>(dbResults, GetJsonSerializerSettings());
                   }
                   else
                   {
                       var data = await dataRetrievalMethodAsync.Invoke();
                       var strSet = JsonConvert.SerializeObject(data, GetJsonSerializerSettings());
                       await db.StringSetAsync(key, strSet, timeToLive, When.Always, CommandFlags.FireAndForget);
                       return data;
                   }
               }
               catch (Exception)
               {
                   return await dataRetrievalMethodAsync.Invoke();
               }
           });

        }

        public T HashGetByKey<T>(string hashsetKey, string itemKey)
        {
            try
            {
                var db = clientManager.GetDatabase();
                var item = db.HashGet(hashsetKey, itemKey);
                if (item.HasValue)
                {
                    var val = JsonConvert.DeserializeObject<T>(item, GetJsonSerializerSettings());
                    return val;
                }

                return default(T);
            }
            catch (Exception e)
            {
                return default(T);
            }



        }

        public void HashSetByKey<T>(string hashsetKey, string itemKey, T item)
        {
            try
            {
                var db = clientManager.GetDatabase();
                var strSet = JsonConvert.SerializeObject(item, GetJsonSerializerSettings());
                db.HashSet(hashsetKey, itemKey, strSet, When.Always, CommandFlags.FireAndForget);
            }
            catch (Exception)
            {


            }
        }

        public void DeleteHashSet(string hashsetKey)
        {
            try
            {
                var db = clientManager.GetDatabase();
                var set = db.HashKeys(hashsetKey);
               
               var id =  db.HashDelete(hashsetKey, set);
             
            }
            catch (Exception)
            {


            }
        }

        public bool HashSetNotEmpty(string hashSetKey)
        {
            var db = clientManager.GetDatabase();
            var set = db.HashGetAll(hashSetKey);
            return set != null && set.Length > 0;
        }
        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects, TypeNameHandling = TypeNameHandling.Objects };
        }


    }
}