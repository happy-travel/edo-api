using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Files
{
    public class ContractFileService : IContractFileService
    {
        public ContractFileService(IContractFileManagementService contractFileManagementService,
            EdoContext edoContext)
        {
            _contractFileManagementService = contractFileManagementService;
            _edoContext = edoContext;
        }


        public async Task<Result<(Stream stream, string contentType)>> Get(AgentContext agentContext)
        {
            return await GetAgency()
                .Ensure(agency => agency.ParentId == null, "Couldn't get a contract file")
                .Bind(_ => _contractFileManagementService.Get(agentContext.CounterpartyId));


            async Task<Result<Agency>> GetAgency() => await _edoContext.Agencies.SingleAsync(a => a.Id == agentContext.AgencyId && a.IsActive);
        }


        private readonly IContractFileManagementService _contractFileManagementService;
        private readonly EdoContext _edoContext;
    }
}
