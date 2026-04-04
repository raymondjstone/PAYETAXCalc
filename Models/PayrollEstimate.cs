namespace PAYETAXCalc.Models
{
    public enum PayFrequency
    {
        Monthly = 12,
        Weekly = 52,
    }

    public enum PensionContributionType
    {
        PercentOfGross,
        FixedAmount,
    }

    public record PayrollInput
    {
        public string TaxYear { get; set; } = "";
        public PayFrequency Frequency { get; set; } = PayFrequency.Monthly;
        public decimal AnnualGross { get; set; }
        public string TaxCode { get; set; } = "1257L";
        public bool IsScottish { get; set; }
        public bool IsWelsh { get; set; }
        public PensionContributionType EmployeePensionType { get; set; } = PensionContributionType.PercentOfGross;
        public decimal EmployeePensionValue { get; set; }  // % (e.g. 5 for 5%) or fixed £ per period
        public PensionContributionType EmployerPensionType { get; set; } = PensionContributionType.PercentOfGross;
        public decimal EmployerPensionValue { get; set; }  // % or fixed £ per period
    }

    public class PayrollPeriodResult
    {
        public int PeriodNumber { get; set; }
        public string PeriodLabel { get; set; } = "";
        public decimal GrossPay { get; set; }
        public decimal EmployeeTax { get; set; }
        public decimal EmployeeNI { get; set; }
        public decimal EmployeePension { get; set; }
        public decimal NetPay { get; set; }
        public decimal EmployerNI { get; set; }
        public decimal EmployerPension { get; set; }
        public decimal CumulativeGross { get; set; }
        public decimal CumulativeTax { get; set; }
        public decimal CumulativeEmployeeNI { get; set; }
    }
}
