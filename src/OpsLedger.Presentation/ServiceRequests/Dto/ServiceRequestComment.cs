namespace OpsLedger.Presentation.ServiceRequests.Dto;

public sealed record ServiceRequestComment(
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAt);
