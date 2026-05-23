using FluentAssertions;
using OpsLedger.Core.Common.Models;
using OpsLedger.Core.ServiceRequests;
using OpsLedger.Core.ServiceRequests.Constants;
using OpsLedger.Core.ServiceRequests.Dto;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.UnitTests.ServiceRequests;

public sealed class ServiceRequestTransitionTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 23, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ChangedAt = CreatedAt.AddHours(1);

    [Fact]
    public void Assign_moves_request_to_in_progress_and_records_activity()
    {
        ServiceRequest request = NewRequest();

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.Assign(
            request,
            new AssignServiceRequestCommand("  Morgan Lee  "),
            ChangedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RequestStatus.InProgress);
        result.Value.AssigneeName.Should().Be("Morgan Lee");
        result.Value.Activity.Should().Contain(activity =>
            activity.Type == RequestActivityType.Assigned &&
            activity.OccurredAt == ChangedAt &&
            activity.Description == "Assigned to Morgan Lee.");
    }

    [Fact]
    public void Assign_rejects_missing_assignee()
    {
        ServiceRequest request = NewRequest();

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.Assign(
            request,
            new AssignServiceRequestCommand(" "),
            ChangedAt);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Assignee name is required.");
    }

    [Fact]
    public void Resolve_requires_resolution_notes()
    {
        ServiceRequest request = NewRequest();

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.Resolve(
            request,
            new ResolveServiceRequestCommand(" "),
            ChangedAt);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Resolution notes are required.");
    }

    [Fact]
    public void Resolve_moves_request_to_resolved_and_records_activity()
    {
        ServiceRequest request = NewRequest();

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.Resolve(
            request,
            new ResolveServiceRequestCommand("Display was replaced and tested."),
            ChangedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RequestStatus.Resolved);
        result.Value.ResolutionNotes.Should().Be("Display was replaced and tested.");
        result.Value.Activity.Should().Contain(activity =>
            activity.Type == RequestActivityType.Resolved &&
            activity.OccurredAt == ChangedAt &&
            activity.Description == "Request resolved.");
    }

    [Fact]
    public void AddComment_rejects_missing_author_and_body()
    {
        ServiceRequest request = NewRequest();

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.AddComment(
            request,
            new AddServiceRequestCommentCommand(" ", ""),
            ChangedAt);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(new[]
        {
            "Comment author is required.",
            "Comment body is required."
        });
    }

    [Fact]
    public void AddComment_records_comment_and_activity()
    {
        ServiceRequest request = NewRequest();

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.AddComment(
            request,
            new AddServiceRequestCommentCommand("  Morgan Lee  ", "  Waiting on replacement display.  "),
            ChangedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Comments.Should().ContainSingle(comment =>
            comment.AuthorName == "Morgan Lee" &&
            comment.Body == "Waiting on replacement display." &&
            comment.CreatedAt == ChangedAt);
        result.Value.Activity.Should().Contain(activity =>
            activity.Type == RequestActivityType.CommentAdded &&
            activity.OccurredAt == ChangedAt &&
            activity.Description == "Comment added by Morgan Lee.");
    }

    private static ServiceRequest NewRequest()
    {
        OperationResult<ServiceRequest> result = ServiceRequestFactory.Create(
            new CreateServiceRequestCommand(
                Title: "Replace conference room display",
                Category: RequestCategory.Facilities,
                Priority: RequestPriority.High,
                Description: "The main display in conference room 4A is flickering.",
                RequesterName: "Priya Shah",
                RequesterEmail: "priya.shah@example.com",
                ImpactDetails: null),
            CreatedAt);

        return result.Value;
    }
}
