using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HappyTravel.Edo.NotificationCenter.Services.Hubs
{
    public class NotificationCenterHub : Hub<INotificationCenterHub>
    {
        public Task FireNotificationAddedEvent(int userId, int messageId, string message) 
            => Clients.Group($"{GroupNamePrefix}-{userId}").NotificationAdded(messageId, message);


        public Task Join(string groupName) 
            => Groups.AddToGroupAsync(Context.ConnectionId, groupName);


        public Task Leave(string groupName) 
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        
        private const string GroupNamePrefix = "notifications";
    }
}