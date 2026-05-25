#if WINDOWS
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;

namespace OpsLedger.BddTests.Support.UiAutomation;

public sealed class WindowsElementFinder
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private readonly Window window;

    public WindowsElementFinder(Window window)
    {
        this.window = window;
    }

    public AutomationElement FindByAutomationId(string automationId)
    {
        AutomationElement? element = TryFindByAutomationId(automationId, DefaultTimeout);

        if (element is null)
        {
            throw new InvalidOperationException($"Could not find UI element with automation id '{automationId}'.");
        }

        return element;
    }

    public AutomationElement? TryFindByAutomationId(string automationId, TimeSpan timeout)
    {
        ConditionFactory conditionFactory = new(window.Automation.PropertyLibrary);
        RetryResult<AutomationElement?> result = Retry.WhileNull(
            () => window.FindFirstDescendant(conditionFactory.ByAutomationId(automationId)),
            timeout,
            PollInterval);

        return result.Success ? result.Result : null;
    }

    public AutomationElement FindByName(string name)
    {
        ConditionFactory conditionFactory = new(window.Automation.PropertyLibrary);
        RetryResult<AutomationElement?> result = Retry.WhileNull(
            () => window.FindFirstDescendant(conditionFactory.ByName(name)),
            DefaultTimeout,
            PollInterval);

        if (!result.Success || result.Result is null)
        {
            throw new InvalidOperationException($"Could not find UI element named '{name}'.");
        }

        return result.Result;
    }

    public void EnterText(string automationId, string value)
    {
        TextBox textBox = FindByAutomationId(automationId).AsTextBox();
        textBox.Text = string.Empty;
        textBox.Enter(value);
        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
    }

    public void SelectPickerValue(string automationId, string value)
    {
        ComboBox comboBox = FindByAutomationId(automationId).AsComboBox();
        comboBox.Select(value);
        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
    }

    public void ClickButton(string automationId)
    {
        Button button = FindByAutomationId(automationId).AsButton();
        button.Invoke();
        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
    }

    public string WaitForLabelText(string automationId, TimeSpan timeout)
    {
        RetryResult<string> result = Retry.While<string>(
            () =>
            {
                AutomationElement? labelElement = TryFindByAutomationId(automationId, TimeSpan.FromSeconds(1));
                return labelElement?.AsLabel().Text ?? string.Empty;
            },
            labelText => string.IsNullOrWhiteSpace(labelText),
            timeout,
            PollInterval);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Result))
        {
            throw new InvalidOperationException($"Timed out waiting for text from '{automationId}'.");
        }

        return result.Result;
    }
}
#endif
