namespace OpsLedger.Api.Modules.ServiceRequests.Dto;

public sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
