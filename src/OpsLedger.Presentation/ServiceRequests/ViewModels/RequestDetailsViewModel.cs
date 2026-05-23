using System.Collections.ObjectModel;
using System.Windows.Input;
using OpsLedger.Presentation.Common.Commands;
using OpsLedger.Presentation.Common.ViewModels;
using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.Presentation.ServiceRequests.ViewModels;

public sealed class RequestDetailsViewModel : ObservableObject
{
    private readonly IServiceRequestClient client;
    private string? requestId;
    private string? title;
    private string? category;
    private string? priority;
    private string? status;
    private string? assigneeName;
    private string? resolutionNotes;
    private string? commentAuthorName;
    private string? newCommentBody;
    private string? successMessage;
    private string? errorMessage;

    public RequestDetailsViewModel(IServiceRequestClient client)
    {
        this.client = client;
        LoadCommand = new AsyncRelayCommand(() => LoadAsync());
        AssignCommand = new AsyncRelayCommand(() => AssignAsync());
        ResolveCommand = new AsyncRelayCommand(() => ResolveAsync());
        AddCommentCommand = new AsyncRelayCommand(() => AddCommentAsync());
    }

    public ObservableCollection<ServiceRequestComment> Comments { get; } = [];

    public ObservableCollection<string> Activity { get; } = [];

    public ICommand LoadCommand { get; }

    public ICommand AssignCommand { get; }

    public ICommand ResolveCommand { get; }

    public ICommand AddCommentCommand { get; }

    public string? RequestId
    {
        get => requestId;
        set => SetProperty(ref requestId, value);
    }

    public string? Title
    {
        get => title;
        private set => SetProperty(ref title, value);
    }

    public string? Category
    {
        get => category;
        private set => SetProperty(ref category, value);
    }

    public string? Priority
    {
        get => priority;
        private set => SetProperty(ref priority, value);
    }

    public string? Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public string? AssigneeName
    {
        get => assigneeName;
        set => SetProperty(ref assigneeName, value);
    }

    public string? ResolutionNotes
    {
        get => resolutionNotes;
        set => SetProperty(ref resolutionNotes, value);
    }

    public string? CommentAuthorName
    {
        get => commentAuthorName;
        set => SetProperty(ref commentAuthorName, value);
    }

    public string? NewCommentBody
    {
        get => newCommentBody;
        set => SetProperty(ref newCommentBody, value);
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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ClearMessages();

        if (string.IsNullOrWhiteSpace(RequestId))
        {
            ErrorMessage = "Request ID is required.";
            return;
        }

        ServiceRequestSummary? request = await client.GetAsync(RequestId.Trim(), cancellationToken);

        if (request is null)
        {
            ErrorMessage = "Service request was not found.";
            return;
        }

        Apply(request);
    }

    public async Task AssignAsync(CancellationToken cancellationToken = default)
    {
        ClearMessages();

        if (!TryGetRequestId(out string id))
        {
            return;
        }

        ServiceRequestClientResult result = await client.AssignAsync(
            id,
            new AssignServiceRequestInput(AssigneeName ?? string.Empty),
            cancellationToken);

        ApplyResult(result, "Assignment saved.");
    }

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        ClearMessages();

        if (!TryGetRequestId(out string id))
        {
            return;
        }

        ServiceRequestClientResult result = await client.ResolveAsync(
            id,
            new ResolveServiceRequestInput(ResolutionNotes ?? string.Empty),
            cancellationToken);

        ApplyResult(result, "Request resolved.");
    }

    public async Task AddCommentAsync(CancellationToken cancellationToken = default)
    {
        ClearMessages();

        if (!TryGetRequestId(out string id))
        {
            return;
        }

        ServiceRequestClientResult result = await client.AddCommentAsync(
            id,
            new AddServiceRequestCommentInput(CommentAuthorName ?? string.Empty, NewCommentBody ?? string.Empty),
            cancellationToken);

        ApplyResult(result, "Comment added.");

        if (result.IsSuccess)
        {
            NewCommentBody = null;
        }
    }

    private bool TryGetRequestId(out string id)
    {
        if (string.IsNullOrWhiteSpace(RequestId))
        {
            ErrorMessage = "Request ID is required.";
            id = string.Empty;
            return false;
        }

        id = RequestId.Trim();
        return true;
    }

    private void ApplyResult(ServiceRequestClientResult result, string successMessage)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            ErrorMessage = string.Join(' ', result.Errors);
            return;
        }

        Apply(result.Value);
        SuccessMessage = successMessage;
    }

    private void Apply(ServiceRequestSummary request)
    {
        RequestId = request.Id;
        Title = request.Title;
        Category = request.Category;
        Priority = request.Priority;
        Status = request.Status;
        AssigneeName = request.AssigneeName;
        ResolutionNotes = request.ResolutionNotes;

        Comments.Clear();
        foreach (ServiceRequestComment comment in request.Comments ?? [])
        {
            Comments.Add(comment);
        }

        Activity.Clear();
        foreach (string activity in request.Activity ?? [])
        {
            Activity.Add(activity);
        }
    }

    private void ClearMessages()
    {
        SuccessMessage = null;
        ErrorMessage = null;
    }
}
