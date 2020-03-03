using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Customers;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        public CustomerService(EdoContext context, IDateTimeProvider dateTimeProvider, ICustomerContext customerContext)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _customerContext = customerContext;
        }


        public async Task<Result<Customer>> Add(CustomerRegistrationInfo customerRegistration,
            string externalIdentity,
            string email)
        {
            var (_, isFailure, error) = await Validate(customerRegistration, externalIdentity);
            if (isFailure)
                return Result.Fail<Customer>(error);

            var createdCustomer = new Customer
            {
                Title = customerRegistration.Title,
                FirstName = customerRegistration.FirstName,
                LastName = customerRegistration.LastName,
                Position = customerRegistration.Position,
                Email = email,
                IdentityHash = HashGenerator.ComputeSha256(externalIdentity),
                Created = _dateTimeProvider.UtcNow()
            };

            _context.Customers.Add(createdCustomer);
            await _context.SaveChangesAsync();

            return Result.Ok(createdCustomer);
        }


        public async Task<Result<Customer>> GetMasterCustomer(int companyId)
        {
            var master = await (from c in _context.Customers
                join rel in _context.CustomerCompanyRelations on c.Id equals rel.CustomerId
                where rel.CompanyId == companyId && rel.Type == CustomerCompanyRelationTypes.Master
                select c).FirstOrDefaultAsync();

            if (master is null)
                return Result.Fail<Customer>("Master customer does not exist");

            return Result.Ok(master);
        }


        private enum MarkupObserveLevel
        {
            None,
            Branch,
            Company
        }

        public async Task<Result<List<CustomerInfoInSearch>>> GetCustomers(int companyId, int branchId = default)
        {
            var customer = await _customerContext.GetCustomer();
            var (_, failure, error) = ChechCompanyAndBranch(customer, companyId, branchId);
            if (failure)
                return Result.Fail<List<CustomerInfoInSearch>>(error);

            var markupObserveLevel = MarkupObserveLevel.None;
            if (customer.InCompanyPermissions.HasFlag(InCompanyPermissions.ObserveMarkupInBranch))
                markupObserveLevel = MarkupObserveLevel.Branch;
            if (customer.InCompanyPermissions.HasFlag(InCompanyPermissions.ObserveMarkupInCompany))
                markupObserveLevel = MarkupObserveLevel.Company;

            var query = from cr in _context.CustomerCompanyRelations
                join cu in _context.Customers
                    on cr.CustomerId equals cu.Id
                join co in _context.Companies
                    on cr.CompanyId equals co.Id
                join br in _context.Branches
                    on cr.BranchId equals br.Id
                join mp in _context.MarkupPolicies
                    on cr.CustomerId equals mp.CustomerId into mpn from mp in mpn.DefaultIfEmpty()
                where branchId == default ? cr.CompanyId == companyId : cr.BranchId == branchId
                select new {cr, cu, co, br, mp};

            var data = await query.ToListAsync();

            var results = data.Select(o => 
                new CustomerInfoInSearch(o.cu.Id, o.cu.FirstName, o.cu.LastName, o.co.Id, o.co.Name, o.br.Id, o.br.Title,
                    GetMarkupIfPermission(o.mp, o.cr)))
                .ToList();

            return Result.Ok(results);

            MarkupPolicySettings? GetMarkupIfPermission(MarkupPolicy policy, CustomerCompanyRelation relation)
            {
                if (policy == null)
                    return null;

                if (markupObserveLevel == MarkupObserveLevel.Company
                    || markupObserveLevel == MarkupObserveLevel.Branch && relation.BranchId == branchId)
                    return new MarkupPolicySettings(policy.Description, policy.TemplateId,
                        policy.TemplateSettings, policy.Order, policy.Currency);

                return null;
            }
        }


        public async Task<Result<CustomerInfo>> GetCustomer(int companyId, int branchId, int customerId)
        {
            var customer = await _customerContext.GetCustomer();
            var (_, failure, error) = ChechCompanyAndBranch(customer, companyId, branchId);
            if (failure)
                return Result.Fail<CustomerInfo>(error);

            // TODO this needs to be reworked when customers will be able to belong to more than one branch within a company
            var foundCustomer = await (
                    from cr in _context.CustomerCompanyRelations
                    join c in _context.Customers
                        on cr.CustomerId equals c.Id
                    join co in _context.Companies
                        on cr.CompanyId equals co.Id
                    where (branchId == default ? cr.CompanyId == companyId : cr.BranchId == branchId)
                        && cr.CustomerId == customerId
                    select (CustomerInfo?) new CustomerInfo(c.Id, c.FirstName, c.LastName, c.Email, c.Title, c.Position, co.Id, co.Name, cr.BranchId,
                        cr.Type == CustomerCompanyRelationTypes.Master, cr.InCompanyPermissions))
                .SingleOrDefaultAsync();

            if (foundCustomer == null)
                return Result.Fail<CustomerInfo>("Customer not found in specified company or branch");

            return Result.Ok(foundCustomer.Value);
        }


        public async Task<Result<List<InCompanyPermissions>>> UpdateCustomerPermissions(int companyId, int branchId, int customerId, 
            List<InCompanyPermissions> newPermissionsList)
        {
            var customer = await _customerContext.GetCustomer();
            bool restrictWithSameBranch = false;

            if (!customer.InCompanyPermissions.HasFlag(InCompanyPermissions.PermissionManagementInBranch)
            && !customer.InCompanyPermissions.HasFlag(InCompanyPermissions.PermissionManagementInCompany))
                return Result.Fail<List<InCompanyPermissions>>("Permission to update customers permissions denied");

            if (!customer.InCompanyPermissions.HasFlag(InCompanyPermissions.PermissionManagementInCompany))
                restrictWithSameBranch = true;

            var (_, failure, error) = ChechCompanyAndBranch(customer, companyId, restrictWithSameBranch ? branchId : default);
            if (failure)
                return Result.Fail<List<InCompanyPermissions>>(error);

            var newPermissions = newPermissionsList.Aggregate((p1, p2) => p1 | p2);

            var relationToUpdate = await _context.CustomerCompanyRelations.Where(
                    r => r.CustomerId == customerId && r.CompanyId == companyId && r.BranchId == branchId)
                .SingleOrDefaultAsync();

            if (relationToUpdate == null)
                return Result.Fail<List<InCompanyPermissions>>("Customer not found in specified company or branch");

            relationToUpdate.InCompanyPermissions = newPermissions;

            _context.CustomerCompanyRelations.Update(relationToUpdate);
            await _context.SaveChangesAsync();

            return Result.Ok(relationToUpdate.InCompanyPermissions.ToList());
        }


        private Result ChechCompanyAndBranch(CustomerInfo customer, int companyId, int branchId)
        {
            if (customer.CompanyId != companyId)
                return Result.Fail("The customer isn't affiliated with the company");

            // TODO When branch system gets ierarchic, this needs to be changed so that customer can see customers/markups of his own branch and its subbranches
            if (branchId != default && customer.BranchId != branchId)
                return Result.Fail("The customer isn't affiliated with the branch");

            return Result.Ok();
        }


        private async ValueTask<Result> Validate(CustomerRegistrationInfo customerRegistration, string externalIdentity)
        {
            var fieldValidateResult = GenericValidator<CustomerRegistrationInfo>.Validate(v =>
            {
                v.RuleFor(c => c.Title).NotEmpty();
                v.RuleFor(c => c.FirstName).NotEmpty();
                v.RuleFor(c => c.LastName).NotEmpty();
            }, customerRegistration);

            if (fieldValidateResult.IsFailure)
                return fieldValidateResult;

            return await CheckIdentityIsUnique(externalIdentity);
        }


        private async Task<Result> CheckIdentityIsUnique(string identity)
        {
            return await _context.Customers.AnyAsync(c => c.IdentityHash == HashGenerator.ComputeSha256(identity))
                ? Result.Fail("User is already registered")
                : Result.Ok();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICustomerContext _customerContext;
    }
}