using Microsoft.UI.Xaml.Controls;
using PAYETAXCalc.Services;

namespace PAYETAXCalc
{
    public sealed partial class SplashScreen : Page
    {
        public SplashScreen()
        {
            this.InitializeComponent();
            VersionText.Text = $"v{UpdateCheckService.GetCurrentVersion()}";
        }
    }
}
