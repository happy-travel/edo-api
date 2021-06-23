﻿using System;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using HappyTravel.Edo.Api.AdministratorServices.Models;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Data.Agents;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AgentRolesService : IAgentRolesService
    {
        public AgentRolesService(EdoContext context)
        {
            _context = context;
        }


        public Task<List<AgentRoleInfo>> GetAllRoles()
            => _context.AgentRoles.Select(r => r.ToAgentRoleInfo()).ToListAsync();


        public Task<Result> Add(AgentRoleInfo roleInfo)
        {
            return Validate(roleInfo)
                .Ensure(IsUnique, "A role with the same name or permission set already exists")
                .Tap(Add);


            async Task<bool> IsUnique()
                => !await _context.AgentRoles.AnyAsync(r => r.Name.Equals(roleInfo.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    r.Permissions == roleInfo.Permissions);


            Task Add()
            {
                _context.AgentRoles.Add(roleInfo.ToAgentRole());
                return _context.SaveChangesAsync();
            }
        }


        public async Task<Result> Edit(int roleId, AgentRoleInfo roleInfo)
        {
            return await Validate(roleInfo)
                .Ensure(IsUnique, "A role with the same name or permission set already exists")
                .Bind(() => Get(roleId))
                .Tap(Edit);
                

            async Task<bool> IsUnique()
                => !await _context.AgentRoles.AnyAsync(r => (r.Name.Equals(roleInfo.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    r.Permissions == roleInfo.Permissions) && r.Id != roleId);


            Task Edit(AgentRole role)
            {
                role.Name = roleInfo.Name;
                role.Permissions = roleInfo.Permissions;

                _context.Update(role);
                return _context.SaveChangesAsync();
            }
        }


        public async Task<Result> Delete(int roleId)
        {
            return await Get(roleId)
                .Ensure(IsUnused, "This role is in use and connot be deleted")
                .Tap(Delete);


            async Task<bool> IsUnused(AgentRole _)
                => !await _context.AgentAgencyRelations.AnyAsync(r => r.AgentRoleIds.Contains(roleId));


            Task Delete(AgentRole role)
            {
                _context.AgentRoles.Remove(role);
                return _context.SaveChangesAsync();
            }
        }


        private Result Validate(AgentRoleInfo roleInfo)
            => GenericValidator<AgentRoleInfo>.Validate(v =>
                {
                    v.RuleFor(r => r.Name).NotEmpty();
                    v.RuleFor(r => r.Permissions.ToList()).NotEmpty();
                },
                roleInfo);


        private async Task<Result<AgentRole>> Get(int roleId)
            => await _context.AgentRoles.SingleOrDefaultAsync(r => r.Id == roleId)
                ?? Result.Failure<AgentRole>("A role with specified Id does not exist");


        private readonly EdoContext _context;
    }
}
