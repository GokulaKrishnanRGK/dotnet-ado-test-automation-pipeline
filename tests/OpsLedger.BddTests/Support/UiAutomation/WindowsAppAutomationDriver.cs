#if WINDOWS
using FlaUI.Core.AutomationElements;
using OpsLedger.BddTests.Support.UiAutomation.Models;
using OpsLedger.BddTests.Support.UiAutomation.Pages;

namespace OpsLedger.BddTests.Support.UiAutomation;

public sealed class WindowsAppAutomationDriver : IDisposable
{
    private const string ExecutablePathVariableName = "OPSLEDGER_BDD_APP_EXECUTABLE_PATH";
    private readonly WindowsAppAutomationSession session;
    private readonly UiEvidenceReport evidenceReport;

    private WindowsAppAutomationDriver(string executablePath, string scenarioName)
    {
        session = new WindowsAppAutomationSession(executablePath);
        evidenceReport = new UiEvidenceReport(scenarioName);
    }

    public static WindowsAppAutomationDriver FromEnvironment(string scenarioName)
    {
        string? executablePath = Environment.GetEnvironmentVariable(ExecutablePathVariableName);

        Skip.If(string.IsNullOrWhiteSpace(executablePath), $"{ExecutablePathVariableName} must point to the published Windows executable.");
        Skip.IfNot(File.Exists(executablePath), $"Published Windows executable was not found at '{executablePath}'.");

        return new WindowsAppAutomationDriver(executablePath!, scenarioName);
    }

    public string SubmitServiceRequest(
        string title,
        string category,
        string priority,
        string description,
        string requesterName,
        string requesterEmail,
        string impactDetails)
    {
        CreateRequestPageObject createRequestPage = FillCreateRequestForm(new UiServiceRequestInput(
            title,
            category,
            priority,
            description,
            requesterName,
            requesterEmail,
            impactDetails));

        createRequestPage.SubmitRequest();
        CaptureEvidence("Create request submitted");

        string successMessage = createRequestPage.WaitForSuccessMessage();
        CaptureEvidence("Success message displayed");

        return successMessage;
    }

    public string SubmitInvalidServiceRequest(
        string title,
        string category,
        string priority,
        string description,
        string requesterName,
        string requesterEmail,
        string impactDetails)
    {
        CreateRequestPageObject createRequestPage = FillCreateRequestForm(new UiServiceRequestInput(
            title,
            category,
            priority,
            description,
            requesterName,
            requesterEmail,
            impactDetails));

        createRequestPage.SubmitRequest();
        CaptureEvidence("Invalid create request submitted");

        string errorMessage = createRequestPage.WaitForErrorMessage();
        CaptureEvidence("Validation message displayed");

        return errorMessage;
    }

    public void Dispose()
    {
        CaptureFinalEvidence();
        session.Dispose();
    }

    private void CaptureEvidence(string label)
    {
        try
        {
            evidenceReport.Capture(label, session.CaptureMainWindow);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"UI evidence capture failed for '{label}': {exception.Message}");
        }
    }

    private void CaptureFinalEvidence()
    {
        if (!session.HasMainWindow)
        {
            return;
        }

        try
        {
            evidenceReport.Capture("Final UI state", session.CaptureExistingMainWindow);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"UI evidence capture failed for final state: {exception.Message}");
        }
    }

    private CreateRequestPageObject FillCreateRequestForm(UiServiceRequestInput request)
    {
        Window window = session.GetMainWindow();
        CaptureEvidence("App launched");

        WindowsShellPage shellPage = new(window);
        CreateRequestPageObject createRequestPage = shellPage.OpenCreateRequestPage();
        CaptureEvidence("Create request page opened");

        createRequestPage.FillRequest(request);
        CaptureEvidence("Create request form filled");

        return createRequestPage;
    }
}
#endif
