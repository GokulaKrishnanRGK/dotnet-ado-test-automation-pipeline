namespace OpsLedger.Core.ServiceRequests.Dto;

public sealed record AddServiceRequestCommentCommand(string AuthorName, string Body);
