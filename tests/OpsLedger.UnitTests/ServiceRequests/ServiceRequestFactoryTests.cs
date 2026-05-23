using FluentAssertions;
using OpsLedger.Core.Common.Models;
using OpsLedger.Core.ServiceRequests;
using OpsLedger.Core.ServiceRequests.Constants;
using OpsLedger.Core.ServiceRequests.Dto;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.UnitTests.ServiceRequests;

public sealed class ServiceRequestFactoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 23, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_returns_new_request_with_initial_activity_and_sla_due_date()
    {
        CreateServiceRequestCommand command = new(
            Title: "Replace conference room display",
            Category: RequestCategory.Facilities,
            Priority: RequestPriority.High,
            Description: "The main display in conference room 4A is flickering.",
            RequesterName: "Priya Shah",
            RequesterEmail: "priya.shah@example.com",
            ImpactDetails: null);

        OperationResult<ServiceRequest> result = ServiceRequestFactory.Create(command, CreatedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Replace conference room display");
        result.Value.Status.Should().Be(RequestStatus.New);
        result.Value.SlaDueAt.Should().Be(CreatedAt.AddHours(8));
        result.Value.Activity.Should().ContainSingle(activity =>
            activity.Type == RequestActivityType.Created &&
            activity.OccurredAt == CreatedAt);
    }

    [Fact]
    public void Create_rejects_missing_required_fields()
    {
        CreateServiceRequestCommand command = new(
            Title: " ",
            Category: RequestCategory.IT,
            Priority: RequestPriority.Normal,
            Description: "",
            RequesterName: "",
            RequesterEmail: "not-an-email",
            ImpactDetails: null);

        OperationResult<ServiceRequest> result = ServiceRequestFactory.Create(command, CreatedAt);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(new[]
        {
            "Title is required.",
            "Description is required.",
            "Requester name is required.",
            "Requester email must be a valid email address."
        });
    }

    [Fact]
    public void Create_requires_impact_details_for_critical_requests()
    {
        CreateServiceRequestCommand command = new(
            Title: "Badge reader offline",
            Category: RequestCategory.Security,
            Priority: RequestPriority.Critical,
            Description: "The main entrance badge reader is offline.",
            RequesterName: "Morgan Lee",
            RequesterEmail: "morgan.lee@example.com",
            ImpactDetails: " ");

        OperationResult<ServiceRequest> result = ServiceRequestFactory.Create(command, CreatedAt);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Critical requests require impact details.");
    }

    [Theory]
    [InlineData(RequestPriority.Low, 72)]
    [InlineData(RequestPriority.Normal, 24)]
    [InlineData(RequestPriority.High, 8)]
    [InlineData(RequestPriority.Critical, 4)]
    public void Create_calculates_sla_due_date_from_priority(RequestPriority priority, int expectedHours)
    {
        CreateServiceRequestCommand command = new(
            Title: "Request title",
            Category: RequestCategory.IT,
            Priority: priority,
            Description: "Request description",
            RequesterName: "Alex Rivera",
            RequesterEmail: "alex.rivera@example.com",
            ImpactDetails: priority == RequestPriority.Critical ? "Business critical outage." : null);

        OperationResult<ServiceRequest> result = ServiceRequestFactory.Create(command, CreatedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.SlaDueAt.Should().Be(CreatedAt.AddHours(expectedHours));
    }
}
