using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpsLedger.Infrastructure.Persistence;

namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlServiceRequestApiTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture fixture;
    private readonly HttpClient client;

    public PostgreSqlServiceRequestApiTests(PostgreSqlFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.Factory.CreateClient();
    }

    [PostgreSqlFact]
    public async Task Post_service_requests_persists_request_and_activity_through_stored_procedure()
    {
        CreateServiceRequestApiRequest request = NewRequest("Persisted PostgreSQL request", "Facilities", "High");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/service-requests", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        ServiceRequestApiResponse? created = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        created.Should().NotBeNull();
        created!.Activity.Should().Contain("Created");

        using HttpResponseMessage detailResponse = await client.GetAsync($"/service-requests/{created.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ServiceRequestApiResponse? details = await detailResponse.Content.ReadFromJsonAsync<ServiceRequestApiResponse>();
        details.Should().NotBeNull();
        details!.Title.Should().Be(request.Title);
        details.Activity.Should().Contain("Created");
    }

    [PostgreSqlFact]
    public async Task Post_comment_rolls_back_activity_when_request_does_not_exist()
    {
        await using OpsLedgerDbContext dbContext = CreateDbContext();
        Int32 activityCountBefore = await dbContext.RequestActivity.CountAsync();

        Func<Task> act = async () => await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT opsledger_add_service_request_comment(
                 {"missing-request"},
                 {"Morgan Lee"},
                 {"This should not persist."},
                 {DateTimeOffset.UtcNow})
             """);

        await act.Should().ThrowAsync<DbUpdateException>();

        Int32 activityCountAfter = await dbContext.RequestActivity.CountAsync();
        activityCountAfter.Should().Be(activityCountBefore);
    }

    private OpsLedgerDbContext CreateDbContext()
    {
        DbContextOptionsBuilder<OpsLedgerDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(fixture.ConnectionString);
        return new OpsLedgerDbContext(optionsBuilder.Options);
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
        string? AssigneeName,
        string? ResolutionNotes,
        IReadOnlyList<ServiceRequestCommentApiResponse> Comments,
        IReadOnlyList<string> Activity);

    private sealed record ServiceRequestCommentApiResponse(
        string AuthorName,
        string Body,
        DateTimeOffset CreatedAt);
}
