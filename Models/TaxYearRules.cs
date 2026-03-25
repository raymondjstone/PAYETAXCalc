namespace PAYETAXCalc.Models
{
    public class TaxYearRules
    {
        public string TaxYear { get; set; } = "";

        // Personal Allowance
        public decimal PersonalAllowance { get; set; }
        public decimal PersonalAllowanceTaperThreshold { get; set; }

        // Income Tax Bands (England/Wales/NI)
        public decimal BasicRateBandWidth { get; set; }
        public decimal BasicRate { get; set; }
        public decimal HigherRate { get; set; }
        public decimal AdditionalRateThresholdGross { get; set; }
        public decimal AdditionalRate { get; set; }

        // Employee Class 1 NIC
        public decimal NICPrimaryThreshold { get; set; }
        public decimal NICUpperEarningsLimit { get; set; }
        public decimal NICMainRate { get; set; }
        public decimal NICUpperRate { get; set; }

        // Savings
        public decimal PersonalSavingsAllowanceBasic { get; set; }
        public decimal PersonalSavingsAllowanceHigher { get; set; }
        public decimal StartingRateForSavingsLimit { get; set; }

        // Marriage Allowance
        public decimal MarriageAllowanceTransfer { get; set; }

        // Blind Person's Allowance
        public decimal BlindPersonsAllowance { get; set; }

        // Dividend Allowance
        public decimal DividendAllowance { get; set; }

        // HMRC Flat Rate Expenses
        public decimal WorkFromHomeWeeklyRate { get; set; }          // £6/week
        public decimal MileageRateFirst10000 { get; set; }           // 45p/mile
        public decimal MileageRateOver10000 { get; set; }            // 25p/mile
        public decimal FlatRateUniformAllowance { get; set; }        // General flat rate for laundering uniform
    }
}
