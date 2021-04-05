using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HappyTravel.Edo.NotificationCenter.Services.Hub
{
    public class SignalRSender : Hub<INotificationCenter>
    {
        public Task FireNotificationAddedEvent(int userId, int messageId, string message)
            => Clients.Group(BuildUserGroupName(userId)).NotificationAdded(messageId, message);


        public Task FireSearchStateChangedEvent(Guid searchId) 
            => Clients.Group($"search-{searchId}").SearchStateChanged();


        public Task Join(string roomName) 
            => Groups.AddToGroupAsync(Context.ConnectionId, roomName);


        public Task Leave(string roomName) 
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);


        private static string BuildUserGroupName(int userId) 
            => $"user-{userId}";
    }
}