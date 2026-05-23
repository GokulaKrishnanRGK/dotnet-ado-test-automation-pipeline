using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OpsLedger.IntegrationTests.ServiceRequests;

public sealed class ServiceRequestApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ServiceRequestApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_service_requests_creates_new_request()
    {
        CreateServiceRequestApiRequest request = new(
            Title: "Replace conference room display",
            Category: "Facilities",
            Priority: "High",
            Description: "The main display in conference room 4A is flickering.",
            RequesterName: "Priya Shah",
            RequesterEmail: "priya.shah@example.com",
            ImpactDetails: null);

        using HttpResponseMessage response = await _client.PostAsJsonAsync("/service-requests", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        ServiceRequestApiResponse? body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be(request.Title);
        body.Status.Should().Be("New");
        body.SlaDueAt.Should().BeAfter(body.CreatedAt);
        body.Activity.Should().Contain("Created");
    }

    [Fact]
    public async Task Post_service_requests_returns_bad_request_for_invalid_request()
    {
        CreateServiceRequestApiRequest request = new(
            Title: "",
            Category: "IT",
            Priority: "Critical",
            Description: "",
            RequesterName: "",
            RequesterEmail: "invalid-email",
            ImpactDetails: "");

        using HttpResponseMessage response = await _client.PostAsJsonAsync("/service-requests", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body.Should().NotBeNull();
        body!.Errors.Should().Contain(new[]
        {
            "Title is required.",
            "Description is required.",
            "Requester name is required.",
            "Requester email must be a valid email address.",
            "Critical requests require impact details."
        });
    }

    [Fact]
    public async Task Get_service_requests_returns_created_requests()
    {
        CreateServiceRequestApiRequest request = NewRequest("Queue visible request", "Facilities", "Normal");

        using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/service-requests", request);
        createResponse.EnsureSuccessStatusCode();

        using HttpResponseMessage listResponse = await _client.GetAsync("/service-requests");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        IReadOnlyList<ServiceRequestApiResponse>? body = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<ServiceRequestApiResponse>>();
        body.Should().NotBeNull();
        body.Should().Contain(item =>
            item.Title == request.Title &&
            item.Status == "New" &&
            item.Priority == "Normal" &&
            item.Category == "Facilities");
    }

    [Fact]
    public async Task Get_service_requests_filters_by_priority_and_status()
    {
        CreateServiceRequestApiRequest highPriority = NewRequest("Filtered high priority request", "IT", "High");
        CreateServiceRequestApiRequest lowPriority = NewRequest("Filtered low priority request", "IT", "Low");

        using HttpResponseMessage highResponse = await _client.PostAsJsonAsync("/service-requests", highPriority);
        using HttpResponseMessage lowResponse = await _client.PostAsJsonAsync("/service-requests", lowPriority);
        highResponse.EnsureSuccessStatusCode();
        lowResponse.EnsureSuccessStatusCode();

        using HttpResponseMessage listResponse = await _client.GetAsync("/service-requests?priority=High&status=New");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        IReadOnlyList<ServiceRequestApiResponse>? body = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<ServiceRequestApiResponse>>();
        body.Should().NotBeNull();
        body.Should().Contain(item => item.Title == highPriority.Title);
        body.Should().NotContain(item => item.Title == lowPriority.Title);
    }

    private static CreateServiceRequestApiRequest NewRequest(
        string title,
        string category,
        string priority)
    {
        return new CreateServiceRequestApiRequest(
            Title: title,
            Category: category,
            Priority: priority,
            Description: $"Description for {title}.",
            RequesterName: "Casey Morgan",
            RequesterEmail: "casey.morgan@example.com",
            ImpactDetails: priority == "Critical" ? "Critical business impact." : null);
    }

    private sealed record CreateServiceRequestApiRequest(
        string Title,
        string Category,
        string Priority,
        string Description,
        string RequesterName,
        string RequesterEmail,
        string? ImpactDetails);

    private sealed record ServiceRequestApiResponse(
        string Id,
        string Title,
        string Category,
        string Priority,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset SlaDueAt,
        IReadOnlyList<string> Activity);

    private sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
}
