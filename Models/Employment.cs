using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class Employment : NotifyBase
    {
        private string _employerName = "";
        private string _payeReference = "";
        private bool _isPensionOrAnnuity;
        private double _grossSalary;
        private double _taxPaid;
        private double _nationalInsurancePaid;
        private double _benefitsInKind;
        private double _pensionContributions;
        private double _workFromHomeWeeks;
        private double _businessMiles;
        private double _professionalSubscriptions;
        private double _uniformAllowance;
        private double _otherExpenses;
        private string _otherExpensesDescription = "";
        private bool _employmentEnded;
        private bool _isCombinedTaxAndNI;

        // Company Car
        private bool _hasCompanyCar;
        private double _carListPrice;
        private int _carCO2Emissions;
        private bool _carIsElectric;
        private double _carFuelBenefit; // If employer provides fuel

        public string EmployerName
        {
            get => _employerName;
            set => SetProperty(ref _employerName, value);
        }

        public string PayeReference
        {
            get => _payeReference;
            set => SetProperty(ref _payeReference, value);
        }

        public bool IsPensionOrAnnuity
        {
            get => _isPensionOrAnnuity;
            set => SetProperty(ref _isPensionOrAnnuity, value);
        }

        public double GrossSalary
        {
            get => _grossSalary;
            set => SetProperty(ref _grossSalary, double.IsNaN(value) ? 0 : value);
        }

        public double TaxPaid
        {
            get => _taxPaid;
            set => SetProperty(ref _taxPaid, double.IsNaN(value) ? 0 : value);
        }

        public double NationalInsurancePaid
        {
            get => _nationalInsurancePaid;
            set => SetProperty(ref _nationalInsurancePaid, double.IsNaN(value) ? 0 : value);
        }

        public double BenefitsInKind
        {
            get => _benefitsInKind;
            set => SetProperty(ref _benefitsInKind, double.IsNaN(value) ? 0 : value);
        }

        public double PensionContributions
        {
            get => _pensionContributions;
            set => SetProperty(ref _pensionContributions, double.IsNaN(value) ? 0 : value);
        }

        public double WorkFromHomeWeeks
        {
            get => _workFromHomeWeeks;
            set => SetProperty(ref _workFromHomeWeeks, double.IsNaN(value) ? 0 : value);
        }

        public double BusinessMiles
        {
            get => _businessMiles;
            set => SetProperty(ref _businessMiles, double.IsNaN(value) ? 0 : value);
        }

        public double ProfessionalSubscriptions
        {
            get => _professionalSubscriptions;
            set => SetProperty(ref _professionalSubscriptions, double.IsNaN(value) ? 0 : value);
        }

        public double UniformAllowance
        {
            get => _uniformAllowance;
            set => SetProperty(ref _uniformAllowance, double.IsNaN(value) ? 0 : value);
        }

        public double OtherExpenses
        {
            get => _otherExpenses;
            set => SetProperty(ref _otherExpenses, double.IsNaN(value) ? 0 : value);
        }

        public string OtherExpensesDescription
        {
            get => _otherExpensesDescription;
            set => SetProperty(ref _otherExpensesDescription, value);
        }

        public bool EmploymentEnded
        {
            get => _employmentEnded;
            set => SetProperty(ref _employmentEnded, value);
        }

        public bool IsCombinedTaxAndNI
        {
            get => _isCombinedTaxAndNI;
            set => SetProperty(ref _isCombinedTaxAndNI, value);
        }

        // Company Car
        public bool HasCompanyCar
        {
            get => _hasCompanyCar;
            set => SetProperty(ref _hasCompanyCar, value);
        }

        public double CarListPrice
        {
            get => _carListPrice;
            set => SetProperty(ref _carListPrice, double.IsNaN(value) ? 0 : value);
        }

        public int CarCO2Emissions
        {
            get => _carCO2Emissions;
            set => SetProperty(ref _carCO2Emissions, value);
        }

        public bool CarIsElectric
        {
            get => _carIsElectric;
            set => SetProperty(ref _carIsElectric, value);
        }

        public double CarFuelBenefit
        {
            get => _carFuelBenefit;
            set => SetProperty(ref _carFuelBenefit, double.IsNaN(value) ? 0 : value);
        }
    }
}
