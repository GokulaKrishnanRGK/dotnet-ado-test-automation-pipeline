using FluentAssertions;
using OpsLedger.Presentation.ServiceRequests;
using OpsLedger.Presentation.ServiceRequests.Dto;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

namespace OpsLedger.UnitTests.Presentation;

public sealed class RequestDetailsViewModelTests
{
    [Fact]
    public async Task LoadAsync_loads_request_details()
    {
        ServiceRequestSummary request = NewRequest("req-1", "New");
        CapturingServiceRequestClient client = new(request);
        RequestDetailsViewModel viewModel = new(client)
        {
            RequestId = "req-1"
        };

        await viewModel.LoadAsync();

        client.LastGetId.Should().Be("req-1");
        viewModel.Title.Should().Be("Replace display");
        viewModel.Status.Should().Be("New");
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task AssignAsync_assigns_loaded_request_and_updates_status()
    {
        ServiceRequestSummary assigned = NewRequest("req-1", "InProgress") with
        {
            AssigneeName = "Morgan Lee"
        };
        CapturingServiceRequestClient client = new(assigned);
        RequestDetailsViewModel viewModel = new(client)
        {
            RequestId = "req-1",
            AssigneeName = "Morgan Lee"
        };

        await viewModel.AssignAsync();

        client.LastAssignId.Should().Be("req-1");
        client.LastAssignRequest.Should().Be(new AssignServiceRequestInput("Morgan Lee"));
        viewModel.Status.Should().Be("InProgress");
        viewModel.SuccessMessage.Should().Be("Assignment saved.");
    }

    [Fact]
    public async Task AddCommentAsync_sends_comment_and_refreshes_comments()
    {
        ServiceRequestSummary commented = NewRequest("req-1", "New") with
        {
            Comments =
            [
                new ServiceRequestComment(
                    "Morgan Lee",
                    "Waiting on replacement hardware.",
                    DateTimeOffset.UtcNow)
            ]
        };
        CapturingServiceRequestClient client = new(commented);
        RequestDetailsViewModel viewModel = new(client)
        {
            RequestId = "req-1",
            CommentAuthorName = "Morgan Lee",
            NewCommentBody = "Waiting on replacement hardware."
        };

        await viewModel.AddCommentAsync();

        client.LastCommentId.Should().Be("req-1");
        client.LastCommentRequest.Should().Be(new AddServiceRequestCommentInput(
            "Morgan Lee",
            "Waiting on replacement hardware."));
        viewModel.Comments.Should().ContainSingle().Which.Body.Should().Be("Waiting on replacement hardware.");
        viewModel.NewCommentBody.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_surfaces_validation_errors()
    {
        CapturingServiceRequestClient client = new(ServiceRequestClientResult.Invalid(["Resolution notes are required."]));
        RequestDetailsViewModel viewModel = new(client)
        {
            RequestId = "req-1"
        };

        await viewModel.ResolveAsync();

        viewModel.ErrorMessage.Should().Be("Resolution notes are required.");
        viewModel.SuccessMessage.Should().BeNull();
    }

    private static ServiceRequestSummary NewRequest(string id, string status)
    {
        return new ServiceRequestSummary(
            id,
            "Replace display",
            "Facilities",
            "High",
            status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(8),
            Comments: [],
            Activity: ["Created"]);
    }

    private sealed class CapturingServiceRequestClient : IServiceRequestClient
    {
        private readonly ServiceRequestSummary request;
        private readonly ServiceRequestClientResult? result;

        public CapturingServiceRequestClient(ServiceRequestSummary request)
        {
            this.request = request;
        }

        public CapturingServiceRequestClient(ServiceRequestClientResult result)
        {
            request = NewRequest("req-1", "New");
            this.result = result;
        }

        public string? LastGetId { get; private set; }

        public string? LastAssignId { get; private set; }

        public AssignServiceRequestInput? LastAssignRequest { get; private set; }

        public string? LastCommentId { get; private set; }

        public AddServiceRequestCommentInput? LastCommentRequest { get; private set; }

        public Task<ServiceRequestClientResult> CreateAsync(
            CreateServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }

        public Task<IReadOnlyList<ServiceRequestSummary>> ListAsync(
            ServiceRequestFilter filter,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ServiceRequestSummary>>([]);
        }

        public Task<ServiceRequestSummary?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            LastGetId = id;
            return Task.FromResult<ServiceRequestSummary?>(request);
        }

        public Task<ServiceRequestClientResult> AssignAsync(
            string id,
            AssignServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            LastAssignId = id;
            LastAssignRequest = request;
            return Task.FromResult(ServiceRequestClientResult.Created(this.request));
        }

        public Task<ServiceRequestClientResult> ResolveAsync(
            string id,
            ResolveServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result ?? ServiceRequestClientResult.Created(this.request));
        }

        public Task<ServiceRequestClientResult> AddCommentAsync(
            string id,
            AddServiceRequestCommentInput request,
            CancellationToken cancellationToken = default)
        {
            LastCommentId = id;
            LastCommentRequest = request;
            return Task.FromResult(ServiceRequestClientResult.Created(this.request));
        }
    }
}
