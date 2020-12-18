using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Common.Enums.AgencySettings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class RestrictedRateChecker
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAccommodationBookingSettingsService _bookingSettingsService;


        public RestrictedRateChecker(IDateTimeProvider dateTimeProvider,
            IAccommodationBookingSettingsService bookingSettingsService)
        {
            _dateTimeProvider = dateTimeProvider;
            _bookingSettingsService = bookingSettingsService;
        }
        
        
        public async Task<Result> CheckRateRestrictions(BookingAvailabilityInfo availabilityInfo, AgentContext agentContext)
        {
            var settings = await _bookingSettingsService.Get(agentContext);
            if (!AreAprSettingsSuitable(availabilityInfo, settings))
                return Result.Failure("You can't book the restricted contract without explicit approval from a Happytravel.com officer.");

            if (!AreDeadlineSettingsSuitable(availabilityInfo, settings))
                return Result.Failure("You can't book the contract within deadline without explicit approval from a Happytravel.com officer.");

            return Result.Success();
                
                
            bool AreDeadlineSettingsSuitable(BookingAvailabilityInfo availabilityInfo,
                AccommodationBookingSettings settings)
            {
                var deadlineDate = availabilityInfo.RoomContractSet.Deadline.Date ?? availabilityInfo.CheckInDate;
                if (deadlineDate.Date > _dateTimeProvider.UtcTomorrow())
                    return true;

                return settings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.CardAndAccountPurchases ||
                    settings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.CardPurchasesOnly;
            }


            static bool AreAprSettingsSuitable(BookingAvailabilityInfo availabilityInfo,
                AccommodationBookingSettings settings)
            {
                if (!availabilityInfo.RoomContractSet.IsAdvancePurchaseRate)
                    return true;

                return settings.AprMode == AprMode.CardPurchasesOnly ||
                    settings.AprMode == AprMode.CardAndAccountPurchases;
            }
        }
    }
}