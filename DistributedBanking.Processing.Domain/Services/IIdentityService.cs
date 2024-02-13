using Contracts.Models;
using DistributedBanking.Processing.Domain.Models.Identity;

namespace DistributedBanking.Processing.Domain.Services;

public interface IIdentityService
{
    Task<OperationResult> RegisterUser(EndUserRegistrationModel registrationModel, string role);
    Task<OperationResult> DeleteUser(string email);
    Task<OperationResult> UpdateCustomerPersonalInformation(string customerId, CustomerPassportModel customerPassport);
}