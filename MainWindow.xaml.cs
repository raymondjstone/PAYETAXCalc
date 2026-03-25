using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using PAYETAXCalc.Controls;
using PAYETAXCalc.Models;
using PAYETAXCalc.Services;
using Windows.Graphics;

namespace PAYETAXCalc
{
    public sealed partial class MainWindow : Window
    {
        private AppData _appData = null!;
        private AppWindow _appWindow = null!;
        private bool _isLoading;
        private bool _windowReady;

        public MainWindow()
        {
            InitializeComponent();

            // Get AppWindow for position/size management
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Set up custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Load data
            _appData = DataService.Load();

            // Restore window position before showing
            RestoreWindowPosition();

            LoadTabs();

            // Track window moves/resizes so position is always saved
            _appWindow.Changed += AppWindow_Changed;
            _appWindow.Closing += AppWindow_Closing;

            // Mark window as ready after a short delay to avoid saving default position
            DispatcherQueue.TryEnqueue(() => { _windowReady = true; });
        }

        private void RestoreWindowPosition()
        {
            var ws = _appData.Window;

            int width = ws.Width > 100 ? ws.Width : 1100;
            int height = ws.Height > 100 ? ws.Height : 800;

            if (ws.X >= 0 && ws.Y >= 0)
            {
                // Validate the position is reasonable (not off-screen)
                // Allow some tolerance - at least part of the window should be visible
                if (ws.X < 10000 && ws.Y < 10000)
                {
                    _appWindow.MoveAndResize(new RectInt32(ws.X, ws.Y, width, height));
                }
                else
                {
                    _appWindow.Resize(new SizeInt32(width, height));
                }
            }
            else
            {
                _appWindow.Resize(new SizeInt32(width, height));
            }
        }

        private void SaveWindowPosition()
        {
            try
            {
                var pos = _appWindow.Position;
                var size = _appWindow.Size;

                // Only save if the window isn't minimized (size would be weird)
                if (size.Width > 100 && size.Height > 100)
                {
                    _appData.Window.X = pos.X;
                    _appData.Window.Y = pos.Y;
                    _appData.Window.Width = size.Width;
                    _appData.Window.Height = size.Height;
                }
            }
            catch
            {
                // Window may be in an invalid state during shutdown
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!_windowReady || _isLoading) return;

            // Save position whenever the window is moved or resized
            if (args.DidPositionChange || args.DidSizeChange)
            {
                SaveWindowPosition();
                DataService.Save(_appData);
            }
        }

        private void LoadTabs()
        {
            _isLoading = true;

            if (_appData.TaxYears.Count == 0)
            {
                string currentYear = TaxRulesProvider.GetCurrentTaxYear();
                var data = DataService.CreateNewTaxYear(currentYear, null);
                _appData.TaxYears.Add(data);
            }

            foreach (var taxYear in _appData.TaxYears)
            {
                AddTab(taxYear);
            }

            if (TaxYearTabs.TabItems.Count > 0)
                TaxYearTabs.SelectedIndex = 0;

            _isLoading = false;
        }

        private TabViewItem AddTab(TaxYearData taxYearData)
        {
            var content = new TaxYearTabContent();
            content.LoadData(taxYearData);
            content.DataChanged += (s, e) => SaveData();

            var tab = new TabViewItem
            {
                Header = taxYearData.TaxYear,
                Content = content,
                Tag = taxYearData,
                IsClosable = _appData.TaxYears.Count > 1,
            };

            TaxYearTabs.TabItems.Add(tab);
            UpdateTabClosability();
            return tab;
        }

        private void UpdateTabClosability()
        {
            bool canClose = TaxYearTabs.TabItems.Count > 1;
            foreach (var item in TaxYearTabs.TabItems)
            {
                if (item is TabViewItem tab)
                    tab.IsClosable = canClose;
            }
        }

