using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices.Models;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public interface IAgencyDiscountManagementService
    {
        Task<List<DiscountInfo>> Get(int agencyId);
        
        Task<Result> Start(int agencyId, int discountId);
        
        Task<Result> Stop(int agencyId, int discountId);

        Task<Result> Add(int agencyId, CreateDiscountRequest discountInfo);

        Task<Result> Update(int agencyId, int discountId, EditDiscountRequest editDiscountRequest);
    }
}