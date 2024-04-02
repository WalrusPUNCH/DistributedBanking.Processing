using Refit;
using TransactionalClock.Integration.Models;

namespace TransactionalClock.Integration;

public interface ITransactionalClockClient
{
    [Put("/mongodb")]
    Task<TransactionalClockResponse> Update<T>(
        [Header(Constants.Header.Id)] string id,
        [Header(Constants.Header.Database)] string database,
        [Header(Constants.Header.Collection)] string collection,
        [Header(Constants.Header.CreatedAt)] string createdAt,
        [Body] T payload,
        [Header(Constants.Header.Priority)] int? priority = 50);
    
    [Post("/mongodb")]
    Task<TransactionalClockResponse> Create<T>(
        [Header(Constants.Header.Database)] string database,
        [Header(Constants.Header.Collection)] string collection,
        [Body] T payload,
        [Header(Constants.Header.Priority)] int? priority = 50);
    
    [Delete("/mongodb")]
    Task<TransactionalClockResponse> Delete(
        [Header(Constants.Header.Id)] string id,
        [Header(Constants.Header.Database)] string database,
        [Header(Constants.Header.Collection)] string collection,
        [Header(Constants.Header.Priority)] int? priority = 50);
}