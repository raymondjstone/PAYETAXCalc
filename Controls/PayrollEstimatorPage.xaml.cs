using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PAYETAXCalc.Models;
using PAYETAXCalc.Services;

namespace PAYETAXCalc.Controls
{
    /// <summary>
    /// Display DTO for a single pay period row in the results table.
    /// Pre-formatted strings avoid the need for value converters in the DataTemplate.
    /// </summary>
    public sealed class PayrollRowDisplay
    {
        public string PeriodLabel { get; init; } = "";
        public string GrossText { get; init; } = "";
        public string TaxText { get; init; } = "";
        public string EmpNIText { get; init; } = "";
        public string EmpPensionText { get; init; } = "";
        public string NetPayText { get; init; } = "";
        public string ErNIText { get; init; } = "";
        public string ErPensionText { get; init; } = "";
    }

    public sealed partial class PayrollEstimatorPage : UserControl
    {
        private List<TaxYearData>? _taxYearDataList;

        public PayrollEstimatorPage()
        {
            InitializeComponent();
            PopulateTaxYears();
            TaxYearCombo.SelectionChanged += TaxYearCombo_SelectionChanged;
        }

        public void SetTaxYearDataList(List<TaxYearData> taxYearDataList)
        {
            _taxYearDataList = taxYearDataList;

            // The initial selection already fired before the list was available,
            // so silently copy data if the estimator is still empty.
            string? selectedYear = TaxYearCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedYear)) return;
            if (!double.IsNaN(GrossSalaryBox.Value) && GrossSalaryBox.Value > 0) return;

            var matchingData = _taxYearDataList.FirstOrDefault(t => t.TaxYear == selectedYear);
            if (matchingData == null) return;

            double totalGross = matchingData.Employments.Sum(emp => emp.GrossSalary);
            if (totalGross <= 0) return;

