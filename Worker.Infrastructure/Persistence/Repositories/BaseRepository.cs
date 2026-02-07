using Dapper;
using System.Collections;
using Worker.Domain.Entities;
using Worker.Domain.Repositories;

namespace Worker.Infrastructure.Persistence.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly DbSession _session;
        protected readonly string _tableName;

        protected BaseRepository(DbSession session)
        {
            _session = session;
            _tableName = typeof(T).Name + "s";
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Id = @Id AND IsDeleted = 0";
            return await _session.Connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id }, _session.Transaction);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var sql = $"SELECT * FROM {_tableName} WHERE IsDeleted = 0";
            return await _session.Connection.QueryAsync<T>(sql,null, transaction: _session.Transaction);
        }

        public async Task<bool> AddAsync(T entity)
        {
            var properties = GetProperties(entity);
            var columns = string.Join(", ", properties);
            var parameters = string.Join(", ", properties.Select(p => "@" + p));

            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters})";

            var rows = await _session.Connection.ExecuteAsync(sql, entity, _session.Transaction);

            return rows > 0;
        }

        //Generic Generator to insert
        public async Task<bool> UpdateAsync(T entity)
        {
            var properties = GetProperties(entity);
            var updates = string.Join(", ", properties.Select(p => $"{p} = @{p}"));

            var sql = $"UPDATE {_tableName} SET {updates} WHERE Id = @Id";

            var rows = await _session.Connection.ExecuteAsync(sql, entity, _session.Transaction);

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var sql = $"UPDATE {_tableName} SET IsDeleted = 1, UpdateAt = GETUTCDATE() WHERE Id = @Id";

            var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, _session.Transaction);

            return rows > 0;
        }

        // Helper method to get properties via Reflection (excluding Id and lists)
        private static List<string> GetProperties(T entity)
        {
            return typeof(T).GetProperties()
                .Where(p => p.Name != "Id" && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
                .Select(p => p.Name)
                .ToList();
        }
    }
}
