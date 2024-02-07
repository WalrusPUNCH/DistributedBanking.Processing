namespace DistributedBanking.Processing.Domain.Models.Transaction;

public class TwoWayTransactionModel
{
    public required string SourceAccountId { get; set; }
    public required string SourceAccountSecurityCode { get; set; }
    public required string DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}