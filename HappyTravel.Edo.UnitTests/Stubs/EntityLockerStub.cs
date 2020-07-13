using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;

namespace HappyTravel.Edo.UnitTests.Stubs
{
    class EntityLockerStub : IEntityLocker
    {
        public Task<Result> Acquire<TEntity>(string entityId, string locker) => Task.FromResult(Result.Success());

        public Task<EntityLock<TEntity>> CreateLock<TEntity>(string entityId, string locker) =>
            Task.FromResult(new EntityLock<TEntity>(true, entityId, string.Empty, this));

        public Task Release<TEntity>(string entityId) => Task.CompletedTask;
    }
}
