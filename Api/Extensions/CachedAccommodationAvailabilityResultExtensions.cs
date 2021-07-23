using System.Linq;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Storage;
using HappyTravel.EdoContracts.General;
using HappyTravel.Money.Models;
using DailyRate = HappyTravel.Edo.Api.Models.Accommodations.DailyRate;
using Rate = HappyTravel.Edo.Api.Models.Accommodations.Rate;

namespace HappyTravel.Edo.Api.Extensions
{
    public static class CachedAccommodationAvailabilityResultExtensions
    {
        public static AccommodationAvailabilityResult Map(this CachedAccommodationAvailabilityResult result)
        {
            return new (timestamp: result.Timestamp,
                availabilityId: result.AvailabilityId,
                roomContractSets: result.RoomContractSets.Select(rcs => new RoomContractSet(id: rcs.Id,
                    rate: new Rate(finalPrice: new MoneyAmount(rcs.Rate.FinalPrice.Amount, rcs.Rate.FinalPrice.Currency),
                        gross: new MoneyAmount(rcs.Rate.Gross.Amount, rcs.Rate.Gross.Currency),
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
                        dailyRoomRates: r.DailyRoomRates
                            .Select(d => new DailyRate(fromDate: d.FromDate, 
                                toDate: d.ToDate, 
                                finalPrice: new MoneyAmount(d.FinalPrice.Amount, d.FinalPrice.Currency), 
                                gross: new MoneyAmount(d.Gross.Amount, d.Gross.Currency),
                                type: d.Type, 
                                description: d.Description))
                            .ToList(),
                        rate: new Rate(finalPrice: new MoneyAmount(r.Rate.FinalPrice.Amount, r.Rate.FinalPrice.Currency),
                            gross: new MoneyAmount(r.Rate.Gross.Amount, r.Rate.Gross.Currency),
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
                minPrice: result.MinPrice,
                maxPrice: result.MaxPrice,
                checkInDate: result.CheckInDate,
                checkOutDate: result.CheckOutDate,
                htId: result.HtId,
                supplierAccommodationCode: result.SupplierAccommodationCode);
        }
    }
}