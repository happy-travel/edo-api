using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupBonusMaterializationService  : IMarkupBonusMaterializationService
    {
        public MarkupBonusMaterializationService(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public Task<List<int>> GetForMaterialize(DateTime dateTime)
        {
            var query =
                from appliedMarkup in _context.AppliedBookingMarkups
                join booking in _context.Bookings on appliedMarkup.ReferenceCode equals booking.ReferenceCode
                join policy in _context.MarkupPolicies on appliedMarkup.PolicyId equals policy.Id
                where 
                    booking.Status == BookingStatuses.Confirmed &&
                    booking.PaymentStatus == BookingPaymentStatuses.Captured &&
                    booking.CheckOutDate.Date >= dateTime && 
                    appliedMarkup.Paid == null &&
                    SupportedPolicyScopeTypes.Contains(policy.ScopeType)
                select appliedMarkup.Id;

            return query.ToListAsync();
        }


        public async Task<Result<BatchOperationResult>> Materialize(List<int> markupsForMaterialization)
        {
            var hasErrors = false;
            var stringBuilder = new StringBuilder();

            foreach (var materializationData in await GetData(markupsForMaterialization))
            {
                var (_, isFailure, error) = await ApplyBonus(materializationData);
                if (isFailure)
                {
                    hasErrors = true;
                    stringBuilder.Append(error);
                }
            }

            return new BatchOperationResult($"{markupsForMaterialization.Count} markups materialized. {stringBuilder}", hasErrors);
        }


        private Task<List<MaterializationData>> GetData(ICollection<int> markupsForMaterialization)
        {
            var query =
                from appliedMarkup in _context.AppliedBookingMarkups
                join booking in _context.Bookings on appliedMarkup.ReferenceCode equals booking.ReferenceCode
                join policy in _context.MarkupPolicies on appliedMarkup.PolicyId equals policy.Id
                where 
                    markupsForMaterialization.Contains(appliedMarkup.Id) &&
                    appliedMarkup.Paid == null &&
                    policy.AgencyId != null
                select new MaterializationData
                {
                    PolicyId = appliedMarkup.PolicyId,
                    ReferenceCode = appliedMarkup.ReferenceCode,
                    AgencyId = policy.AgencyId.Value,
                    Amount = new MoneyAmount
                    {
                        Amount = appliedMarkup.Amount,
                        Currency = appliedMarkup.Currency
                    },
                    ScopeType = policy.ScopeType
                };

            return query.ToListAsync();
        }


        private async Task<Result> ApplyBonus(MaterializationData data)
        {
            var applyBonusTask = data.ScopeType switch
            {
                MarkupPolicyScopeType.Agency => ApplyAgencyScopeBonus(),
                MarkupPolicyScopeType.Agent => ApplyAgentScopeBonus(),
                _ => Task.FromResult(Result.Failure($"MarkupPolicyScopeType {data.ScopeType} is not supported"))
            };

            return await applyBonusTask;


            Task<Result> ApplyAgentScopeBonus() 
                => ApplyAgencyBonus(data.PolicyId, data.ReferenceCode, data.AgencyId, data.Amount);


            async Task<Result> ApplyAgencyScopeBonus()
            {
                var parentAgencyId = await _context.Agencies
                    .Where(a => a.Id == data.AgencyId)
                    .Select(a => a.ParentId)
                    .SingleOrDefaultAsync();
                
                if (parentAgencyId is null)
                    return Result.Failure($"Cannot retrieve parent agency for agency id '{data.AgencyId}'");
                
                return await ApplyAgencyBonus(data.PolicyId, data.ReferenceCode, parentAgencyId.Value, data.Amount);
            }
        }


        private async Task<Result> ApplyAgencyBonus(int policyId, string referenceCode, int agencyId, MoneyAmount amount)
        {
            var agencyAccount = await _context.AgencyAccounts
                .SingleOrDefaultAsync(a => a.AgencyId == agencyId && a.Currency == amount.Currency);

            if (agencyAccount is null)
                return Result.Failure($"Account for agency '{agencyId}' with currency {amount.Currency} not found");

            var paidDate = _dateTimeProvider.UtcNow();

            return await Result.Success()
                .BindWithTransaction(_context, () => Result.Success()
                    .Tap(UpdateBalance)
                    .Tap(MarkAsPaid)
                    .Tap(WriteLog)
                );


            async Task UpdateBalance()
            {
                agencyAccount.Balance += amount.Amount;
                _context.AgencyAccounts.Update(agencyAccount);
                await _context.SaveChangesAsync();
                _context.Detach(agencyAccount);
            }


            async Task MarkAsPaid()
            {
                var appliedMarkup = await _context.AppliedBookingMarkups
                    .SingleOrDefaultAsync(a => a.PolicyId == policyId && a.ReferenceCode == referenceCode);

                appliedMarkup.Paid = paidDate;
                _context.AppliedBookingMarkups.Update(appliedMarkup);
                await _context.SaveChangesAsync();
                _context.Detach(appliedMarkup);
            }


            async Task WriteLog()
            {
                await _context.MaterializationBonusLogs.AddAsync(new MaterializationBonusLog
                {
                    PolicyId = policyId,
                    ReferenceCode = referenceCode,
                    AgencyAccountId = agencyAccount.Id,
                    Amount = amount.Amount,
                    Created = paidDate
                });
                await _context.SaveChangesAsync();
            }
        }


        private static readonly HashSet<MarkupPolicyScopeType> SupportedPolicyScopeTypes 
            = new() {MarkupPolicyScopeType.Agent, MarkupPolicyScopeType.Agency};


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}