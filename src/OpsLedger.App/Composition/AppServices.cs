using OpsLedger.Presentation.ServiceRequests;

namespace OpsLedger.App.Composition;

internal static class AppServices
{
    private const string ApiBaseAddressEnvironmentVariable = "OPSLEDGER_API_BASE_ADDRESS";
    private const string DefaultApiBaseAddress = "http://localhost:5184";

    public static IServiceRequestClient CreateServiceRequestClient()
    {
        string apiBaseAddress = Environment.GetEnvironmentVariable(ApiBaseAddressEnvironmentVariable) ??
            DefaultApiBaseAddress;

        return new OpsLedgerServiceRequestClient(new HttpClient
        {
            BaseAddress = new Uri(apiBaseAddress)
        });
    }
}
