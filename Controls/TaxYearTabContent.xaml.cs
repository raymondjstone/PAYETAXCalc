using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PAYETAXCalc.Models;
using PAYETAXCalc.Services;
using Windows.Storage.Pickers;

namespace PAYETAXCalc.Controls
{
    public sealed partial class TaxYearTabContent : UserControl
    {
        public TaxYearData? TaxYearData { get; private set; }
        public TaxYearRules? Rules { get; private set; }
        private TaxCalculationResult? _lastResult;

        public event EventHandler? DataChanged;

        public TaxYearTabContent()
        {
            InitializeComponent();
        }

        public void LoadData(TaxYearData data)
        {
            TaxYearData = data;
            Rules = TaxRulesProvider.GetOrEstimateRules(data.TaxYear);

            EmploymentsPanel.ItemsSource = data.Employments;
            SavingsPanel.ItemsSource = data.SavingsIncomes;

            MarriageAllowanceCheck.IsChecked = data.ClaimMarriageAllowance;
            BlindPersonCheck.IsChecked = data.ClaimBlindPersonsAllowance;
            ScottishTaxpayerCheck.IsChecked = data.IsScottishTaxpayer;
            GiftAidBox.Value = data.GiftAidDonations;

            if (data.IsMarriageAllowanceReceiver)
                MAReceiver.IsChecked = true;
            else
                MATransferrer.IsChecked = true;

            RulesInfoBar.Title = $"Tax Rules for {data.TaxYear}";
            RulesInfoBar.Message = TaxRulesProvider.GetRulesSummary(Rules);
            if (TaxRulesProvider.IsEstimated(data.TaxYear))
                RulesInfoBar.Severity = InfoBarSeverity.Warning;

            ResultsPanel.Visibility = Visibility.Collapsed;
        }

        private void SyncDataFromUI()
        {
            if (TaxYearData == null) return;

            TaxYearData.ClaimMarriageAllowance = MarriageAllowanceCheck.IsChecked == true;
            TaxYearData.IsMarriageAllowanceReceiver = MAReceiver.IsChecked == true;
            TaxYearData.ClaimBlindPersonsAllowance = BlindPersonCheck.IsChecked == true;
            TaxYearData.IsScottishTaxpayer = ScottishTaxpayerCheck.IsChecked == true;
            TaxYearData.GiftAidDonations = double.IsNaN(GiftAidBox.Value) ? 0 : GiftAidBox.Value;
        }

