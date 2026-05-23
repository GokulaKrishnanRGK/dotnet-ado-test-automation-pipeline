namespace OpsLedger.Core.ServiceRequests.Entities;

public sealed record RequestComment(
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAt);
