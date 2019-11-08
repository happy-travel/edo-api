using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Users;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public interface ICustomerContext
    {
        ValueTask<Result<CustomerInfo>> GetCustomerInfo();
        Task<Result<UserInfo>> GetUserInfo();
    }
}
