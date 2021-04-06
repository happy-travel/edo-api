using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums.Notifications;
using HappyTravel.Edo.Common.Models.Notifications;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.NotificationCenter.Models;
using HappyTravel.Edo.NotificationCenter.Services.Hubs;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.NotificationCenter.Services.Notification
{
    public class NotificationService : INotificationService
    {
        public NotificationService(EdoContext context, NotificationCenterHub notificationCenterHub)
        {
            _context = context;
            _notificationCenterHub = notificationCenterHub;
        }
        
        public async Task Add(Models.Notification notification)
        {
            var entry = _context.Notifications.Add(new Data.Notifications.Notification
            {
                UserId = notification.UserId,
                Message = notification.Message,
                SendingSettings = notification.SendingSettings,
                Created = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var tasks = new List<Task>();

            foreach (var (protocol, settings) in notification.SendingSettings)
            {
                var task = protocol switch
                {
                    ProtocolTypes.Email when settings is EmailSettings emailSettings 
                        => SendEmail(emailSettings),
                    
                    ProtocolTypes.WebSocket when settings is WebSocketSettings webSocketSettings 
                        => SendWebSocket(notification.UserId, entry.Entity.Id, notification.Message),
                    
                    _ => throw new ArgumentException($"Unsupported protocol '{protocol}' or incorrect settings type")
                };
                
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }


        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications
                .SingleOrDefaultAsync(n => n.Id == notificationId && !n.IsRead);

            if (notification is not null)
            {
                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
            }
        }


        public Task<List<SlimNotification>> GetNotifications(int userId, int top, int skip)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId)
                .Take(top)
                .Skip(skip)
                .Select(n => new SlimNotification
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    Created = n.Created,
                    IsRead = n.IsRead
                })
                .ToListAsync();
        }


        private Task SendEmail(EmailSettings settings)
        {
            // TODO: implement sending e-mails
            return Task.CompletedTask;
        }


        private Task SendWebSocket(int userId, int messageId, string message) 
            => _notificationCenterHub.FireNotificationAddedEvent(userId, messageId, message);


        private readonly NotificationCenterHub _notificationCenterHub;
        private readonly EdoContext _context;
    }
}