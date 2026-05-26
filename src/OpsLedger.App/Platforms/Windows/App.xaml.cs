using Microsoft.UI.Xaml;

namespace OpsLedger.App.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp()
    {
        return OpsLedger.App.MauiProgram.CreateMauiApp();
    }
}
