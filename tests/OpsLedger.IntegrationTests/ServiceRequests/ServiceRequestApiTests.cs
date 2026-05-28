using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OpsLedger.Api.Modules.ServiceRequests.Dto;

namespace OpsLedger.IntegrationTests.ServiceRequests;

public sealed class ServiceRequestApiTests : IClassFixture<InMemoryApiFactory>
{
    private readonly HttpClient _client;

    public ServiceRequestApiTests(InMemoryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_health_returns_service_status()
    {
        using HttpResponseMessage response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HealthResponse? body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Service.Should().Be("OpsLedger.Api");
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

    [Fact]
    public async Task Patch_assignment_assigns_request_and_moves_it_to_in_progress()
    {
        ServiceRequestApiResponse created = await CreateRequestAsync(NewRequest("Assign this request", "IT", "Normal"));

        using HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/service-requests/{created.Id}/assignment",
            new AssignServiceRequestApiRequest("Morgan Lee"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ServiceRequestApiResponse? body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Status.Should().Be("InProgress");
        body.AssigneeName.Should().Be("Morgan Lee");
        body.Activity.Should().Contain("Assigned");
    }

    [Fact]
    public async Task Patch_resolution_requires_resolution_notes()
    {
        ServiceRequestApiResponse created = await CreateRequestAsync(NewRequest("Reject empty resolution", "Facilities", "Normal"));

        using HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/service-requests/{created.Id}/resolution",
            new ResolveServiceRequestApiRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body.Should().NotBeNull();
        body!.Errors.Should().Contain("Resolution notes are required.");
    }

    [Fact]
    public async Task Patch_resolution_resolves_request_with_notes()
    {
        ServiceRequestApiResponse created = await CreateRequestAsync(NewRequest("Resolve this request", "Facilities", "Normal"));

        using HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/service-requests/{created.Id}/resolution",
            new ResolveServiceRequestApiRequest("Display replaced and verified."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ServiceRequestApiResponse? body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Status.Should().Be("Resolved");
        body.ResolutionNotes.Should().Be("Display replaced and verified.");
        body.Activity.Should().Contain("Resolved");
    }

    [Fact]
    public async Task Get_service_request_by_id_returns_request_details()
    {
        ServiceRequestApiResponse created = await CreateRequestAsync(NewRequest("Inspect this request", "Security", "High"));

        using HttpResponseMessage response = await _client.GetAsync($"/service-requests/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ServiceRequestApiResponse? body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Title.Should().Be("Inspect this request");
        body.Category.Should().Be("Security");
    }

    [Fact]
    public async Task Get_service_request_by_id_returns_not_found_for_unknown_request()
    {
        using HttpResponseMessage response = await _client.GetAsync("/service-requests/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_comment_rejects_missing_comment_body()
    {
        ServiceRequestApiResponse created = await CreateRequestAsync(NewRequest("Reject empty comment", "IT", "Normal"));

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/service-requests/{created.Id}/comments",
            new AddServiceRequestCommentApiRequest("Morgan Lee", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body.Should().NotBeNull();
        body!.Errors.Should().Contain("Comment body is required.");
    }

    [Fact]
    public async Task Post_comment_adds_comment_to_request_details()
    {
        ServiceRequestApiResponse created = await CreateRequestAsync(NewRequest("Comment on this request", "IT", "Normal"));

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/service-requests/{created.Id}/comments",
            new AddServiceRequestCommentApiRequest("Morgan Lee", "Waiting on replacement hardware."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ServiceRequestApiResponse? body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Comments.Should().ContainSingle(comment =>
            comment.AuthorName == "Morgan Lee" &&
            comment.Body == "Waiting on replacement hardware.");
        body.Activity.Should().Contain("CommentAdded");
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

    private async Task<ServiceRequestApiResponse> CreateRequestAsync(CreateServiceRequestApiRequest request)
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/service-requests", request);
        response.EnsureSuccessStatusCode();

        ServiceRequestApiResponse? body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        body.Should().NotBeNull();

        return body!;
    }

    private sealed record HealthResponse(string Status, string Service);
}
