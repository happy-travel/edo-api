using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public interface IEntityLocker
    {
        Task Release<TEntity>(string entityId);

        Task<EntityLock<TEntity>> CreateLock<TEntity>(string entityId, string locker);
    }
}