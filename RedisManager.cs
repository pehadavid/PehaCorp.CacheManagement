using System;
using StackExchange.Redis;

namespace UnitSense.CacheManagement
{
    public static class RedisManager
    {
        private static ConnectionMultiplexer clientsManager;
        private static ConfigurationOptions redisConfig;

        public static void Initialize(ConfigurationOptions config)
        {

            redisConfig = config;
            clientsManager = ConnectionMultiplexer.Connect(config);
        }

        public static ConfigurationOptions GetOptions(string host, string password, int port)
        {
            return  new ConfigurationOptions()
            {
                Password = password,
                EndPoints = { { host, port } },
                ConnectTimeout = 60000
            };
        }
        public static ConnectionMultiplexer GetClientManager()
        {
            if (redisConfig == null || clientsManager == null)
                throw new AccessViolationException("Not initialized. Please call Initiliaze before any operations.");

            return clientsManager;
        }


        public static ICacheManager GetRedisCacheManager()
        {
            return new RedisCacheManager(GetClientManager());
        }
    }
    public class BroadcastItem
    {
        public Guid UniqueID { get; set; }
        public DateTime DateGen { get; set; }
        public object InnerObject { get; set; }
        public string Key { get; set; }
        public string HashsetKey { get; set; }
        public BroadcastOperation Operation { get; set; }

        public string BroadcastMessage { get; set; }
        public BroadcastItem()
        {
            this.UniqueID = Guid.NewGuid();
            this.DateGen = DateTime.UtcNow;
            
        }

        public BroadcastItem(object innerObject, BroadcastOperation operation, string hashsetKey) 
        {
            this.UniqueID = Guid.NewGuid();
            this.DateGen = DateTime.UtcNow;
            this.InnerObject = innerObject;
            this.Operation = operation;
            this.HashsetKey = hashsetKey;

        }
    }

    public enum BroadcastOperation
    {
        WRITE,
        DELETE       
    }
}