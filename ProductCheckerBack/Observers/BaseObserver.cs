using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ProductCheckerBack.Observers
{
    internal abstract class BaseObserver<TEntity> : IObserver<TEntity>
    {
        protected ArtemisDbContext _artemisDbContext { get; set; }

        public BaseObserver(ArtemisDbContext artemisDbContext)
        {
            _artemisDbContext = artemisDbContext;
        }

        public void Creating(EntityEntry entityEntry)
        {
        }

        public void Updating(EntityEntry entityEntry)
        {
        }

        public void Created(EntityEntry entityEntry)
        {
        }

        public void Updated(EntityEntry entityEntry)
        {
        }
    }
}