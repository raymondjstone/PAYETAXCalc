using System.Collections.ObjectModel;
using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class TaxYearData : NotifyBase
    {
        private string _taxYear = "";
        private bool _isScottishTaxpayer;
        private bool _claimMarriageAllowance;
        private bool _isMarriageAllowanceReceiver;
        private bool _claimBlindPersonsAllowance;
        private double _giftAidDonations;
        private double _reliefAtSourcePensionContributions;

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

        public ObservableCollection<Employment> Employments { get; set; } = new();
        public ObservableCollection<SavingsIncome> SavingsIncomes { get; set; } = new();

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

        /// <summary>
        /// Pension contributions made via Relief at Source schemes (personal pensions, SIPPs).
        /// These receive automatic basic rate tax relief at source; higher/additional rate relief must be claimed.
        /// </summary>
        public double ReliefAtSourcePensionContributions
        {
            get => _reliefAtSourcePensionContributions;
            set => SetProperty(ref _reliefAtSourcePensionContributions, double.IsNaN(value) ? 0 : value);
        }
    }
}