        private void AddEmployment_Click(object sender, RoutedEventArgs e)
        {
            TaxYearData?.Employments.Add(new Employment());
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveEmployment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Employment emp && TaxYearData != null)
            {
                TaxYearData.Employments.Remove(emp);
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AddSavings_Click(object sender, RoutedEventArgs e)
        {
            TaxYearData?.SavingsIncomes.Add(new SavingsIncome());
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveSavings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SavingsIncome sav && TaxYearData != null)
            {
                TaxYearData.SavingsIncomes.Remove(sav);
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void MarriageAllowance_Changed(object sender, RoutedEventArgs e)
        {
            if (MarriageDirectionPanel != null && MarriageAllowanceCheck != null)
            {
                MarriageDirectionPanel.Visibility = MarriageAllowanceCheck.IsChecked == true
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            if (TaxYearData == null || Rules == null) return;

            // Re-fetch rules in case they've been updated for this year
            Rules = TaxRulesProvider.GetOrEstimateRules(TaxYearData.TaxYear);

            SyncDataFromUI();
            _lastResult = TaxCalculator.Calculate(TaxYearData, Rules);
            DisplayResults(_lastResult);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (TaxYearData == null || Rules == null || _lastResult == null) return;
            await ExportToFile("Excel Workbook", ".xlsx", new[] { ".xlsx" },
                path => ExportService.ExportToExcel(path, TaxYearData, Rules, _lastResult));
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (TaxYearData == null || Rules == null || _lastResult == null) return;
            await ExportToFile("PDF Document", ".pdf", new[] { ".pdf" },
                path => ExportService.ExportToPdf(path, TaxYearData, Rules, _lastResult));
        }

        private async void ExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (TaxYearData == null || Rules == null || _lastResult == null) return;
            await ExportToFile("Word Document", ".docx", new[] { ".docx" },
                path => ExportService.ExportToWord(path, TaxYearData, Rules, _lastResult));
        }

        private async System.Threading.Tasks.Task ExportToFile(
            string typeName, string defaultExt, string[] extensions, Action<string> exportAction)
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"TaxCalculation_{TaxYearData!.TaxYear.Replace("/", "-")}";
            picker.FileTypeChoices.Add(typeName, extensions);

            // WinUI 3 requires HWND
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                ((Microsoft.UI.Xaml.Application.Current as App)!).m_window!);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    exportAction(file.Path);

                    var dialog = new ContentDialog
                    {
                        Title = "Export Complete",
                        Content = $"File saved to:\n{file.Path}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot,
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Export Failed",
                        Content = $"Error: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot,
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private void DisplayResults(TaxCalculationResult r)
        {
            ResultsPanel.Visibility = Visibility.Visible;

            ResEmploymentIncome.Text = $"£{r.TotalEmploymentIncome:N2}";
            ResBIK.Text = $"£{r.TotalBenefitsInKind:N2}";

            if (r.TotalPensionContributions > 0)
            {
                ResPensionRow.Visibility = Visibility.Visible;
                ResPension.Text = $"-£{r.TotalPensionContributions:N2}";
            }
            else
            {
                ResPensionRow.Visibility = Visibility.Collapsed;
            }

            if (r.TotalEmploymentExpenses > 0)
            {
                ResExpensesRow.Visibility = Visibility.Visible;
                ResExpenses.Text = $"-£{r.TotalEmploymentExpenses:N2}";
                if (!string.IsNullOrEmpty(r.ExpensesBreakdown))
                {
                    ResExpensesBreakdown.Visibility = Visibility.Visible;
                    ResExpensesBreakdown.Text = r.ExpensesBreakdown;
                }
                else
                {
                    ResExpensesBreakdown.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ResExpensesRow.Visibility = Visibility.Collapsed;
                ResExpensesBreakdown.Visibility = Visibility.Collapsed;
            }

            ResSavings.Text = $"£{r.TotalSavingsInterest:N2}";
            ResTaxFreeSavings.Text = $"£{r.TotalTaxFreeSavings:N2}";
            ResGross.Text = $"£{r.GrossIncome:N2}";
            ResPA.Text = $"£{r.PersonalAllowanceUsed:N2}";

            if (r.GiftAidExtension > 0)
            {
                ResGiftAidRow.Visibility = Visibility.Visible;
                ResGiftAid.Text = $"£{r.GiftAidExtension:N2}";
            }
            else
            {
                ResGiftAidRow.Visibility = Visibility.Collapsed;
            }

            if (r.MarriageAllowanceCredit > 0)
            {
                ResMarriageRow.Visibility = Visibility.Visible;
                ResMarriage.Text = $"-£{r.MarriageAllowanceCredit:N2}";
            }
            else
            {
                ResMarriageRow.Visibility = Visibility.Collapsed;
            }

            // Dynamic tax breakdown (supports both Scottish and rUK bands)
            string regime = TaxYearData?.IsScottishTaxpayer == true ? "Scottish" : "rUK";
            ResTaxBreakdownHeader.Text = $"Income Tax Breakdown ({regime} rates)";
            TaxBreakdownItems.ItemsSource = r.TaxBreakdown;

            ResTotalTaxDue.Text = $"£{r.TotalIncomeTaxDue:N2}";
            ResTotalTaxPaid.Text = $"£{r.TotalTaxPaidViaPAYE:N2}";

            if (r.TaxOverUnderPayment > 0)
            {
                ResOverUnderLabel.Text = "Tax Underpaid:";
                ResOverUnder.Text = $"£{r.TaxOverUnderPayment:N2}";
                ResOverUnder.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                ResOverUnderLabel.Foreground = ResOverUnder.Foreground;
            }
            else if (r.TaxOverUnderPayment < 0)
            {
                ResOverUnderLabel.Text = "Tax Overpaid (refund due):";
                ResOverUnder.Text = $"£{Math.Abs(r.TaxOverUnderPayment):N2}";
                ResOverUnder.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
                ResOverUnderLabel.Foreground = ResOverUnder.Foreground;
            }
            else
            {
                ResOverUnderLabel.Text = "Difference:";
                ResOverUnder.Text = "£0.00";
                ResOverUnder.Foreground = null!;
                ResOverUnderLabel.Foreground = null!;
            }

            ResNIPaid.Text = $"£{r.TotalNIPaid:N2}";
            ResExpectedNI.Text = $"£{r.ExpectedNI:N2}";

            ResultInfoBar.Message = r.Summary;
            if (r.TaxOverUnderPayment > 0)
                ResultInfoBar.Severity = InfoBarSeverity.Warning;
            else if (r.TaxOverUnderPayment < 0)
                ResultInfoBar.Severity = InfoBarSeverity.Success;
            else
                ResultInfoBar.Severity = InfoBarSeverity.Informational;
        }
    }
}
