namespace OpsLedger.App.Pages;

using OpsLedger.App.Composition;
using OpsLedger.Presentation.ServiceRequests.ViewModels;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
        : this(new DashboardViewModel(AppServices.CreateServiceRequestClient()))
    {
    }

    internal DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DashboardViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