            GrossSalaryBox.Value = totalGross;
            if (!string.IsNullOrWhiteSpace(matchingData.TaxCode))
                TaxCodeBox.Text = matchingData.TaxCode;
            ScottishCheck.IsChecked = matchingData.IsScottishTaxpayer;
            WelshCheck.IsChecked = matchingData.IsWelshTaxpayer;
        }

        private void PopulateTaxYears()
        {
            var years = TaxRulesProvider.GetAvailableTaxYears().ToList();
            TaxYearCombo.ItemsSource = years;
            string current = TaxRulesProvider.GetCurrentTaxYear();
            int idx = years.IndexOf(current);
            TaxYearCombo.SelectedIndex = idx >= 0 ? idx : Math.Max(0, years.Count - 1);
        }

        private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            string? taxYear = TaxYearCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(taxYear)) return;


            decimal annualGross = 0m;
            if (AnnualGrossRadio.IsChecked == true)
            {
                annualGross = double.IsNaN(GrossSalaryBox.Value) ? 0m : (decimal)GrossSalaryBox.Value;
            }
            else
            {
                decimal weeklyGross = double.IsNaN(GrossWeeklyBox.Value) ? 0m : (decimal)GrossWeeklyBox.Value;
                annualGross = weeklyGross * 52;
            }
            if (annualGross <= 0m)
            {
                var dlg = new ContentDialog
                {
                    Title = "Input Required",
                    Content = AnnualGrossRadio.IsChecked == true ? "Please enter a valid annual gross salary." : "Please enter a valid weekly gross salary.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                await dlg.ShowAsync();
                return;
            }

            var rules = TaxRulesProvider.GetOrEstimateRules(taxYear);
            string taxCode = string.IsNullOrWhiteSpace(TaxCodeBox.Text) ? "1257L" : TaxCodeBox.Text.Trim();

            bool empPensionFixed = EmpPensionTypeCombo.SelectedIndex == 1;
            decimal empPensionValue = double.IsNaN(EmpPensionValueBox.Value) ? 0m : (decimal)EmpPensionValueBox.Value;
            bool erPensionFixed = ErPensionTypeCombo.SelectedIndex == 1;
            decimal erPensionValue = double.IsNaN(ErPensionValueBox.Value) ? 0m : (decimal)ErPensionValueBox.Value;

            var input = new PayrollInput
            {
                TaxYear = taxYear,
                Frequency = MonthlyRadio.IsChecked == true ? PayFrequency.Monthly : PayFrequency.Weekly,
                AnnualGross = annualGross,
                TaxCode = taxCode,
                IsScottish = ScottishCheck.IsChecked == true,
                IsWelsh = WelshCheck.IsChecked == true,
                EmployeePensionType = empPensionFixed ? PensionContributionType.FixedAmount : PensionContributionType.PercentOfGross,
                EmployeePensionValue = empPensionValue,
                EmployerPensionType = erPensionFixed ? PensionContributionType.FixedAmount : PensionContributionType.PercentOfGross,
                EmployerPensionValue = erPensionValue,
            };

            var results = PayrollCalculatorService.Calculate(input, rules);
            DisplayResults(input, results);
        }

        private void DisplayResults(PayrollInput input, List<PayrollPeriodResult> results)
        {
            if (results.Count == 0) return;

            string freqLabel = input.Frequency == PayFrequency.Monthly ? "monthly" : "weekly";
            ResultsTitleText.Text = $"{input.TaxYear} — {freqLabel} payroll ({results.Count} periods)";

            decimal totalGross = results.Sum(r => r.GrossPay);
            decimal totalTax = results.Sum(r => r.EmployeeTax);
            decimal totalEmpNI = results.Sum(r => r.EmployeeNI);
            decimal totalEmpPension = results.Sum(r => r.EmployeePension);
            decimal totalNet = results.Sum(r => r.NetPay);
            decimal totalErNI = results.Sum(r => r.EmployerNI);
            decimal totalErPension = results.Sum(r => r.EmployerPension);

            SummaryGrossText.Text = Fmt(totalGross);
            SummaryTaxText.Text = Fmt(totalTax);
            SummaryEmpNIText.Text = Fmt(totalEmpNI);
            SummaryEmpPensionText.Text = Fmt(totalEmpPension);
            SummaryNetText.Text = Fmt(totalNet);
            SummaryErNIText.Text = Fmt(totalErNI);
            SummaryErPensionText.Text = Fmt(totalErPension);

            TotalsGrossText.Text = Fmt(totalGross);
            TotalsTaxText.Text = Fmt(totalTax);
            TotalsEmpNIText.Text = Fmt(totalEmpNI);
            TotalsEmpPensionText.Text = Fmt(totalEmpPension);
            TotalsNetText.Text = Fmt(totalNet);
            TotalsErNIText.Text = Fmt(totalErNI);
            TotalsErPensionText.Text = Fmt(totalErPension);

            PeriodsItemsControl.ItemsSource = results.Select(r => new PayrollRowDisplay
            {
                PeriodLabel = r.PeriodLabel,
                GrossText = Fmt(r.GrossPay),
                TaxText = Fmt(r.EmployeeTax),
                EmpNIText = Fmt(r.EmployeeNI),
                EmpPensionText = Fmt(r.EmployeePension),
                NetPayText = Fmt(r.NetPay),
                ErNIText = Fmt(r.EmployerNI),
                ErPensionText = Fmt(r.EmployerPension),
            }).ToList();

            ResultsBorder.Visibility = Visibility.Visible;
        }

        private static string Fmt(decimal value) => $"£{value:N2}";

        private async void TaxYearCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_taxYearDataList == null || Content?.XamlRoot == null) return;

            string? selectedYear = TaxYearCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedYear)) return;

            var matchingData = _taxYearDataList.FirstOrDefault(t => t.TaxYear == selectedYear);
            if (matchingData == null) return;

            // Sum gross salary across all employments
            double totalGross = matchingData.Employments.Sum(emp => emp.GrossSalary);
            if (totalGross <= 0) return;

            var dialog = new ContentDialog
            {
                Title = "Copy Data from Tax Year?",
                Content = $"The {selectedYear} tax year tab has salary data " +
                          $"(£{totalGross:N2} gross across {matchingData.Employments.Count} employment(s)).\n\n" +
                          "Would you like to copy the salary and settings to the payroll estimator?",
                PrimaryButtonText = "Copy",
                CloseButtonText = "No thanks",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            GrossSalaryBox.Value = totalGross;

            if (!string.IsNullOrWhiteSpace(matchingData.TaxCode))
                TaxCodeBox.Text = matchingData.TaxCode;

            ScottishCheck.IsChecked = matchingData.IsScottishTaxpayer;
            WelshCheck.IsChecked = matchingData.IsWelshTaxpayer;
        }

        private void ScottishCheck_Checked(object sender, RoutedEventArgs e) =>
            WelshCheck.IsChecked = false;

        private void WelshCheck_Checked(object sender, RoutedEventArgs e) =>
            ScottishCheck.IsChecked = false;

        private void GrossInputType_Checked(object sender, RoutedEventArgs e)
        {
            if (AnnualGrossRadio != null && GrossSalaryBox != null && GrossWeeklyBox != null)
            {
                if (AnnualGrossRadio.IsChecked == true)
                {
                    GrossSalaryBox.Visibility = Visibility.Visible;
                    GrossWeeklyBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    GrossSalaryBox.Visibility = Visibility.Collapsed;
                    GrossWeeklyBox.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
