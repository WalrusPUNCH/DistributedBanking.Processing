using Contracts;
using DistributedBanking.Processing.Domain.Models.Identity;

namespace DistributedBanking.Processing.Domain.Services;

public interface IIdentityService
{
    Task<IdentityOperationResult> RegisterUser(
        EndUserRegistrationModel registrationModel, string role);
    
    Task<OperationStatusModel> DeleteUser(string email);
    
    Task<OperationStatusModel> UpdateCustomerPersonalInformation(string customerId, CustomerPassportModel customerPassport);
}