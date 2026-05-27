#if WINDOWS
using OpsLedger.BddTests.Support.UiAutomation.Models;

namespace OpsLedger.BddTests.Support.UiAutomation.Pages;

public sealed class CreateRequestPageObject
{
    private static readonly TimeSpan PageOpenTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SuccessMessageTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ErrorMessageTimeout = TimeSpan.FromSeconds(20);
    private readonly WindowsElementFinder elementFinder;

    public CreateRequestPageObject(WindowsElementFinder elementFinder)
    {
        this.elementFinder = elementFinder;
    }

    public bool IsOpen()
    {
        return elementFinder.TryFindByAutomationId("RequestTitleEntry", TimeSpan.FromSeconds(2)) is not null;
    }

    public void WaitUntilOpen()
    {
        if (elementFinder.TryFindByAutomationId("RequestTitleEntry", PageOpenTimeout) is null)
        {
            throw new InvalidOperationException("The Create Request page did not open.");
        }
    }

    public void FillRequest(UiServiceRequestInput request)
    {
        elementFinder.EnterText("RequestTitleEntry", request.Title);
        elementFinder.SelectPickerValue("RequestCategoryPicker", request.Category);
        elementFinder.SelectPickerValue("RequestPriorityPicker", request.Priority);
        elementFinder.EnterText("RequestDescriptionEditor", request.Description);
        elementFinder.EnterText("RequestImpactEditor", request.ImpactDetails);
        elementFinder.EnterText("RequesterNameEntry", request.RequesterName);
        elementFinder.EnterText("RequesterEmailEntry", request.RequesterEmail);
    }

    public void SubmitRequest()
    {
        elementFinder.ClickButton("SubmitRequestButton");
    }

    public string WaitForSuccessMessage()
    {
        string successMessage = elementFinder.WaitForLabelText("SubmitSuccessLabel", SuccessMessageTimeout);
        elementFinder.ScrollToBottom();

        return successMessage;
    }

    public string WaitForErrorMessage()
    {
        string errorMessage = elementFinder.WaitForLabelText("SubmitErrorLabel", ErrorMessageTimeout);
        elementFinder.ScrollToBottom();

        return errorMessage;
    }
}
#endif
