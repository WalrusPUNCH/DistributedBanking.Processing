namespace DistributedBanking.Processing.Domain.Models.Identity;

public class AddressModel
{
    public required string Country { get; set; }
    public required string Region { get; set; }
    public required string City { get; set; }
    public required string Street { get; set; }
    public required string Building { get; set; }
    public required string PostalCode { get; set; }
}