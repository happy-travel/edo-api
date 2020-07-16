using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public readonly struct EntityLock<TEntity> : IEntityLock
    {
        private readonly string _entityId;


        public EntityLock(string entityId)
        {
            _entityId = entityId;
        }


        public Task Release(IEntityLocker entityLocker) => entityLocker.Release<TEntity>(_entityId);
    }
}