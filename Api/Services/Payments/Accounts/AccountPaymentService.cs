using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Infrastructure.DatabaseExtensions;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public class AccountPaymentService : IAccountPaymentService
    {
        public AccountPaymentService(IAdministratorContext adminContext,
            IAccountPaymentProcessingService accountPaymentProcessingService,
            EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IServiceAccountContext serviceAccountContext,
            ICustomerContext customerContext,
            IPaymentNotificationService notificationService,
            IAccountManagementService accountManagementService,
            ILogger<AccountPaymentService> logger)
        {
            _adminContext = adminContext;
            _accountPaymentProcessingService = accountPaymentProcessingService;
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _serviceAccountContext = serviceAccountContext;
            _customerContext = customerContext;
            _accountManagementService = accountManagementService;
            _logger = logger;
            _notificationService = notificationService;
        }


        public async Task<bool> CanPayWithAccount(CustomerInfo customerInfo)
        {
            var companyId = customerInfo.CompanyId;
            return await _context.PaymentAccounts
                .Where(a => a.CompanyId == companyId)
                .AnyAsync(a => a.Balance + a.CreditLimit > 0);
        }


        public async Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency)
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return Result.Fail<AccountBalanceInfo>(error);

            var accountInfo = await _context.PaymentAccounts.FirstOrDefaultAsync(a => a.Currency == currency && a.CompanyId == customerInfo.CompanyId);
            return accountInfo == null
                ? Result.Fail<AccountBalanceInfo>($"Payments with accounts for currency {currency} is not available for current company")
                : Result.Ok(new AccountBalanceInfo(accountInfo.Balance, accountInfo.CreditLimit, accountInfo.Currency));
        }


        public async Task<Result<string>> CaptureMoney(Booking booking)
        {
            var (_, isUserFailure, user, userError) = await _serviceAccountContext.GetUserInfo();
            if (isUserFailure)
                return Result.Fail<string>(userError);

            if (booking.PaymentMethod != PaymentMethods.BankTransfer)
                return Result.Fail<string>($"Invalid payment method: {booking.PaymentMethod}");

            var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);
            var currency = bookingAvailability.Agreement.Price.Currency;
            
            return await Result.Ok(booking)
                .OnSuccessWithTransaction(_context, _ =>
                    CapturePayment()
                        .OnSuccess(ChangePaymentStatusToCaptured)
                )
                .OnBoth(CreateResult);


            async Task<Result> CapturePayment()
            {
                var (_, isAccountFailure, account, accountError) = await GetAccount();
                if (isAccountFailure)
                    return Result.Fail(accountError);

                var (_, isFailure, paymentEntity, error) = await GetPayment(booking.Id);
                if (isFailure)
                    return Result.Fail<string>(error);

                return await CaptureAccountPayment()
                    .OnSuccess(UpdatePaymentStatus);


                Task<Result<PaymentAccount>> GetAccount() => _accountManagementService.Get(booking.CompanyId, currency);


                async Task<Result> CaptureAccountPayment()
                {
                    // Hack. Error for updating same entity several times in different SaveChanges
                    _context.Detach(account);
                    var forVoid = bookingAvailability.Agreement.Price.NetTotal - paymentEntity.Amount;

                    var result = await _accountPaymentProcessingService.CaptureMoney(account.Id, new AuthorizedMoneyData(
                            currency: account.Currency,
                            amount: bookingAvailability.Agreement.Price.NetTotal,
                            referenceCode: booking.ReferenceCode,
                            reason: $"Capture money for the booking '{booking.ReferenceCode}' after check-in"),
                        user);

                    if (forVoid <= 0m || result.IsFailure)
                        return result;

                    _context.Detach(account);
                    return await _accountPaymentProcessingService.VoidMoney(account.Id, new AuthorizedMoneyData(
                            currency: account.Currency,
                            amount: forVoid,
                            referenceCode: booking.ReferenceCode,
                            reason: $"Void money for the booking '{booking.ReferenceCode}' after capture (booking was changed)"),
                        user);
                }


                async Task UpdatePaymentStatus()
                {
                    paymentEntity.Status = PaymentStatuses.Captured;
                    _context.Payments.Update(paymentEntity);
                    await _context.SaveChangesAsync();
                }
            }


            Task ChangePaymentStatusToCaptured() => ChangeBookingPaymentStatusToCaptured(booking);


            Result<string> CreateResult(Result result)
                => result.IsSuccess
                    ? Result.Ok($"Payment for the booking '{booking.ReferenceCode}' completed.")
                    : Result.Fail<string>($"Unable to complete payment for the booking '{booking.ReferenceCode}'. Reason: {result.Error}");
        }


        public Task<Result> ReplenishAccount(int accountId, PaymentData payment)
        {
            return Result.Ok()
                .Ensure(HasPermission, "Permission denied")
                .OnSuccess(AddMoney);


            Task<bool> HasPermission() => _adminContext.HasPermission(AdministratorPermissions.AccountReplenish);


            Task<Result> AddMoney()
            {
                return GetUserInfo()
                    .OnSuccess(AddMoneyWithUser);


                Task<Result> AddMoneyWithUser(UserInfo user)
                    => _accountPaymentProcessingService.AddMoney(accountId,
                        payment,
                        user);
            }
        }


        public Task<Result<PaymentResponse>> AuthorizeMoney(AccountPaymentRequest request, CustomerInfo customerInfo, string ipAddress)
        {
            return GetBooking()
                .OnSuccessWithTransaction(_context, booking =>
                    Authorize(booking)
                        .OnSuccess(_ => ChangePaymentStatusToAuthorized(booking)));


            async Task<Result<Booking>> GetBooking()
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.ReferenceCode == request.ReferenceCode);
                if (booking == null)
                    return Result.Fail<Booking>($"Could not find booking with reference code {request.ReferenceCode}");
                if (booking.CustomerId != customerInfo.CustomerId)
                    return Result.Fail<Booking>($"User does not have access to booking with reference code '{booking.ReferenceCode}'");

                return Result.Ok(booking);
            }


            async Task<Result<PaymentResponse>> Authorize(Booking booking)
            {
                var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

                var currency = bookingAvailability.Agreement.Price.Currency;

                var (_, isAmountFailure, amount, amountError) = await GetAmount();
                if (isAmountFailure)
                    return Result.Fail<PaymentResponse>(amountError);

                var (_, isUserFailure, user, userError) = await _customerContext.GetUserInfo();
                if (isUserFailure)
                    return Result.Fail<PaymentResponse>(userError);

                var (_, isAccountFailure, account, accountError) = await _accountManagementService.Get(customerInfo.CompanyId, currency);
                if (isAccountFailure)
                    return Result.Fail<PaymentResponse>(accountError);
               
                return await Result.Ok()
                    .Ensure(CanAuthorize, $"Could not authorize money for the booking '{booking.ReferenceCode}")
                    .OnSuccess(AuthorizeMoney)
                    .OnSuccess(StorePayment)
                    .OnSuccess(SendBillToCustomer)
                    .OnSuccess(CreateResult);


                Task<Result<decimal>> GetAmount() => GetPendingAmount(booking).Map(p => p.NetTotal);


                bool CanAuthorize()
                    => booking.PaymentMethod == PaymentMethods.BankTransfer &&
                        BookingStatusesForAuthorization.Contains(booking.Status);


                Task<Result> AuthorizeMoney()
                    => _accountPaymentProcessingService.AuthorizeMoney(account.Id, new AuthorizedMoneyData(
                            currency: account.Currency,
                            amount: amount,
                            reason: $"Authorize money after booking '{booking.ReferenceCode}'",
                            referenceCode: booking.ReferenceCode),
                        user);


                async Task StorePayment()
                {
                    var now = _dateTimeProvider.UtcNow();
                    var (_, isFailure, payment, _) = await GetPayment(booking.Id);
                    if (isFailure)
                    {
                        // New payment
                        var info = new AccountPaymentInfo(ipAddress);
                        payment = new Payment
                        {
                            Amount = amount,
                            BookingId = booking.Id,
                            AccountNumber = account.Id.ToString(),
                            Currency = currency.ToString(),
                            Created = now,
                            Modified = now,
                            Status = PaymentStatuses.Authorized,
                            Data = JsonConvert.SerializeObject(info),
                            AccountId = account.Id,
                            PaymentMethod = PaymentMethods.BankTransfer
                        };
                        _context.Payments.Add(payment);
                    }
                    else
                    {
                        // Partial payment
                        payment.Amount += amount;
                        payment.Modified = now;
                        _context.Payments.Update(payment);
                    }

                    await _context.SaveChangesAsync();
                }


                async Task SendBillToCustomer()
                {
                    var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == booking.CustomerId);
                    if (customer == default)
                    {
                        _logger.LogWarning("Send bill after payment from account: could not find customer with id '{0}' for the booking '{1}'", booking.CustomerId,
                            booking.ReferenceCode);
                        return;
                    }

                    await _notificationService.SendBillToCustomer(new PaymentBill(customer.Email,
                        amount,
                        currency,
                        _dateTimeProvider.UtcNow(),
                        PaymentMethods.BankTransfer,
                        booking.ReferenceCode,
                        $"{customer.LastName} {customer.FirstName}"));
                }


                PaymentResponse CreateResult() => new PaymentResponse(string.Empty, CreditCardPaymentStatuses.Success, string.Empty);
            }


            async Task ChangePaymentStatusToAuthorized(Booking booking)
            {
                if (booking.PaymentStatus == BookingPaymentStatuses.Authorized)
                    return;

                booking.PaymentStatus = BookingPaymentStatuses.Authorized;
                _context.Update(booking);
                await _context.SaveChangesAsync();
            }
        }


        public async Task<Result> VoidMoney(Booking booking)
        {
            // TODO: Implement refund money if status is paid with deadline penalty
            if (booking.PaymentStatus != BookingPaymentStatuses.Authorized && booking.PaymentStatus != BookingPaymentStatuses.PartiallyAuthorized)
                return Result.Ok();

            var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

            var currency = bookingAvailability.Agreement.Price.Currency;

            if (booking.PaymentMethod != PaymentMethods.BankTransfer)
                return Result.Fail($"Could not void money for the booking with a payment method  '{booking.PaymentMethod}'");

            return await GetCustomer()
                .OnSuccess(GetAccount)
                .OnSuccess(VoidMoneyFromAccount);

            async Task<Result<CustomerInfo>> GetCustomer() => await _customerContext.GetCustomerInfo();

            Task<Result<PaymentAccount>> GetAccount(CustomerInfo customerInfo) => _accountManagementService.Get(customerInfo.CompanyId, currency);


            async Task<Result> VoidMoneyFromAccount(PaymentAccount account)
            {
                var (_, isUserFailure, userInfo, userError) = await GetUserInfo();
                if (isUserFailure)
                    return Result.Fail(userError);

                var (_, isFailure, paymentEntity, error) = await GetPayment(booking.Id);
                if (isFailure)
                    return Result.Fail(error);

                return await Void()
                    .OnSuccess(UpdatePaymentStatus);


                Task<Result> Void() => _accountPaymentProcessingService.VoidMoney(account.Id, new AuthorizedMoneyData(paymentEntity.Amount, currency,
                    reason: $"Void money after booking cancellation '{booking.ReferenceCode}'", referenceCode: booking.ReferenceCode), userInfo);


                async Task UpdatePaymentStatus()
                {
                    paymentEntity.Status = PaymentStatuses.Voided;
                    _context.Payments.Update(paymentEntity);
                    await _context.SaveChangesAsync();
                }
            }
        }


        public async Task<Result<Price>> GetPendingAmount(Booking booking)
        {
            var availabilityInfo = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

            var currency = availabilityInfo.Agreement.Price.Currency;
            if (booking.PaymentMethod != PaymentMethods.BankTransfer)
                return Result.Fail<Price>($"Unsupported payment method for pending payment: {booking.PaymentMethod}");
            var total = availabilityInfo.Agreement.Price.NetTotal;

            var payment = await _context.Payments.Where(p => p.BookingId == booking.Id).FirstOrDefaultAsync();
            var paid = payment?.Amount ?? 0m;

            var forPay = total - paid;
            return forPay <= 0m
                ? Result.Fail<Price>("Nothing to pay")
                : Result.Ok(new Price(currency, forPay, forPay, PriceTypes.Supplement));
        }


        private Task ChangeBookingPaymentStatusToCaptured(Booking booking)
        {
            booking.PaymentStatus = BookingPaymentStatuses.Captured;
            _context.Bookings.Update(booking);
            return _context.SaveChangesAsync();
        }


        private Task<Result<UserInfo>> GetUserInfo()
            => _customerContext.GetUserInfo()
                .OnFailureCompensate(_serviceAccountContext.GetUserInfo)
                .OnFailureCompensate(_adminContext.GetUserInfo);


        private async Task<Result<Payment>> GetPayment(int bookingId)
        {
            var paymentEntity = await _context.Payments.Where(p => p.BookingId == bookingId).FirstOrDefaultAsync();
            if (paymentEntity == default)
                return Result.Fail<Payment>(
                    $"Could not find a payment record with the booking ID {bookingId}");

            // Payment can be completed before. Nothing to do now.
            if (paymentEntity.Status != PaymentStatuses.Authorized)
                return Result.Fail<Payment>($"Invalid status for the payment entity with id '{paymentEntity.Id}': {paymentEntity.Status}");

            return Result.Ok(paymentEntity);
        }


        private static readonly HashSet<BookingStatusCodes> BookingStatusesForAuthorization = new HashSet<BookingStatusCodes>
        {
            BookingStatusCodes.Pending, BookingStatusCodes.Confirmed, BookingStatusCodes.WaitingForResponse
        };

        private readonly IAccountManagementService _accountManagementService;
        private readonly IAdministratorContext _adminContext;
        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AccountPaymentService> _logger;
        private readonly IPaymentNotificationService _notificationService;
        private readonly IAccountPaymentProcessingService _accountPaymentProcessingService;
        private readonly IServiceAccountContext _serviceAccountContext;
    }
}