using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Markups
{
    public readonly struct DataWithMarkup<TData>
    {
        public DataWithMarkup(TData data, List<AppliedMarkup> appliedMarkups)
        {
            Data = data;
            AppliedMarkups = appliedMarkups;
        }
        
        public TData Data { get; }
        public List<AppliedMarkup> AppliedMarkups { get; }
    }
    
    public static class DataWithMarkup
    {
        public static DataWithMarkup<TProviderData> Create<TProviderData>(TProviderData data, List<AppliedMarkup> policies) => new DataWithMarkup<TProviderData>(data, policies);
    }
}