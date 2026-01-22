using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.Observers;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ProductCheckerBack
{
    public abstract class BaseDbContext : DbContext
    {
        private readonly List<dynamic> _observers = new List<dynamic>();
        private bool _savingChanges = false;

        public BaseDbContext() : base()
        {
            RegisterObservers();
        }

        private void RegisterObservers()
        {
            var entityTypes = GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var observerType = typeof(BaseObserver<>).MakeGenericType(entityType);
                var observerInstances = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.IsSubclassOf(observerType) && !t.IsAbstract)
                    .Select(t => Activator.CreateInstance(t, this));

                foreach (var observer in observerInstances)
                {
                    _observers.Add(observer);
                }
            }
        }

        public override int SaveChanges()
        {
            if (_savingChanges)
            {
                return 0;
            }

            _savingChanges = true;
            var trackedEntities = new List<EntityEntry>();
            EntityEntry entityEntry = null;
            while ((entityEntry = ChangeTracker.Entries().Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && trackedEntities.FirstOrDefault(trackedEntity => trackedEntity.Entity == e.Entity) == null).FirstOrDefault()) != null)
            {
                trackedEntities.Add(entityEntry);
                var entityType = entityEntry.Entity.GetType();
                var observerType = typeof(BaseObserver<>).MakeGenericType(entityType);
                var relevantObservers = _observers.Where(o => o.GetType().IsSubclassOf(observerType) && !o.GetType().IsAbstract);

                foreach (var observer in relevantObservers)
                {
                    if (entityEntry.State == EntityState.Added)
                    {
                        observer.Creating(entityEntry);
                    }
                    else if (entityEntry.State == EntityState.Modified)
                    {
                        observer.Updating(entityEntry);
                    }
                }
            }

            int result = base.SaveChanges();
            _savingChanges = false;

            foreach (var entityEntry2 in trackedEntities)
            {
                var entityType = entityEntry2.Entity.GetType();
                var observerType = typeof(BaseObserver<>).MakeGenericType(entityType);
                var relevantObservers = _observers.Where(o => o.GetType().IsSubclassOf(observerType) && !o.GetType().IsAbstract);

                foreach (var observer in relevantObservers)
                {
                    if (entityEntry2.State == EntityState.Added)
                    {
                        observer.Created(entityEntry2);
                    }
                    else if (entityEntry2.State == EntityState.Modified)
                    {
                        observer.Updated(entityEntry2);
                    }
                }
            }

            return result;
        }

        private IEnumerable<Type> GetEntityTypes()
        {
            return GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments().First())
                .ToList();
        }
    }
}