using System.Collections.ObjectModel;
using System.Windows.Input;
using OpsLedger.Presentation.Common.Commands;
using OpsLedger.Presentation.Common.ViewModels;
using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.Presentation.ServiceRequests.ViewModels;

public sealed class RequestQueueViewModel : ObservableObject
{
    private readonly IServiceRequestClient client;
    private string? selectedStatus = "All";
    private string? selectedPriority = "All";
    private string? errorMessage;

    public RequestQueueViewModel(IServiceRequestClient client)
    {
        this.client = client;
        LoadCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    public IReadOnlyList<string> Statuses { get; } = ["All", "New", "InProgress", "Resolved", "Closed"];

    public IReadOnlyList<string> Priorities { get; } = ["All", "Low", "Normal", "High", "Critical"];

    public ObservableCollection<ServiceRequestSummary> Requests { get; } = [];

    public ICommand LoadCommand { get; }

    public string? SelectedStatus
    {
        get => selectedStatus;
        set => SetProperty(ref selectedStatus, value);
    }

    public string? SelectedPriority
    {
        get => selectedPriority;
        set => SetProperty(ref selectedPriority, value);
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;
        IReadOnlyList<ServiceRequestSummary> requests = await client.ListAsync(new ServiceRequestFilter(SelectedStatus, SelectedPriority), cancellationToken);

        Requests.Clear();
        foreach (ServiceRequestSummary request in requests)
        {
            Requests.Add(request);
        }
    }
}
