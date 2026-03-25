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
    }
}
