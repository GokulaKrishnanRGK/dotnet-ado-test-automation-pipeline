namespace OpsLedger.App.Pages;

using OpsLedger.App.Composition;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

public partial class RequestDetailsPage : ContentPage
{
    public RequestDetailsPage()
        : this(new RequestDetailsViewModel(AppServices.CreateServiceRequestClient()))
    {
    }

    internal RequestDetailsPage(RequestDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
