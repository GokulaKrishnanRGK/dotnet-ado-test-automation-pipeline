using OpsLedger.Presentation.ServiceRequests;

namespace OpsLedger.App.Composition;

internal static class AppServices
{
    public static IServiceRequestClient CreateServiceRequestClient()
    {
        return new OpsLedgerServiceRequestClient(new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
    }
}
