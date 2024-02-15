using Contracts.Models;
using DistributedBanking.Processing.Data.Repositories;
using DistributedBanking.Processing.Domain.Models.Identity;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.EndUsers;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public class IdentityService : IIdentityService
{
    private readonly IUserManager _usersManager;
    private readonly ICustomersRepository _customersRepository;
    private readonly IWorkersRepository _workersRepository;
    private readonly IAccountService _accountService;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        IUserManager userManager,
        ICustomersRepository customersRepository,
        IWorkersRepository workersRepository,
        IAccountService accountService,
        ILogger<IdentityService> logger)
    {
        _usersManager = userManager;
        _customersRepository = customersRepository;
        _workersRepository = workersRepository;
        _accountService = accountService;
        _logger = logger;
    }
    
    public async Task<OperationResult> RegisterUser(
        EndUserRegistrationModel registrationModel, string role)
    {
        return await RegisterUserInternal(registrationModel, role);
    }
    
    private async Task<OperationResult> RegisterUserInternal(EndUserRegistrationModel registrationModel, string role)
    {
        var existingUser = await _usersManager.GetByEmailAsync(registrationModel.Email);
        if (existingUser != null)
        {
            return OperationResult.BadRequest("User with the same email already exists");
        }
        
        ObjectId endUserId;
        if (string.Equals(role, RoleNames.Customer, StringComparison.InvariantCultureIgnoreCase))
        {
            var customerEntity = registrationModel.Adapt<CustomerEntity>();
            await _customersRepository.AddAsync(customerEntity);

            endUserId = customerEntity.Id;
        }
        else if (string.Equals(role, RoleNames.Worker, StringComparison.InvariantCultureIgnoreCase))
        {
            var workerEntity = registrationModel.Adapt<WorkerEntity>();
            await _workersRepository.AddAsync(workerEntity);
            
            endUserId = workerEntity.Id;
        }
        else if (string.Equals(role, RoleNames.Administrator, StringComparison.InvariantCultureIgnoreCase))
        {
            var workerEntity = registrationModel.Adapt<WorkerEntity>();
            await _workersRepository.AddAsync(workerEntity);
            
            endUserId = workerEntity.Id;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Specified role is not supported");
        }
        
        var userCreationResult = await _usersManager.CreateAsync(endUserId.ToString()!, registrationModel, new []{ role });
        if (userCreationResult.Status != OperationStatus.Success)
        {
            return userCreationResult;
        }
        
        _logger.LogInformation("New user '{Email}' has been registered and assigned a '{Role}' role",
            registrationModel.Email, role);
            
        return userCreationResult;
    }
    
    public async Task<OperationResult> DeleteUser(string id)
    {
        var appUser = await _usersManager.GetByIdAsync(id);
        if (appUser == null)
        {
            return OperationResult.BadRequest("Specified user does not exist");
        }

        if (await _usersManager.IsInRoleAsync(appUser.Id, RoleNames.Customer))
        {
            var customer = await _customersRepository.GetAsync(new ObjectId(appUser.EndUserId));
            if (customer == null)
            {
                _logger.LogError("Customer with the end ID {Id} specified in service user does not exist", appUser.EndUserId);
                return OperationResult.InternalFail("Error occured while trying to delete user. Try again later");
            }
            foreach (var customerAccountId in customer.Accounts)
            {
                await _accountService.DeleteAsync(customerAccountId);
            }
                
            await _customersRepository.RemoveAsync(new ObjectId(appUser.EndUserId));
        }
        else
        {
            await _workersRepository.RemoveAsync(new ObjectId(appUser.EndUserId));
        }
            
        await _usersManager.DeleteAsync(appUser.Id);
        
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateCustomerPersonalInformation(string customerId, CustomerPassportModel customerPassport)
    {
        try
        {
            var customer = await _customersRepository.GetAsync(new ObjectId(customerId));
            if (customer == null)
            {
                _logger.LogWarning("Customer with id {Id} does not exist", customerId);
                return OperationResult.BadRequest("Customer with the specified ID does not exist");
            }

            customer.Passport = customerPassport.Adapt<CustomerPassport>();
            await _customersRepository.UpdateAsync(customer);
            
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while trying to update personal information for customer with ID {Id}",
                customerId);
            throw;
        }
    }
}