using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public static class BookingPaymentMethodsHelper
    {
        public static List<PaymentMethods> GetAvailablePaymentMethods(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
            AccommodationBookingSettings settings, CounterpartyContractKind contractKind, DateTime date)
            => AllAvailablePaymentMethods
                .Intersect(GetAprPaymentMethods(availability, settings))
                .Intersect(GetPassedDeadlinePaymentMethods(availability, settings, date))
                .Intersect(GetContractKindPaymentMethods(contractKind))
                .ToList();


        private static List<PaymentMethods> GetAprPaymentMethods(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
            AccommodationBookingSettings settings)
        {
            if (!availability.RoomContractSet.IsAdvancePurchaseRate)
                return AllAvailablePaymentMethods;

            return settings.AprMode switch
            {
                AprMode.Hide => EmptyPaymentMethodsList,
                AprMode.DisplayOnly => EmptyPaymentMethodsList,
                AprMode.CardPurchasesOnly => new List<PaymentMethods> {PaymentMethods.CreditCard},
                AprMode.CardAndAccountPurchases => new List<PaymentMethods> {PaymentMethods.BankTransfer, PaymentMethods.CreditCard},
                _ => throw new ArgumentOutOfRangeException(nameof(settings.AprMode), $"Invalid value {settings.AprMode}")
            };
        }


        private static List<PaymentMethods> GetPassedDeadlinePaymentMethods(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
            AccommodationBookingSettings settings, DateTime date)
        {
            var deadlineDate = availability.RoomContractSet.Deadline.Date ?? availability.CheckInDate;
            if (date.AddDays(1) <= deadlineDate.Date)
                return AllAvailablePaymentMethods;

            return settings.PassedDeadlineOffersMode switch
            {
                PassedDeadlineOffersMode.Hide => EmptyPaymentMethodsList,
                PassedDeadlineOffersMode.DisplayOnly => EmptyPaymentMethodsList,
                PassedDeadlineOffersMode.CardPurchasesOnly => new List<PaymentMethods> {PaymentMethods.CreditCard},
                PassedDeadlineOffersMode.CardAndAccountPurchases => new List<PaymentMethods> {PaymentMethods.BankTransfer, PaymentMethods.CreditCard},
                _ => throw new ArgumentOutOfRangeException(nameof(settings.PassedDeadlineOffersMode), $"Invalid value {settings.PassedDeadlineOffersMode}")
            };
        }


        private static List<PaymentMethods> GetContractKindPaymentMethods(CounterpartyContractKind contractKind)
        {
            return contractKind switch
            {
                CounterpartyContractKind.CashPayments => new List<PaymentMethods> { PaymentMethods.Offline, PaymentMethods.CreditCard },
                CounterpartyContractKind.CreditCardPayments => new List<PaymentMethods> { PaymentMethods.Offline, PaymentMethods.CreditCard },
                CounterpartyContractKind.CreditPayments => new List<PaymentMethods> { PaymentMethods.BankTransfer, PaymentMethods.CreditCard },
                _ => throw new ArgumentOutOfRangeException(nameof(contractKind), $"Invalid value {contractKind}")
            };
        }


        private static readonly List<PaymentMethods> AllAvailablePaymentMethods = new()
        {
            PaymentMethods.BankTransfer,
            PaymentMethods.CreditCard,
            PaymentMethods.Offline
        };

        private static readonly List<PaymentMethods> EmptyPaymentMethodsList = new(0);
    }
}