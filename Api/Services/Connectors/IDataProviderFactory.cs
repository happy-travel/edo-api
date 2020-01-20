using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IDataProviderFactory
    {
        IDataProvider Get(DataProviders dataProvider);
        IEnumerable<(DataProviders Key, IDataProvider Provider)> GetAll();
    }
}