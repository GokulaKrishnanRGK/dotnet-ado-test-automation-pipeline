using System.Windows.Input;
using OpsLedger.Presentation.Common.Commands;
using OpsLedger.Presentation.Common.ViewModels;
using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.Presentation.ServiceRequests.ViewModels;

public sealed class CreateRequestViewModel : ObservableObject
{
    private readonly IServiceRequestClient client;
    private string? title;
    private string? category = "IT";
    private string? priority = "Normal";
    private string? description;
    private string? requesterName;
    private string? requesterEmail;
    private string? impactDetails;
    private string? successMessage;
    private string? errorMessage;

    public CreateRequestViewModel(IServiceRequestClient client)
    {
        this.client = client;
        SubmitCommand = new AsyncRelayCommand(() => SubmitAsync());
    }

    public IReadOnlyList<string> Categories { get; } = ["IT", "Facilities", "HR", "Security", "Finance"];

    public IReadOnlyList<string> Priorities { get; } = ["Low", "Normal", "High", "Critical"];

    public ICommand SubmitCommand { get; }

    public string? Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public string? Category
    {
        get => category;
        set => SetProperty(ref category, value);
    }

    public string? Priority
    {
        get => priority;
        set => SetProperty(ref priority, value);
    }

    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    public string? RequesterName
    {
        get => requesterName;
        set => SetProperty(ref requesterName, value);
    }

    public string? RequesterEmail
    {
        get => requesterEmail;
        set => SetProperty(ref requesterEmail, value);
    }

    public string? ImpactDetails
    {
        get => impactDetails;
        set => SetProperty(ref impactDetails, value);
    }

    public string? SuccessMessage
    {
        get => successMessage;
        private set => SetProperty(ref successMessage, value);
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public async Task SubmitAsync(CancellationToken cancellationToken = default)
    {
        SuccessMessage = null;
        ErrorMessage = null;

        ServiceRequestClientResult result;

        try
        {
            result = await client.CreateAsync(
                new CreateServiceRequestInput(
                    Title ?? string.Empty,
                    Category ?? string.Empty,
                    Priority ?? string.Empty,
                    Description ?? string.Empty,
                    RequesterName ?? string.Empty,
                    RequesterEmail ?? string.Empty,
                    ImpactDetails),
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to reach OpsLedger API. Confirm the API is running and the database connection is available.";
            return;
        }

        if (result.IsSuccess)
        {
            SuccessMessage = "Service request created.";
            return;
        }

        ErrorMessage = string.Join(' ', result.Errors);
    }
}
