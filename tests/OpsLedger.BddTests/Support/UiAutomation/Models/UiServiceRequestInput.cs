#if WINDOWS
namespace OpsLedger.BddTests.Support.UiAutomation.Models;

public sealed record UiServiceRequestInput(
    string Title,
    string Category,
    string Priority,
    string Description,
    string RequesterName,
    string RequesterEmail,
    string ImpactDetails);
#endif
