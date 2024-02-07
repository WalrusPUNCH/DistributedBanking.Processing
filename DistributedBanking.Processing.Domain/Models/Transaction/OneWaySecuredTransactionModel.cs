namespace DistributedBanking.Processing.Domain.Models.Transaction;

public class OneWaySecuredTransactionModel : OneWayTransactionModel
{
    public required string SecurityCode { get; set; }
}