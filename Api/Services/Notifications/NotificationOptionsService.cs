using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Notifications.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Notifications;
using Microsoft.EntityFrameworkCore;
using HappyTravel.Edo.Notifications.Models;
using HappyTravel.Edo.Notifications.Infrastructure;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Notifications
{
    public class NotificationOptionsService : INotificationOptionsService
    {
        public NotificationOptionsService(EdoContext context)
        {
            _context = context;
        }


        public async Task<Result<SlimNotificationOptions>> GetNotificationOptions(int userId, ApiCallerTypes userType, int? agencyId, NotificationTypes notificationType)
        {
            var options = await GetOptions(userId, userType, agencyId, notificationType);

            return options is null 
                ? NotificationOptionsHelper.TryGetDefaultOptions(notificationType) 
                : new SlimNotificationOptions {EnabledProtocols = options.EnabledProtocols, IsMandatory = options.IsMandatory};
        }


        public async Task<Result> Update(int userId, ApiCallerTypes userType, int? agencyId, NotificationTypes notificationType, SlimNotificationOptions options)
        {
            return await Validate()
                .Bind(SaveOptions);


            Result<SlimNotificationOptions> Validate()
            {
                var defaultOptions = NotificationOptionsHelper.TryGetDefaultOptions(notificationType);
                if (defaultOptions.IsFailure)
                    return Result.Failure<SlimNotificationOptions>(defaultOptions.Error);

                if (defaultOptions.Value.IsMandatory && options.EnabledProtocols != default)
                    return Result.Failure<SlimNotificationOptions>($"Notification type '{notificationType}' is mandatory");

                return defaultOptions;
            }


            async Task<Result> SaveOptions(SlimNotificationOptions defaultOptions)
            {
                var entity = await GetOptions(userId, userType, agencyId, notificationType);

                if (entity is null)
                {
                    _context.NotificationOptions.Add(new NotificationOptions
                    {
                        UserId = userId,
                        UserType = userType,
                        AgencyId = agencyId,
                        EnabledProtocols = options.EnabledProtocols,
                        IsMandatory = defaultOptions.IsMandatory
                    });
                }
                else
                {
                    entity.EnabledProtocols = options.EnabledProtocols;
                    _context.Update(entity);
                }

                await _context.SaveChangesAsync();
                return Result.Success();
            }
        }


        private Task<NotificationOptions> GetOptions(int userId, ApiCallerTypes userType, int? agencyId, NotificationTypes notificationType) 
            => _context.NotificationOptions
                .SingleOrDefaultAsync(o => o.UserId == userId && o.UserType == userType && o.AgencyId == agencyId && o.Type == notificationType);


        private readonly EdoContext _context;
    }
}