using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ProductCheckerBack.Observers
{
    internal interface IObserver<TEntity>
    {
        void Updating(EntityEntry entityEntry);
        void Creating(EntityEntry entityEntry);
        void Updated(EntityEntry entityEntry);
        void Created(EntityEntry entityEntry);
    }
}