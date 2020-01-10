using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public class HttpBasedCustomerContext : ICustomerContext
    {
        public HttpBasedCustomerContext(EdoContext context,
            ITokenInfoAccessor tokenInfoAccessor)
        {
            _context = context;
            _tokenInfoAccessor = tokenInfoAccessor;
        }


        public async ValueTask<Result<CustomerInfo>> GetCustomerInfo()
        {
            // TODO: Add caching
            if (_customerInfo.Equals(default))
            {
                var identityHash = GetUserIdentityHash();
                // TODO: use company information from headers to get company id
                _customerInfo = await (from customer in _context.Customers
                        from customerCompanyRelation in _context.CustomerCompanyRelations.Where(r => r.CustomerId == customer.Id)
                        from company in _context.Companies.Where(c => c.Id == customerCompanyRelation.CompanyId)
                        from branch in _context.Branches.Where(b => b.Id == customerCompanyRelation.BranchId).DefaultIfEmpty()
                        where customer.IdentityHash == identityHash
                        select new CustomerInfo(customer.Id,
                            customer.FirstName,
                            customer.LastName,
                            customer.Email,
                            customer.Title,
                            customer.Position,
                            company.Id,
                            company.Name,
                            Maybe<int>.None, // TODO: change this to branch when EF core issue will be resolved
                            customerCompanyRelation.Type == CustomerCompanyRelationTypes.Master,
                            customerCompanyRelation.InCompanyPermissions))
                    .SingleOrDefaultAsync();
            }

            return _customerInfo.Equals(default)
                ? Result.Fail<CustomerInfo>("Could not get customer data")
                : Result.Ok(_customerInfo);
        }


        public async Task<Result<UserInfo>> GetUserInfo()
        {
            return (await GetCustomerInfo())
                .OnSuccess(customer => new UserInfo(customer.CustomerId, UserTypes.Customer));
        }


        public async Task<List<CustomerCompanyInfo>> GetCustomerCompanies()
        {
            var (_, isFailure, customerInfo, _) = await GetCustomerInfo();
            if (isFailure)
                return new List<CustomerCompanyInfo>(0);

            return await _context.CustomerCompanyRelations
                .Where(cr => cr.CustomerId == customerInfo.CustomerId)
                .Join(_context.Companies, cr => cr.CompanyId, company => company.Id, (cr, company) => new CustomerCompanyInfo(
                    company.Id,
                    company.Name,
                    cr.Type == CustomerCompanyRelationTypes.Master,
                    cr.InCompanyPermissions.ToList()))
                .ToListAsync();
        }


        private string GetUserIdentityHash()
        {
            var identityClaim = _tokenInfoAccessor.GetIdentity();
            if (identityClaim != null)
                return HashGenerator.ComputeSha256(identityClaim);

            var clientIdClaim = _tokenInfoAccessor.GetClientId();
#warning TODO: Remove this after implementing client-customer relation
            if (clientIdClaim != null)
                return clientIdClaim;

            return string.Empty;
        }


        private readonly EdoContext _context;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
        private CustomerInfo _customerInfo;
    }
}