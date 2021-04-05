using System.Threading.Tasks;

namespace HappyTravel.Edo.NotificationCenter.Services.Hub
{
    public interface IHub
    {
        Task NotificationAdded(int messageId, string message);
        Task SearchStateChanged();
    }
}