using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingPaymentMethodsHelperTests
    {
        [Fact]
        public void Hidden_apr_should_return_no_payment_methods()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.Hide);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,11));
            
            Assert.Equal(new List<PaymentTypes>(), availablePaymentTypes);
        }

        
        [Fact]
        public void Display_only_apr_should_return_no_payment_methods()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.DisplayOnly);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,11));
            
            Assert.Equal(new List<PaymentTypes>(), availablePaymentTypes);
        }
        
        
        [Fact]
        public void Card_only_apr_should_return_card_payment_method_when_deadline_is_not_reached()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardPurchasesOnly);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,15));
            
            Assert.Equal(new List<PaymentTypes> {PaymentTypes.CreditCard}, availablePaymentTypes);
        }
        
        
        [Fact]
        public void Card_only_apr_should_return_no_payment_methods_when_deadline_is_reached()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardPurchasesOnly);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,23));
            
            Assert.Equal(new List<PaymentTypes>(), availablePaymentTypes);
        }
        
        
        [Fact]
        public void Card_and_account_apr_should_return_no_payment_methods_when_deadline_is_reached()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                    CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,23));
            
            Assert.Equal(new List<PaymentTypes>(), availablePaymentTypes);
        }
        
        
        [Fact]
        public void Not_apr_should_return_all_payment_methods_when_deadline_is_not_reached()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,15));
            
            Assert.Equal(new List<PaymentTypes> {PaymentTypes.VirtualAccount, PaymentTypes.CreditCard}, availablePaymentTypes);
        }
        
        
        [Fact]
        public void Not_apr_should_return_no_payment_methods_when_deadline_is_not_reached_and_hidden()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.Hide, deadlineOffersMode: PassedDeadlineOffersMode.Hide);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,22));
            
            Assert.Equal(new List<PaymentTypes>(), availablePaymentTypes);
        }
        
        
        [Fact]
        public void Not_apr_should_return_credit_card_when_deadline_is_reached_and_allowed_credit_card()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.Hide, deadlineOffersMode: PassedDeadlineOffersMode.CardPurchasesOnly);
            
            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11 ,22));
            
            Assert.Equal(new List<PaymentTypes> {PaymentTypes.CreditCard}, availablePaymentTypes);
        }


        [Fact]
        public void Allowed_apr_should_return_all_payment_methods_when_deadline_is_reached_and_allowed()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases, deadlineOffersMode: PassedDeadlineOffersMode.CardAndAccountPurchases);

            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11, 22));

            Assert.Equal(new List<PaymentTypes> { PaymentTypes.VirtualAccount, PaymentTypes.CreditCard }, availablePaymentTypes);
        }


        [Fact]
        public void Contract_kind_card_payments_should_match()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases, deadlineOffersMode: PassedDeadlineOffersMode.CardAndAccountPurchases);

            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditCardPayments, new DateTime(2020, 11, 20));

            Assert.Equal(new List<PaymentTypes> { PaymentTypes.CreditCard, PaymentTypes.Offline }, availablePaymentTypes);
        }


        [Fact]
        public void Contract_kind_cash_payments_should_match()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases, deadlineOffersMode: PassedDeadlineOffersMode.CardAndAccountPurchases);

            var availablePaymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CashPayments, new DateTime(2020, 11, 20));

            Assert.Equal(new List<PaymentTypes> { PaymentTypes.CreditCard, PaymentTypes.Offline }, availablePaymentTypes);
        }


        [Fact]
        public void Contract_kind_credit_payments_should_match()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases, deadlineOffersMode: PassedDeadlineOffersMode.CardAndAccountPurchases);

            var paymentTypes = BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settingsWithHiddenApr,
                CounterpartyContractKind.CreditPayments, new DateTime(2020, 11, 20));

            Assert.Equal(new List<PaymentTypes> { PaymentTypes.VirtualAccount, PaymentTypes.CreditCard }, paymentTypes);
        }


        private static AccommodationBookingSettings CreateSettings(AprMode aprMode = default, PassedDeadlineOffersMode deadlineOffersMode = default)
            => new(default, aprMode, deadlineOffersMode, isMarkupDisabled: default, 
                isSupplierVisible: default, default, isDirectContractFlagVisible: default, default);


        private static RoomContractSetAvailability CreateAvailability(bool isApr = false, DateTime? checkInDate = null, DateTime? deadlineDate = null)
        {
            var deadline = new Deadline(deadlineDate);
            var roomContractSetWithApr = new RoomContractSet(default, default, deadline, default,
                default, default, isAdvancePurchaseRate: isApr);

            return new RoomContractSetAvailability(default, checkInDate ?? default,
                default, default, default, roomContractSetWithApr);
        }
    }
}