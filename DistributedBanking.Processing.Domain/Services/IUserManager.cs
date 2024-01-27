using DistributedBanking.Processing.Domain.Models.Identity;
using MongoDB.Bson;

namespace DistributedBanking.Processing.Domain.Services;

public interface IUserManager
{
    Task<IdentityOperationResult> CreateAsync(string endUserId, EndUserRegistrationModel registrationModel, IEnumerable<string>? roles = null);
    Task<UserModel?> FindByEmailAsync(string email);
    Task<IdentityOperationResult> PasswordSignInAsync(string email, string password);
    Task<IEnumerable<string>> GetRolesAsync(ObjectId userId);
    Task<bool> IsInRoleAsync(ObjectId userId, string roleName);
    Task<IdentityOperationResult> DeleteAsync(ObjectId userId);
}
