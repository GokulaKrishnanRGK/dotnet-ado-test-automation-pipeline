using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.BddTests.Support;
using Reqnroll;

namespace OpsLedger.BddTests.StepDefinitions;

[Binding]
public sealed class ServiceRequestWorkflowSteps : IDisposable
{
    private BddApiFactory? apiFactory;
    private HttpClient? client;
    private HttpResponseMessage? lastResponse;
    private IReadOnlyList<string>? lastValidationErrors;
    private ServiceRequestApiResponse? submittedRequest;
    private ServiceRequestApiResponse? currentRequest;
    private IReadOnlyList<ServiceRequestApiResponse>? filteredQueue;

    [Given("the OpsLedger API is available")]
    public void GivenTheOpsLedgerApiIsAvailable()
    {
        apiFactory = new BddApiFactory();
        client = apiFactory.CreateClient();
    }

    [When("an employee submits a {string} priority {string} request titled {string}")]
    public async Task WhenAnEmployeeSubmitsARequest(string priority, string category, string title)
    {
        lastResponse = await SubmitRequestAsync(priority, category, title);
        lastValidationErrors = null;

        if (lastResponse.StatusCode == HttpStatusCode.Created)
        {
            submittedRequest = await lastResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
            currentRequest = submittedRequest;
        }
    }

    [Given("an employee submitted a {string} priority {string} request titled {string}")]
    public async Task GivenAnEmployeeSubmittedARequest(string priority, string category, string title)
    {
        lastResponse = await SubmitRequestAsync(priority, category, title);
        lastValidationErrors = null;
        lastResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        ServiceRequestApiResponse? created =
            await lastResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();

        created.Should().NotBeNull();
        submittedRequest = created;
        currentRequest = created;
    }

    [When("an employee submits a critical request without impact details")]
    public async Task WhenAnEmployeeSubmitsACriticalRequestWithoutImpactDetails()
    {
        HttpClient apiClient = GetClient();
        CreateServiceRequestApiRequest request = new(
            Title: "Restore payroll access",
            Category: "IT",
            Priority: "Critical",
            Description: "Payroll team cannot access the approval queue.",
            RequesterName: "Casey Morgan",
            RequesterEmail: "casey.morgan@example.com",
            ImpactDetails: "");

        lastResponse = await apiClient.PostAsJsonAsync("/service-requests", request);
        lastValidationErrors = null;
    }

    [When("an employee submits a request with a missing title and invalid requester email")]
    public async Task WhenAnEmployeeSubmitsARequestWithAMissingTitleAndInvalidRequesterEmail()
    {
        HttpClient apiClient = GetClient();
        CreateServiceRequestApiRequest request = new(
            Title: "",
            Category: "Facilities",
            Priority: "Normal",
            Description: "The team area monitor is flickering.",
            RequesterName: "Riley Chen",
            RequesterEmail: "not-an-email",
            ImpactDetails: null);

        lastResponse = await apiClient.PostAsJsonAsync("/service-requests", request);
        lastValidationErrors = null;
    }

    [When("an operator assigns the request to {string}")]
    public async Task WhenAnOperatorAssignsTheRequestTo(string assigneeName)
    {
        currentRequest.Should().NotBeNull();

        HttpClient apiClient = GetClient();
        lastResponse = await apiClient.PatchAsJsonAsync(
            $"/service-requests/{currentRequest!.Id}/assignment",
            new AssignServiceRequestApiRequest(assigneeName));
        lastValidationErrors = null;

        if (lastResponse.StatusCode == HttpStatusCode.OK)
        {
            currentRequest = await lastResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        }
    }

    [When("an operator resolves the request without resolution notes")]
    public async Task WhenAnOperatorResolvesTheRequestWithoutResolutionNotes()
    {
        await ResolveCurrentRequestAsync(string.Empty);
    }

    [When("an operator resolves the request with {string}")]
    public async Task WhenAnOperatorResolvesTheRequestWith(string resolutionNotes)
    {
        await ResolveCurrentRequestAsync(resolutionNotes);
    }

    [When("an operator adds the comment {string}")]
    public async Task WhenAnOperatorAddsTheComment(string commentBody)
    {
        await AddCommentToCurrentRequestAsync("Morgan Lee", commentBody);
    }

    [When("an operator adds an empty comment")]
    public async Task WhenAnOperatorAddsAnEmptyComment()
    {
        await AddCommentToCurrentRequestAsync("Morgan Lee", string.Empty);
    }

    [When("an operator filters the queue by {string} priority and {string} status")]
    public async Task WhenAnOperatorFiltersTheQueueByPriorityAndStatus(string priority, string status)
    {
        HttpClient apiClient = GetClient();
        string query = $"priority={Uri.EscapeDataString(priority)}&status={Uri.EscapeDataString(status)}";
        filteredQueue = await apiClient.GetFromJsonAsync<IReadOnlyList<ServiceRequestApiResponse>>(
            $"/service-requests?{query}");
    }

