using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public interface IEntityLock
    {
        Task Release(IEntityLocker entityLocker);
    }
}
