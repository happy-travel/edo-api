using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Storage;
using HappyTravel.EdoContracts.General;
using HappyTravel.SuppliersCatalog;
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
                    Results = g.Select(c => new AccommodationAvailabilityResult(timestamp: c.Timestamp,
                            availabilityId: c.AvailabilityId,
                            roomContractSets: c.RoomContractSets.Select(rcs => new RoomContractSet(id: rcs.Id,
                                rate: new Rate(finalPrice: rcs.Rate.FinalPrice,
                                    gross: rcs.Rate.Gross,
                                    discounts: rcs.Rate.Discounts
                                        .Select(d => new Discount(d.Percent, d.Description))
                                        .ToList(),
                                    type: rcs.Rate.Type,
                                    description: rcs.Rate.Description),
                                deadline: rcs.Deadline,
                                rooms: rcs.Rooms.Select(r => new RoomContract(boardBasis: r.BoardBasis,
                                    mealPlan: r.MealPlan,
                                    contractTypeCode: r.ContractTypeCode,
                                    isAdvancePurchaseRate: r.IsAdvancePurchaseRate,
                                    isAvailableImmediately: r.IsAvailableImmediately,
                                    isDynamic: r.IsDynamic,
                                    contractDescription: r.ContractDescription,
                                    remarks: r.Remarks,
                                    dailyRoomRates: r.DailyRoomRates,
                                    rate: new Rate(finalPrice: r.Rate.FinalPrice,
                                        gross: r.Rate.Gross,
                                        discounts: r.Rate.Discounts
                                            .Select(d => new Discount(d.Percent, d.Description))
                                            .ToList(),
                                        type: r.Rate.Type,
                                        description: r.Rate.Description),
                                    adultsNumber: r.AdultsNumber,
                                    childrenAges: r.ChildrenAges,
                                    type: r.Type,
                                    isExtraBedNeeded: r.IsExtraBedNeeded,
                                    deadline: r.Deadline)).ToList(),
                                isAdvancePurchaseRate: rcs.IsAdvancePurchaseRate,
                                supplier: rcs.Supplier,
                                tags: rcs.Tags,
                                isDirectContract: rcs.IsDirectContract
                            )).ToList(),
                            minPrice: c.MinPrice,
                            maxPrice: c.MaxPrice,
                            checkInDate: c.CheckInDate,
                            checkOutDate: c.CheckOutDate,
                            htId: c.HtId,
                            supplierAccommodationCode: c.SupplierAccommodationCode))
                        .ToList()
                })
                .Select(r => (r.Supplier, r.Results))
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
            return _availabilityStorage.Save(results.Select(r => new CachedAccommodationAvailabilityResult
            {
                SearchId = searchId,
                Supplier = supplier,
                Created = DateTime.UtcNow,
                Timestamp = r.Timestamp,
                AvailabilityId = r.AvailabilityId,
                RoomContractSets = r.RoomContractSets.Select(rcs => new CachedRoomContractSet
                {
                    Id = rcs.Id,
                    Rate = new CachedRate
                    {
                        Currency = rcs.Rate.Currency,
                        Description = rcs.Rate.Description,
                        Gross = rcs.Rate.Gross,
                        Discounts = rcs.Rate.Discounts.Select(d => new CachedDiscount
                        {
                            Description = d.Description,
                            Percent = d.Percent
                        }).ToList(),
                        FinalPrice = rcs.Rate.FinalPrice,
                        Type = rcs.Rate.Type
                    },
                    Deadline = rcs.Deadline,
                    IsAdvancePurchaseRate = rcs.IsAdvancePurchaseRate,
                    Rooms = rcs.Rooms.Select(room => new CachedRoomContract
                    {
                        BoardBasis = room.BoardBasis,
                        MealPlan = room.MealPlan,
                        ContractTypeCode = room.ContractTypeCode,
                        IsAvailableImmediately = room.IsAvailableImmediately,
                        IsDynamic = room.IsDynamic,
                        ContractDescription = room.ContractDescription,
                        Rate = new CachedRate
                        {
                            Currency = room.Rate.Currency,
                            Description = room.Rate.Description,
                            Gross = room.Rate.Gross,
                            Discounts = room.Rate.Discounts.Select(d => new CachedDiscount
                            {
                                Description = d.Description,
                                Percent = d.Percent
                            }).ToList(),
                            FinalPrice = room.Rate.FinalPrice,
                            Type = room.Rate.Type
                        },
                        Remarks = room.Remarks,
                        AdultsNumber = room.AdultsNumber,
                        ChildrenAges = room.ChildrenAges,
                        IsExtraBedNeeded = room.IsExtraBedNeeded,
                        Deadline = room.Deadline,
                        IsAdvancePurchaseRate = room.IsAdvancePurchaseRate,
                        DailyRoomRates = room.DailyRoomRates,
                        Type = room.Type
                    }).ToList(),
                    Supplier = rcs.Supplier,
                    Tags = rcs.Tags,
                    IsDirectContract = rcs.IsDirectContract
                }).ToList(),
                MinPrice = r.MinPrice,
                MaxPrice = r.MaxPrice,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                HtId = r.HtId,
                SupplierAccommodationCode = r.SupplierAccommodationCode 
            }).ToList());
        }
        
        private readonly IMultiProviderAvailabilityStorage _multiProviderAvailabilityStorage;
        private readonly IAvailabilityStorage _availabilityStorage;
    }
}