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
    private ServiceRequestApiResponse? submittedRequest;

    [Given("the OpsLedger API is available")]
    public void GivenTheOpsLedgerApiIsAvailable()
    {
        apiFactory = new BddApiFactory();
        client = apiFactory.CreateClient();
    }

    [When("an employee submits a {string} priority {string} request titled {string}")]
    public async Task WhenAnEmployeeSubmitsARequest(string priority, string category, string title)
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

        lastResponse = await apiClient.PostAsJsonAsync("/service-requests", request);

        if (lastResponse.StatusCode == HttpStatusCode.Created)
        {
            submittedRequest = await lastResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        }
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

    [Then("the request is rejected with {string}")]
    public async Task ThenTheRequestIsRejectedWith(string expectedError)
    {
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        ValidationErrorResponse? validation =
            await lastResponse.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        validation.Should().NotBeNull();
        validation!.Errors.Should().Contain(expectedError);
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
}
