using Shared.Data.Entities.Constants;

namespace DistributedBanking.Processing.Domain.Models.Transaction;

public class TransactionResponseModel
{
    public required string SourceAccountId { get; set; }
    public string? DestinationAccountId { get; set; }
    public required TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }
}