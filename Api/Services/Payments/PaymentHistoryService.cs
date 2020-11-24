using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PaymentHistoryService : IPaymentHistoryService
    {
        public PaymentHistoryService(EdoContext edoContext)
        {
            _edoContext = edoContext;
        }


        public async Task<Result<List<PaymentHistoryData>>> GetAgentHistory(PaymentHistoryRequest paymentHistoryRequest, AgentContext agent)
        {
            var validationResult = Validate(paymentHistoryRequest);
            if (validationResult.IsFailure)
                return Result.Failure<List<PaymentHistoryData>>(validationResult.Error);

            var accountHistoryQuery = from account in _edoContext.AgencyAccounts
                join auditLogEntry in _edoContext.AccountBalanceAuditLogs
                    on account.Id equals auditLogEntry.AccountId
                join booking in _edoContext.Bookings
                    on auditLogEntry.ReferenceCode equals booking.ReferenceCode
                where account.AgencyId == agent.AgencyId &&
                    auditLogEntry.UserId == agent.AgentId &&
                    auditLogEntry.UserType == UserTypes.Agent &&
                    auditLogEntry.Created <= paymentHistoryRequest.ToDate &&
                    auditLogEntry.Created >= paymentHistoryRequest.FromDate
                select new PaymentHistoryData(auditLogEntry.Created,
                    auditLogEntry.Amount,
                    JObject.Parse(auditLogEntry.EventData),
                    account.Currency.ToString(),
                    auditLogEntry.UserId,
                    ToPaymentHistoryType(auditLogEntry.Type),
                    PaymentMethods.BankTransfer,
                    booking.AccommodationName,
                    booking.MainPassengerName,
                    booking.Id,
                    booking.ReferenceCode);

            
            var cardHistoryQuery = from auditLogEntry in _edoContext.CreditCardAuditLogs
                join booking in _edoContext.Bookings
                    on auditLogEntry.ReferenceCode equals booking.ReferenceCode
                where booking.AgencyId == agent.AgencyId &&
                    auditLogEntry.UserId == agent.AgentId &&
                    auditLogEntry.UserType == UserTypes.Agent &&
                    auditLogEntry.Created <= paymentHistoryRequest.ToDate &&
                    auditLogEntry.Created >= paymentHistoryRequest.FromDate
                select new PaymentHistoryData(auditLogEntry.Created,
                    auditLogEntry.Amount,
                    JObject.Parse(auditLogEntry.EventData),
                    auditLogEntry.Currency.ToString(),
                    auditLogEntry.UserId,
                    ToPaymentHistoryType(auditLogEntry.Type),
                    PaymentMethods.CreditCard,
                    booking.AccommodationName,
                    booking.MainPassengerName,
                    booking.Id,
                    booking.ReferenceCode);


            return (await accountHistoryQuery.ToListAsync())
                .Union(await cardHistoryQuery.ToListAsync())
                .OrderByDescending(h => h.Created)
                .ToList();
        }


        public async Task<Result<List<PaymentHistoryData>>> GetAgencyHistory(PaymentHistoryRequest paymentHistoryRequest, AgentContext agent)
        {
            var validationResult = Validate(paymentHistoryRequest);
            if (validationResult.IsFailure)
                return Result.Failure<List<PaymentHistoryData>>(validationResult.Error);

            var accountHistoryQuery = from account in _edoContext.AgencyAccounts
                join auditLogEntry in _edoContext.AccountBalanceAuditLogs
                    on account.Id equals auditLogEntry.AccountId
                join booking in _edoContext.Bookings
                    on auditLogEntry.ReferenceCode equals booking.ReferenceCode
                where account.AgencyId == agent.AgencyId &&
                    auditLogEntry.UserType == UserTypes.Agent &&
                    auditLogEntry.Created <= paymentHistoryRequest.ToDate &&
                    auditLogEntry.Created >= paymentHistoryRequest.FromDate
                select new PaymentHistoryData(auditLogEntry.Created,
                    auditLogEntry.Amount,
                    JObject.Parse(auditLogEntry.EventData),
                    account.Currency.ToString(),
                    auditLogEntry.UserId,
                    ToPaymentHistoryType(auditLogEntry.Type),
                    PaymentMethods.BankTransfer,
                    booking.AccommodationName,
                    booking.MainPassengerName,
                    booking.Id,
                    booking.ReferenceCode);

            
            var cardHistoryQuery = from auditLogEntry in _edoContext.CreditCardAuditLogs
                join booking in _edoContext.Bookings
                    on auditLogEntry.ReferenceCode equals booking.ReferenceCode
                where booking.AgencyId == agent.AgencyId &&
                    auditLogEntry.UserType == UserTypes.Agent &&
                    auditLogEntry.Created <= paymentHistoryRequest.ToDate &&
                    auditLogEntry.Created >= paymentHistoryRequest.FromDate
                select new PaymentHistoryData(auditLogEntry.Created,
                    auditLogEntry.Amount,
                    JObject.Parse(auditLogEntry.EventData),
                    auditLogEntry.Currency.ToString(),
                    auditLogEntry.UserId,
                    ToPaymentHistoryType(auditLogEntry.Type),
                    PaymentMethods.CreditCard,
                    booking.AccommodationName,
                    booking.MainPassengerName,
                    booking.Id,
                    booking.ReferenceCode);

            return (await accountHistoryQuery.ToListAsync())
                .Union(await cardHistoryQuery.ToListAsync())
                .OrderByDescending(h => h.Created)
                .ToList();
        }


        private Result Validate(PaymentHistoryRequest paymentHistoryRequest)
        {
            return GenericValidator<PaymentHistoryRequest>.Validate(setup =>
            {
                setup.RuleFor(i => i.ToDate)
                    .GreaterThanOrEqualTo(request => request.FromDate)
                    .WithMessage($"{nameof(paymentHistoryRequest.ToDate)} must be greater then {nameof(paymentHistoryRequest.FromDate)}");
                setup.RuleFor(i => (i.ToDate - i.FromDate).Days)
                    .LessThanOrEqualTo(MaxRequestDaysNumber)
                    .WithMessage(
                        $"Total days between {nameof(paymentHistoryRequest.FromDate)} and {nameof(paymentHistoryRequest.ToDate)} should be less or equal {MaxRequestDaysNumber}");
            }, paymentHistoryRequest);
        }


        private static PaymentHistoryType ToPaymentHistoryType(AccountEventType type)
        {
            switch (type)
            {
                case AccountEventType.None: return PaymentHistoryType.None;
                case AccountEventType.Add: return PaymentHistoryType.Add;
                case AccountEventType.Charge: return PaymentHistoryType.Charge;
                case AccountEventType.Authorize: return PaymentHistoryType.Authorize;
                case AccountEventType.Capture: return PaymentHistoryType.Capture;
                case AccountEventType.Void: return PaymentHistoryType.Void;
                case AccountEventType.Refund: return PaymentHistoryType.Refund;
                case AccountEventType.CounterpartyTransferToAgency: return PaymentHistoryType.Add;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }


        private static PaymentHistoryType ToPaymentHistoryType(CreditCardEventType type)
        {
            switch (type)
            {
                case CreditCardEventType.Authorize: return PaymentHistoryType.Authorize;
                case CreditCardEventType.Capture: return PaymentHistoryType.Capture;
                case CreditCardEventType.Void: return PaymentHistoryType.None;
                case CreditCardEventType.None: return PaymentHistoryType.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }


        private const int MaxRequestDaysNumber = 3650;

        private readonly EdoContext _edoContext;
    }
}