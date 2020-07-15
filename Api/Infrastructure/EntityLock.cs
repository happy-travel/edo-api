using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public readonly struct EntityLock<TEntity> : IAsyncDisposable
    {
        public EntityLock(bool acquired, string entityId, string error, IEntityLocker entityLocker)
        {
            Acquired = acquired;
            _entityId = entityId;
            Error = error;
            _entityLocker = entityLocker;
        }


        public async ValueTask DisposeAsync() =>
            await (Acquired
                ? _entityLocker.Release<TEntity>(_entityId)
                : Task.CompletedTask);


        public bool Acquired { get; }
        public string Error { get; }

        private readonly string _entityId;
        private readonly IEntityLocker _entityLocker;
    }
}
