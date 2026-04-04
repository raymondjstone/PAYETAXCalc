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
        private TabViewItem _payrollTab = null!;
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

            // Record first-use timestamp for the coffee prompt delay
            if (_appData.FirstAppUse == null)
            {
                _appData.FirstAppUse = DateTimeOffset.UtcNow;
                DataService.Save(_appData);
            }

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

            _payrollTab = CreatePayrollTab();
            TaxYearTabs.TabItems.Add(_payrollTab);

            if (TaxYearTabs.TabItems.Count > 0)
                TaxYearTabs.SelectedIndex = 0;

            _isLoading = false;
        }

        private TabViewItem CreatePayrollTab()
        {
            var page = new PayrollEstimatorPage();
            page.SetTaxYearDataList(_appData.TaxYears);
            return new TabViewItem
            {
                Header = "Payroll Estimator",
                Content = page,
                IsClosable = false,
                Tag = "PAYROLL_ESTIMATOR",
            };
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
            bool canClose = _appData.TaxYears.Count > 1;
            foreach (var item in TaxYearTabs.TabItems)
            {
                if (item is TabViewItem tab && tab.Tag is TaxYearData)
                    tab.IsClosable = canClose;
            }
        }

        private async void TaxYearTabs_AddTabButtonClick(TabView sender, object args)
        {
            var suggestedYears = TaxRulesProvider.GetAvailableTaxYears();
            var existingYears = _appData.TaxYears.Select(t => t.TaxYear).ToHashSet();
            var newYears = suggestedYears.Where(y => !existingYears.Contains(y)).ToList();

            var combo = new ComboBox
            {
                ItemsSource = newYears,
                SelectedIndex = newYears.Count > 0 ? newYears.Count - 1 : -1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0),
                IsEditable = true,
                PlaceholderText = "e.g. 2027/28 or 2027",
            };

            var infoText = new TextBlock
            {
                Text = "Select a suggested year or type any year (e.g. 2027/28).\n" +
                       "Non-ended employments and savings will be carried forward.\n" +
                       "Future years use the latest known tax rates as estimates.",
                Margin = new Thickness(0, 12, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            };

            var dialog = new ContentDialog
            {
                Title = "Add Tax Year",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Select or enter a tax year:" },
                        combo,
                        infoText,
                    },
                },
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            // Get the selected or typed value
            string? input = combo.SelectedItem as string ?? combo.Text;
            if (string.IsNullOrWhiteSpace(input)) return;

            // Parse and validate the tax year
            string selectedYear;
            if (TaxRulesProvider.TryParseTaxYear(input, out string parsed))
            {
                selectedYear = parsed;
            }
            else
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Invalid Tax Year",
                    Content = "Please enter a valid tax year in YYYY/YY format (e.g. 2027/28) or just the start year (e.g. 2027).",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await errorDialog.ShowAsync();
                return;
            }

            // Check if already exists
            if (existingYears.Contains(selectedYear))
            {
                var existsDialog = new ContentDialog
                {
                    Title = "Already Added",
                    Content = $"Tax year {selectedYear} has already been added.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await existsDialog.ShowAsync();
                return;
            }

            // Warn if estimated rates
            if (TaxRulesProvider.IsEstimated(selectedYear))
            {
                var warnDialog = new ContentDialog
                {
                    Title = "Estimated Rates",
                    Content = $"No specific tax rules are defined for {selectedYear}.\n\n" +
                              "The latest known rates will be used as an estimate. " +
                              "Rates will be updated automatically if specific rules become available in a future version.",
                    PrimaryButtonText = "Add with Estimated Rates",
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
            TaxYearTabs.TabItems.Add(_payrollTab);
            if (_payrollTab.Content is PayrollEstimatorPage payrollPage)
                payrollPage.SetTaxYearDataList(_appData.TaxYears);

            var newTabIndex = _appData.TaxYears.FindIndex(t => t.TaxYear == selectedYear);
            if (newTabIndex >= 0)
                TaxYearTabs.SelectedIndex = newTabIndex;

            SaveData();
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

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            var confirmDialog = new ContentDialog
            {
                Title = "Import Backup",
                Content = $"This will replace ALL current data with the contents of:\n{file.Path}\n\nThis cannot be undone. Continue?",
                PrimaryButtonText = "Import",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot,
            };

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary) return;

            var imported = DataService.ImportBackup(file.Path);
            if (imported == null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Import Failed",
                    Content = "The selected file could not be read as a valid backup.\nPlease choose a file exported by this application.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await errorDialog.ShowAsync();
                return;
            }

            _appData = imported;
            TaxYearTabs.TabItems.Clear();

            if (_appData.TaxYears.Count == 0)
            {
                string currentYear = TaxRulesProvider.GetCurrentTaxYear();
                var data = DataService.CreateNewTaxYear(currentYear, null);
                _appData.TaxYears.Add(data);
            }

            foreach (var ty in _appData.TaxYears)
                AddTab(ty);

            _payrollTab = CreatePayrollTab();
            TaxYearTabs.TabItems.Add(_payrollTab);

            if (TaxYearTabs.TabItems.Count > 0)
                TaxYearTabs.SelectedIndex = 0;

            DataService.Save(_appData);

            var successDialog = new ContentDialog
            {
                Title = "Import Successful",
                Content = $"Data imported from:\n{file.Path}",
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot,
            };
            await successDialog.ShowAsync();
        }

        private async void Backup_Click(object sender, RoutedEventArgs e)
        {
            SyncAllTabs();
            SaveData();

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            // Required for unpackaged (MSI) apps — associate the picker with the window handle
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON Backup", new System.Collections.Generic.List<string> { ".json" });
            picker.SuggestedFileName = $"PAYETAXCalc-backup-{DateTime.Now:yyyy-MM-dd}";

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            try
            {
                DataService.ExportBackup(_appData, file.Path);

                var dialog = new ContentDialog
                {
                    Title = "Backup Saved",
                    Content = $"All data exported to:\n{file.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Backup Failed",
                    Content = $"Could not write backup file:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await dialog.ShowAsync();
            }
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

        private void BuyMeCoffeeButton_Click(object sender, RoutedEventArgs e)
        {
            _appData.BuyMeCoffeeClicked = true;
            _appData.LastCoffeePrompt = DateTimeOffset.UtcNow;
            DataService.Save(_appData);
        }

        public async void ShowCoffeePromptIfNeeded()
        {
            if (_appData.BuyMeCoffeeClicked)
                return;

            var now = DateTimeOffset.UtcNow;

            // Don't show within the first 2 hours of first use
            if (_appData.FirstAppUse.HasValue && (now - _appData.FirstAppUse.Value).TotalHours < 2)
                return;

            // Don't show more than once a fortnight
            if (_appData.LastCoffeePrompt.HasValue && (now - _appData.LastCoffeePrompt.Value).TotalDays < 14)
                return;

            _appData.LastCoffeePrompt = now;
            DataService.Save(_appData);

            var dialog = new ContentDialog
            {
                Title = "Support PAYETAXCalc",
                Content = "If you find this app useful, please consider buying me a coffee! " +
                          "Clicking the button means you won't see this message again.\n\n" +
                          "☕ https://buymeacoffee.com/raymondjstone",
                PrimaryButtonText = "Open Buy Me a Coffee",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot,
            };

            if (dialog.XamlRoot == null) return;

            dialog.PrimaryButtonClick += (s, e) =>
            {
                BuyMeCoffeeButton_Click(null!, null!);
                _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://buymeacoffee.com/raymondjstone"));
            };

            await dialog.ShowAsync();
        }
    }
}
