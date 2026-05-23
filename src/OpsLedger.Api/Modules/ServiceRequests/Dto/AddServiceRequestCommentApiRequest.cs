namespace OpsLedger.Api.Modules.ServiceRequests.Dto;

public sealed record AddServiceRequestCommentApiRequest(string AuthorName, string Body);
