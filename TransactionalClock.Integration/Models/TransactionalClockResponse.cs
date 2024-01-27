using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace TransactionalClock.Integration.Models;

public record TransactionalClockResponse([property: JsonPropertyName("id")] ObjectId Id);