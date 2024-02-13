using Contracts.Models;
using DistributedBanking.Processing.Domain.Models.Identity;
using MongoDB.Bson;

namespace DistributedBanking.Processing.Domain.Services;

public interface IUserManager
{
    Task<OperationResult> CreateAsync(string endUserId, EndUserRegistrationModel registrationModel, IEnumerable<string>? roles = null);
    Task<UserModel?> GetByEmailAsync(string email);
    Task<UserModel?> GetByIdAsync(string id);
    Task<OperationResult> PasswordSignInAsync(string email, string password);
    Task<IEnumerable<string>> GetRolesAsync(ObjectId userId);
    Task<bool> IsInRoleAsync(ObjectId userId, string roleName);
    Task<OperationResult> DeleteAsync(ObjectId userId);
}
