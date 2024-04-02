using MongoDB.Bson;
using Shared.Data.Entities;
using System.Linq.Expressions;

namespace DistributedBanking.Processing.Data.Repositories.Base;

public interface IRepositoryBase<T> where T : BaseEntity
{
    Task AddAsync(T entity, int priority = 50);
    Task<IReadOnlyCollection<T>> GetAllAsync();
    Task<T?> GetAsync(ObjectId id);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? filter = null);
    Task RemoveAsync(ObjectId id);
    Task UpdateAsync(T entity, int priority = 50);
}