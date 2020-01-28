using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct ProviderData<TData>
    {
        public ProviderData(DataProviders dataProvider, TData data)
        {
            DataProvider = dataProvider;
            Data = data;
        }
        
        public DataProviders DataProvider { get; }
        public TData Data { get; }
    }
    
    public static class ProviderData
    {
        public static ProviderData<TProviderData> Create<TProviderData>(DataProviders dataProvider, TProviderData data) => new ProviderData<TProviderData>(dataProvider, data);
    }
}