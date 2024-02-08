using Contracts.Extensions;
using DistributedBanking.Processing.Data.Repositories;
using DistributedBanking.Processing.Domain.Models.Identity;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Shared.Data.Entities.Identity;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public class UserManager : IUserManager
{
    private readonly IUsersRepository _usersRepository;
    private readonly IRolesRepository _rolesRepository;
    private readonly IPasswordHashingService _passwordService;
    private readonly ILogger<UserManager> _logger;

    public UserManager(
        IUsersRepository usersRepository,
        IRolesRepository rolesManager, 
        IPasswordHashingService passwordService,
        ILogger<UserManager> logger)
    {
        _usersRepository = usersRepository;
        _rolesRepository = rolesManager;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<IdentityOperationResult> CreateAsync(string endUserId, EndUserRegistrationModel registrationModel, IEnumerable<string>? roles = null)
    {
        try
        {
            var roleNames = roles?.ToList();
            var roleIds = new List<string>();
            if (roleNames != null && roleNames.Any())
            {
                foreach (var roleName in roleNames)
                {
                    var roleId = (await _rolesRepository.GetAsync(r => r.NormalizedName == roleName.NormalizeString())).FirstOrDefault()?.Id;
                    if (roleId != null)
                    {
                        roleIds.Add(roleId.Value.ToString()!);
                    }
                }
            }

            var existingUser = await _usersRepository.GetByEmailAsync(registrationModel.Email);
            if (existingUser != null)
            {
                return IdentityOperationResult.Failed("User with the same email already exists");
            }
            
            var user = new ApplicationUser
            {
                Email = registrationModel.Email,
                NormalizedEmail = registrationModel.Email.NormalizeString(),
                PhoneNumber = registrationModel.PhoneNumber,
                PasswordHash = registrationModel.PasswordHash,
                PasswordSalt = registrationModel.Salt,
                CreatedOn = DateTime.UtcNow,
                Roles = roleIds,
                EndUserId = endUserId
            };

            await _usersRepository.AddAsync(user);
            
            return IdentityOperationResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while trying to create new user");
            return IdentityOperationResult.Failed();
        }
    }

    public async Task<UserModel?> GetByEmailAsync(string email)
    {
        try
        {
            var user = await _usersRepository.GetByEmailAsync(email);

            return user?.Adapt<UserModel>();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while trying to find user by email");

            return null;
        }
    }

    public async Task<UserModel?> GetByIdAsync(string id)
    {
        try
        {
            var user = await _usersRepository.GetAsync(new ObjectId(id));

            return user?.Adapt<UserModel>();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while trying to find user by email");

            return null;
        }
    }

    public async Task<IdentityOperationResult> PasswordSignInAsync(string email, string password)
    {
        try
        {
            var user = await _usersRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return IdentityOperationResult.Failed("User with such email doesn't exist");
            }
        
        
            var successfulSignIn = _passwordService.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);

            return successfulSignIn
                ? IdentityOperationResult.Success
                : IdentityOperationResult.Failed("Incorrect email or password");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while trying to sign in user");
            return IdentityOperationResult.Failed();
        }
    }

    public async Task<IEnumerable<string>> GetRolesAsync(ObjectId userId)
    {
        var user = await _usersRepository.GetAsync(userId);
        if (user == null || !user.Roles.Any())
        {
            return Array.Empty<string>();
        }

        var roleNames = new List<string>();
        foreach (var roleId in user.Roles)
        {
            var roleName = (await _rolesRepository.GetAsync(new ObjectId(roleId)))?.Name;
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                roleNames.Add(roleName);
            }
        }

        return roleNames;
    }

    public async Task<bool> IsInRoleAsync(ObjectId userId, string roleName)
    {
        var user = await _usersRepository.GetAsync(userId);
        if (user == null)
        {
            return false;
        }
        
        var roleId = (await _rolesRepository.GetAsync(r => r.NormalizedName == roleName.NormalizeString())).FirstOrDefault()?.Id;
        
        return roleId != null && user.Roles.Contains(roleId.Value.ToString());
    }

    public async Task<IdentityOperationResult> DeleteAsync(ObjectId userId)
    {
        try
        {
            await _usersRepository.RemoveAsync(userId);

            return IdentityOperationResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while trying to sign in user");
            return IdentityOperationResult.Failed();
        }
    }
}