using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OpsLedger.Presentation.ServiceRequests;
using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.UnitTests.Presentation;

public sealed class OpsLedgerServiceRequestClientTests
{
    [Fact]
    public async Task CreateAsync_returns_validation_errors_from_bad_request()
    {
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new ValidationErrorResponse(["Title is required."]))
        });
        OpsLedgerServiceRequestClient client = NewClient(handler);

        ServiceRequestClientResult result = await client.CreateAsync(new CreateServiceRequestInput(
            " ",
            "Facilities",
            "Normal",
            "Description",
            "Requester",
            "requester@example.com",
            null));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Title is required.");
        handler.LastRequestUri.Should().Be("https://opsledger.local/service-requests");
    }

    [Fact]
    public async Task CreateAsync_returns_empty_response_error_when_success_body_is_empty()
    {
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.Created));
        OpsLedgerServiceRequestClient client = NewClient(handler);

        ServiceRequestClientResult result = await client.CreateAsync(new CreateServiceRequestInput(
            "Replace display",
            "Facilities",
            "High",
            "Description",
            "Requester",
            "requester@example.com",
            null));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("The service request response was empty.");
    }

    [Fact]
    public async Task ListAsync_omits_all_filters_and_encodes_real_filter_values()
    {
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<ServiceRequestSummary>>([])
        });
        OpsLedgerServiceRequestClient client = NewClient(handler);

        IReadOnlyList<ServiceRequestSummary> results = await client.ListAsync(new ServiceRequestFilter("All", "Critical Impact"));

        results.Should().BeEmpty();
        handler.LastRequestUri.Should().Be("https://opsledger.local/service-requests?priority=Critical%20Impact");
    }

    [Fact]
    public async Task GetAsync_returns_null_for_not_found()
    {
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.NotFound));
        OpsLedgerServiceRequestClient client = NewClient(handler);

        ServiceRequestSummary? result = await client.GetAsync("req/123");

        result.Should().BeNull();
        handler.LastRequestUri.Should().Be("https://opsledger.local/service-requests/req%2F123");
    }

    [Fact]
    public async Task AssignAsync_escapes_identifier_and_returns_updated_request()
    {
        ServiceRequestSummary updated = NewSummary("req/123");
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(updated)
        });
        OpsLedgerServiceRequestClient client = NewClient(handler);

        ServiceRequestClientResult result = await client.AssignAsync(
            "req/123",
            new AssignServiceRequestInput("Morgan Lee"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(updated);
        handler.LastMethod.Should().Be(HttpMethod.Patch);
        handler.LastRequestUri.Should().Be("https://opsledger.local/service-requests/req%2F123/assignment");
    }

    [Fact]
    public async Task ResolveAsync_returns_default_validation_error_when_bad_request_body_is_empty()
    {
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.BadRequest));
        OpsLedgerServiceRequestClient client = NewClient(handler);

        ServiceRequestClientResult result = await client.ResolveAsync(
            "req-1",
            new ResolveServiceRequestInput(""));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("The request was invalid.");
    }

    [Fact]
    public async Task AddCommentAsync_returns_empty_response_error_when_success_body_is_empty()
    {
        StubHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        OpsLedgerServiceRequestClient client = NewClient(handler);

        ServiceRequestClientResult result = await client.AddCommentAsync(
            "req-1",
            new AddServiceRequestCommentInput("Morgan Lee", "Waiting on hardware."));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("The service request response was empty.");
    }

    private static OpsLedgerServiceRequestClient NewClient(StubHttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://opsledger.local/")
        };

        return new OpsLedgerServiceRequestClient(httpClient);
    }

    private static ServiceRequestSummary NewSummary(string id)
    {
        return new ServiceRequestSummary(
            id,
            "Replace display",
            "Facilities",
            "High",
            "New",
            DateTimeOffset.Parse("2026-05-27T10:00:00Z"),
            DateTimeOffset.Parse("2026-05-27T18:00:00Z"));
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

        public HttpMethod? LastMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.AbsoluteUri;
            LastMethod = request.Method;

            return Task.FromResult(response);
        }
    }

    private sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
}
