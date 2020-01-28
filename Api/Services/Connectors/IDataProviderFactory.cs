using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IDataProviderFactory
    {
        IReadOnlyCollection<(DataProviders Key, IDataProvider Provider)> GetAll();

        IDataProvider Get(DataProviders key);
    }
}