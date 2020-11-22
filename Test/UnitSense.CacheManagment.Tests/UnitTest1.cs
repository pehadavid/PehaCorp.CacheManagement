using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitSense.CacheManagement;
using Xunit;

namespace UnitSense.CacheManagment.Tests
{
    public class MemoyCacheTests
    {
        [Fact]
        public async Task TestSimpleKeys()
        {
            var localCache = new LocalCacheManager();
            var testData = CreateTestDataCache();
            localCache.SetValue(testData.Item1, testData.Item2, TimeSpan.FromSeconds(5));
            localCache.GetByKey<KeyValuePair<string, Guid>>(testData.Item1, out var fetchBack);
            Assert.True(testData.Item2.Value == fetchBack.Value);
            await Task.Delay(6000);
            var refetch = localCache.GetByKey<KeyValuePair<string, Guid>>(testData.Item1, out fetchBack);
            Assert.False(refetch);
        }

        [Fact]
        public async Task TestSimpleGetOrStore()
        {
            var testData = CreateTestDataCache();
            var localCache = new LocalCacheManager();
            var gosdata = localCache.GetOrStore<TestData>(testData.Item1, () =>
            {
                return  new TestData() { DummyGuid = testData.Item2.Value, DummyString = testData.Item2.Key };
            }, TimeSpan.FromSeconds(5));
            Assert.True( gosdata.DummyGuid == testData.Item2.Value);
        }
        
        [Fact]
        public async Task TestSimpleHashSet()
        {
            var testData = CreateTestDataCache();
            var localCache = new LocalCacheManager();
            var hashsetKey = Guid.NewGuid().ToString();
            
            localCache.HashSetByKey(hashsetKey, testData.Item1, new TestData() { DummyGuid = testData.Item2.Value});
            var back = localCache.HashGetByKey<TestData>(hashsetKey, testData.Item1);
            Assert.True(back.DummyGuid == testData.Item2.Value);
            await Task.Delay(TimeSpan.FromMinutes(1));
            back = localCache.HashGetByKey<TestData>(hashsetKey, testData.Item1);
            Assert.Null(back);

        }
        
        [Fact]
        public async Task TestHashGetOrStore()
        {
            var testData = CreateTestDataCache();
            var localCache = new LocalCacheManager();
            var hashsetKey = Guid.NewGuid().ToString();

            bool generated = false;
            var gosData = localCache.GetOrStore(hashsetKey, testData.Item1, () =>
            {
                generated = true;
                return new TestData() {DummyGuid = testData.Item2.Value};
            }, TimeSpan.FromSeconds(10) );
            
            Assert.True(generated);
            Assert.True(gosData.DummyGuid == testData.Item2.Value);
            generated = false;
            
             gosData = localCache.GetOrStore(hashsetKey, testData.Item1, () =>
            {
                generated = true;
                return new TestData() {DummyGuid = testData.Item2.Value};
            }, TimeSpan.FromSeconds(10) );
             
             Assert.False(generated);

        }

        private (string, KeyValuePair<string, Guid>) CreateTestDataCache()
        {
            var key = Guid.NewGuid().ToString();
            KeyValuePair<string, Guid> kvp = new KeyValuePair<string, Guid>(Guid.NewGuid().ToString(), Guid.NewGuid());
            return (key, kvp);
        }
    }

    public class TestData
    {
        public string DummyString { get; set; }
        public Guid DummyGuid { get; set; }
    }
}