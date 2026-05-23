using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Dto;

public sealed record ServiceRequestCommentApiResponse(
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAt)
{
    public static ServiceRequestCommentApiResponse From(RequestComment comment)
    {
        return new ServiceRequestCommentApiResponse(
            comment.AuthorName,
            comment.Body,
            comment.CreatedAt);
    }
}
