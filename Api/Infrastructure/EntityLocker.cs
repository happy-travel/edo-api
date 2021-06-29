using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Data;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public class EntityLocker : IEntityLocker
    {
        public EntityLocker(EdoContext context, ILogger<EntityLocker> logger)
        {
            _context = context;
            _logger = logger;
        }


        public Task<Result> Acquire<TEntity>(string entityId, string lockerName) => Acquire(typeof(TEntity), entityId, lockerName);


        public async Task<Result> Acquire(Type entityType, string entityId, string lockerName)
        {
            var entityDescriptor = GetEntityDescriptor(entityType, entityId);
            var token = Guid.NewGuid().ToString();

            var lockTaken = await GetRetryPolicy()
                .ExecuteAsync(() => _context.TryAddEntityLock(entityDescriptor, lockerName, token));

            if (lockTaken)
                return Result.Success();

            _logger.LogEntityLockFailed(entityType.Name, entityId);

            return Result.Failure($"Failed to acquire lock for {entityType.Name}");


            AsyncRetryPolicy<bool> GetRetryPolicy()
            {
                return Policy
                    .HandleResult(false)
                    .WaitAndRetryAsync(MaxLockRetryCount, attemptNumber => GetRandomDelay());
            }


            TimeSpan GetRandomDelay()
                => TimeSpan.FromMilliseconds(_random.Next(MinRetryPeriodMilliseconds,
                    MaxRetryPeriodMilliseconds));
        }


        public Task Release<TEntity>(string entityId) => Release(typeof(TEntity), entityId);
        public Task Release(Type entityType, string entityId) => _context.RemoveEntityLock(GetEntityDescriptor(entityType, entityId));


        private static string GetEntityDescriptor(Type entityType, string id) => $"{entityType.Name}_{id}";

        private const int MinRetryPeriodMilliseconds = 20;
        private const int MaxRetryPeriodMilliseconds = 100;
        private const int MaxLockRetryCount = 20;

        private readonly EdoContext _context;
        private readonly ILogger<EntityLocker> _logger;
        private readonly Random _random = new Random();
    }
}