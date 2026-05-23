using OpsLedger.Api.Modules.Health.Dto;

namespace OpsLedger.Api.Modules.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy", "OpsLedger.Api")))
            .WithName("GetHealth");

        return endpoints;
    }
}
