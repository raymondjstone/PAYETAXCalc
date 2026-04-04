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
            DividendsPanel.ItemsSource = data.DividendIncomes;
            CapitalGainsPanel.ItemsSource = data.CapitalGains;

            MarriageAllowanceCheck.IsChecked = data.ClaimMarriageAllowance;
            BlindPersonCheck.IsChecked = data.ClaimBlindPersonsAllowance;
            ScottishTaxpayerCheck.IsChecked = data.IsScottishTaxpayer;
            WelshTaxpayerCheck.IsChecked = data.IsWelshTaxpayer;
            GiftAidBox.Value = data.GiftAidDonations;
            ReliefAtSourceBox.Value = data.ReliefAtSourcePensionContributions;

            if (data.IsMarriageAllowanceReceiver)
                MAReceiver.IsChecked = true;
            else
                MATransferrer.IsChecked = true;

            // Student Loans
            HasStudentLoanCheck.IsChecked = data.HasStudentLoan;
            StudentLoanPlanPanel.Visibility = data.HasStudentLoan ? Visibility.Visible : Visibility.Collapsed;
            switch (data.StudentLoanPlan)
            {
                case 1: SLPlan1.IsChecked = true; break;
                case 2: SLPlan2.IsChecked = true; break;
                case 4: SLPlan4.IsChecked = true; break;
                case 5: SLPlan5.IsChecked = true; break;
            }
            HasPostgraduateLoanCheck.IsChecked = data.HasPostgraduateLoan;

            // Child Benefit
            NumberOfChildrenBox.Value = data.NumberOfChildren;
            ChildBenefitAmountBox.Value = data.ChildBenefitAmount;

            // Capital Gains
            CapitalGainsLossesBox.Value = data.CapitalGainsLosses;
            CapitalGainsLossesPanel.Visibility = data.CapitalGains.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Rental
            RentalIncomeBox.Value = data.RentalIncome;
            RentalExpensesBox.Value = data.RentalExpenses;
            MortgageInterestBox.Value = data.MortgageInterest;
            UsePropertyAllowanceCheck.IsChecked = data.UsePropertyAllowance;

            // Trading
            TradingIncomeBox.Value = data.TradingIncome;
            TradingExpensesBox.Value = data.TradingExpenses;
            UseTradingAllowanceCheck.IsChecked = data.UseTradingAllowance;

            // Investment Reliefs
            EisBox.Value = data.EisInvestment;
            SeisBox.Value = data.SeisInvestment;
            VctBox.Value = data.VctInvestment;

            // Tax Code
            TaxCodeBox.Text = data.TaxCode;
            PriorYearTaxBox.Value = data.PriorYearTaxOwed;

            // Wire up student loan checkbox
            HasStudentLoanCheck.Checked += StudentLoan_Changed;
            HasStudentLoanCheck.Unchecked += StudentLoan_Changed;

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
            TaxYearData.IsWelshTaxpayer = WelshTaxpayerCheck.IsChecked == true;
            TaxYearData.GiftAidDonations = double.IsNaN(GiftAidBox.Value) ? 0 : GiftAidBox.Value;
            TaxYearData.ReliefAtSourcePensionContributions = double.IsNaN(ReliefAtSourceBox.Value) ? 0 : ReliefAtSourceBox.Value;

            // Student Loans
            TaxYearData.HasStudentLoan = HasStudentLoanCheck.IsChecked == true;
            if (SLPlan1.IsChecked == true) TaxYearData.StudentLoanPlan = 1;
            else if (SLPlan2.IsChecked == true) TaxYearData.StudentLoanPlan = 2;
            else if (SLPlan4.IsChecked == true) TaxYearData.StudentLoanPlan = 4;
            else if (SLPlan5.IsChecked == true) TaxYearData.StudentLoanPlan = 5;
            TaxYearData.HasPostgraduateLoan = HasPostgraduateLoanCheck.IsChecked == true;

            // Child Benefit
            TaxYearData.NumberOfChildren = double.IsNaN(NumberOfChildrenBox.Value) ? 0 : (int)NumberOfChildrenBox.Value;
            TaxYearData.ChildBenefitAmount = double.IsNaN(ChildBenefitAmountBox.Value) ? 0 : ChildBenefitAmountBox.Value;

            // Capital Gains
            TaxYearData.CapitalGainsLosses = double.IsNaN(CapitalGainsLossesBox.Value) ? 0 : CapitalGainsLossesBox.Value;

            // Rental
            TaxYearData.RentalIncome = double.IsNaN(RentalIncomeBox.Value) ? 0 : RentalIncomeBox.Value;
            TaxYearData.RentalExpenses = double.IsNaN(RentalExpensesBox.Value) ? 0 : RentalExpensesBox.Value;
            TaxYearData.MortgageInterest = double.IsNaN(MortgageInterestBox.Value) ? 0 : MortgageInterestBox.Value;
            TaxYearData.UsePropertyAllowance = UsePropertyAllowanceCheck.IsChecked == true;

            // Trading
            TaxYearData.TradingIncome = double.IsNaN(TradingIncomeBox.Value) ? 0 : TradingIncomeBox.Value;
            TaxYearData.TradingExpenses = double.IsNaN(TradingExpensesBox.Value) ? 0 : TradingExpensesBox.Value;
            TaxYearData.UseTradingAllowance = UseTradingAllowanceCheck.IsChecked == true;

            // Investment Reliefs
            TaxYearData.EisInvestment = double.IsNaN(EisBox.Value) ? 0 : EisBox.Value;
            TaxYearData.SeisInvestment = double.IsNaN(SeisBox.Value) ? 0 : SeisBox.Value;
            TaxYearData.VctInvestment = double.IsNaN(VctBox.Value) ? 0 : VctBox.Value;

            // Tax Code
            TaxYearData.TaxCode = TaxCodeBox.Text ?? "";
            TaxYearData.PriorYearTaxOwed = double.IsNaN(PriorYearTaxBox.Value) ? 0 : PriorYearTaxBox.Value;
        }

        private void ScottishTaxpayerCheck_Checked(object sender, RoutedEventArgs e) =>
            WelshTaxpayerCheck.IsChecked = false;

        private void WelshTaxpayerCheck_Checked(object sender, RoutedEventArgs e) =>
            ScottishTaxpayerCheck.IsChecked = false;

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

        private void AddDividend_Click(object sender, RoutedEventArgs e)
        {
            TaxYearData?.DividendIncomes.Add(new DividendIncome());
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveDividend_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DividendIncome div && TaxYearData != null)
            {
                TaxYearData.DividendIncomes.Remove(div);
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AddCapitalGain_Click(object sender, RoutedEventArgs e)
        {
            TaxYearData?.CapitalGains.Add(new CapitalGain());
            CapitalGainsLossesPanel.Visibility = Visibility.Visible;
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveCapitalGain_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CapitalGain cg && TaxYearData != null)
            {
                TaxYearData.CapitalGains.Remove(cg);
                if (TaxYearData.CapitalGains.Count == 0)
                    CapitalGainsLossesPanel.Visibility = Visibility.Collapsed;
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

        private void StudentLoan_Changed(object sender, RoutedEventArgs e)
        {
            if (StudentLoanPlanPanel != null)
            {
                StudentLoanPlanPanel.Visibility = HasStudentLoanCheck.IsChecked == true
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            if (TaxYearData == null || Rules == null) return;

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

            // Company Car
            if (r.TotalCompanyCarBenefit > 0)
            {
                ResCompanyCarRow.Visibility = Visibility.Visible;
                ResCompanyCar.Text = $"£{r.TotalCompanyCarBenefit:N2}";
            }
            else
            {
                ResCompanyCarRow.Visibility = Visibility.Collapsed;
            }

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

            // Rental
            if (r.RentalTaxableIncome > 0)
            {
                ResRentalRow.Visibility = Visibility.Visible;
                ResRental.Text = $"£{r.RentalTaxableIncome:N2}";
            }
            else
            {
                ResRentalRow.Visibility = Visibility.Collapsed;
            }

            // Trading
            if (r.TradingTaxableIncome > 0)
            {
                ResTradingRow.Visibility = Visibility.Visible;
                ResTrading.Text = $"£{r.TradingTaxableIncome:N2}";
            }
            else
            {
                ResTradingRow.Visibility = Visibility.Collapsed;
            }

            ResSavings.Text = $"£{r.TotalSavingsInterest:N2}";
            ResTaxFreeSavings.Text = $"£{r.TotalTaxFreeSavings:N2}";

            if (r.TotalDividendIncome > 0)
            {
                ResDividendRow.Visibility = Visibility.Visible;
                ResDividends.Text = $"£{r.TotalDividendIncome:N2}";
            }
            else
            {
                ResDividendRow.Visibility = Visibility.Collapsed;
            }

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

            // Mortgage interest relief
            if (r.MortgageInterestRelief > 0)
            {
                ResMortgageReliefRow.Visibility = Visibility.Visible;
                ResMortgageRelief.Text = $"-£{r.MortgageInterestRelief:N2}";
            }
            else
            {
                ResMortgageReliefRow.Visibility = Visibility.Collapsed;
            }

            // Investment relief
            if (r.TotalInvestmentRelief > 0)
            {
                ResInvestmentReliefRow.Visibility = Visibility.Visible;
                ResInvestmentRelief.Text = $"-£{r.TotalInvestmentRelief:N2}";
            }
            else
            {
                ResInvestmentReliefRow.Visibility = Visibility.Collapsed;
            }

            // Tax Breakdown
            string regime = TaxYearData?.IsScottishTaxpayer == true ? "Scottish" : "rUK";
            ResTaxBreakdownHeader.Text = $"Income Tax Breakdown ({regime} rates)";
            TaxBreakdownItems.ItemsSource = r.TaxBreakdown;

            ResTotalTaxDue.Text = $"£{r.TotalIncomeTaxDue:N2}";
            ResTotalTaxPaid.Text = $"£{r.TotalTaxPaidViaPAYE:N2}";

            // Prior year tax collected
            if (r.PriorYearTaxCollected > 0)
            {
                ResPriorYearTaxRow.Visibility = Visibility.Visible;
                ResEffectiveTaxPaidRow.Visibility = Visibility.Visible;
                ResPriorYearTax.Text = $"-£{r.PriorYearTaxCollected:N2}";
                decimal effectivePaid = r.TotalTaxPaidViaPAYE - r.PriorYearTaxCollected;
                ResEffectiveTaxPaid.Text = $"£{effectivePaid:N2}";
            }
            else
            {
                ResPriorYearTaxRow.Visibility = Visibility.Collapsed;
                ResEffectiveTaxPaidRow.Visibility = Visibility.Collapsed;
            }

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

            if (!string.IsNullOrEmpty(r.NIEstimationInfo))
            {
                ResNIEstimationInfo.Text = r.NIEstimationInfo;
                ResNIEstimationInfo.Visibility = Visibility.Visible;
            }
            else
            {
                ResNIEstimationInfo.Visibility = Visibility.Collapsed;
            }

            // Student Loan
            if (r.StudentLoanRepayment > 0 || !string.IsNullOrEmpty(r.StudentLoanInfo))
            {
                ResStudentLoanSection.Visibility = Visibility.Visible;
                ResStudentLoanPanel.Visibility = Visibility.Visible;
                ResStudentLoan.Text = $"£{r.StudentLoanRepayment:N2}";
                ResStudentLoanInfo.Text = r.StudentLoanInfo;
            }
            else
            {
                ResStudentLoanSection.Visibility = Visibility.Collapsed;
                ResStudentLoanPanel.Visibility = Visibility.Collapsed;
            }

            // Child Benefit Charge
            if (!string.IsNullOrEmpty(r.ChildBenefitInfo))
            {
                ResChildBenefitSection.Visibility = Visibility.Visible;
                ResChildBenefitPanel.Visibility = Visibility.Visible;
                ResChildBenefit.Text = r.ChildBenefitCharge > 0 ? $"£{r.ChildBenefitCharge:N2}" : "£0.00 (no charge)";
                ResChildBenefitInfo.Text = r.ChildBenefitInfo;
            }
            else
            {
                ResChildBenefitSection.Visibility = Visibility.Collapsed;
                ResChildBenefitPanel.Visibility = Visibility.Collapsed;
            }

            // Capital Gains Tax
            if (r.TotalCapitalGains > 0 || r.CapitalGainsTax > 0 || !string.IsNullOrEmpty(r.CapitalGainsInfo))
            {
                ResCGTSection.Visibility = Visibility.Visible;
                ResCGTPanel.Visibility = Visibility.Visible;
                ResCGT.Text = $"£{r.CapitalGainsTax:N2}";
                ResCGTInfo.Text = r.CapitalGainsInfo;
            }
            else
            {
                ResCGTSection.Visibility = Visibility.Collapsed;
                ResCGTPanel.Visibility = Visibility.Collapsed;
            }

            // Pension Annual Allowance Charge
            if (r.PensionAnnualAllowanceCharge > 0)
            {
                ResPensionAACSection.Visibility = Visibility.Visible;
                ResPensionAACPanel.Visibility = Visibility.Visible;
                ResPensionAAC.Text = $"£{r.PensionAnnualAllowanceCharge:N2}";
                ResPensionAACInfo.Text = r.PensionAACInfo;
            }
            else
            {
                ResPensionAACSection.Visibility = Visibility.Collapsed;
                ResPensionAACPanel.Visibility = Visibility.Collapsed;
            }

            // Tax Code Validation
            if (!string.IsNullOrEmpty(r.TaxCodeValidation))
            {
                ResTaxCodeSection.Visibility = Visibility.Visible;
                ResTaxCodePanel.Visibility = Visibility.Visible;
                ResTaxCodeInfo.Text = r.TaxCodeValidation;
                ResTaxCodeInfo.Foreground = r.TaxCodeHasWarning
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed)
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
            }
            else
            {
                ResTaxCodeSection.Visibility = Visibility.Collapsed;
                ResTaxCodePanel.Visibility = Visibility.Collapsed;
            }

            // Pension Tax Credit
            if (r.ReliefAtSourceContributions > 0)
            {
                ResPensionTaxCreditSection.Visibility = Visibility.Visible;
                ResPensionTaxCreditPanel.Visibility = Visibility.Visible;
                ResReliefAtSource.Text = $"£{r.ReliefAtSourceContributions:N2}";
                ResPensionCreditInfo.Text = r.PensionTaxCreditInfo;

                if (r.CanClaimPensionTaxCredit && r.PensionTaxCreditClaimable > 0)
                {
                    ResPensionCreditRow.Visibility = Visibility.Visible;
                    ResPensionCredit.Text = $"£{r.PensionTaxCreditClaimable:N2}";
                }
                else
                {
                    ResPensionCreditRow.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ResPensionTaxCreditSection.Visibility = Visibility.Collapsed;
                ResPensionTaxCreditPanel.Visibility = Visibility.Collapsed;
            }

            // Company Car details
            if (!string.IsNullOrEmpty(r.CompanyCarInfo))
            {
                ResCompanyCarSection.Visibility = Visibility.Visible;
                ResCompanyCarPanel.Visibility = Visibility.Visible;
                ResCompanyCarInfo.Text = r.CompanyCarInfo;
            }
            else
            {
                ResCompanyCarSection.Visibility = Visibility.Collapsed;
                ResCompanyCarPanel.Visibility = Visibility.Collapsed;
            }

            // Investment Relief details
            if (!string.IsNullOrEmpty(r.InvestmentReliefInfo))
            {
                ResInvestmentSection.Visibility = Visibility.Visible;
                ResInvestmentPanel.Visibility = Visibility.Visible;
                ResInvestmentInfo.Text = r.InvestmentReliefInfo;
            }
            else
            {
                ResInvestmentSection.Visibility = Visibility.Collapsed;
                ResInvestmentPanel.Visibility = Visibility.Collapsed;
            }

            // Rental details
            if (!string.IsNullOrEmpty(r.RentalInfo))
            {
                ResRentalSection.Visibility = Visibility.Visible;
                ResRentalPanel.Visibility = Visibility.Visible;
                ResRentalInfo.Text = r.RentalInfo;
            }
            else
            {
                ResRentalSection.Visibility = Visibility.Collapsed;
                ResRentalPanel.Visibility = Visibility.Collapsed;
            }

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
