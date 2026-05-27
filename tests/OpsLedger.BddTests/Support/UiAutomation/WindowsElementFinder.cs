#if WINDOWS
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Exceptions;
using FlaUI.Core.Input;
using FlaUI.Core.Patterns;
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
        AutomationElement element = FindByAutomationId(automationId);

        if (!TrySetValuePatternText(element, value))
        {
            element.Focus();
            Keyboard.TypeSimultaneously([VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A]);
            Keyboard.Type(value);
        }

        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
    }

    public void SelectPickerValue(string automationId, string value)
    {
        AutomationElement picker = FindByAutomationId(automationId);

        try
        {
            picker.AsComboBox().Select(value);
        }
        catch (Exception exception) when (IsRecoverableAutomationException(exception))
        {
            TryClickElement(picker);
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));

            AutomationElement option = FindByName(value);
            TryClickElement(option);
        }

        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
    }

    public void ClickButton(string automationId)
    {
        ClickElement(FindByAutomationId(automationId));
    }

    public void ClickElement(AutomationElement element)
    {
        if (!TryInvokeElement(element))
        {
            TryClickElement(element);
        }

        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
    }

    public string WaitForLabelText(string automationId, TimeSpan timeout)
    {
        RetryResult<string> result = Retry.While<string>(
            () =>
            {
                AutomationElement? labelElement = TryFindByAutomationId(automationId, TimeSpan.FromSeconds(1));
                return labelElement is null ? string.Empty : ReadElementText(labelElement);
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

    private static string ReadElementText(AutomationElement element)
    {
        string valueText = ReadValuePatternText(element);

        if (!string.IsNullOrWhiteSpace(valueText))
        {
            return valueText;
        }

        string nameText = ReadNameText(element);

        return nameText;
    }

    private static string ReadValuePatternText(AutomationElement element)
    {
        try
        {
            IValuePattern? valuePattern = element.Patterns.Value.PatternOrDefault;
            return valuePattern?.Value.Value ?? string.Empty;
        }
        catch (PropertyNotSupportedException)
        {
            return string.Empty;
        }
        catch (PatternNotSupportedException)
        {
            return string.Empty;
        }
    }

    private static bool TrySetValuePatternText(AutomationElement element, string value)
    {
        try
        {
            IValuePattern? valuePattern = element.Patterns.Value.PatternOrDefault;

            if (valuePattern is null)
            {
                return false;
            }

            valuePattern.SetValue(value);
            return true;
        }
        catch (PropertyNotSupportedException)
        {
            return false;
        }
        catch (PatternNotSupportedException)
        {
            return false;
        }
    }

    private static bool TryInvokeElement(AutomationElement element)
    {
        try
        {
            IInvokePattern? invokePattern = element.Patterns.Invoke.PatternOrDefault;

            if (invokePattern is null)
            {
                return false;
            }

            invokePattern.Invoke();
            return true;
        }
        catch (PropertyNotSupportedException)
        {
            return false;
        }
        catch (PatternNotSupportedException)
        {
            return false;
        }
    }

    private static void TryClickElement(AutomationElement element)
    {
        try
        {
            element.Click();
        }
        catch (Exception exception) when (IsRecoverableAutomationException(exception))
        {
            throw new InvalidOperationException("The UI element could not be clicked through UI Automation.", exception);
        }
    }

    private static string ReadNameText(AutomationElement element)
    {
        try
        {
            return element.Properties.Name.Value ?? string.Empty;
        }
        catch (PropertyNotSupportedException)
        {
            return string.Empty;
        }
    }

    private static bool IsRecoverableAutomationException(Exception exception)
    {
        return exception is PropertyNotSupportedException ||
            exception is PatternNotSupportedException ||
            exception is InvalidOperationException;
    }
}
#endif
