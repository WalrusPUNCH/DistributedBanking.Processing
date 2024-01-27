namespace DistributedBanking.Processing.Domain.Options;

public record DatabaseOptions(
    string ConnectionString,
    string DatabaseName);