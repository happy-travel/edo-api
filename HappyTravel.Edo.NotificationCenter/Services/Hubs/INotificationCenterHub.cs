using System.Threading.Tasks;

namespace HappyTravel.Edo.NotificationCenter.Services.Hubs
{
    public interface INotificationCenterHub
    {
        Task NotificationAdded(int messageId, string message);
    }
}