using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Customers;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public interface IRegistrationService
    {
        ValueTask<Result> RegisterMasterCustomer(CompanyRegistrationInfo company, CustomerRegistrationInfo masterCustomer, string externalIdentity);
    }
}