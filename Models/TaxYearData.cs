using System.Collections.ObjectModel;
using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class TaxYearData : NotifyBase
    {
        private string _taxYear = "";
        private bool _claimMarriageAllowance;
        private bool _isMarriageAllowanceReceiver;
        private bool _claimBlindPersonsAllowance;
        private double _giftAidDonations;

        public string TaxYear
        {
            get => _taxYear;
            set => SetProperty(ref _taxYear, value);
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
    }
}
