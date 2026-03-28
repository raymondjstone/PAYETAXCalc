using PAYETAXCalc.Helpers;

namespace PAYETAXCalc.Models
{
    public class DividendIncome : NotifyBase
    {
        private string _companyName = "";
        private double _grossDividend;
        private double _taxPaid;

        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        public double GrossDividend
        {
            get => _grossDividend;
            set => SetProperty(ref _grossDividend, double.IsNaN(value) ? 0 : value);
        }

        public double TaxPaid
        {
            get => _taxPaid;
            set => SetProperty(ref _taxPaid, double.IsNaN(value) ? 0 : value);
        }
    }
}
