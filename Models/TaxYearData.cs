using System.Collections.ObjectModel;
using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class TaxYearData : NotifyBase
    {
        private string _taxYear = "";
        private bool _isScottishTaxpayer;
        private bool _isWelshTaxpayer;
        private bool _claimMarriageAllowance;
        private bool _isMarriageAllowanceReceiver;
        private bool _claimBlindPersonsAllowance;
        private double _giftAidDonations;
        private double _reliefAtSourcePensionContributions;

        // Student Loans
        private bool _hasStudentLoan;
        private int _studentLoanPlan; // 1, 2, 4, 5
        private bool _hasPostgraduateLoan;

        // High Income Child Benefit Charge
        private int _numberOfChildren;
        private double _childBenefitAmount; // Annual amount received

        // Capital Gains
        private double _capitalGainsLosses;

        // Rental/Property Income
        private double _rentalIncome;
        private double _rentalExpenses;
        private double _mortgageInterest;
        private bool _usePropertyAllowance;

        // Trading Income
        private double _tradingIncome;
        private double _tradingExpenses;
        private bool _useTradingAllowance;

        // Investment Reliefs
        private double _eisInvestment;
        private double _seisInvestment;
        private double _vctInvestment;

        // Prior Year Tax Collected via PAYE
        private double _priorYearTaxOwed;

        // Tax Code
        private string _taxCode = "";

        public string TaxYear
        {
            get => _taxYear;
            set => SetProperty(ref _taxYear, value);
        }

        public bool IsScottishTaxpayer
        {
            get => _isScottishTaxpayer;
            set => SetProperty(ref _isScottishTaxpayer, value);
        }

        public bool IsWelshTaxpayer
        {
            get => _isWelshTaxpayer;
            set => SetProperty(ref _isWelshTaxpayer, value);
        }

        public ObservableCollection<Employment> Employments { get; set; } = new();
        public ObservableCollection<SavingsIncome> SavingsIncomes { get; set; } = new();
        public ObservableCollection<DividendIncome> DividendIncomes { get; set; } = new();
        public ObservableCollection<CapitalGain> CapitalGains { get; set; } = new();

        public bool ClaimMarriageAllowance
        {
            get => _claimMarriageAllowance;
            set => SetProperty(ref _claimMarriageAllowance, value);
        }

        public bool IsMarriageAllowanceReceiver
        {
            get => _isMarriageAllowanceReceiver;
            set => SetProperty(ref _isMarriageAllowanceReceiver, value);
        }

        public bool ClaimBlindPersonsAllowance
        {
            get => _claimBlindPersonsAllowance;
            set => SetProperty(ref _claimBlindPersonsAllowance, value);
        }

        public double GiftAidDonations
        {
            get => _giftAidDonations;
            set => SetProperty(ref _giftAidDonations, double.IsNaN(value) ? 0 : value);
        }

        public double ReliefAtSourcePensionContributions
        {
            get => _reliefAtSourcePensionContributions;
            set => SetProperty(ref _reliefAtSourcePensionContributions, double.IsNaN(value) ? 0 : value);
        }

        // Student Loans
        public bool HasStudentLoan
        {
            get => _hasStudentLoan;
            set => SetProperty(ref _hasStudentLoan, value);
        }

        public int StudentLoanPlan
        {
            get => _studentLoanPlan;
            set => SetProperty(ref _studentLoanPlan, value);
        }

        public bool HasPostgraduateLoan
        {
            get => _hasPostgraduateLoan;
            set => SetProperty(ref _hasPostgraduateLoan, value);
        }

        // High Income Child Benefit Charge
        public int NumberOfChildren
        {
            get => _numberOfChildren;
            set => SetProperty(ref _numberOfChildren, value);
        }

        public double ChildBenefitAmount
        {
            get => _childBenefitAmount;
            set => SetProperty(ref _childBenefitAmount, double.IsNaN(value) ? 0 : value);
        }

        // Capital Gains
        public double CapitalGainsLosses
        {
            get => _capitalGainsLosses;
            set => SetProperty(ref _capitalGainsLosses, double.IsNaN(value) ? 0 : value);
        }

        // Rental/Property Income
        public double RentalIncome
        {
            get => _rentalIncome;
            set => SetProperty(ref _rentalIncome, double.IsNaN(value) ? 0 : value);
        }

        public double RentalExpenses
        {
            get => _rentalExpenses;
            set => SetProperty(ref _rentalExpenses, double.IsNaN(value) ? 0 : value);
        }

        public double MortgageInterest
        {
            get => _mortgageInterest;
            set => SetProperty(ref _mortgageInterest, double.IsNaN(value) ? 0 : value);
        }

        public bool UsePropertyAllowance
        {
            get => _usePropertyAllowance;
            set => SetProperty(ref _usePropertyAllowance, value);
        }

        // Trading Income
        public double TradingIncome
        {
            get => _tradingIncome;
            set => SetProperty(ref _tradingIncome, double.IsNaN(value) ? 0 : value);
        }

        public double TradingExpenses
        {
            get => _tradingExpenses;
            set => SetProperty(ref _tradingExpenses, double.IsNaN(value) ? 0 : value);
        }

        public bool UseTradingAllowance
        {
            get => _useTradingAllowance;
            set => SetProperty(ref _useTradingAllowance, value);
        }

        // Investment Reliefs
        public double EisInvestment
        {
            get => _eisInvestment;
            set => SetProperty(ref _eisInvestment, double.IsNaN(value) ? 0 : value);
        }

        public double SeisInvestment
        {
            get => _seisInvestment;
            set => SetProperty(ref _seisInvestment, double.IsNaN(value) ? 0 : value);
        }

        public double VctInvestment
        {
            get => _vctInvestment;
            set => SetProperty(ref _vctInvestment, double.IsNaN(value) ? 0 : value);
        }

        // Prior Year Tax Collected via PAYE
        public double PriorYearTaxOwed
        {
            get => _priorYearTaxOwed;
            set => SetProperty(ref _priorYearTaxOwed, double.IsNaN(value) ? 0 : value);
        }

        // Tax Code
        public string TaxCode
        {
            get => _taxCode;
            set => SetProperty(ref _taxCode, value ?? "");
        }
    }
}
