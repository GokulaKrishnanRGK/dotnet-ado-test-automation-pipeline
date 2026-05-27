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
    private readonly ScenarioContext scenarioContext;
    private string? lastUiMessage;

#if WINDOWS
    private WindowsAppAutomationDriver? driver;
#endif

    public WindowsAppWorkflowSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

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
        driver = WindowsAppAutomationDriver.FromEnvironment(scenarioContext.ScenarioInfo.Title);
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
            requesterEmail: "avery.stone@example.com",
            impactDetails: string.Empty);
#else
        throw new SkipException("Interactive Windows UI BDD requires the Windows test target.");
#endif
    }

    [When("an employee submits a critical service request with impact details through the Windows app")]
    public void WhenAnEmployeeSubmitsACriticalServiceRequestWithImpactDetailsThroughTheWindowsApp()
    {
#if WINDOWS
        driver.Should().NotBeNull();
        lastUiMessage = driver!.SubmitServiceRequest(
            title: $"Restore payroll export {Guid.NewGuid():N}",
            category: "Finance",
            priority: "Critical",
            description: "Payroll export is blocked before the submission deadline.",
            requesterName: "Priya Nair",
            requesterEmail: "priya.nair@example.com",
            impactDetails: "Payroll cannot be finalized until the export is restored.");
#else
        throw new SkipException("Interactive Windows UI BDD requires the Windows test target.");
#endif
    }

    [When("an employee submits a service request without a title through the Windows app")]
    public void WhenAnEmployeeSubmitsAServiceRequestWithoutATitleThroughTheWindowsApp()
    {
#if WINDOWS
        driver.Should().NotBeNull();
        lastUiMessage = driver!.SubmitInvalidServiceRequest(
            title: string.Empty,
            category: "Facilities",
            priority: "Normal",
            description: "The west stairwell light is flickering.",
            requesterName: "Jordan Blake",
            requesterEmail: "jordan.blake@example.com",
            impactDetails: string.Empty);
#else
        throw new SkipException("Interactive Windows UI BDD requires the Windows test target.");
#endif
    }

    [When("an employee submits a critical service request without impact details through the Windows app")]
    public void WhenAnEmployeeSubmitsACriticalServiceRequestWithoutImpactDetailsThroughTheWindowsApp()
    {
#if WINDOWS
        driver.Should().NotBeNull();
        lastUiMessage = driver!.SubmitInvalidServiceRequest(
            title: $"Restore badge reader {Guid.NewGuid():N}",
            category: "Security",
            priority: "Critical",
            description: "The south entrance badge reader is rejecting all staff badges.",
            requesterName: "Morgan Lee",
            requesterEmail: "morgan.lee@example.com",
            impactDetails: string.Empty);
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
