using System;
using System.Collections.Generic;
using System.Linq;
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


        public Task Release<TEntity>(string entityId) => _context.RemoveEntityLock(GetEntityDescriptor<TEntity>(entityId));


        private async Task<Result> Acquire<TEntity>(string entityId, string locker)
        {
            var entityDescriptor = GetEntityDescriptor<TEntity>(entityId);
            var token = Guid.NewGuid().ToString();

            var lockTaken = await GetRetryPolicy()
                .ExecuteAsync(() => _context.TryAddEntityLock(entityDescriptor, locker, token));

            if (lockTaken)
                return Result.Ok();

            _logger.LogEntityLockFailed($"Failed to lock entity {typeof(TEntity).Name} with id: {entityId}");

            return Result.Failure($"Failed to acquire lock for {typeof(TEntity).Name}");


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


        public Task ReleaseLocks(IList<IEntityLock> locksHolder) => Task.WhenAll(locksHolder.Select(l => l.Release(this)));


        public async Task<Result> AddLock<TEntity>(IList<IEntityLock> locksHolder, string entityId, string locker)
        {
            var (_, isFailure, error) = await Acquire<TEntity>(entityId, locker);
            if (isFailure)
                return Result.Failure(error);

            locksHolder.Add(new EntityLock<TEntity>(entityId));
            return Result.Success();
        }


        public Task<Result> AddEntityLock<TEntity>(IList<IEntityLock> locksHolder, TEntity entity, string locker) where TEntity : IEntity =>
            AddLock<TEntity>(locksHolder, entity.Id.ToString(), locker);


        private static string GetEntityDescriptor<TEntity>(string id) => $"{typeof(TEntity).Name}_{id}";

        private const int MinRetryPeriodMilliseconds = 20;
        private const int MaxRetryPeriodMilliseconds = 100;
        private const int MaxLockRetryCount = 20;

        private readonly EdoContext _context;
        private readonly ILogger<EntityLocker> _logger;
        private readonly Random _random = new Random();
    }
}