using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class SavingsIncome : NotifyBase
    {
        private string _providerName = "";
        private double _interestAmount;
        private bool _isTaxFree;

        public string ProviderName
        {
            get => _providerName;
            set => SetProperty(ref _providerName, value);
        }

        public double InterestAmount
        {
            get => _interestAmount;
            set => SetProperty(ref _interestAmount, double.IsNaN(value) ? 0 : value);
        }

        public bool IsTaxFree
        {
            get => _isTaxFree;
            set => SetProperty(ref _isTaxFree, value);
        }
    }
}
