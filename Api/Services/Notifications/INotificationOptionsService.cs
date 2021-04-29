using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Notifications.Enums;
using HappyTravel.Edo.Notifications.Models;

namespace HappyTravel.Edo.Api.Services.Notifications
{
    public interface INotificationOptionsService
    {
        Task<Result<SlimNotificationOptions>> GetNotificationOptions(int userId, ApiCallerTypes userType, int? agencyId, NotificationTypes notificationType);

        Task<Result> Update(int userId, ApiCallerTypes userType, int? agencyId, NotificationTypes notificationType, SlimNotificationOptions options);
    }
}