#if WINDOWS
using FlaUI.Core.AutomationElements;
using OpsLedger.BddTests.Support.UiAutomation.Models;
using OpsLedger.BddTests.Support.UiAutomation.Pages;

namespace OpsLedger.BddTests.Support.UiAutomation;

public sealed class WindowsAppAutomationDriver : IDisposable
{
    private const string ExecutablePathVariableName = "OPSLEDGER_BDD_APP_EXECUTABLE_PATH";
    private readonly WindowsAppAutomationSession session;

    private WindowsAppAutomationDriver(string executablePath)
    {
        session = new WindowsAppAutomationSession(executablePath);
    }

    public static WindowsAppAutomationDriver FromEnvironment()
    {
        string? executablePath = Environment.GetEnvironmentVariable(ExecutablePathVariableName);

        Skip.If(string.IsNullOrWhiteSpace(executablePath), $"{ExecutablePathVariableName} must point to the published Windows executable.");
        Skip.IfNot(File.Exists(executablePath), $"Published Windows executable was not found at '{executablePath}'.");

        return new WindowsAppAutomationDriver(executablePath!);
    }

    public string SubmitServiceRequest(
        string title,
        string category,
        string priority,
        string description,
        string requesterName,
        string requesterEmail)
    {
        Window window = session.GetMainWindow();
        WindowsShellPage shellPage = new(window);
        CreateRequestPageObject createRequestPage = shellPage.OpenCreateRequestPage();

        createRequestPage.SubmitRequest(new UiServiceRequestInput(
            title,
            category,
            priority,
            description,
            requesterName,
            requesterEmail));

        return createRequestPage.WaitForSuccessMessage();
    }

    public void Dispose()
    {
        session.Dispose();
    }
}
#endif
