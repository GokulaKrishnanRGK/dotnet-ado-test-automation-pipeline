namespace OpsLedger.App.Pages;

using OpsLedger.App.Composition;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

public partial class CreateRequestPage : ContentPage
{
    public CreateRequestPage()
        : this(new CreateRequestViewModel(AppServices.CreateServiceRequestClient()))
    {
    }

    internal CreateRequestPage(CreateRequestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
