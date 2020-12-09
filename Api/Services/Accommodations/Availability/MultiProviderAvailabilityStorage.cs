using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class MultiProviderAvailabilityStorage : IMultiProviderAvailabilityStorage
    {
        public MultiProviderAvailabilityStorage(IDistributedFlow distributedFlow, IMemoryFlow memoryFlow)
        {
            _distributedFlow = distributedFlow;
            _memoryFlow = memoryFlow;
        }


        public Task<(Suppliers Supplier, TObject Result)[]> Get<TObject>(string keyPrefix, List<Suppliers> suppliers, bool isCachingEnabled = false)
        {
            var supplierTasks = suppliers
                .Select(async p =>
                {
                    var key = BuildKey<TObject>(keyPrefix, p);
                    return (
                        ProviderKey: p,
                        Object: await Get(key, isCachingEnabled)
                    );
                });
            
            return Task.WhenAll(supplierTasks);


            async ValueTask<TObject> Get(string key, bool isCachingEnabled)
            {
                if (!isCachingEnabled)
                    return await _distributedFlow.GetAsync<TObject>(key);
                
                if (_memoryFlow.TryGetValue(key, out TObject value))
                    return value;
                    
                value = await _distributedFlow.GetAsync<TObject>(key);
                if (value != null && !value.Equals(default))
                    _memoryFlow.Set(key, value, CacheExpirationTime);
                
                return value;
            }
        }


        public Task Save<TObject>(string keyPrefix, TObject @object, Suppliers supplier)
        {
            var key = BuildKey<TObject>(keyPrefix, supplier);
            return _distributedFlow.SetAsync(key, @object, CacheExpirationTime);
        }


        private string BuildKey<TObjectType>(string keyPrefix, Suppliers supplier)
            => _distributedFlow.BuildKey(nameof(MultiProviderAvailabilityStorage),
                keyPrefix,
                typeof(TObjectType).Name,
                supplier.ToString());


        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromMinutes(15);

        private readonly IDistributedFlow _distributedFlow;
        private readonly IMemoryFlow _memoryFlow;
    }
}