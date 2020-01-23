using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public class CreditCardPaymentService : ICreditCardPaymentService
    {
        public CreditCardPaymentService(EdoContext context,
            IPayfortService payfortService,
            IDateTimeProvider dateTimeProvider,
            ICreditCardService creditCardService,
            IPaymentNotificationService notificationService,
            IEntityLocker locker,
            ILogger<CreditCardPaymentService> logger,
            ICreditCardAuditService creditCardAuditService)
        {
            _context = context;
            _payfortService = payfortService;
            _dateTimeProvider = dateTimeProvider;
            _creditCardService = creditCardService;
            _locker = locker;
            _logger = logger;
            _creditCardAuditService = creditCardAuditService;
            _notificationService = notificationService;
        }


        public async Task<Result<PaymentResponse>> AuthorizeMoney(PaymentRequest request, string languageCode, string ipAddress,
            CustomerInfo customerInfo)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.ReferenceCode == request.ReferenceCode);
            var (_, isValidationFailure, validationError) = await Validate(request, customerInfo, booking);
            if (isValidationFailure)
                return Result.Fail<PaymentResponse>(validationError);

            var availabilityInfo = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

            var currency = availabilityInfo.Agreement.Price.Currency;
 
            var (_, isAmountFailure, amount, amountError) = await GetAmount();
            if (isAmountFailure)
                return Result.Fail<PaymentResponse>(amountError);

            return await Result.Ok()
                .OnSuccess(CreateRequest)
                .OnSuccess(Authorize)
                .OnSuccessIf(IsPaymentComplete, SendBillToCustomer)
                .OnSuccessWithTransaction(_context, payment => Result.Ok(payment.result)
                    .OnSuccess(StorePayment)
                    .OnSuccessIf(IsPaymentCompleteForResult, WriteAuditLog)
                    .OnSuccessIf(IsPaymentCompleteForResult, ChangePaymentStatusForBookingToAuthorized)
                    .OnSuccessIf(IsPaymentCompleteForResult, MarkCreditCardAsUsed)
                    .OnSuccess(CreateResponse));


            Task<Result<decimal>> GetAmount() => GetPendingAmount(booking).Map(p => p.NetTotal);


            async Task<CreditCardPaymentRequest> CreateRequest()
            {
                var isNewCard = true;
                if (request.Token.Type == PaymentTokenTypes.Stored)
                {
                    var token = request.Token.Code;
                    var card = await _context.CreditCards.FirstAsync(c => c.Token == token);
                    isNewCard = card.IsUsedForPayments != true;
                }

                return new CreditCardPaymentRequest(currency: currency,
                    amount: amount,
                    token: request.Token,
                    customerName: $"{customerInfo.FirstName} {customerInfo.LastName}",
                    customerEmail: customerInfo.Email,
                    customerIp: ipAddress,
                    referenceCode: request.ReferenceCode,
                    languageCode: languageCode,
                    securityCode: request.SecurityCode,
                    isNewCard: isNewCard,
                    merchantReference: await GetMerchantReference());


                async Task<string> GetMerchantReference()
                {
                    var count = await _context.Payments.Where(p => p.BookingId == booking.Id).CountAsync();
                    return count == 0
                        ? request.ReferenceCode
                        : $"{request.ReferenceCode}-{count}";
                }
            }


            async Task<Result<(CreditCardPaymentRequest request, CreditCardPaymentResult result)>> Authorize(CreditCardPaymentRequest paymentRequest)
            {
                var (_, isFailure, payment, error) = await _payfortService.Authorize(paymentRequest);
                if (isFailure)
                    return Result.Fail<(CreditCardPaymentRequest, CreditCardPaymentResult)>(error);

                return payment.Status == CreditCardPaymentStatuses.Failed
                    ? Result.Fail<(CreditCardPaymentRequest, CreditCardPaymentResult)>($"Payment error: {payment.Message}")
                    : Result.Ok((paymentRequest, payment));
            }


            bool IsPaymentComplete((CreditCardPaymentRequest, CreditCardPaymentResult) creditCardPaymentData)
            {
                var (_, result) = creditCardPaymentData;
                return IsPaymentCompleteForResult(result);
            }


            bool IsPaymentCompleteForResult(CreditCardPaymentResult result)
            {
                return result.Status == CreditCardPaymentStatuses.Success;
            }


            Task SendBillToCustomer((CreditCardPaymentRequest, CreditCardPaymentResult) creditCardPaymentData)
            {
                var (paymentRequest, _) = creditCardPaymentData;
                return _notificationService.SendBillToCustomer(new PaymentBill(paymentRequest.CustomerEmail,
                    paymentRequest.Amount,
                    paymentRequest.Currency,
                    _dateTimeProvider.UtcNow(),
                    PaymentMethods.CreditCard,
                    paymentRequest.ReferenceCode,
                    paymentRequest.CustomerName));
            }


            async Task StorePayment(CreditCardPaymentResult payment)
            {
                var token = request.Token.Code;
                var card = request.Token.Type == PaymentTokenTypes.Stored
                    ? await _context.CreditCards.FirstOrDefaultAsync(c => c.Token == token)
                    : null;
                var now = _dateTimeProvider.UtcNow();
                var info = new CreditCardPaymentInfo(ipAddress, payment.ExternalCode, payment.Message, payment.AuthorizationCode, payment.ExpirationDate, payment.MerchantReference);
                _context.Payments.Add(new Payment
                {
                    Amount = payment.Amount,
                    BookingId = booking.Id,
                    AccountNumber = payment.CardNumber,
                    Currency = currency.ToString(),
                    Created = now,
                    Modified = now,
                    Status = ToPaymentStatus(payment.Status),
                    Data = JsonConvert.SerializeObject(info),
                    AccountId = card?.Id,
                    PaymentMethod = PaymentMethods.CreditCard
                });

                await _context.SaveChangesAsync();
            }


            Task WriteAuditLog(CreditCardPaymentResult result)
                => WriteAuthorizeAuditLog(result, booking);
        }


        public Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject response)
        {
            return _payfortService.ParsePaymentResponse(response)
                .OnSuccess(ProcessPaymentResponse);


            async Task<Result<PaymentResponse>> ProcessPaymentResponse(CreditCardPaymentResult paymentResult)
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.ReferenceCode == paymentResult.ReferenceCode);
                if (booking == null)
                    return Result.Fail<PaymentResponse>($"Could not find a booking by the reference code {paymentResult.ReferenceCode}");

                var payments = await _context.Payments.Where(p => p.BookingId == booking.Id).ToListAsync();
                var paymentEntity = payments.FirstOrDefault(p =>
                {
                    var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(p.Data);
                    return info.InternalReferenceCode?.Equals(paymentResult.MerchantReference, StringComparison.InvariantCultureIgnoreCase) == true;
                });
                if (paymentEntity == default)
                    return Result.Fail<PaymentResponse>(
                        $"Could not find a payment record with the booking ID {booking.Id} with internal reference code '{paymentResult.MerchantReference}'");

                // Payment can be completed before. Nothing to do now.
                if (paymentEntity.Status == PaymentStatuses.Authorized)
                    return Result.Ok(new PaymentResponse(string.Empty, CreditCardPaymentStatuses.Success, CreditCardPaymentStatuses.Success.ToString()));

                var (_, isFailure, error) = await _locker.Acquire<Payment>(paymentEntity.Id.ToString(), nameof(CreditCardPaymentService));
                if (isFailure)
                    return Result.Fail<PaymentResponse>(error);

                return await Result.Ok(paymentResult)
                    .OnSuccessWithTransaction(_context, payment => Result.Ok(payment)
                        .OnSuccess(UpdatePayment)
                        .OnSuccess(CheckPaymentStatusNotFailed)
                        .OnSuccessIf(IsPaymentComplete, WriteAuditLog)
                        .OnSuccessIf(IsPaymentComplete, SendBillToCustomer)
                        .OnSuccessIf(IsPaymentComplete, ChangePaymentStatus)
                        .OnSuccessIf(IsPaymentComplete, MarkCreditCardAsUsed)
                        .OnSuccess(CreateResponse))
                    .OnBoth(ReleaseEntityLock);


                Task UpdatePayment(CreditCardPaymentResult payment)
                {
                    var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(paymentEntity.Data);
                    var newInfo = new CreditCardPaymentInfo(info.CustomerIp, payment.ExternalCode, payment.Message, payment.AuthorizationCode,
                        payment.ExpirationDate, info.InternalReferenceCode);
                    paymentEntity.Status = ToPaymentStatus(payment.Status);
                    paymentEntity.Data = JsonConvert.SerializeObject(newInfo);
                    paymentEntity.Modified = _dateTimeProvider.UtcNow();
                    _context.Update(paymentEntity);
                    return _context.SaveChangesAsync();
                }


                Result<CreditCardPaymentResult> CheckPaymentStatusNotFailed(CreditCardPaymentResult payment)
                    => payment.Status == CreditCardPaymentStatuses.Failed
                        ? Result.Fail<CreditCardPaymentResult>($"Payment error: {payment.Message}")
                        : Result.Ok(payment);


                bool IsPaymentComplete(CreditCardPaymentResult cardPaymentResult) => cardPaymentResult.Status == CreditCardPaymentStatuses.Success;


                Task WriteAuditLog(CreditCardPaymentResult result)
                    => WriteAuthorizeAuditLog(result, booking);


                async Task SendBillToCustomer()
                {
                    var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == booking.CustomerId);
                    if (customer == default)
                    {
                        _logger.LogWarning("Send bill after credit card payment: could not find customer with id '{0}' for the booking '{1}'", booking.CustomerId,
                            booking.ReferenceCode);
                        return;
                    }

                    Enum.TryParse<Currencies>(paymentEntity.Currency, out var currency);
                    await _notificationService.SendBillToCustomer(new PaymentBill(customer.Email,
                        paymentEntity.Amount,
                        currency,
                        _dateTimeProvider.UtcNow(),
                        PaymentMethods.CreditCard,
                        booking.ReferenceCode,
                        $"{customer.LastName} {customer.FirstName}"));
                }


                Task ChangePaymentStatus() => ChangePaymentStatusForBookingToAuthorized(booking);


                async Task<Result<PaymentResponse>> ReleaseEntityLock(Result<PaymentResponse> result)
                {
                    await _locker.Release<Payment>(paymentEntity.Id.ToString());
                    return result;
                }
            }
        }


        public Task<Result<string>> CaptureMoney(Booking booking)
        {
            var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);
            var currency = bookingAvailability.Agreement.Price.Currency;
            
            return Result.Ok(booking)
                .OnSuccessWithTransaction(_context, _ =>
                    CapturePayment()
                        .OnSuccess(ChangePaymentStatusToCaptured)
                )
                .OnBoth(CreateResult);


            Task<Result> CapturePayment()
            {
                if (booking.PaymentMethod != PaymentMethods.CreditCard)
                    return Task.FromResult(Result.Fail($"Invalid payment method: {booking.PaymentMethod}"));

                return GetPayments(booking)
                        .OnSuccess(CapturePayments);


                async Task<Result> CapturePayments(List<Payment> payments)
                {
                    var total = bookingAvailability.Agreement.Price.NetTotal;
                    var results = new List<Result>();
                    var captured = payments.Where(p => p.Status == PaymentStatuses.Captured).Sum(p => p.Amount);
                    total -= captured;
                    foreach (var payment in payments.Where(p => p.Status == PaymentStatuses.Authorized))
                    {
                        var amount = Math.Min(total, payment.Amount);
                        results.Add(await Capture(payment, amount));
                        total -= amount;
                        if (total <= 0m)
                            break;
                    }

                    if (total > 0)
                    {
                        var message = $"Could not capture all amount for the booking '{booking.ReferenceCode}'. Pending: {total}";
                        _logger.LogUnableCaptureAllAmountForBooking(message);
                        results.Add(Result.Fail(message));
                    }

                    return Result.Combine(results.ToArray());


                    async Task<Result> Capture(Payment payment, decimal amount)
                    {
                        return await CaptureInPayfort()
                            .OnSuccess(WriteAuditLog)
                            .OnSuccess(UpdatePaymentStatus);


                        Task<Result<CreditCardCaptureResult>> CaptureInPayfort()
                        {
                            var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                            return _payfortService.Capture(new CreditCardCaptureMoneyRequest(currency: currency,
                                amount: amount,
                                externalId: info.ExternalId,
                                merchantReference: info.InternalReferenceCode,
                                languageCode: "en"));
                        }


                        Task WriteAuditLog(CreditCardCaptureResult captureResult)
                        {
                            var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                            var eventData = new CreditCardLogEventData($"Capture money for the booking '{booking.ReferenceCode}'", captureResult.ExternalCode,
                                captureResult.Message, info.InternalReferenceCode);
                            return _creditCardAuditService.Write(CreditCardEventType.Capture,
                                payment.AccountNumber,
                                amount,
                                new UserInfo(booking.CustomerId, UserTypes.Customer),
                                eventData,
                                booking.ReferenceCode,
                                booking.CustomerId,
                                currency);
                        }


                        Task UpdatePaymentStatus(CreditCardCaptureResult captureResult)
                        {
                            payment.Status = PaymentStatuses.Captured;
                            _context.Payments.Update(payment);
                            return Task.CompletedTask;
                        }
                    }
                }
            }


            Task ChangePaymentStatusToCaptured() => ChangeBookingPaymentStatusToCaptured(booking);


            Result<string> CreateResult(Result result)
                => result.IsSuccess
                    ? Result.Ok($"Payment for the booking '{booking.ReferenceCode}' completed.")
                    : Result.Fail<string>($"Unable to complete payment for the booking '{booking.ReferenceCode}'. Reason: {result.Error}");
                
            
        }


        public Task<Result> VoidMoney(Booking booking)
        {
            // TODO: Implement refund money if status is paid with deadline penalty
            if (booking.PaymentStatus != BookingPaymentStatuses.Authorized && booking.PaymentStatus != BookingPaymentStatuses.PartiallyAuthorized)
                return Task.FromResult(Result.Ok());

            if (booking.PaymentMethod != PaymentMethods.CreditCard)
                return Task.FromResult(Result.Fail($"Could not void money for the booking '{booking.ReferenceCode}' with a payment method '{booking.PaymentMethod}'"));

            var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);
            var currency = bookingAvailability.Agreement.Price.Currency;

            return GetPayments(booking)
                .OnSuccess(VoidPayments);


            async Task<Result> VoidPayments(List<Payment> payments)
            {
                var result = new List<Result>();
                foreach (var payment in payments)
                {
                    result.Add(await Void(payment));
                }
                return Result.Combine(result.ToArray());


                async Task<Result> Void(Payment payment)
                {
                    if (payment.Status != PaymentStatuses.Authorized)
                    {
                        return Result.Ok();
                    }

                    return await VoidInPayfort()
                        .OnSuccess(WriteAuditLog)
                        .OnSuccess(UpdatePaymentStatus);


                    Task<Result<CreditCardVoidResult>> VoidInPayfort()
                    {
                        var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                        return _payfortService.Void(new CreditCardVoidMoneyRequest(
                            externalId: info.ExternalId,
                            merchantReference: info.InternalReferenceCode,
                            languageCode: "en"));
                    }


                    Task WriteAuditLog(CreditCardVoidResult voidResult)
                    {
                        var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                        var eventData = new CreditCardLogEventData($"Void money for the booking '{booking.ReferenceCode}'", voidResult.ExternalCode,
                            voidResult.Message, info.InternalReferenceCode);
                        return _creditCardAuditService.Write(CreditCardEventType.Void,
                            payment.AccountNumber,
                            payment.Amount,
                            new UserInfo(booking.CustomerId, UserTypes.Customer), 
                            eventData,
                            booking.ReferenceCode,
                            booking.CustomerId,
                            currency);
                    }


                    Task UpdatePaymentStatus(CreditCardVoidResult voidResult)
                    {
                        payment.Status = PaymentStatuses.Voided;
                        _context.Payments.Update(payment);
                        return Task.CompletedTask;
                    }
                }
            }
        }


        public Task<Result<Price>> GetPendingAmount(Booking booking)
        {
            var availabilityInfo = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

            var currency = availabilityInfo.Agreement.Price.Currency;

            return booking.PaymentMethod != PaymentMethods.CreditCard
                ? Task.FromResult(Result.Fail<Price>($"Unsupported payment method for pending payment: {booking.PaymentMethod}"))
                : GetPendingForCard();


            async Task<Result<Price>> GetPendingForCard()
            {
                var paid = await _context.Payments.Where(p => p.BookingId == booking.Id).SumAsync(p => p.Amount);
                var total = availabilityInfo.Agreement.Price.NetTotal;
                var forPay = total - paid;
                return forPay <= 0m
                    ? Result.Fail<Price>("Nothing to pay")
                    : Result.Ok(new Price(currency, forPay, forPay, PriceTypes.Supplement));
            }
        }


        private static PaymentResponse CreateResponse(CreditCardPaymentResult payment)
            => new PaymentResponse(payment.Secure3d, payment.Status, payment.Message);


        private async Task ChangePaymentStatusForBookingToAuthorized(CreditCardPaymentResult payment)
        {
            // ReferenceCode should always contain valid booking reference code. We check it in CheckReferenceCode or StorePayment
            var booking = await _context.Bookings.FirstAsync(b => b.ReferenceCode == payment.ReferenceCode);
            await ChangePaymentStatusForBookingToAuthorized(booking);
        }


        private Task ChangeBookingPaymentStatusToCaptured(Booking booking)
        {
            booking.PaymentStatus = BookingPaymentStatuses.Captured;
            _context.Bookings.Update(booking);
            return _context.SaveChangesAsync();
        }


        private async Task ChangePaymentStatusForBookingToAuthorized(Booking booking)
        {
            booking.PaymentStatus = BookingPaymentStatuses.Authorized;
            _context.Update(booking);
            await _context.SaveChangesAsync();
        }


        private async Task MarkCreditCardAsUsed(CreditCardPaymentResult payment)
        {
            var query = from booking in _context.Bookings
                join payments in _context.Payments on booking.Id equals payments.BookingId
                join cards in _context.CreditCards on payments.AccountId equals cards.Id
                where booking.ReferenceCode == payment.ReferenceCode
                select cards;

            // Maybe null for onetime tokens
            var card = await query.FirstOrDefaultAsync();
            if (card?.IsUsedForPayments != false)
                return;

            card.IsUsedForPayments = true;
            _context.Update(card);
            await _context.SaveChangesAsync();
        }


        private async Task WriteAuthorizeAuditLog(CreditCardPaymentResult payment, Booking booking)
        {
            var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);
            var currency = bookingAvailability.Agreement.Price.Currency;
            var eventData = new CreditCardLogEventData($"Authorize money for the booking '{booking.ReferenceCode}'", payment.ExternalCode, payment.Message, payment.MerchantReference);
            await _creditCardAuditService.Write(CreditCardEventType.Authorize,
                payment.CardNumber,
                payment.Amount,
                new UserInfo(booking.CustomerId, UserTypes.Customer), 
                eventData,
                payment.ReferenceCode,
                booking.CustomerId,
                currency);
        }


        private async Task<Result> Validate(PaymentRequest request, CustomerInfo customerInfo, Booking booking)
        {
            var fieldValidateResult = GenericValidator<PaymentRequest>.Validate(v =>
            {
                v.RuleFor(c => c.ReferenceCode).NotEmpty();
                v.RuleFor(c => c.Token.Code).NotEmpty();
                v.RuleFor(c => c.Token.Type).NotEmpty().IsInEnum().Must(c => c != PaymentTokenTypes.Unknown);
                v.RuleFor(c => c.SecurityCode)
                    .Must(code => request.Token.Type == PaymentTokenTypes.OneTime || !string.IsNullOrEmpty(code))
                    .WithMessage("SecurityCode cannot be empty");
            }, request);

            if (fieldValidateResult.IsFailure)
                return fieldValidateResult;

            return Result.Combine(CheckBooking(),
                await CheckToken());


            Result CheckBooking()
            {
                if (booking == null)
                    return Result.Fail("Invalid Reference code");

                return GenericValidator<Booking>.Validate(v =>
                {
                    v.RuleFor(c => c.CustomerId).Equal(customerInfo.CustomerId)
                        .WithMessage($"User does not have access to booking with reference code '{booking.ReferenceCode}'");
                    v.RuleFor(c => c.Status).Must(s => BookingStatusesForPayment.Contains(s))
                        .WithMessage($"Invalid booking status: {booking.Status.ToString()}");
                    v.RuleFor(c => c.PaymentMethod).Must(c => c == PaymentMethods.CreditCard)
                        .WithMessage($"Booking with reference code {booking.ReferenceCode} can be paid only with {booking.PaymentMethod.ToString()}");
                    v.RuleFor(b => b.PaymentStatus)
                        .Must(status => status == BookingPaymentStatuses.NotPaid || status == BookingPaymentStatuses.PartiallyAuthorized)
                        .WithMessage($"Could not pay for the booking with status {booking.PaymentStatus}");
                }, booking);
            }


            async Task<Result> CheckToken()
            {
                if (request.Token.Type != PaymentTokenTypes.Stored)
                    return Result.Ok();

                var token = request.Token.Code;
                var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Token == token);
                return await ChecksCreditCardExists()
                    .OnSuccess(CanUseCreditCard);

                Result ChecksCreditCardExists() => card != null ? Result.Ok() : Result.Fail("Cannot find a credit card by payment token");

                async Task<Result> CanUseCreditCard() => await _creditCardService.Get(card.Id, customerInfo);
            }
        }


        private async Task<Result<List<Payment>>> GetPayments(Booking booking)
        {
            var payments = await _context.Payments.Where(p => p.BookingId == booking.Id).ToListAsync();

            return payments.Any()
                ? Result.Ok(payments)
                : Result.Fail<List<Payment>>($"Cannot find external payments for the booking '{booking.ReferenceCode}'");
        }


        private PaymentStatuses ToPaymentStatus(CreditCardPaymentStatuses status)
        {
            switch (status)
            {
                case CreditCardPaymentStatuses.Created: return PaymentStatuses.Created;
                case CreditCardPaymentStatuses.Success: return PaymentStatuses.Authorized;
                case CreditCardPaymentStatuses.Secure3d: return PaymentStatuses.Secure3d;
                case CreditCardPaymentStatuses.Failed: return PaymentStatuses.Failed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }


        private static readonly HashSet<BookingStatusCodes> BookingStatusesForPayment = new HashSet<BookingStatusCodes>
        {
            BookingStatusCodes.Pending, BookingStatusCodes.Confirmed
        };

        private readonly EdoContext _context;
        private readonly ICreditCardService _creditCardService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEntityLocker _locker;
        private readonly ILogger<CreditCardPaymentService> _logger;
        private readonly ICreditCardAuditService _creditCardAuditService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly IPayfortService _payfortService;
    }
}