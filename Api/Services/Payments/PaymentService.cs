using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
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
            IDateTimeProvider dateTimeProvider)
        {
            _adminContext = adminContext;
            _paymentProcessingService = paymentProcessingService;
            _context = context;
            _payfortService = payfortService;
            _dateTimeProvider = dateTimeProvider;
        }

        public IReadOnlyCollection<Currencies> GetCurrencies() => new ReadOnlyCollection<Currencies>(Currencies);
        public IReadOnlyCollection<PaymentMethods> GetAvailableCustomerPaymentMethods() => new ReadOnlyCollection<PaymentMethods>(PaymentMethods);

        public Task<Result<PaymentResponse>> Pay(PaymentRequest request, string languageCode, string ipAddress, CustomerInfo customerInfo)
        {
            return Validate(request)
                .OnSuccess(Pay)
                .OnSuccess(CheckStatus)
                .OnSuccess(StorePayment);

            Task<Result<CreditCardPaymentResult>> Pay()
            {
                return _payfortService.Pay(new CreditCardPaymentRequest(amount: request.Amount,
                    currency: request.Currency,
                    token: request.Token, 
                    customerName: $"{customerInfo.FirstName} {customerInfo.LastName}",
                    customerEmail: customerInfo.Email,
                    customerIp: ipAddress,
                    referenceCode: request.ReferenceCode,
                    languageCode: languageCode));
            }

            Result<CreditCardPaymentResult> CheckStatus(CreditCardPaymentResult payment)
                => payment.Status == PaymentStatuses.Failed ?
                    Result.Fail<CreditCardPaymentResult>($"Payment error: {payment.Message}") :
                    Result.Ok(payment);

            async Task<Result<PaymentResponse>> StorePayment(CreditCardPaymentResult payment)
            {
                var booking = await _context.Bookings.FirstAsync(b => b.ReferenceCode == request.ReferenceCode);
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
                    Data = JsonConvert.SerializeObject(info)
                });

                await _context.SaveChangesAsync();
                return Result.Ok(new PaymentResponse(payment.Secure3d, payment.Status));
            }
        }

        public Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject response)
        {
            return _payfortService.ProcessPaymentResponse(response)
                .OnSuccess(StorePayment);

            async Task<Result<PaymentResponse>> StorePayment(CreditCardPaymentResult payment)
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.ReferenceCode == payment.ReferenceCode);
                if (booking == null)
                    return Result.Fail<PaymentResponse>($"Cannot find booking by reference code {payment.ReferenceCode}");

                var paymentEntity = await _context.ExternalPayments.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
                if (paymentEntity == null)
                    return Result.Fail<PaymentResponse>($"Cannot find payment by booking id {booking.Id}");

                var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(paymentEntity.Data);
                var newInfo = new CreditCardPaymentInfo(info.CustomerIp, payment.ExternalCode, payment.Message, payment.AuthorizationCode,
                    payment.ExpirationDate);
                paymentEntity.Status = payment.Status;
                paymentEntity.Data = JsonConvert.SerializeObject(newInfo);
                paymentEntity.Modified = _dateTimeProvider.UtcNow();
                _context.Update(paymentEntity);
                await _context.SaveChangesAsync();

                if (payment.Status == PaymentStatuses.Failed)
                    Result.Fail<PaymentResponse>($"Payment error: {payment.Message}");

                return Result.Ok(new PaymentResponse(payment.Secure3d, payment.Status));
            }
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

            return Result.Combine(await CheckReferenceCode(request.ReferenceCode));
        }

        private async Task<Result> CheckReferenceCode(string referenceCode)
        {
            var booking = await _context.Bookings.Where(b => b.ReferenceCode == referenceCode).FirstOrDefaultAsync();
            if (booking == null)
                return Result.Fail("Invalid Reference code");
            
            if (InvalidBookingStatuses.Contains(booking.Status))
                return Result.Fail($"Invalid booking status: {booking.Status.ToString()}");
            
            return Result.Ok();
        }

        public Task<Result> ReplenishAccount(int accountId, PaymentData payment)
        {
            return Result.Ok()
                .Ensure(HasPermission, "Permission denied")
                .OnSuccess(AddMoney);

            Task<bool> HasPermission()
            {
                return _adminContext.HasPermission(AdministratorPermissions.AccountReplenish);
            }

            async Task<Result> AddMoney()
            {
                var userInfo = await _adminContext.GetUserInfo();
                return await _paymentProcessingService.AddMoney(accountId,
                    payment, 
                    userInfo);
            }
        }

        private static readonly Currencies[] Currencies = Enum.GetValues(typeof(Currencies))
            .Cast<Currencies>()
            .ToArray();

        private static readonly PaymentMethods[] PaymentMethods = Enum.GetValues(typeof(PaymentMethods))
            .Cast<PaymentMethods>()
            .ToArray();

        private static readonly BookingStatusCodes[] InvalidBookingStatuses = new[]
            {BookingStatusCodes.Cancelled, BookingStatusCodes.Invalid, BookingStatusCodes.Rejected};

        private readonly IAdministratorContext _adminContext;
        private readonly IPaymentProcessingService _paymentProcessingService;
        private readonly EdoContext _context;
        private readonly IPayfortService _payfortService;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}