    [Then("the request is accepted")]
    public void ThenTheRequestIsAccepted()
    {
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.Created);
        submittedRequest.Should().NotBeNull();
    }

    [Then("the request appears in the queue with status {string}")]
    public async Task ThenTheRequestAppearsInTheQueueWithStatus(string status)
    {
        submittedRequest.Should().NotBeNull();

        HttpClient apiClient = GetClient();
        IReadOnlyList<ServiceRequestApiResponse>? requests =
            await apiClient.GetFromJsonAsync<IReadOnlyList<ServiceRequestApiResponse>>("/service-requests");

        requests.Should().NotBeNull();
        requests.Should().Contain(request =>
            request.Id == submittedRequest!.Id &&
            request.Title == submittedRequest.Title &&
            request.Status == status);
    }

    [Then("the request status is {string}")]
    public void ThenTheRequestStatusIs(string status)
    {
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        currentRequest.Should().NotBeNull();
        currentRequest!.Status.Should().Be(status);
    }

    [Then("the request assignee is {string}")]
    public void ThenTheRequestAssigneeIs(string assigneeName)
    {
        currentRequest.Should().NotBeNull();
        currentRequest!.AssigneeName.Should().Be(assigneeName);
    }

    [Then("the request resolution notes are {string}")]
    public void ThenTheRequestResolutionNotesAre(string resolutionNotes)
    {
        currentRequest.Should().NotBeNull();
        currentRequest!.ResolutionNotes.Should().Be(resolutionNotes);
    }

    [Then("the request contains a comment from {string} saying {string}")]
    public void ThenTheRequestContainsACommentFromSaying(string authorName, string commentBody)
    {
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        currentRequest.Should().NotBeNull();
        currentRequest!.Comments.Should().ContainSingle(comment =>
            comment.AuthorName == authorName &&
            comment.Body == commentBody);
    }

    [Then("the request activity includes {string}")]
    public void ThenTheRequestActivityIncludes(string activityType)
    {
        currentRequest.Should().NotBeNull();
        currentRequest!.Activity.Should().Contain(activityType);
    }

    [Then("the queue includes {string}")]
    public void ThenTheQueueIncludes(string title)
    {
        filteredQueue.Should().NotBeNull();
        filteredQueue.Should().Contain(request => request.Title == title);
    }

    [Then("the queue does not include {string}")]
    public void ThenTheQueueDoesNotInclude(string title)
    {
        filteredQueue.Should().NotBeNull();
        filteredQueue.Should().NotContain(request => request.Title == title);
    }

    [Then("the request is rejected with {string}")]
    public async Task ThenTheRequestIsRejectedWith(string expectedError)
    {
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        IReadOnlyList<string> errors = await GetLastValidationErrorsAsync();
        errors.Should().Contain(expectedError);
    }

    public void Dispose()
    {
        lastResponse?.Dispose();
        client?.Dispose();
        apiFactory?.Dispose();
    }

    private HttpClient GetClient()
    {
        client.Should().NotBeNull("the API must be initialized by the Given step");
        return client!;
    }

    private Task<HttpResponseMessage> SubmitRequestAsync(string priority, string category, string title)
    {
        HttpClient apiClient = GetClient();
        CreateServiceRequestApiRequest request = new(
            Title: title,
            Category: category,
            Priority: priority,
            Description: $"Description for {title}.",
            RequesterName: "Priya Shah",
            RequesterEmail: "priya.shah@example.com",
            ImpactDetails: priority == "Critical" ? "Critical workflow impact." : null);

        return apiClient.PostAsJsonAsync("/service-requests", request);
    }

    private async Task ResolveCurrentRequestAsync(string resolutionNotes)
    {
        currentRequest.Should().NotBeNull();

        HttpClient apiClient = GetClient();
        lastResponse = await apiClient.PatchAsJsonAsync(
            $"/service-requests/{currentRequest!.Id}/resolution",
            new ResolveServiceRequestApiRequest(resolutionNotes));
        lastValidationErrors = null;

        if (lastResponse.StatusCode == HttpStatusCode.OK)
        {
            currentRequest = await lastResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        }
    }

    private async Task AddCommentToCurrentRequestAsync(string authorName, string commentBody)
    {
        currentRequest.Should().NotBeNull();

        HttpClient apiClient = GetClient();
        lastResponse = await apiClient.PostAsJsonAsync(
            $"/service-requests/{currentRequest!.Id}/comments",
            new AddServiceRequestCommentApiRequest(authorName, commentBody));
        lastValidationErrors = null;

        if (lastResponse.StatusCode == HttpStatusCode.OK)
        {
            currentRequest = await lastResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        }
    }

    private async Task<IReadOnlyList<string>> GetLastValidationErrorsAsync()
    {
        if (lastValidationErrors is not null)
        {
            return lastValidationErrors;
        }

        lastResponse.Should().NotBeNull();

        ValidationErrorResponse? validation =
            await lastResponse!.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        validation.Should().NotBeNull();
        lastValidationErrors = validation!.Errors;

        return lastValidationErrors;
    }
}
