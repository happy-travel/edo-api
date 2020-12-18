using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public static class ContractsMappingExtensions
    {
        public static List<RoomContractSet> ToRoomContractSetList(this IEnumerable<EdoContracts.Accommodations.Internals.RoomContractSet> roomContractSets, Suppliers? supplier = null)
        {
            return roomContractSets
                .Select(rs => ToRoomContractSet(rs, supplier))
                .ToList();
        }


        public static List<RoomContractSet> ApplySearchFilters(this IEnumerable<RoomContractSet> roomContractSets,
            AccommodationBookingSettings searchSettings, IDateTimeProvider dateTimeProvider, DateTime checkInDate)
        {
            return roomContractSets.Where(roomSet =>
                {
                    if (searchSettings.AprMode == AprMode.Hide && roomSet.IsAdvancePurchaseRate)
                        return false;

                    if (searchSettings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.Hide)
                    {
                        var tomorrow = dateTimeProvider.UtcTomorrow();
                        if (checkInDate <= tomorrow)
                            return false;

                        var deadlineDate = roomSet.Deadline.Date;
                        if (deadlineDate.HasValue && deadlineDate.Value.Date <= tomorrow)
                            return false;
                    }

                    return true;
                })
                .ToList();
        }


        public static RoomContractSet ToRoomContractSet(this in EdoContracts.Accommodations.Internals.RoomContractSet roomContractSet, Suppliers? supplier)
        {
            return new RoomContractSet(roomContractSet.Id,
                roomContractSet.Rate.ToRate(),
                roomContractSet.Deadline.ToDeadline(),
                roomContractSet.RoomContracts.ToRoomContractList(),
                roomContractSet.IsAdvancePurchaseRate,
                supplier);
        }


        public static RoomContractSetAvailability? ToRoomContractSetAvailability(this in EdoContracts.Accommodations.RoomContractSetAvailability? availability, Suppliers? supplier)
        {
            if (availability is null)
                return null;

            var availabilityValue = availability.Value;
            return new RoomContractSetAvailability(availabilityValue.AvailabilityId,
                availabilityValue.CheckInDate,
                availabilityValue.CheckOutDate,
                availabilityValue.NumberOfNights,
                availabilityValue.Accommodation,
                availabilityValue.RoomContractSet.ToRoomContractSet(supplier));
        }
        
        
        public static Deadline ToDeadline(this in EdoContracts.Accommodations.Deadline deadline)
        {
            return new Deadline(deadline.Date, deadline.Policies.ToPolicyList(), deadline.Remarks, deadline.IsFinal);
        }


        private static List<CancellationPolicy> ToPolicyList(this IEnumerable<EdoContracts.Accommodations.Internals.CancellationPolicy> policies)
        {
            return policies
                .Select(ToCancellationPolicy)
                .ToList();
        }
        

        private static RoomContract ToRoomContract(this EdoContracts.Accommodations.Internals.RoomContract roomContract)
        {
            return new RoomContract(roomContract.BoardBasis, roomContract.MealPlan,
                roomContract.ContractTypeCode, roomContract.IsAvailableImmediately, roomContract.IsDynamic,
                roomContract.ContractDescription, roomContract.Remarks, roomContract.DailyRoomRates.ToDailyRateList(), roomContract.Rate.ToRate(),
                roomContract.AdultsNumber, roomContract.ChildrenAges, roomContract.Type, roomContract.IsExtraBedNeeded,
                roomContract.Deadline.ToDeadline(), roomContract.IsAdvancePurchaseRate);
        }
        
        
        private static Rate ToRate(this EdoContracts.General.Rate rate)
        {
            return new Rate(rate.FinalPrice, rate.Gross, rate.Discounts, rate.Type, rate.Description);
        }


        private static CancellationPolicy ToCancellationPolicy(EdoContracts.Accommodations.Internals.CancellationPolicy policy)
        {
            return new CancellationPolicy(policy.FromDate, policy.Percentage);
        }
        
        
        private static List<DailyRate> ToDailyRateList(this IEnumerable<EdoContracts.General.DailyRate> rates)
        {
            return rates
                .Select(r => new DailyRate(r.FromDate,
                    r.ToDate,
                    r.FinalPrice,
                    r.Gross,
                    r.Type,
                    r.Description))
                .ToList();
        }
        
        
        private static List<RoomContract> ToRoomContractList(
            this IEnumerable<EdoContracts.Accommodations.Internals.RoomContract> roomContractSets)
        {
            return roomContractSets
                .Select(ToRoomContract)
                .ToList();
        }
    }
}