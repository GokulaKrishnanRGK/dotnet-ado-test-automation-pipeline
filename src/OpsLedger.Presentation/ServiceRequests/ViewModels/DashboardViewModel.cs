using System.Collections.ObjectModel;
using System.Windows.Input;
using OpsLedger.Presentation.Common.Commands;
using OpsLedger.Presentation.Common.ViewModels;
using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.Presentation.ServiceRequests.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IServiceRequestClient client;
    private Int32 newCount;
    private Int32 inProgressCount;
    private Int32 blockedCount;
    private Int32 resolvedCount;
    private Int32 criticalOpenCount;
    private string? errorMessage;

    public DashboardViewModel(IServiceRequestClient client)
    {
        this.client = client;
        LoadCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    public ObservableCollection<ServiceRequestSummary> RecentRequests { get; } = [];

    public ICommand LoadCommand { get; }

    public Int32 NewCount
    {
        get => newCount;
        private set => SetProperty(ref newCount, value);
    }

    public Int32 InProgressCount
    {
        get => inProgressCount;
        private set => SetProperty(ref inProgressCount, value);
    }

    public Int32 BlockedCount
    {
        get => blockedCount;
        private set => SetProperty(ref blockedCount, value);
    }

    public Int32 ResolvedCount
    {
        get => resolvedCount;
        private set => SetProperty(ref resolvedCount, value);
    }

    public Int32 CriticalOpenCount
    {
        get => criticalOpenCount;
        private set => SetProperty(ref criticalOpenCount, value);
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;
        IReadOnlyList<ServiceRequestSummary> requests = await client.ListAsync(
            new ServiceRequestFilter("All", "All"),
            cancellationToken);

        NewCount = CountByStatus(requests, "New");
        InProgressCount = CountByStatus(requests, "InProgress");
        BlockedCount = CountByStatus(requests, "Blocked");
        ResolvedCount = CountByStatus(requests, "Resolved");
        CriticalOpenCount = requests.Count(request =>
            string.Equals(request.Priority, "Critical", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, "Resolved", StringComparison.OrdinalIgnoreCase));

        RecentRequests.Clear();
        foreach (ServiceRequestSummary request in requests
            .OrderByDescending(request => request.CreatedAt)
            .Take(5))
        {
            RecentRequests.Add(request);
        }
    }

    private static Int32 CountByStatus(IReadOnlyList<ServiceRequestSummary> requests, string status)
    {
        return requests.Count(request =>
            string.Equals(request.Status, status, StringComparison.OrdinalIgnoreCase));
    }
}
