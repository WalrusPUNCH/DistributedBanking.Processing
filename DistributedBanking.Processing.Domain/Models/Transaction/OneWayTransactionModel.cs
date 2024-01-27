namespace DistributedBanking.Processing.Domain.Models.Transaction;

public class OneWayTransactionModel
{
    public string SourceAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}