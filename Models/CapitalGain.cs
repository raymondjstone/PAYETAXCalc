using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class CapitalGain : NotifyBase
    {
        private string _description = "";
        private double _gainAmount;
        private bool _isResidentialProperty;

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public double GainAmount
        {
            get => _gainAmount;
            set => SetProperty(ref _gainAmount, double.IsNaN(value) ? 0 : value);
        }

        public bool IsResidentialProperty
        {
            get => _isResidentialProperty;
            set => SetProperty(ref _isResidentialProperty, value);
        }
    }
}
