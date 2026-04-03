using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using System;

namespace PAYETAXCalc
{
    public class SplashHostWindow : Window
    {
        public Frame Frame { get; }

        public SplashHostWindow()
        {
            Frame = new Frame();
            Content = Frame;
            Frame.Navigate(typeof(SplashScreen));

            SetWindowSize(720, 480);
            CenterWindow();
        }

        private void SetWindowSize(int width, int height)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var finalWidth = Math.Max(width, 720);
                var finalHeight = Math.Max(height, 480);
                appWindow.Resize(new Windows.Graphics.SizeInt32(finalWidth, finalHeight));

                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                    presenter.SetBorderAndTitleBar(false, false);
                }
            }
        }

        private void CenterWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    var centerX = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                    var centerY = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                    appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                }
            }
        }
    }
}
