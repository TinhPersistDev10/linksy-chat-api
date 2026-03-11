using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly LinksyDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(LinksyDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);
            var totalCount = await query.CountAsync();
            if (orderBy != null)
                query = orderBy(query);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        #region Query methods
        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }
        public virtual async Task<T?> GetByIdAsync(params object[] keyValues)
        {
            return await _dbSet.FindAsync(keyValues);
        }
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate)
        {
            return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
        }
        #endregion
        #region Command Methods
        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }
        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }
        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }
        public IQueryable<T> QueryAsNoTracking()
        {
            return _dbSet.AsNoTracking();
        }

        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }
        #endregion
    }
}