using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Abstractions;
using System.Linq.Expressions;
using PetAdoption.Infrastructure.Data;

namespace PetAdoption.Infrastructure.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _set;

        public GenericRepository(AppDbContext db)
        {
            _db = db; _set = db.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

        public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> q = _set.AsNoTracking(); 
            if (predicate != null) q = q.Where(predicate);
            foreach (var inc in includes) q = q.Include(inc);
            return await q.ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _set.AddAsync(entity);
            return entity;
        }

        public Task UpdateAsync(T entity)
        {
            _set.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            _set.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}