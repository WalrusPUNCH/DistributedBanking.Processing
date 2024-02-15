using Contracts.Extensions;
using Contracts.Models;
using DistributedBanking.Processing.Data.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Data.Entities.Identity;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public class RolesManager : IRolesManager
{
    private readonly IRolesRepository _rolesRepository;
    private readonly ILogger<RolesManager> _logger;

    public RolesManager(
        IRolesRepository rolesRepository,
        ILogger<RolesManager> logger)
    {
        _rolesRepository = rolesRepository;
        _logger = logger;
    }
    
    public async Task<OperationResult> CreateAsync(ApplicationRole role)
    {
        try
        {
            if (await RoleExists(role.NormalizedName))
            {
                return OperationResult.BadRequest("Role with the same name already exists");
            }

            await _rolesRepository.AddAsync(role);
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception occurred while trying to create a new role");
            
            return OperationResult.InternalFail("Error occurred while trying to create a new role");
        }
    }

    public async Task<bool> RoleExists(string roleName)
    {
        return (await _rolesRepository.GetAsync(r => r.NormalizedName == roleName.NormalizeString())).Any();
    }
}