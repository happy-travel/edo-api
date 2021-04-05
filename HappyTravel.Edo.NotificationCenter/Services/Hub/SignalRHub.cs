using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HappyTravel.Edo.NotificationCenter.Services.Hub
{
    public class SignalRHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public Task SendEventToGroup(string groupName, string eventName, params string[] args) 
            => Clients.Group(groupName).SendAsync(eventName, args);


        public Task Join(string groupName) 
            => Groups.AddToGroupAsync(Context.ConnectionId, groupName);


        public Task Leave(string groupName) 
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}