using System.Collections.Generic;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Common.Enums;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class DataProviderFactory : IDataProviderFactory
    {
        public DataProviderFactory(IOptions<DataProviderOptions> options, IDataProviderClient dataProviderClient)
        {
            _dataProviders = new Dictionary<DataProviders, IDataProvider>
            {
                // TODO: Add other data providers.
                {DataProviders.Netstorming, new DataProvider(dataProviderClient, options.Value.Netstorming)}
            };
        }


        public IDataProvider Get(DataProviders dataProvider) => _dataProviders[dataProvider];

        private readonly Dictionary<DataProviders, IDataProvider> _dataProviders;
    }
}