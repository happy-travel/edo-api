using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;
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
            IAgentService agentService,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _agentService = agentService;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task<Result<CounterpartyInfo>> Add(CounterpartyEditRequest counterparty)
        {
            var (_, isFailure, error) = CounterpartyValidator.Validate(counterparty);
            if (isFailure)
                return Result.Failure<CounterpartyInfo>(error);

            var now = _dateTimeProvider.UtcNow();
            var createdCounterparty = new Counterparty
            {
                Address = counterparty.Address,
                City = counterparty.City,
                CountryCode = counterparty.CountryCode,
                Fax = counterparty.Fax,
                Name = counterparty.Name,
                Phone = counterparty.Phone,
                Website = counterparty.Website,
                PostalCode = counterparty.PostalCode,
                PreferredCurrency = counterparty.PreferredCurrency,
                PreferredPaymentMethod = counterparty.PreferredPaymentMethod,
                State = CounterpartyStates.PendingVerification,
                Created = now,
                Updated = now
            };

            _context.Counterparties.Add(createdCounterparty);
            await _context.SaveChangesAsync();

            var defaultAgency = new Agency
            {
                Name = createdCounterparty.Name,
                CounterpartyId = createdCounterparty.Id,
                Created = now,
                Modified = now,
                ParentId = null,
            };
            _context.Agencies.Add(defaultAgency);

            await _context.SaveChangesAsync();
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


        public async Task<Result<AgencyInfo>> GetAgency(int agencyId, AgentContext agent)
        {
            var agentRelations = await _agentService.GetAgentRelations(agent);
            if (agentRelations.All(r => r.AgencyId != agencyId))
                return Result.Failure<AgencyInfo>("The agent is not affiliated with agency");

            var agency = await _context.Agencies.SingleAsync(a => a.Id == agencyId);

            return Result.Success(new AgencyInfo(agency.Name, agency.Id));
        }


        public Task<Agency> GetDefaultAgency(int counterpartyId)
            => _context.Agencies
                .SingleAsync(a => a.CounterpartyId == counterpartyId && a.ParentId == null);


        public async Task<Result<CounterpartyInfo>> Get(int counterpartyId, string languageCode = LocalizationHelper.DefaultLanguageCode)
        {
            return await GetCounterpartyInfo(counterpartyId, languageCode);
        }


        private async Task<Result<CounterpartyInfo>> GetCounterpartyInfo(int counterpartyId, string languageCode = LocalizationHelper.DefaultLanguageCode )
        {
            var result = await (from cp in _context.Counterparties
                join c in _context.Countries on cp.CountryCode equals c.Code
                where cp.Id == counterpartyId
                select new
                {
                    Counterparty = cp,
                    Country = c
                }).SingleOrDefaultAsync();

            if (result == default)
                return Result.Failure<CounterpartyInfo>("Could not find counterparty with specified id");

            return Result.Success(new CounterpartyInfo(
                result.Counterparty.Id,
                result.Counterparty.Name,
                result.Counterparty.Address,
                result.Counterparty.CountryCode,
                LocalizationHelper.GetValueFromSerializedString(result.Country.Names, languageCode),
                result.Counterparty.City,
                result.Counterparty.Phone,
                result.Counterparty.Fax,
                result.Counterparty.PostalCode,
                result.Counterparty.PreferredCurrency,
                result.Counterparty.PreferredPaymentMethod,
                result.Counterparty.Website,
                result.Counterparty.VatNumber,
                result.Counterparty.BillingEmail));
        }


        private readonly IAgentService _agentService;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}