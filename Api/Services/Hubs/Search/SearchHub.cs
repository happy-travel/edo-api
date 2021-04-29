using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Hubs.Search
{
    [Authorize]
    public class SearchHub : Hub<ISearchHub>
    {
        public SearchHub(EdoContext context)
        {
            _context = context;
        }
        
        
        public override async Task OnConnectedAsync()
        {
            var identityId = Context.User?.FindFirstValue("sub");
            if (string.IsNullOrEmpty(identityId))
                return;

            var agentId = await _context.Agents
                .Where(a => a.IdentityHash == HashGenerator.ComputeSha256(identityId))
                .Select(a => a.Id)
                .SingleOrDefaultAsync();
                
            if (agentId == default)
                return;
            
            await Groups.AddToGroupAsync(Context.ConnectionId, agentId.ToString());
            await base.OnConnectedAsync();
        }


        private readonly EdoContext _context;
    }
}