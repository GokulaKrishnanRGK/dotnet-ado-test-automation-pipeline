namespace OpsLedger.App.Pages;

using OpsLedger.App.Composition;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

public partial class RequestQueuePage : ContentPage
{
    public RequestQueuePage()
        : this(new RequestQueueViewModel(AppServices.CreateServiceRequestClient()))
    {
    }

    internal RequestQueuePage(RequestQueueViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is RequestQueueViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
