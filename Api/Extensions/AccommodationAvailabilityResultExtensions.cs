using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Storage;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Edo.Api.Extensions
{
    public static class AccommodationAvailabilityResultExtensions
    {
        public static CachedAccommodationAvailabilityResult Map(this AccommodationAvailabilityResult result, Guid searchId, Suppliers supplier)
        {
            return new CachedAccommodationAvailabilityResult
            {
                SearchId = searchId,
                Supplier = supplier,
                Created = DateTime.UtcNow,
                Timestamp = result.Timestamp,
                AvailabilityId = result.AvailabilityId,
                RoomContractSets = result.RoomContractSets.Select(rcs => new CachedRoomContractSet
                {
                    Id = rcs.Id,
                    Rate = new CachedRate
                    {
                        Currency = rcs.Rate.Currency,
                        Description = rcs.Rate.Description,
                        Gross = new CachedMoneyAmount
                        {
                            Amount = rcs.Rate.Gross.Amount,
                            Currency = rcs.Rate.Gross.Currency
                        },
                        Discounts = rcs.Rate.Discounts.Select(d => new CachedDiscount
                        {
                            Description = d.Description,
                            Percent = d.Percent
                        }).ToList(),
                        FinalPrice = new CachedMoneyAmount
                        {
                            Amount = rcs.Rate.FinalPrice.Amount,
                            Currency = rcs.Rate.FinalPrice.Currency
                        },
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
                            Gross = new CachedMoneyAmount
                            {
                                Amount = room.Rate.Gross.Amount,
                                Currency = room.Rate.Gross.Currency
                            },
                            Discounts = room.Rate.Discounts.Select(d => new CachedDiscount
                            {
                                Description = d.Description,
                                Percent = d.Percent
                            }).ToList(),
                            FinalPrice = new CachedMoneyAmount
                            {
                                Amount = room.Rate.FinalPrice.Amount,
                                Currency = room.Rate.FinalPrice.Currency
                            },
                            Type = room.Rate.Type
                        },
                        Remarks = room.Remarks,
                        AdultsNumber = room.AdultsNumber,
                        ChildrenAges = room.ChildrenAges,
                        IsExtraBedNeeded = room.IsExtraBedNeeded,
                        Deadline = room.Deadline,
                        IsAdvancePurchaseRate = room.IsAdvancePurchaseRate,
                        DailyRoomRates = room.DailyRoomRates.Select(d => new CachedDailyRate
                        {
                            FromDate = d.FromDate,
                            ToDate = d.ToDate,
                            Gross = new CachedMoneyAmount
                            {
                                Amount = d.Gross.Amount,
                                Currency = d.Gross.Currency
                            },
                            FinalPrice = new CachedMoneyAmount
                            {
                                Amount = d.FinalPrice.Amount,
                                Currency = d.FinalPrice.Currency
                            },
                            Type = d.Type,
                            Description = d.Description
                        }).ToList(),
                        Type = room.Type
                    }).ToList(),
                    Supplier = rcs.Supplier,
                    Tags = rcs.Tags,
                    IsDirectContract = rcs.IsDirectContract
                }).ToList(),
                MinPrice = result.MinPrice,
                MaxPrice = result.MaxPrice,
                CheckInDate = result.CheckInDate,
                CheckOutDate = result.CheckOutDate,
                HtId = result.HtId,
                SupplierAccommodationCode = result.SupplierAccommodationCode
            };
        }
    }
}