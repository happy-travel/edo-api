using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HappyTravel.Edo.NotificationCenter.Services.Hubs
{
    public class SearchHub : Hub<ISearchHub>
    {
        public Task FireSearchStateChangedEvent(Guid searchId) 
            => Clients.Group($"{GroupNamePrefix}-{searchId}").SearchStateChanged();


        public Task Join(string groupName) 
            => Groups.AddToGroupAsync(Context.ConnectionId, groupName);


        public Task Leave(string groupName) 
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        
        private const string GroupNamePrefix = "search";
    }
}