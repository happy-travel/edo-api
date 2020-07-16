using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public interface IEntityLocker
    {
        Task Release<TEntity>(string entityId);

        Task ReleaseLocks(IList<IEntityLock> locksHolder);

        Task<Result> AddLock<TEntity>(IList<IEntityLock> locksHolder, string entityId, string locker);

        Task<Result> AddEntityLock<TEntity>(IList<IEntityLock> locksHolder, TEntity entity, string locker) where TEntity : IEntity;
    }
}