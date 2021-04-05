using System;
using System.Threading.Tasks;

namespace HappyTravel.Edo.NotificationCenter.Services.Hub
{
    public interface INotificationCenter
    {
        Task NotificationAdded(int messageId, string message);
        Task SearchStateChanged();
    }
}