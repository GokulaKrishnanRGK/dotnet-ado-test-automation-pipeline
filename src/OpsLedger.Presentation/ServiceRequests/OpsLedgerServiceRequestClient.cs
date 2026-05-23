using System.Net;
using System.Net.Http.Json;
using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.Presentation.ServiceRequests;

public sealed class OpsLedgerServiceRequestClient(HttpClient httpClient) : IServiceRequestClient
{
    public async Task<ServiceRequestClientResult> CreateAsync(
        CreateServiceRequestInput request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("service-requests", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            ValidationErrorResponse? validation = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(cancellationToken);
            return ServiceRequestClientResult.Invalid(validation?.Errors ?? ["Unable to create service request."]);
        }

        response.EnsureSuccessStatusCode();

        ServiceRequestSummary? created = await response.Content.ReadFromJsonAsync<ServiceRequestSummary>(cancellationToken);
        if (created is null)
        {
            return ServiceRequestClientResult.Invalid(["The service request response was empty."]);
        }

        return ServiceRequestClientResult.Created(created);
    }

    public async Task<IReadOnlyList<ServiceRequestSummary>> ListAsync(
        ServiceRequestFilter filter,
        CancellationToken cancellationToken = default)
    {
        List<string> query = new();
        AddQueryValue(query, "status", filter.Status);
        AddQueryValue(query, "priority", filter.Priority);

        string path = query.Count == 0
            ? "service-requests"
            : $"service-requests?{string.Join("&", query)}";

        IReadOnlyList<ServiceRequestSummary>? requests = await httpClient.GetFromJsonAsync<IReadOnlyList<ServiceRequestSummary>>(path, cancellationToken);
        return requests ?? [];
    }

    public async Task<ServiceRequestSummary?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync($"service-requests/{Uri.EscapeDataString(id)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceRequestSummary>(cancellationToken);
    }

    public Task<ServiceRequestClientResult> AssignAsync(
        string id,
        AssignServiceRequestInput request,
        CancellationToken cancellationToken = default)
    {
        return SendForRequestResultAsync(
            () => httpClient.PatchAsJsonAsync($"service-requests/{Uri.EscapeDataString(id)}/assignment", request, cancellationToken),
            cancellationToken);
    }

    public Task<ServiceRequestClientResult> ResolveAsync(
        string id,
        ResolveServiceRequestInput request,
        CancellationToken cancellationToken = default)
    {
        return SendForRequestResultAsync(
            () => httpClient.PatchAsJsonAsync($"service-requests/{Uri.EscapeDataString(id)}/resolution", request, cancellationToken),
            cancellationToken);
    }

    public Task<ServiceRequestClientResult> AddCommentAsync(
        string id,
        AddServiceRequestCommentInput request,
        CancellationToken cancellationToken = default)
    {
        return SendForRequestResultAsync(
            () => httpClient.PostAsJsonAsync($"service-requests/{Uri.EscapeDataString(id)}/comments", request, cancellationToken),
            cancellationToken);
    }

    private static void AddQueryValue(ICollection<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != "All")
        {
            query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
    }

    private static async Task<ServiceRequestClientResult> SendForRequestResultAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await send();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            ValidationErrorResponse? validation = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(cancellationToken);
            return ServiceRequestClientResult.Invalid(validation?.Errors ?? ["The request was invalid."]);
        }

        response.EnsureSuccessStatusCode();

        ServiceRequestSummary? updated = await response.Content.ReadFromJsonAsync<ServiceRequestSummary>(cancellationToken);
        if (updated is null)
        {
            return ServiceRequestClientResult.Invalid(["The service request response was empty."]);
        }

        return ServiceRequestClientResult.Created(updated);
    }

    private sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
}
