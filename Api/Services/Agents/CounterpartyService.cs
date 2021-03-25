using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class CounterpartyService : ICounterpartyService
    {
        public CounterpartyService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IAdminAgencyManagementService agencyManagementService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _agencyManagementService = agencyManagementService;
        }


        public async Task<Result<CounterpartyInfo>> Add(CounterpartyCreateRequest request)
        {
            var (_, isFailure, error) = AgencyValidator.Validate(request);
            if (isFailure)
                return Result.Failure<CounterpartyInfo>(error);

            var now = _dateTimeProvider.UtcNow();
            var createdCounterparty = new Counterparty
            {
                Name = request.Name,
                PreferredPaymentMethod = request.PreferredPaymentMethod,
                State = CounterpartyStates.PendingVerification,
                Created = now,
                Updated = now
            };

            _context.Counterparties.Add(createdCounterparty);
            await _context.SaveChangesAsync();

            await _agencyManagementService.Create(createdCounterparty.Name, createdCounterparty.Id, request.Address, request.BillingEmail,
                request.City, request.CountryCode, request.Fax, request.Phone, request.PostalCode, request.Website, null);

            return await GetCounterpartyInfo(createdCounterparty.Id);
        }


        //public Task<Result<Agency>> AddAgency(int counterpartyId, AgencyInfo agency)
        //{
        //    Counterparty counterparty = null;

        //    return CheckCounterpartyExists()
        //        .Ensure(HasPermissions, "Permission to create agencies denied")
        //        .Ensure(IsAgencyNameUnique, $"Agency with name {agency.Name} already exists")
        //        .Map(SaveAgency)
        //        .Bind(CreateAccountIfVerified);


        //    async Task<bool> HasPermissions()
        //    {
        //        var agent = await _agentContextService.GetAgent();
        //        return agent.IsMaster && agent.CounterpartyId == counterpartyId;
        //    }


        //    async Task<Result> CheckCounterpartyExists()
        //    {
        //        counterparty = await _context.Counterparties.Where(c => c.Id == counterpartyId).SingleOrDefaultAsync();
        //        return counterparty == null
        //            ? Result.Failure("Could not find the counterparty with specified id")
        //            : Result.Success();
        //    }


        //    async Task<bool> IsAgencyNameUnique()
        //    {
        //        return !await _context.Agencies.Where(a => a.CounterpartyId == counterpartyId &&
        //                EF.Functions.ILike(a.Name, agency.Name))
        //            .AnyAsync();
        //    }


        //    async Task<Agency> SaveAgency()
        //    {
        //        var now = _dateTimeProvider.UtcNow();
        //        var createdAgency = new Agency
        //        {
        //            Name = agency.Name,
        //            CounterpartyId = counterpartyId,
        //            IsRoot = false,
        //            Created = now,
        //            Modified = now,
        //        };
        //        _context.Agencies.Add(createdAgency);
        //        await _context.SaveChangesAsync();

        //        return createdAgency;
        //    }


        //    async Task<Result<Agency>> CreateAccountIfVerified(Agency createdAgency)
        //    {
        //        if (!new[] {CounterpartyStates.FullAccess, CounterpartyStates.ReadOnly}.Contains(counterparty.State))
        //            return Result.Success(createdAgency);

        //        var (_, isFailure, error) = await _accountManagementService.CreateForAgency(createdAgency, counterparty.PreferredCurrency);
        //        if (isFailure)
        //            return Result.Failure<Agency>(error);

        //        return Result.Success(createdAgency);
        //    }
        //}


        public Task<Agency> GetDefaultAgency(int counterpartyId)
            => _context.Agencies
                .SingleAsync(a => a.CounterpartyId == counterpartyId && a.ParentId == null);


        public async Task<Result<CounterpartyInfo>> Get(int counterpartyId)
        {
            return await GetCounterpartyInfo(counterpartyId);
        }


        private async Task<Result<CounterpartyInfo>> GetCounterpartyInfo(int counterpartyId)
        {
            var result = await _context.Counterparties
                .Where(cp => cp.Id == counterpartyId)
                .SingleOrDefaultAsync();

            if (result == default)
                return Result.Failure<CounterpartyInfo>("Could not find counterparty with specified id");

            return Result.Success(new CounterpartyInfo(
                result.Id,
                result.Name,
                result.PreferredPaymentMethod,
                result.VatNumber,
                result.IsContractUploaded));
        }

        
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAdminAgencyManagementService _agencyManagementService;
    }
}