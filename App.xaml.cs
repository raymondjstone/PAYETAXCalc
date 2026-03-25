using Microsoft.UI.Xaml;

namespace PAYETAXCalc
{
    public partial class App : Application
    {
        public Window? m_window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
