using System.Runtime.InteropServices;
using FluentAssertions;
using Reqnroll;

#if WINDOWS
using OpsLedger.BddTests.Support.UiAutomation;
#endif

namespace OpsLedger.BddTests.StepDefinitions;

[Binding]
public sealed class WindowsAppWorkflowSteps : IDisposable
{
    private const string RunUiBddVariableName = "OPSLEDGER_RUN_UI_BDD";
    private string? lastUiMessage;

#if WINDOWS
    private WindowsAppAutomationDriver? driver;
#endif

    [Given("the OpsLedger Windows app automation run is enabled")]
    public void GivenTheOpsLedgerWindowsAppAutomationRunIsEnabled()
    {
        string? runUiBddValue = Environment.GetEnvironmentVariable(RunUiBddVariableName);
        bool isEnabled = string.Equals(runUiBddValue, "true", StringComparison.OrdinalIgnoreCase);

        Skip.IfNot(isEnabled, $"{RunUiBddVariableName}=true is required for interactive Windows UI BDD.");
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Interactive Windows UI BDD requires a Windows agent.");

#if !WINDOWS
        throw new SkipException("Interactive Windows UI BDD requires the Windows test target.");
#endif
    }

    [Given("the published OpsLedger Windows app is available")]
    public void GivenThePublishedOpsLedgerWindowsAppIsAvailable()
    {
#if WINDOWS
        driver = WindowsAppAutomationDriver.FromEnvironment();
#else
        lastUiMessage = null;
        throw new SkipException("Interactive Windows UI BDD requires the Windows test target.");
#endif
    }

    [When("an employee submits a service request through the Windows app")]
    public void WhenAnEmployeeSubmitsAServiceRequestThroughTheWindowsApp()
    {
#if WINDOWS
        driver.Should().NotBeNull();
        lastUiMessage = driver!.SubmitServiceRequest(
            title: $"Replace onboarding display {Guid.NewGuid():N}",
            category: "Facilities",
            priority: "High",
            description: "The lobby onboarding display is offline.",
            requesterName: "Avery Stone",
            requesterEmail: "avery.stone@example.com");
#else
        throw new SkipException("Interactive Windows UI BDD requires the Windows test target.");
#endif
    }

    [Then("the Windows app shows {string}")]
    public void ThenTheWindowsAppShows(string expectedMessage)
    {
        lastUiMessage.Should().Be(expectedMessage);
    }

    public void Dispose()
    {
#if WINDOWS
        driver?.Dispose();
#endif
    }
}
