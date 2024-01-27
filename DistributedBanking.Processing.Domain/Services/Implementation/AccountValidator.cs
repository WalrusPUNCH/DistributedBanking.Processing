using Shared.Data.Entities;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public static class AccountValidator
{
    public static bool IsAccountValid(AccountEntity account, string enteredSecurityCode)
    {
        return account.ExpirationDate > DateTime.UtcNow && string.Equals(enteredSecurityCode, account.SecurityCode);
    }
    
    public static bool IsAccountValid(AccountEntity account)
    {
        return account.ExpirationDate > DateTime.UtcNow;
    }
}