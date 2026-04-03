using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace PAYETAXCalc
{
    public partial class App : Application
    {
        public Window? m_window;

        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            var crashLog = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PAYETAXCalc", "crash.log");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
                File.AppendAllText(crashLog,
                    $"[{DateTime.Now:O}] {e.Exception.GetType().FullName}: {e.Exception.Message}\n{e.Exception.StackTrace}\n\n");
            }
            catch { }
            e.Handled = true;
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var splash = new SplashHostWindow();
            splash.Activate();

            await System.Threading.Tasks.Task.Delay(2000);

            m_window = new MainWindow();
            m_window.Activate();
            splash.Close();

            if (m_window is MainWindow mw)
                mw.ShowCoffeePromptIfNeeded();
        }
    }
}
