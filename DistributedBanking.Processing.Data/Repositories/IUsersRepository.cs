using DistributedBanking.Processing.Data.Repositories.Base;
using Shared.Data.Entities.Identity;

namespace DistributedBanking.Processing.Data.Repositories;

public interface IUsersRepository : IRepositoryBase<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
}