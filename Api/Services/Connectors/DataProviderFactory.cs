using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Common.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class DataProviderFactory : IDataProviderFactory
    {
        public DataProviderFactory(IOptions<DataProviderOptions> options, 
            IDataProviderClient dataProviderClient,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _dataProviders = new Dictionary<DataProviders, IDataProvider>
            {
                // TODO: Add other data providers.
                {DataProviders.Netstorming, new DataProvider(dataProviderClient, _options.Netstorming, serviceProvider.GetRequiredService<ILogger<DataProvider>>())}
            };
        }

        
        public IReadOnlyCollection<(DataProviders, IDataProvider)> GetAll()
        {
            var dataProvidersList = _dataProviders
                .Where(dp=> _options.EnabledProviders.Contains(dp.Key))
                .Select(dp => (dp.Key, dp.Value))
                .ToList();
            
            return new ReadOnlyCollection<(DataProviders, IDataProvider)>(dataProvidersList);
        }


        public IDataProvider Get(DataProviders key) => _dataProviders[key];

        private readonly Dictionary<DataProviders, IDataProvider> _dataProviders;
        private readonly DataProviderOptions _options;
    }
}