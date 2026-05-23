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

    private static void AddQueryValue(ICollection<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != "All")
        {
            query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
    }

    private sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
}
