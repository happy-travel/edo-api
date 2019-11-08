using System.Linq;
using HappyTravel.Edo.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Infrastructure.DatabaseExtentions
{
    public static class ContextExtentions
    {
        public static void Detach<TEntity>(this EdoContext context, int id)
            where TEntity : class, IEntity
        {
            var local = context.Set<TEntity>()
                .Local
                .FirstOrDefault(entry => entry.Id.Equals(id));
            if (local != null)
            {
                context.Entry(local).State = EntityState.Detached;
            }
        }
        
        public static void Detach<TEntity>(this EdoContext context, TEntity entity)
            where TEntity : class, IEntity
        {
            Detach<TEntity>(context, entity.Id);
        }
    }
}
