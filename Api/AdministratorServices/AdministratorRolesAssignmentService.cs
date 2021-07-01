using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Management;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AdministratorRolesAssignmentService : IAdministratorRolesAssignmentService
    {
        public AdministratorRolesAssignmentService(EdoContext context,
            IManagementAuditService managementAuditService)
        {
            _context = context;
            _managementAuditService = managementAuditService;
        }


        public async Task<Result> SetAdministratorRoles(int administratorId, List<int> roleIds, Administrator initiator)
        {
            return await GetAdministrator()
                .Ensure(AllProvidedRolesExist, "Some of specified roles do not exist")
                .BindWithTransaction(_context, a => Result.Success(a)
                    .Tap(SetRoles)
                    .Bind(WriteAuditLog));


            async Task<Result<Administrator>> GetAdministrator()
            {
                var admin = await _context.Administrators
                    .SingleOrDefaultAsync(a => a.Id == administratorId);

                return admin is null
                    ? Result.Failure<Administrator>($"Could not find administrator with id {administratorId}")
                    : Result.Success(admin);
            }


            async Task<bool> AllProvidedRolesExist(Administrator _)
            {
                var allRolesIds = await _context.AdministratorRoles.Select(r => r.Id).ToListAsync();
                return roleIds.All(allRolesIds.Contains);
            }


            async Task SetRoles(Administrator assignee)
            {
                assignee.AdministratorRoleIds = roleIds.ToArray();

                _context.Administrators.Update(assignee);
                await _context.SaveChangesAsync();
            }


            Task<Result> WriteAuditLog(Administrator assignee)
                => _managementAuditService.Write(ManagementEventType.AdministratorRolesAssignment,
                    new AdministratorRoleAssignmentEventData(
                        initiatorAdministratorId: initiator.Id,
                        assigneeAdministratorId: assignee.Id,
                        newRoleIds: roleIds));
        }


        private readonly EdoContext _context;
        private readonly IManagementAuditService _managementAuditService;
    }
}