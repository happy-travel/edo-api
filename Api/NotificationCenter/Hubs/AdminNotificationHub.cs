using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.NotificationCenter.Hubs
{
    [Authorize]
    public class AdminNotificationHub : Hub<INotificationClient>
    {
        public AdminNotificationHub(EdoContext context)
        {
            _context = context;
        }


        public override async Task OnConnectedAsync()
        {
            var identityId = Context.User?.FindFirstValue("sub");
            if (string.IsNullOrEmpty(identityId))
                return;

            var adminId = await _context.Administrators
                .Where(a => a.IdentityHash == HashGenerator.ComputeSha256(identityId))
                .Select(a => a.Id)
                .SingleOrDefaultAsync();
            if (adminId == default)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(adminId));
            await base.OnConnectedAsync();
        }


        private static string BuildGroupName(int adminId)
            => $"admin-{adminId}";


        private readonly EdoContext _context;
    }
}