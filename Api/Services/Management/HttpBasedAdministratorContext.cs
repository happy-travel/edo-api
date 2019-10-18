using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Management;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Management
{
    public class HttpBasedAdministratorContext : IAdministratorContext
    {
        public HttpBasedAdministratorContext(EdoContext context, ITokenInfoAccessor tokenInfoAccessor)
        {
            _context = context;
            _tokenInfoAccessor = tokenInfoAccessor;
        }

        public async Task<bool> HasPermission(AdministratorPermissions permission)
        {
            var (_, isFailure, administrator, _) = await GetCurrent();
            if (isFailure)
                return false;

            return await HasGlobalPermission(administrator, permission);
        }

        public async Task<Result<Administrator>> GetCurrent()
        {
            // TODO: replace with valid client_id-customer_id mapping
            var identity = _tokenInfoAccessor.GetIdentity() ?? _tokenInfoAccessor.GetClientId();
            if (string.IsNullOrWhiteSpace(identity))
                return Result.Fail<Administrator>("Identity is empty");

            var identityClaim = _tokenInfoAccessor.GetIdentity();
            if (!(identityClaim is null))
            {
                var identityHash = HashGenerator.ComputeHash(identityClaim);
                var administrator = await _context.Administrators
                    .SingleOrDefaultAsync(c => c.IdentityHash == identityHash);

                if(administrator != default)
                    return Result.Ok(administrator);
            }

            var clientIdClaim = _tokenInfoAccessor.GetClientId();
            if (!(clientIdClaim is null))
            {
#warning TODO: Remove this after implementing client-customer relation
                var administrator = await _context.Administrators
                    .SingleOrDefaultAsync(c => c.IdentityHash == clientIdClaim);

                if(administrator != default)
                    return Result.Ok(administrator);
            }

            return Result.Fail<Administrator>("Could not get administrator");
        }


        public async Task<UserInfo> GetUserInfo()
        {
            var (_, _, admin, _) = await GetCurrent();
            return admin == default
                ? default
                : new UserInfo(admin.Id, UserTypes.Admin);
        }


        private Task<bool> HasGlobalPermission(Administrator administrator, AdministratorPermissions permission)
        {
            // TODO: add employee roles
            return Task.FromResult(true);
        }
        
        private readonly EdoContext _context;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
    }
}