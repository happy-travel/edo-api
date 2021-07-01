using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data.Management;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public interface IAdministratorRolesAssignmentService
    {
        Task<Result> SetAdministratorRoles(int administratorId, List<int> roleIds, Administrator initiator);
    }
}