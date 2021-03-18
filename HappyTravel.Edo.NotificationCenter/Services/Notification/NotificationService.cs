using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums.Notifications;
using HappyTravel.Edo.Common.Models.Notifications;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.NotificationCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.NotificationCenter.Services.Notification
{
    public class NotificationService : INotificationService
    {
        public NotificationService(EdoContext context, Hub.SignalRSender signalRSender)
        {
            _context = context;
            _signalRSender = signalRSender;
        }
        
        public async Task Add(Models.Notification notification)
        {
            var entry = _context.Notifications.Add(new Data.Notifications.Notification
            {
                UserId = notification.UserId,
                Message = notification.Message,
                Protocols = notification.Protocols,
                EmailSettings = JsonSerializer.Serialize(notification.EmailSettings),
                Created = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            if (notification.Protocols.Contains(ProtocolTypes.WebSocket))
                await _signalRSender.FireNotificationAddedEvent(notification.UserId, entry.Entity.Id, notification.Message);

            if (notification.Protocols.Contains(ProtocolTypes.Email) && notification.EmailSettings.HasValue)
                await SendEmail(notification.EmailSettings.Value);
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


        public Task<List<NotificationSlim>> GetNotifications(int userId, int top, int skip)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId)
                .Take(top)
                .Skip(skip)
                .Select(n => new NotificationSlim
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


        private readonly Hub.SignalRSender _signalRSender;
        private readonly EdoContext _context;
    }
}