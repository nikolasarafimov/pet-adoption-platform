using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Infrastructure.Data;

namespace PetAdoption.Infrastructure.Repositories
{
    public class GenericRepository<T> : IRepository<T>
        where T : class
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _set;

        public GenericRepository(AppDbContext db)
        {
            _db = db;
            _set = db.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _set.FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            params Expression<Func<T, object>>[] includes
        )
        {
            IQueryable<T> query = _set.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _set.AddAsync(entity);

            return entity;
        }

        public Task UpdateAsync(T entity)
        {
            var entry = _db.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Modified;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            _set.Remove(entity);

            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}