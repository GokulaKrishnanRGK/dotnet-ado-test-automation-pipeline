#if WINDOWS
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace OpsLedger.BddTests.Support.UiAutomation;

public sealed class WindowsAppAutomationSession : IDisposable
{
    private readonly string executablePath;
    private Application? application;
    private UIA3Automation? automation;
    private Window? mainWindow;

    public WindowsAppAutomationSession(string executablePath)
    {
        this.executablePath = executablePath;
    }

    public Window GetMainWindow()
    {
        if (mainWindow is not null)
        {
            return mainWindow;
        }

        application = Application.Launch(executablePath);
        automation = new UIA3Automation();
        mainWindow = application.GetMainWindow(automation, TimeSpan.FromSeconds(30));

        if (mainWindow is null)
        {
            throw new InvalidOperationException("The OpsLedger Windows app did not expose a main window.");
        }

        return mainWindow;
    }

    public void Dispose()
    {
        mainWindow = null;
        automation?.Dispose();

        if (application is not null && !application.HasExited)
        {
            application.Close();
        }
    }
}
#endif
