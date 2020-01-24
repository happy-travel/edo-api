using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Common.Enums;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class DataProviderFactory : IDataProviderFactory
    {
        private readonly ILocationService _locationService;


        public DataProviderFactory(IOptions<DataProviderOptions> options, IDataProviderClient dataProviderClient, ILocationService locationService)
        {
            _locationService = locationService;
            _dataProviders = new Dictionary<DataProviders, IDataProvider>
            {
                // TODO: Add other data providers.
                {DataProviders.Netstorming, new DataProvider(dataProviderClient, locationService, options.Value.Netstorming)}
            };
        }


        public IDataProvider Get(DataProviders dataProvider) => _dataProviders[dataProvider];

        public IEnumerable<(DataProviders, IDataProvider)> GetAll() => _dataProviders.Select(dp=> (dp.Key, dp.Value));

        private readonly Dictionary<DataProviders, IDataProvider> _dataProviders;
    }
}