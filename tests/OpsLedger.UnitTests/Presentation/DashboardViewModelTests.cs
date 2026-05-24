using FluentAssertions;
using OpsLedger.Presentation.ServiceRequests;
using OpsLedger.Presentation.ServiceRequests.Dto;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

namespace OpsLedger.UnitTests.Presentation;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task LoadAsync_calculates_status_and_priority_counts()
    {
        CapturingServiceRequestClient client = new(
        [
            NewRequest("req-1", "New", "Critical", DateTimeOffset.UtcNow.AddMinutes(-5)),
            NewRequest("req-2", "InProgress", "High", DateTimeOffset.UtcNow.AddMinutes(-10)),
            NewRequest("req-3", "Blocked", "Normal", DateTimeOffset.UtcNow.AddMinutes(-15)),
            NewRequest("req-4", "Resolved", "Low", DateTimeOffset.UtcNow.AddMinutes(-20))
        ]);
        DashboardViewModel viewModel = new(client);

        await viewModel.LoadAsync();

        client.LastFilter.Should().Be(new ServiceRequestFilter("All", "All"));
        viewModel.NewCount.Should().Be(1);
        viewModel.InProgressCount.Should().Be(1);
        viewModel.BlockedCount.Should().Be(1);
        viewModel.ResolvedCount.Should().Be(1);
        viewModel.CriticalOpenCount.Should().Be(1);
        viewModel.RecentRequests.Should().HaveCount(4);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_limits_recent_requests_to_five_newest()
    {
        CapturingServiceRequestClient client = new(
        [
            NewRequest("req-1", "New", "Normal", DateTimeOffset.UtcNow.AddMinutes(-1)),
            NewRequest("req-2", "New", "Normal", DateTimeOffset.UtcNow.AddMinutes(-2)),
            NewRequest("req-3", "New", "Normal", DateTimeOffset.UtcNow.AddMinutes(-3)),
            NewRequest("req-4", "New", "Normal", DateTimeOffset.UtcNow.AddMinutes(-4)),
            NewRequest("req-5", "New", "Normal", DateTimeOffset.UtcNow.AddMinutes(-5)),
            NewRequest("req-6", "New", "Normal", DateTimeOffset.UtcNow.AddMinutes(-6))
        ]);
        DashboardViewModel viewModel = new(client);

        await viewModel.LoadAsync();

        viewModel.RecentRequests.Should().HaveCount(5);
        viewModel.RecentRequests.Select(request => request.Id).Should().Equal(
            "req-1",
            "req-2",
            "req-3",
            "req-4",
            "req-5");
    }

    private static ServiceRequestSummary NewRequest(
        string id,
        string status,
        string priority,
        DateTimeOffset createdAt)
    {
        return new ServiceRequestSummary(
            id,
            $"Request {id}",
            "IT",
            priority,
            status,
            createdAt,
            createdAt.AddHours(24));
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