        private async void TaxYearTabs_AddTabButtonClick(TabView sender, object args)
        {
            var availableYears = TaxRulesProvider.GetAvailableTaxYears();
            var existingYears = _appData.TaxYears.Select(t => t.TaxYear).ToHashSet();
            var newYears = availableYears.Where(y => !existingYears.Contains(y)).ToList();

            if (newYears.Count == 0)
            {
                var noYearsDialog = new ContentDialog
                {
                    Title = "No More Tax Years",
                    Content = "All available tax years have already been added.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await noYearsDialog.ShowAsync();
                return;
            }

            var combo = new ComboBox
            {
                ItemsSource = newYears,
                SelectedIndex = newYears.Count - 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0),
            };

            var dialog = new ContentDialog
            {
                Title = "Add Tax Year",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Select a tax year to add:" },
                        combo,
                        new TextBlock
                        {
                            Text = "Non-ended employments and savings accounts will be carried forward from the previous year.",
                            Margin = new Thickness(0, 12, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                        },
                    },
                },
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && combo.SelectedItem is string selectedYear)
            {
                var rules = TaxRulesProvider.GetRules(selectedYear);
                if (rules == null)
                {
                    var warnDialog = new ContentDialog
                    {
                        Title = "Warning",
                        Content = $"No tax rules found for {selectedYear}. The calculator may not produce accurate results.",
                        PrimaryButtonText = "Add Anyway",
                        CloseButtonText = "Cancel",
                        XamlRoot = Content.XamlRoot,
                    };
                    var warnResult = await warnDialog.ShowAsync();
                    if (warnResult != ContentDialogResult.Primary)
                        return;
                }

                TaxYearData? previousYear = null;
                var sortedExisting = _appData.TaxYears.OrderByDescending(t => t.TaxYear).ToList();
                foreach (var ty in sortedExisting)
                {
                    if (string.Compare(ty.TaxYear, selectedYear) < 0)
                    {
                        previousYear = ty;
                        break;
                    }
                }

                var newData = DataService.CreateNewTaxYear(selectedYear, previousYear);
                _appData.TaxYears.Add(newData);
                _appData.TaxYears = _appData.TaxYears.OrderBy(t => t.TaxYear).ToList();

                TaxYearTabs.TabItems.Clear();
                foreach (var ty in _appData.TaxYears)
                {
                    AddTab(ty);
                }

                var newTabIndex = _appData.TaxYears.FindIndex(t => t.TaxYear == selectedYear);
                if (newTabIndex >= 0)
                    TaxYearTabs.SelectedIndex = newTabIndex;

                SaveData();
            }
        }

        private async void TaxYearTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (args.Tab.Tag is TaxYearData taxYearData)
            {
                var dialog = new ContentDialog
                {
                    Title = "Remove Tax Year",
                    Content = $"Are you sure you want to remove tax year {taxYearData.TaxYear}? All entered data for this year will be lost.",
                    PrimaryButtonText = "Remove",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = Content.XamlRoot,
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _appData.TaxYears.Remove(taxYearData);
                    sender.TabItems.Remove(args.Tab);
                    UpdateTabClosability();
                    SaveData();
                }
            }
        }

        private void TaxYearTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SyncAllTabs();
            SaveData();
        }

        private void SyncAllTabs()
        {
            foreach (var item in TaxYearTabs.TabItems)
            {
                if (item is TabViewItem tab && tab.Content is TaxYearTabContent content)
                {
                    if (content.TaxYearData != null)
                    {
                        content.TaxYearData.ClaimMarriageAllowance =
                            content.FindName("MarriageAllowanceCheck") is CheckBox mc && mc.IsChecked == true;
                        content.TaxYearData.ClaimBlindPersonsAllowance =
                            content.FindName("BlindPersonCheck") is CheckBox bc && bc.IsChecked == true;
                        content.TaxYearData.IsMarriageAllowanceReceiver =
                            content.FindName("MAReceiver") is RadioButton rb && rb.IsChecked == true;
                        if (content.FindName("GiftAidBox") is NumberBox gab)
                            content.TaxYearData.GiftAidDonations = double.IsNaN(gab.Value) ? 0 : gab.Value;
                    }
                }
            }
        }

        private void SaveData()
        {
            if (_isLoading) return;
            SaveWindowPosition();
            DataService.Save(_appData);
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            SyncAllTabs();
            SaveWindowPosition();
            DataService.Save(_appData);
        }
    }
}
