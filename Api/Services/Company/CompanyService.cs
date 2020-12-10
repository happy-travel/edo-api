using System;
using System.Text.Json;
using HappyTravel.Edo.Api.Models.Company;
using HappyTravel.Edo.Data;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Data.StaticData;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Company
{
    public class CompanyService : ICompanyService
    {
        public CompanyService(EdoContext context, IDoubleFlow flow)
        {
            _context = context;
            _flow = flow;
        }


        public async Task<Result<CompanyInfo>> Get()
        {
            var key = _flow.BuildKey(nameof(CompanyService), nameof(Get));
            var info = await _flow.GetOrSetAsync(key, async () =>
            {
                var companyInfo = await _context.StaticData
                    .SingleOrDefaultAsync(sd => sd.Type == StaticDataTypes.CompanyInfo);
                
                if (companyInfo == default)
                    return default;

                return JsonSerializer.Deserialize<CompanyInfo>(companyInfo.Data.RootElement.ToString());
            }, CompanyInfoCacheLifeTime);

            return info ?? Result.Failure<CompanyInfo>("Could not find company information");
        }

        private static readonly TimeSpan CompanyInfoCacheLifeTime = TimeSpan.FromHours(1); 
        
        private readonly EdoContext _context;
        private readonly IDoubleFlow _flow;
    }
}