using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PaymentService : IPaymentService
    {
        public PaymentService(IAdministratorContext adminContext,
            IPaymentProcessingService paymentProcessingService,
            EdoContext context,
            IPayfortService payfortService,
            IDateTimeProvider dateTimeProvider,
            IServiceAccountContext serviceAccountContext)
        {
            _adminContext = adminContext;
            _paymentProcessingService = paymentProcessingService;
            _context = context;
            _payfortService = payfortService;
            _dateTimeProvider = dateTimeProvider;
            _serviceAccountContext = serviceAccountContext;
        }

        public IReadOnlyCollection<Currencies> GetCurrencies() => new ReadOnlyCollection<Currencies>(Currencies);
        public IReadOnlyCollection<PaymentMethods> GetAvailableCustomerPaymentMethods() => new ReadOnlyCollection<PaymentMethods>(PaymentMethods);

        public Task<Result<PaymentResponse>> Pay(PaymentRequest request, string languageCode, string ipAddress, CustomerInfo customerInfo)
        {
            return Validate(request)
                .OnSuccess(CreateRequest)
                .OnSuccess(Pay)
                .OnSuccess(CheckStatus)
                .OnSuccessWithTransaction(_context, payment => Result.Ok(payment)
                    .OnSuccess(StorePayment)
                    .OnSuccess(MarkBookingAsPaid)
                    .OnSuccess(MarkCreditCardAsUsed)
                    .OnSuccess(CreateResponse));


            async Task<CreditCardPaymentRequest> CreateRequest()
            {
                var isNewCard = true;
                if (request.IsStoredToken)
                {
                    var card = await _context.CreditCards.FirstAsync(c => c.Token == request.Token);
                    isNewCard = card.Used != true;
                }

                return new CreditCardPaymentRequest(amount: request.Amount,
                    currency: request.Currency,
                    token: request.Token,
                    customerName: $"{customerInfo.FirstName} {customerInfo.LastName}",
                    customerEmail: customerInfo.Email,
                    customerIp: ipAddress,
                    referenceCode: request.ReferenceCode,
                    languageCode: languageCode,
                    isStoredToken: request.IsStoredToken,
                    securityCode: request.SecurityCode,
                    isNewCard: isNewCard);
            }

            Task<Result<CreditCardPaymentResult>> Pay(CreditCardPaymentRequest paymentRequest)
            {
                return _payfortService.Pay(paymentRequest);
            }

            Result<CreditCardPaymentResult> CheckStatus(CreditCardPaymentResult payment)
                => payment.Status == PaymentStatuses.Failed ?
                    Result.Fail<CreditCardPaymentResult>($"Payment error: {payment.Message}") :
                    Result.Ok(payment);

            async Task<Result<CreditCardPaymentResult>> StorePayment(CreditCardPaymentResult payment)
            {
                var booking = await _context.Bookings.FirstAsync(b => b.ReferenceCode == request.ReferenceCode);
                var card = request.IsStoredToken
                    ? await _context.CreditCards.FirstOrDefaultAsync(c => c.Token == request.Token)
                    : null;
                var now = _dateTimeProvider.UtcNow();
                var info = new CreditCardPaymentInfo(ipAddress, payment.ExternalCode, payment.Message, payment.AuthorizationCode, payment.ExpirationDate);
                _context.ExternalPayments.Add(new ExternalPayment()
                {
                    Amount = request.Amount,
                    BookingId = booking.Id,
                    AccountNumber = payment.CardNumber,
                    Currency = request.Currency.ToString(),
                    Created = now,
                    Modified = now,
                    Status = payment.Status,
                    Data = JsonConvert.SerializeObject(info),
                    CreditCardId = card?.Id
                });

                await _context.SaveChangesAsync();
                return Result.Ok(payment);
            }
        }

        public Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject response)
        {
            return Task.FromResult(_payfortService.ProcessPaymentResponse(response))
                .OnSuccessWithTransaction(_context, payment => Result.Ok(payment)
                    .OnSuccess(StorePayment)
                    .OnSuccess(MarkBookingAsPaid)
                    .OnSuccess(MarkCreditCardAsUsed)
                    .OnSuccess(CreateResponse));


            async Task<Result<CreditCardPaymentResult>> StorePayment(CreditCardPaymentResult payment)
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.ReferenceCode == payment.ReferenceCode);
                if (booking == null)
                    return Result.Fail<CreditCardPaymentResult>($"Cannot find a booking by the reference code {payment.ReferenceCode}");

                var paymentEntity = await _context.ExternalPayments.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
                if (paymentEntity == null)
                    return Result.Fail<CreditCardPaymentResult>($"Cannot find a payment record with the booking ID {booking.Id}");

                var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(paymentEntity.Data);
                var newInfo = new CreditCardPaymentInfo(info.CustomerIp, payment.ExternalCode, payment.Message, payment.AuthorizationCode,
                    payment.ExpirationDate);
                paymentEntity.Status = payment.Status;
                paymentEntity.Data = JsonConvert.SerializeObject(newInfo);
                paymentEntity.Modified = _dateTimeProvider.UtcNow();
                _context.Update(paymentEntity);
                await _context.SaveChangesAsync();

                if (payment.Status == PaymentStatuses.Failed)
                    Result.Fail<CreditCardPaymentResult>($"Payment error: {payment.Message}");

                return Result.Ok(payment);
            }
        }

        private PaymentResponse CreateResponse(CreditCardPaymentResult payment) =>
            new PaymentResponse(payment.Secure3d, payment.Status);

        private async Task<Result<CreditCardPaymentResult>> MarkBookingAsPaid(CreditCardPaymentResult payment)
        {
            if (payment.Status != PaymentStatuses.Success)
                return Result.Ok(payment);

            var booking = await _context.Bookings.FirstAsync(b => b.ReferenceCode == payment.ReferenceCode);
            booking.Status = BookingStatusCodes.PaymentComplete;
            _context.Update(booking);
            await _context.SaveChangesAsync();
            return Result.Ok(payment);
        }

        private async Task<Result<CreditCardPaymentResult>> MarkCreditCardAsUsed(CreditCardPaymentResult payment)
        {
            if (payment.Status != PaymentStatuses.Success)
                return Result.Ok(payment);

            var query = from booking in _context.Bookings
                join payments in _context.ExternalPayments on booking.Id equals payments.BookingId
                join cards in _context.CreditCards on payments.CreditCardId equals cards.Id
                where booking.ReferenceCode == payment.ReferenceCode
                select cards;

            var card = await query.FirstOrDefaultAsync();
            if (card?.Used != false)
                return Result.Ok(payment);

            card.Used = true;
            _context.Update(card);
            await _context.SaveChangesAsync();
            return Result.Ok(payment);
        }


        public async Task<bool> CanPayWithAccount(CustomerInfo customerInfo)
        {
            var companyId = customerInfo.CompanyId;
            return await _context.PaymentAccounts
                .Where(a => a.CompanyId == companyId)
                .AnyAsync(a => a.Balance + a.CreditLimit > 0);
        }


        private async Task<Result> Validate(PaymentRequest request)
        {
            var fieldValidateResult = GenericValidator<PaymentRequest>.Validate(v =>
            {
                v.RuleFor(c => c.Amount).NotEmpty();
                v.RuleFor(c => c.Currency).NotEmpty().IsInEnum().Must(c => c != Common.Enums.Currencies.NotSpecified);
                v.RuleFor(c => c.ReferenceCode).NotEmpty();
                v.RuleFor(c => c.Token).NotEmpty();
            }, request);

            if (fieldValidateResult.IsFailure)
                return fieldValidateResult;

            return Result.Combine(await CheckReferenceCode(request.ReferenceCode),
                await CheckToken());

            async Task<Result> CheckReferenceCode(string referenceCode)
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.ReferenceCode == referenceCode);
                if (booking == null)
                    return Result.Fail("Invalid Reference code");
            
                return InvalidBookingStatuses.Contains(booking.Status)
                    ? Result.Fail($"Invalid booking status: {booking.Status.ToString()}")
                    : Result.Ok();
            }


            async Task<Result> CheckToken()
            {
                if (!request.IsStoredToken)
                    return Result.Ok();

                var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Token == request.Token);
                return card == null
                    ? Result.Fail("Cannot find a credit card by payment token")
                    : Result.Ok();
            }
        }

        public Task<Result> ReplenishAccount(int accountId, PaymentData payment)
        {
            return Result.Ok()
                .Ensure(HasPermission, "Permission denied")
                .OnSuccess(AddMoney);

            Task<bool> HasPermission()
            {
                // TODO: Need refactor? Only admin has permissions?
                return _adminContext.HasPermission(AdministratorPermissions.AccountReplenish);
            }

            Task<Result> AddMoney()
            {
                return GetUserInfo()
                    .OnSuccess(AddMoneyWithUser);

                Task<Result<UserInfo>> GetUserInfo() =>
                    _adminContext.GetUserInfo()
                        .OnFailureCompensate(_serviceAccountContext.GetUserInfo);

                Task<Result> AddMoneyWithUser(UserInfo user)
                {
                    return _paymentProcessingService.AddMoney(accountId,
                        payment,
                        user);
                }
            }
        }

        private static readonly Currencies[] Currencies = Enum.GetValues(typeof(Currencies))
            .Cast<Currencies>()
            .ToArray();

        private static readonly PaymentMethods[] PaymentMethods = Enum.GetValues(typeof(PaymentMethods))
            .Cast<PaymentMethods>()
            .ToArray();

        private static readonly HashSet<BookingStatusCodes> InvalidBookingStatuses = new HashSet<BookingStatusCodes>
            {BookingStatusCodes.Cancelled, BookingStatusCodes.Invalid, BookingStatusCodes.Rejected, BookingStatusCodes.PaymentComplete};

        private readonly IAdministratorContext _adminContext;
        private readonly IPaymentProcessingService _paymentProcessingService;
        private readonly EdoContext _context;
        private readonly IPayfortService _payfortService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IServiceAccountContext _serviceAccountContext;
    }
}
