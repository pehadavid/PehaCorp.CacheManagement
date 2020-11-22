using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace UnitSense.CacheManagement
{
    public class RedisBusHandler
    {
        public string BusName => $"__repositoryBusHandler{envPrefix}";

        private LocalCacheManager localCacheManager;
        private ISubscriber subscriber;
        private string envPrefix;
        private Task subTask;

        public RedisBusHandler(RedisCacheManager cacheManager, LocalCacheManager localCacheManager)
        {
            this.localCacheManager = localCacheManager;
            this.envPrefix = cacheManager.EnvName;
            var multiplexer = cacheManager.GetMultiplexer();
            subscriber = multiplexer.GetSubscriber();
            multiplexer.ConnectionRestored += OnConnectionRestored;
            multiplexer.ConnectionFailed += MultiplexerOnConnectionFailed;
            CreateSubTask();
        }

        private void MultiplexerOnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine("Connection failed to redis");
            this.OnRedisError(new RedisErrorHandlerArgs());
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            HandleSubTaskException();
        }

        private void CreateSubTask()
        {
            subTask = subscriber.SubscribeAsync(BusName, (channel, value) =>
            {
                Debug.WriteLine($"data received from subscriber");

                var item = JsonConvert.DeserializeObject<BroadcastItem>(value,
                    RedisCacheManager.GetJsonSerializerSettings());
                OnRedisReceive(new RedisReceivedHandlerArgs() {BroadcastItem = item});
                //TODO : perform some DateTime check to avoid outdated objects
                if (item.Operation == BroadcastOperation.WRITE)
                    this.localCacheManager.SetValue(item.Key, item.InnerObject, TimeSpan.FromMinutes(5));
                if (item.Operation == BroadcastOperation.DELETE)
                {
                    this.localCacheManager.DeleteAsync(item.Key).GetAwaiter().GetResult();
                }

                // delete hashsets
                this.localCacheManager.DeleteHashSet(item.HashsetKey);
            });

            subTask.ContinueWith(task => { HandleSubTaskException(); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void HandleSubTaskException()
        {
            Console.WriteLine("Cache Redis Synchronize has been faulted, restarting");
            subscriber?.Unsubscribe(BusName);
            CreateSubTask();
        }

        public async Task PublishAsync(string serializeObject)
        {
            await subscriber.PublishAsync(BusName, serializeObject);
        }

        public event RedisReceivedHandler RedisReceive;
        public event RedisErrorHandler RedisError;

        protected virtual void OnRedisReceive(RedisReceivedHandlerArgs args)
        {
            RedisReceive?.Invoke(this, args);
        }

        protected virtual void OnRedisError(RedisErrorHandlerArgs args)
        {
            RedisError?.Invoke(this, args);
        }
    }

    public delegate void RedisErrorHandler(object sender, RedisErrorHandlerArgs args);


    public delegate void RedisReceivedHandler(object sender, RedisReceivedHandlerArgs args);

    public class RedisReceivedHandlerArgs
    {
        public BroadcastItem BroadcastItem { get; set; }
    }

    public class RedisErrorHandlerArgs : EventArgs
    {
    }
}