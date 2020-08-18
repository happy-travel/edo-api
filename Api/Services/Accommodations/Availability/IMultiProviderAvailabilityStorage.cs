using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public interface IMultiProviderAvailabilityStorage
    {
        Task<(DataProviders DataProvider, TObject Result)> GetProviderResult<TObject>(string keyPrefix, DataProviders dataProvider, bool isCachingEnabled = false);
        
        Task<(DataProviders DataProvider, TObject Result)[]> GetProviderResults<TObject>(string keyPrefix, List<DataProviders> dataProviders, bool isCachingEnabled = false);

        Task SaveObject<TObjectType>(string keyPrefix, TObjectType @object, DataProviders? dataProvider = null);
    }
}