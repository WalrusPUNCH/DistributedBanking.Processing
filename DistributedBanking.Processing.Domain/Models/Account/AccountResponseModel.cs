using Shared.Data.Entities.Constants;

namespace DistributedBanking.Processing.Domain.Models.Account;

public class AccountResponseModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required AccountType Type { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required double Balance { get; set; }
    public required DateTime ExpirationDate { get; set; }
    public required string SecurityCode { get; set; }
}