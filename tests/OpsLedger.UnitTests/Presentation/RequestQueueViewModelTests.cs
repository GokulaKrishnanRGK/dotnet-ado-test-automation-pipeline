using FluentAssertions;
using OpsLedger.Presentation.ServiceRequests;
using OpsLedger.Presentation.ServiceRequests.Dto;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

namespace OpsLedger.UnitTests.Presentation;

public sealed class RequestQueueViewModelTests
{
    [Fact]
    public async Task LoadAsync_loads_requests_using_selected_filters()
    {
        ServiceRequestSummary expected = new(
            "req-1",
            "Replace display",
            "Facilities",
            "High",
            "New",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(8));
        CapturingServiceRequestClient client = new([expected]);
        RequestQueueViewModel viewModel = new(client)
        {
            SelectedPriority = "High",
            SelectedStatus = "New"
        };

        await viewModel.LoadAsync();

        client.LastFilter.Should().Be(new ServiceRequestFilter("New", "High"));
        viewModel.Requests.Should().ContainSingle().Which.Title.Should().Be("Replace display");
        viewModel.ErrorMessage.Should().BeNull();
    }

    private sealed class CapturingServiceRequestClient(IReadOnlyList<ServiceRequestSummary> requests)
        : IServiceRequestClient
    {
        public ServiceRequestFilter? LastFilter { get; private set; }

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
            LastFilter = filter;
            return Task.FromResult(requests);
        }
    }
}
