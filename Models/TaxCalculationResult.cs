using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class TaxCalculationResult : NotifyBase
    {
        private decimal _totalEmploymentIncome;
        private decimal _totalBenefitsInKind;
        private decimal _totalPensionContributions;
        private decimal _totalEmploymentExpenses;
        private decimal _totalSavingsInterest;
        private decimal _totalTaxFreeSavings;
        private decimal _grossIncome;
        private decimal _giftAidExtension;
        private decimal _personalAllowanceUsed;
        private decimal _taxableNonSavingsIncome;
        private decimal _taxableSavingsIncome;
        private decimal _taxAtBasicRate;
        private decimal _incomeAtBasicRate;
        private decimal _taxAtHigherRate;
        private decimal _incomeAtHigherRate;
        private decimal _taxAtAdditionalRate;
        private decimal _incomeAtAdditionalRate;
        private decimal _savingsTaxDue;
        private decimal _marriageAllowanceCredit;
        private decimal _totalIncomeTaxDue;
        private decimal _totalTaxPaidViaPAYE;
        private decimal _totalNIPaid;
        private decimal _expectedNI;
        private decimal _taxOverUnderPayment;
        private string _summary = "";
        private string _expensesBreakdown = "";
        private List<TaxBreakdownLine> _taxBreakdown = new();
        private decimal _pensionTaxCreditClaimable;
        private string _pensionTaxCreditInfo = "";
        private bool _canClaimPensionTaxCredit;
        private decimal _reliefAtSourceContributions;
        private decimal _totalDividendIncome;
        private decimal _dividendTaxDue;
        private decimal _totalDividendTaxPaid;

        // Student Loan
        private decimal _studentLoanRepayment;
        private string _studentLoanInfo = "";

        // High Income Child Benefit Charge
        private decimal _childBenefitCharge;
        private string _childBenefitInfo = "";

        // Capital Gains Tax
        private decimal _capitalGainsTax;
        private string _capitalGainsInfo = "";
        private decimal _totalCapitalGains;

        // Rental/Property Income
        private decimal _rentalProfit;
        private decimal _rentalTaxableIncome;
        private decimal _mortgageInterestRelief;
        private string _rentalInfo = "";

        // Trading Income
        private decimal _tradingTaxableIncome;
        private string _tradingInfo = "";

        // Pension Annual Allowance
        private decimal _pensionAnnualAllowanceCharge;
        private string _pensionAACInfo = "";

        // Investment Reliefs
        private decimal _totalInvestmentRelief;
        private string _investmentReliefInfo = "";

        // Company Car
        private decimal _totalCompanyCarBenefit;
        private string _companyCarInfo = "";

        // Prior Year Tax
        private decimal _priorYearTaxCollected;

        // NI Estimation (combined tax+NI)
        private bool _hasSeparateNIFigures;
        private string _niEstimationInfo = "";

        // Tax Code Validation
        private string _taxCodeValidation = "";
        private bool _taxCodeHasWarning;

        public decimal TotalEmploymentIncome { get => _totalEmploymentIncome; set => SetProperty(ref _totalEmploymentIncome, value); }
        public decimal TotalBenefitsInKind { get => _totalBenefitsInKind; set => SetProperty(ref _totalBenefitsInKind, value); }
        public decimal TotalPensionContributions { get => _totalPensionContributions; set => SetProperty(ref _totalPensionContributions, value); }
        public decimal TotalEmploymentExpenses { get => _totalEmploymentExpenses; set => SetProperty(ref _totalEmploymentExpenses, value); }
        public decimal TotalSavingsInterest { get => _totalSavingsInterest; set => SetProperty(ref _totalSavingsInterest, value); }
        public decimal TotalTaxFreeSavings { get => _totalTaxFreeSavings; set => SetProperty(ref _totalTaxFreeSavings, value); }
        public decimal GrossIncome { get => _grossIncome; set => SetProperty(ref _grossIncome, value); }
        public decimal GiftAidExtension { get => _giftAidExtension; set => SetProperty(ref _giftAidExtension, value); }
        public decimal PersonalAllowanceUsed { get => _personalAllowanceUsed; set => SetProperty(ref _personalAllowanceUsed, value); }
        public decimal TaxableNonSavingsIncome { get => _taxableNonSavingsIncome; set => SetProperty(ref _taxableNonSavingsIncome, value); }
        public decimal TaxableSavingsIncome { get => _taxableSavingsIncome; set => SetProperty(ref _taxableSavingsIncome, value); }
        public decimal TaxAtBasicRate { get => _taxAtBasicRate; set => SetProperty(ref _taxAtBasicRate, value); }
        public decimal IncomeAtBasicRate { get => _incomeAtBasicRate; set => SetProperty(ref _incomeAtBasicRate, value); }
        public decimal TaxAtHigherRate { get => _taxAtHigherRate; set => SetProperty(ref _taxAtHigherRate, value); }
        public decimal IncomeAtHigherRate { get => _incomeAtHigherRate; set => SetProperty(ref _incomeAtHigherRate, value); }
        public decimal TaxAtAdditionalRate { get => _taxAtAdditionalRate; set => SetProperty(ref _taxAtAdditionalRate, value); }
        public decimal IncomeAtAdditionalRate { get => _incomeAtAdditionalRate; set => SetProperty(ref _incomeAtAdditionalRate, value); }
        public decimal SavingsTaxDue { get => _savingsTaxDue; set => SetProperty(ref _savingsTaxDue, value); }
        public decimal MarriageAllowanceCredit { get => _marriageAllowanceCredit; set => SetProperty(ref _marriageAllowanceCredit, value); }
        public decimal TotalIncomeTaxDue { get => _totalIncomeTaxDue; set => SetProperty(ref _totalIncomeTaxDue, value); }
        public decimal TotalTaxPaidViaPAYE { get => _totalTaxPaidViaPAYE; set => SetProperty(ref _totalTaxPaidViaPAYE, value); }
        public decimal TotalNIPaid { get => _totalNIPaid; set => SetProperty(ref _totalNIPaid, value); }
        public decimal ExpectedNI { get => _expectedNI; set => SetProperty(ref _expectedNI, value); }
        public decimal TaxOverUnderPayment { get => _taxOverUnderPayment; set => SetProperty(ref _taxOverUnderPayment, value); }
        public string Summary { get => _summary; set => SetProperty(ref _summary, value); }
        public string ExpensesBreakdown { get => _expensesBreakdown; set => SetProperty(ref _expensesBreakdown, value); }
        public List<TaxBreakdownLine> TaxBreakdown { get => _taxBreakdown; set => SetProperty(ref _taxBreakdown, value); }
        public decimal PensionTaxCreditClaimable { get => _pensionTaxCreditClaimable; set => SetProperty(ref _pensionTaxCreditClaimable, value); }
        public string PensionTaxCreditInfo { get => _pensionTaxCreditInfo; set => SetProperty(ref _pensionTaxCreditInfo, value); }
        public bool CanClaimPensionTaxCredit { get => _canClaimPensionTaxCredit; set => SetProperty(ref _canClaimPensionTaxCredit, value); }
        public decimal ReliefAtSourceContributions { get => _reliefAtSourceContributions; set => SetProperty(ref _reliefAtSourceContributions, value); }
        public decimal TotalDividendIncome { get => _totalDividendIncome; set => SetProperty(ref _totalDividendIncome, value); }
        public decimal DividendTaxDue { get => _dividendTaxDue; set => SetProperty(ref _dividendTaxDue, value); }
        public decimal TotalDividendTaxPaid { get => _totalDividendTaxPaid; set => SetProperty(ref _totalDividendTaxPaid, value); }

        // Student Loan
        public decimal StudentLoanRepayment { get => _studentLoanRepayment; set => SetProperty(ref _studentLoanRepayment, value); }
        public string StudentLoanInfo { get => _studentLoanInfo; set => SetProperty(ref _studentLoanInfo, value); }

        // High Income Child Benefit Charge
        public decimal ChildBenefitCharge { get => _childBenefitCharge; set => SetProperty(ref _childBenefitCharge, value); }
        public string ChildBenefitInfo { get => _childBenefitInfo; set => SetProperty(ref _childBenefitInfo, value); }

        // Capital Gains Tax
        public decimal CapitalGainsTax { get => _capitalGainsTax; set => SetProperty(ref _capitalGainsTax, value); }
        public string CapitalGainsInfo { get => _capitalGainsInfo; set => SetProperty(ref _capitalGainsInfo, value); }
        public decimal TotalCapitalGains { get => _totalCapitalGains; set => SetProperty(ref _totalCapitalGains, value); }

        // Rental/Property Income
        public decimal RentalProfit { get => _rentalProfit; set => SetProperty(ref _rentalProfit, value); }
        public decimal RentalTaxableIncome { get => _rentalTaxableIncome; set => SetProperty(ref _rentalTaxableIncome, value); }
        public decimal MortgageInterestRelief { get => _mortgageInterestRelief; set => SetProperty(ref _mortgageInterestRelief, value); }
        public string RentalInfo { get => _rentalInfo; set => SetProperty(ref _rentalInfo, value); }

        // Trading Income
        public decimal TradingTaxableIncome { get => _tradingTaxableIncome; set => SetProperty(ref _tradingTaxableIncome, value); }
        public string TradingInfo { get => _tradingInfo; set => SetProperty(ref _tradingInfo, value); }

        // Pension Annual Allowance Charge
        public decimal PensionAnnualAllowanceCharge { get => _pensionAnnualAllowanceCharge; set => SetProperty(ref _pensionAnnualAllowanceCharge, value); }
        public string PensionAACInfo { get => _pensionAACInfo; set => SetProperty(ref _pensionAACInfo, value); }

        // Investment Reliefs
        public decimal TotalInvestmentRelief { get => _totalInvestmentRelief; set => SetProperty(ref _totalInvestmentRelief, value); }
        public string InvestmentReliefInfo { get => _investmentReliefInfo; set => SetProperty(ref _investmentReliefInfo, value); }

        // Company Car
        public decimal TotalCompanyCarBenefit { get => _totalCompanyCarBenefit; set => SetProperty(ref _totalCompanyCarBenefit, value); }
        public string CompanyCarInfo { get => _companyCarInfo; set => SetProperty(ref _companyCarInfo, value); }

        // Prior Year Tax
        public decimal PriorYearTaxCollected { get => _priorYearTaxCollected; set => SetProperty(ref _priorYearTaxCollected, value); }

        // NI Estimation (combined tax+NI)
        public bool HasSeparateNIFigures { get => _hasSeparateNIFigures; set => SetProperty(ref _hasSeparateNIFigures, value); }
        public string NIEstimationInfo { get => _niEstimationInfo; set => SetProperty(ref _niEstimationInfo, value); }

        // Tax Code Validation
        public string TaxCodeValidation { get => _taxCodeValidation; set => SetProperty(ref _taxCodeValidation, value); }
        public bool TaxCodeHasWarning { get => _taxCodeHasWarning; set => SetProperty(ref _taxCodeHasWarning, value); }
    }
}
