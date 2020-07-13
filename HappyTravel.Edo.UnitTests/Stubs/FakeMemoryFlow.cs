using System;
using System.Threading;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using Microsoft.Extensions.Caching.Memory;

namespace HappyTravel.Edo.UnitTests.Stubs
{
    public class FakeMemoryFlow : IMemoryFlow
    {
        public T GetOrSet<T>(string key, Func<T> getValueFunction, TimeSpan absoluteExpirationRelativeToNow)
        {
            return getValueFunction();
        }


        public T GetOrSet<T>(string key, Func<T> getValueFunction, MemoryCacheEntryOptions options) => throw new NotImplementedException();


        public async ValueTask<T> GetOrSetAsync<T>(string key, Func<Task<T>> getValueFunction, TimeSpan absoluteExpirationRelativeToNow,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await getValueFunction();
        }


        public async ValueTask<T> GetOrSetAsync<T>(string key, Func<Task<T>> getValueFunction, MemoryCacheEntryOptions options,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await getValueFunction();
        }


        public void Remove(string key)
        {
            throw new NotImplementedException();
        }


        public void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            throw new NotImplementedException();
        }


        public void Set<T>(string key, T value, MemoryCacheEntryOptions options)
        {
            throw new NotImplementedException();
        }


        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            return false;
        }


        public IMemoryCache Instance => throw new NotImplementedException();
        public FlowOptions Options => new FlowOptions();
    }
}