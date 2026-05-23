using Foundation;
using UIKit;

namespace OpsLedger.App;

[Register(nameof(AppDelegate))]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        ConfigureTabBarAppearance();

        return base.FinishedLaunching(application, launchOptions);
    }

    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }

    private static void ConfigureTabBarAppearance()
    {
        var dockBackground = UIColor.FromRGB(32, 42, 50);
        var selectedText = UIColor.FromRGB(58, 175, 169);
        var unselectedText = UIColor.FromRGB(244, 247, 249);
        var titleOffset = new UIOffset(0, -9);
        var selectedAttributes = new UIStringAttributes
        {
            Font = UIFont.SystemFontOfSize(13, UIFontWeight.Semibold),
            ForegroundColor = selectedText
        };
        var normalAttributes = new UIStringAttributes
        {
            Font = UIFont.SystemFontOfSize(13, UIFontWeight.Medium),
            ForegroundColor = unselectedText
        };

        UITabBar.Appearance.BackgroundColor = dockBackground;
        UITabBar.Appearance.BarTintColor = dockBackground;
        UITabBar.Appearance.TintColor = selectedText;
        UITabBar.Appearance.UnselectedItemTintColor = unselectedText;
        UITabBar.Appearance.ItemSpacing = 2;
        UITabBarItem.Appearance.SetTitleTextAttributes(normalAttributes, UIControlState.Normal);
        UITabBarItem.Appearance.SetTitleTextAttributes(selectedAttributes, UIControlState.Selected);
        UITabBarItem.Appearance.TitlePositionAdjustment = titleOffset;

        if (OperatingSystem.IsIOSVersionAtLeast(15) || OperatingSystem.IsMacCatalystVersionAtLeast(15))
        {
            var appearance = new UITabBarAppearance();
            appearance.ConfigureWithOpaqueBackground();
            appearance.BackgroundColor = dockBackground;
            appearance.StackedLayoutAppearance.Normal.TitlePositionAdjustment = titleOffset;
            appearance.StackedLayoutAppearance.Normal.TitleTextAttributes = normalAttributes;
            appearance.StackedLayoutAppearance.Selected.TitlePositionAdjustment = titleOffset;
            appearance.StackedLayoutAppearance.Selected.TitleTextAttributes = selectedAttributes;
            appearance.InlineLayoutAppearance.Normal.TitlePositionAdjustment = titleOffset;
            appearance.InlineLayoutAppearance.Normal.TitleTextAttributes = normalAttributes;
            appearance.InlineLayoutAppearance.Selected.TitlePositionAdjustment = titleOffset;
            appearance.InlineLayoutAppearance.Selected.TitleTextAttributes = selectedAttributes;
            appearance.CompactInlineLayoutAppearance.Normal.TitlePositionAdjustment = titleOffset;
            appearance.CompactInlineLayoutAppearance.Normal.TitleTextAttributes = normalAttributes;
            appearance.CompactInlineLayoutAppearance.Selected.TitlePositionAdjustment = titleOffset;
            appearance.CompactInlineLayoutAppearance.Selected.TitleTextAttributes = selectedAttributes;

            UITabBar.Appearance.StandardAppearance = appearance;
            UITabBar.Appearance.ScrollEdgeAppearance = appearance;
        }
    }
}
