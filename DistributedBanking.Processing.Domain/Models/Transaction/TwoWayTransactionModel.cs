namespace DistributedBanking.Processing.Domain.Models.Transaction;

public class TwoWayTransactionModel
{
    public string SourceAccountId { get; set; }
    public required string SourceAccountSecurityCode { get; set; }
    public string DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}