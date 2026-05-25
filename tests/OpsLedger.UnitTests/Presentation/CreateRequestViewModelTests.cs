using FluentAssertions;
using OpsLedger.Presentation.ServiceRequests;
using OpsLedger.Presentation.ServiceRequests.Dto;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

namespace OpsLedger.UnitTests.Presentation;

public sealed class CreateRequestViewModelTests
{
    [Fact]
    public async Task SubmitAsync_sends_form_to_api_and_reports_success()
    {
        CapturingServiceRequestClient client = new(ServiceRequestClientResult.Created(
            new ServiceRequestSummary(
                "req-1",
                "Replace display",
                "Facilities",
                "High",
                "New",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(8))));
        CreateRequestViewModel viewModel = new(client)
        {
            Title = "Replace display",
            Category = "Facilities",
            Priority = "High",
            Description = "Conference room display is flickering.",
            RequesterName = "Priya Shah",
            RequesterEmail = "priya.shah@example.com"
        };

        await viewModel.SubmitAsync();

        client.LastCreateRequest.Should().NotBeNull();
        client.LastCreateRequest!.Title.Should().Be("Replace display");
        viewModel.SuccessMessage.Should().Be("Service request created.");
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SubmitAsync_surfaces_validation_errors()
    {
        CapturingServiceRequestClient client = new(ServiceRequestClientResult.Invalid(
            ["Title is required.", "Description is required."]));
        CreateRequestViewModel viewModel = new(client);

        await viewModel.SubmitAsync();

        viewModel.SuccessMessage.Should().BeNull();
        viewModel.ErrorMessage.Should().Be("Title is required. Description is required.");
    }

    [Fact]
    public async Task SubmitAsync_surfaces_api_connection_failure()
    {
        ThrowingServiceRequestClient client = new(new HttpRequestException("Connection refused."));
        CreateRequestViewModel viewModel = new(client);

        await viewModel.SubmitAsync();

        viewModel.SuccessMessage.Should().BeNull();
        viewModel.ErrorMessage.Should().Be("Unable to reach OpsLedger API. Confirm the API is running and the database connection is available.");
    }

    private sealed class CapturingServiceRequestClient(ServiceRequestClientResult result)
        : IServiceRequestClient
    {
        public CreateServiceRequestInput? LastCreateRequest { get; private set; }

        public Task<ServiceRequestClientResult> CreateAsync(
            CreateServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            LastCreateRequest = request;
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<ServiceRequestSummary>> ListAsync(
            ServiceRequestFilter filter,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ServiceRequestSummary>>([]);
        }

        public Task<ServiceRequestSummary?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ServiceRequestSummary?>(null);
        }

        public Task<ServiceRequestClientResult> AssignAsync(
            string id,
            AssignServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }

        public Task<ServiceRequestClientResult> ResolveAsync(
            string id,
            ResolveServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }

        public Task<ServiceRequestClientResult> AddCommentAsync(
            string id,
            AddServiceRequestCommentInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }
    }

    private sealed class ThrowingServiceRequestClient(Exception exception) : IServiceRequestClient
    {
        public Task<ServiceRequestClientResult> CreateAsync(
            CreateServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<ServiceRequestClientResult>(exception);
        }

        public Task<IReadOnlyList<ServiceRequestSummary>> ListAsync(
            ServiceRequestFilter filter,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ServiceRequestSummary>>([]);
        }

        public Task<ServiceRequestSummary?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ServiceRequestSummary?>(null);
        }

        public Task<ServiceRequestClientResult> AssignAsync(
            string id,
            AssignServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }

        public Task<ServiceRequestClientResult> ResolveAsync(
            string id,
            ResolveServiceRequestInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }

        public Task<ServiceRequestClientResult> AddCommentAsync(
            string id,
            AddServiceRequestCommentInput request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServiceRequestClientResult.Invalid(["Not used."]));
        }
    }
}
