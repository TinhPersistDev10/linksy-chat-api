using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace linksy_backend_api.Repositories
{
    public interface IRepository<T> where T : class
    {
        //Query 
        #region Query methods
        IQueryable<T> Query();
        Task<T?> GetByIdAsync(params object[] keyValue);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllAsync();

        //Kiểm tra danh sach entities thỏa điều kiện
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        #endregion

        #region Command Methods
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void Delete(T entity);
        // Query with includes
        void RemoveRange(IEnumerable<T> entities);
        IQueryable<T> QueryAsNoTracking();
        // Paging
        Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

        // Command
        #endregion
    }
}