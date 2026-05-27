#if WINDOWS
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace OpsLedger.BddTests.Support.UiAutomation.Pages;

public sealed class WindowsShellPage
{
    private readonly WindowsElementFinder elementFinder;

    public WindowsShellPage(Window window)
    {
        elementFinder = new WindowsElementFinder(window);
    }

    public CreateRequestPageObject OpenCreateRequestPage()
    {
        CreateRequestPageObject createRequestPage = new(elementFinder);

        if (createRequestPage.IsOpen())
        {
            return createRequestPage;
        }

        AutomationElement createTab = elementFinder.FindByName("Create");
        elementFinder.ClickElement(createTab);

        createRequestPage.WaitUntilOpen();
        return createRequestPage;
    }
}
#endif
