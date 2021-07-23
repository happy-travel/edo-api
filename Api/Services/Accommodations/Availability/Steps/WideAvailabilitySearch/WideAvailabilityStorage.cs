using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Storage;
using HappyTravel.EdoContracts.General;
using HappyTravel.Money.Models;
using HappyTravel.SuppliersCatalog;
using DailyRate = HappyTravel.Edo.Api.Models.Accommodations.DailyRate;
using Rate = HappyTravel.Edo.Api.Models.Accommodations.Rate;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilityStorage : IWideAvailabilityStorage
    {
        public WideAvailabilityStorage(IMultiProviderAvailabilityStorage multiProviderAvailabilityStorage, IAvailabilityStorage availabilityStorage)
        {
            _multiProviderAvailabilityStorage = multiProviderAvailabilityStorage;
            _availabilityStorage = availabilityStorage;
        }


        public async Task<List<(Suppliers SupplierKey, List<AccommodationAvailabilityResult> AccommodationAvailabilities)>> GetResults(Guid searchId, List<Suppliers> suppliers)
        {
            var cached = await _availabilityStorage.Get(r => r.SearchId == searchId && suppliers.Contains(r.Supplier));

            return cached
                .GroupBy(r => r.Supplier)
                .Select(g => new
                {
                    Supplier = g.Key, 
                    Results = g.Select(c => c.Map()).ToList()
                })
                .Select(r => (r.Supplier, r.Results))
                .ToList();
        }


        public async Task<List<(Suppliers SupplierKey, AccommodationAvailabilityResult AccommodationAvailabilities)>> GetResults(Guid searchId, int top, int skip, List<Suppliers> suppliers)
        {
            var cached = await _availabilityStorage.Get(r => r.SearchId == searchId && suppliers.Contains(r.Supplier), top, skip);
            
            return cached
                .Select(c => (c.Supplier, c.Map()))
                .ToList();
        }


        public async Task<List<(Suppliers SupplierKey, SupplierAvailabilitySearchState States)>> GetStates(Guid searchId,
            List<Suppliers> suppliers)
        {
            return (await _multiProviderAvailabilityStorage
                .Get<SupplierAvailabilitySearchState>(searchId.ToString(), suppliers, false))
                .Where(t => !t.Result.Equals(default))
                .ToList();
        }


        public Task SaveState(Guid searchId, SupplierAvailabilitySearchState state, Suppliers supplier)
        {
            return _multiProviderAvailabilityStorage.Save(searchId.ToString(), state, supplier);
        }


        public Task SaveResults(Guid searchId, Suppliers supplier, List<AccommodationAvailabilityResult> results)
        {
            return _availabilityStorage.Save(results.Select(r => r.Map(searchId, supplier)).ToList());
        }
        
        private readonly IMultiProviderAvailabilityStorage _multiProviderAvailabilityStorage;
        private readonly IAvailabilityStorage _availabilityStorage;
    }
}