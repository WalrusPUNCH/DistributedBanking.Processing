using Shared.Data.Entities.Constants;

namespace DistributedBanking.Processing.Domain.Models.Account;

public class AccountModel
{
    public required string Name { get; set; }
    public AccountType Type { get; set; }
    public double Balance { get; set; }
    public DateTime ExpirationDate { get; set; }
    public required string SecurityCode { get; set; }
    public string? Owner { get; set; }
    public DateTime CreatedAt { get; set; }
}