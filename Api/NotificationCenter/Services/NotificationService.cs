using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Notifications.Enums;
using HappyTravel.Edo.Notifications.Models;
using HappyTravel.Edo.Data;
using Microsoft.EntityFrameworkCore;
using HappyTravel.Edo.Api.NotificationCenter.Hubs;
using HappyTravel.Edo.Api.NotificationCenter.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using HappyTravel.Edo.Api.Infrastructure;

namespace HappyTravel.Edo.Api.NotificationCenter.Services
{
    public class NotificationService : INotificationService
    {
        public NotificationService(EdoContext context, 
            IHubContext<AgentNotificationHub, INotificationClient> agentNotificationHub,
            IHubContext<AdminNotificationHub, INotificationClient> adminNotificationHub,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _agentNotificationHub = agentNotificationHub;
            _adminNotificationHub = adminNotificationHub;
            _dateTimeProvider = dateTimeProvider;
        }
        

        public async Task Add(Notifications.Models.Notification notification)
        {
            var entry = _context.Notifications.Add(new Data.Notifications.Notification
            {
                Receiver = notification.Receiver,
                UserId = notification.UserId,
                AgencyId = notification.AgencyId,
                Message = notification.Message,
                Type = notification.Type,
                SendingSettings = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(notification.SendingSettings, new(JsonSerializerDefaults.Web))),
                Created = _dateTimeProvider.UtcNow()
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
                        => notification.Receiver switch 
                        {
                            ReceiverTypes.AgentApp
                                => SendMessageToAgent(notification.UserId, notification.AgencyId, entry.Entity.Id, notification.Message),
                            
                            ReceiverTypes.AdminPanel
                                => SendMessageToAdmin(notification.UserId, entry.Entity.Id, notification.Message),

                            _ => throw new ArgumentException($"Unsupported receiver '{notification.Receiver}' for notification")
                        },
                    
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


        public async Task<List<SlimNotification>> GetNotifications(ReceiverTypes receiver, int userId, int top, int skip)
        {
            return await _context.Notifications
                .Where(n => n.Receiver == receiver && n.UserId == userId)
                .Take(top)
                .Skip(skip)
                .Select(n => new SlimNotification
                {
                    Receiver = n.Receiver,
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    Type = n.Type,
                    Created = n.Created,
                    IsRead = n.IsRead
                })
                .ToListAsync();
        }


        private Task SendEmail(EmailSettings settings)
        {
            // TODO: Sending e-mails will be implemented later in task AA-128
            return Task.CompletedTask;
        }


        private async Task SendMessageToAgent(int userId, int? agencyId, int messageId, JsonDocument message)
        {
            await _agentNotificationHub.Clients
                .Group($"{agencyId}-{userId}")
                .ReceiveMessage(messageId, message);
        }


        private async Task SendMessageToAdmin(int userId, int messageId, JsonDocument message)
        {
            await _adminNotificationHub.Clients
                .Group($"admin-{userId}")
                .ReceiveMessage(messageId, message);
        }


        private readonly EdoContext _context;
        private readonly IHubContext<AgentNotificationHub, INotificationClient> _agentNotificationHub;
        private readonly IHubContext<AdminNotificationHub, INotificationClient> _adminNotificationHub;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}