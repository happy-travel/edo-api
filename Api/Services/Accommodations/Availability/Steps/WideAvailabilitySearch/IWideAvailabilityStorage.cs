using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public interface IWideAvailabilityStorage
    {
        Task<List<(Suppliers SupplierKey, List<AccommodationAvailabilityResult> AccommodationAvailabilities)>> GetResults(Guid searchId, List<Suppliers> suppliers);
        Task<List<(Suppliers SupplierKey, AccommodationAvailabilityResult AccommodationAvailabilities)>> GetResults(Guid searchId, int top, int skip, List<Suppliers> suppliers);

        Task SaveResults(Guid searchId, Suppliers supplier, List<AccommodationAvailabilityResult> results);

        Task<List<(Suppliers SupplierKey, SupplierAvailabilitySearchState States)>> GetStates(Guid searchId,
            List<Suppliers> suppliers);
        
        Task SaveState(Guid searchId, SupplierAvailabilitySearchState state, Suppliers supplier);
    }
}