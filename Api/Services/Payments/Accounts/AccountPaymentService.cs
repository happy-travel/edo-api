using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public class AccountPaymentService : IAccountPaymentService
    {
        public AccountPaymentService(IAccountPaymentProcessingService accountPaymentProcessingService,
            EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IAccountManagementService accountManagementService,
            IEntityLocker locker,
            IBookingRecordsManager bookingRecordsManager)
        {
            _accountPaymentProcessingService = accountPaymentProcessingService;
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _accountManagementService = accountManagementService;
            _locker = locker;
            _bookingRecordsManager = bookingRecordsManager;
        }


        public async Task<bool> CanPayWithAccount(AgentContext agentInfo)
        {
            var agencyId = agentInfo.AgencyId;
            return await _context.AgencyAccounts
                .Where(a => a.AgencyId == agencyId && a.IsActive)
                .AnyAsync(a => a.Balance > 0);
        }


        public Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency, AgentContext agent) => GetAccountBalance(currency, agent.AgencyId);


        public async Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency, int agencyId)
        {
            var accountInfo = await _context.AgencyAccounts
                .SingleOrDefaultAsync(a => a.Currency == currency && a.AgencyId == agencyId && a.IsActive);

            return accountInfo == null
                ? Result.Failure<AccountBalanceInfo>($"Payments with accounts for currency {currency} is not available for current counterparty")
                : Result.Success(new AccountBalanceInfo(accountInfo.Balance, accountInfo.Currency));
        }


        public async Task<Result> Refund(Booking booking, UserInfo user)
        {
            if (booking.PaymentStatus != BookingPaymentStatuses.Captured)
                return Result.Success();

            if (booking.PaymentMethod != PaymentMethods.BankTransfer)
                return Result.Failure($"Could not refund money for the booking with a payment method '{booking.PaymentMethod}'");

            return await GetAccount()
                .Bind(RefundMoneyToAccount);


            Task<Result<AgencyAccount>> GetAccount() => _accountManagementService.Get(booking.AgencyId, booking.Currency);


            async Task<Result> RefundMoneyToAccount(AgencyAccount account)
            {
                var (_, isFailure, paymentEntity, error) = await GetPayment(booking.Id);
                if (isFailure)
                    return Result.Failure(error);

                var refundableAmount = booking.GetRefundableAmount(_dateTimeProvider.UtcNow());

                return await Refund()
                    .Tap(UpdatePaymentStatus);


                Task<Result> Refund() =>
                    _accountPaymentProcessingService.RefundMoney(
                        account.Id,
                        new ChargedMoneyData(
                            refundableAmount,
                            booking.Currency,
                            reason: $"Refund money after booking cancellation '{booking.ReferenceCode}'",
                            referenceCode: booking.ReferenceCode),
                        user);


                async Task UpdatePaymentStatus()
                {
                    paymentEntity.Status = PaymentStatuses.Refunded;
                    paymentEntity.RefundedAmount = refundableAmount;
                    _context.Payments.Update(paymentEntity);
                    await _context.SaveChangesAsync();
                }
            }
        }


        public Task<Result<PaymentResponse>> Charge(string referenceCode, AgentContext agentContext, string clientIp) =>
            _bookingRecordsManager.Get(referenceCode)
                .Bind(b => Charge(b, agentContext, clientIp));


        public Task<Result<PaymentResponse>> Charge(Booking booking, AgentContext agentContext, string clientIp) =>
            Charge(booking, agentContext.ToUserInfo(), agentContext.AgencyId, clientIp);


        public Task<Result<PaymentResponse>> Charge(Booking booking, UserInfo user, int agencyId, string clientIp)
        {
            return Result.Success()
                .BindWithTransaction(_context, () => 
                    Charge()
                    .Tap(_ => ChangePaymentStatusToCaptured()));


            async Task<Result<PaymentResponse>> Charge()
            {
                var (_, isAmountFailure, amount, amountError) = await GetAmount();
                if (isAmountFailure)
                    return Result.Failure<PaymentResponse>(amountError);

                var (_, isAccountFailure, account, accountError) = await _accountManagementService.Get(agencyId, booking.Currency);
                if (isAccountFailure)
                    return Result.Failure<PaymentResponse>(accountError);

                return await Result.Success()
                    .BindWithLock(_locker, typeof(Booking), booking.Id.ToString(), () => Result.Success()
                        .Ensure(IsNotPayed, $"The booking '{booking.ReferenceCode}' is already paid")
                        .Ensure(CanCharge, $"Could not charge money for the booking '{booking.ReferenceCode}'")
                        .Bind(ChargeMoney)
                        .Bind(StorePayment)
                        .Map(CreateResult));

                Task<Result<decimal>> GetAmount() => GetPendingAmount(booking).Map(p => p.Amount);

                bool IsNotPayed() => booking.PaymentStatus != BookingPaymentStatuses.Captured;

                bool CanCharge() =>
                    booking.PaymentMethod == PaymentMethods.BankTransfer &&
                    ChargeableStatuses.Contains(booking.Status);

                Task<Result> ChargeMoney()
                    => _accountPaymentProcessingService.ChargeMoney(account.Id, new ChargedMoneyData(
                            currency: account.Currency,
                            amount: amount,
                            reason: $"Charge money after booking '{booking.ReferenceCode}'",
                            referenceCode: booking.ReferenceCode),
                        user);

                async Task<Result> StorePayment()
                {
                    var (paymentExistsForBooking, _, _, _) = await GetPayment(booking.Id);
                    if (paymentExistsForBooking)
                        return Result.Failure("Payment for current booking already exists");
                    
                    var now = _dateTimeProvider.UtcNow();
                    var info = new AccountPaymentInfo(clientIp);
                    var payment = new Payment
                    {
                        Amount = amount,
                        BookingId = booking.Id,
                        AccountNumber = account.Id.ToString(),
                        Currency = booking.Currency.ToString(),
                        Created = now,
                        Modified = now,
                        Status = PaymentStatuses.Captured,
                        Data = JsonConvert.SerializeObject(info),
                        AccountId = account.Id,
                        PaymentMethod = PaymentMethods.BankTransfer
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    return Result.Success();
                }

                PaymentResponse CreateResult() => new PaymentResponse(string.Empty, CreditCardPaymentStatuses.Success, string.Empty);
            }


            async Task ChangePaymentStatusToCaptured()
            {
                if (booking.PaymentStatus == BookingPaymentStatuses.Captured)
                    return;

                booking.PaymentStatus = BookingPaymentStatuses.Captured;
                _context.Update(booking);
                await _context.SaveChangesAsync();
            }
        }


        private async Task<Result<MoneyAmount>> GetPendingAmount(Booking booking)
        {
            if (booking.PaymentMethod != PaymentMethods.BankTransfer)
                return Result.Failure<MoneyAmount>($"Unsupported payment method for pending payment: {booking.PaymentMethod}");

            var payment = await _context.Payments.Where(p => p.BookingId == booking.Id).FirstOrDefaultAsync();
            var paid = payment?.Amount ?? 0m;

            var forPay = booking.TotalPrice - paid;
            return forPay <= 0m
                ? Result.Failure<MoneyAmount>("Nothing to pay")
                : Result.Success(new MoneyAmount(forPay, booking.Currency));
        }


        public async Task<Result> TransferToChildAgency(int payerAccountId, int recipientAccountId, MoneyAmount amount, AgentContext agent)
        {
            return await _accountPaymentProcessingService.TransferToChildAgency(payerAccountId, recipientAccountId, amount, agent);
        }


        private async Task<Result<Payment>> GetPayment(int bookingId)
        {
            var paymentEntity = await _context.Payments.Where(p => p.BookingId == bookingId).FirstOrDefaultAsync();
            if (paymentEntity == default)
                return Result.Failure<Payment>(
                    $"Could not find a payment record with the booking ID {bookingId}");

            return Result.Success(paymentEntity);
        }


        private static readonly HashSet<BookingStatuses> ChargeableStatuses = new HashSet<BookingStatuses>
        {
            BookingStatuses.InternalProcessing,
            BookingStatuses.Confirmed,
        };

        private readonly IAccountManagementService _accountManagementService;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAccountPaymentProcessingService _accountPaymentProcessingService;
        private readonly IEntityLocker _locker;
        private readonly IBookingRecordsManager _bookingRecordsManager;
    }
}