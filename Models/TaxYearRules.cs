using System.Collections.Generic;

namespace PAYETAXCalc.Models
{
    public class TaxBand
    {
        public string Name { get; set; } = "";
        public decimal Rate { get; set; }
        public decimal UpperGrossThreshold { get; set; } // 0 = no upper limit (top band)
        public bool ExtendsWithGiftAid { get; set; } = true; // false for Scottish starter
    }

    public class TaxBreakdownLine
    {
        public string Label { get; set; } = "";
        public string IncomeText { get; set; } = "";
        public string TaxText { get; set; } = "";
    }

    public class TaxYearRules
    {
        public string TaxYear { get; set; } = "";

        // Personal Allowance
        public decimal PersonalAllowance { get; set; }
        public decimal PersonalAllowanceTaperThreshold { get; set; }

        // Income Tax Bands - rUK (England/Wales/NI) - gross upper thresholds
        public decimal BasicRateBandWidth { get; set; }
        public decimal BasicRate { get; set; }
        public decimal HigherRate { get; set; }
        public decimal AdditionalRateThresholdGross { get; set; }
        public decimal AdditionalRate { get; set; }

        // rUK bands as list (populated by TaxRulesProvider)
        public List<TaxBand> RestOfUKBands { get; set; } = new();

        // Scottish income tax bands (different rates/thresholds)
        public List<TaxBand> ScottishBands { get; set; } = new();

        // Welsh income tax bands (WRIT — matching rUK for 2023/24 onwards)
        public List<TaxBand> WelshBands { get; set; } = new();

        // Employee Class 1 NIC (same for all UK)
        public decimal NICPrimaryThreshold { get; set; }
        public decimal NICUpperEarningsLimit { get; set; }
        public decimal NICMainRate { get; set; }
        public decimal NICUpperRate { get; set; }

        // Employer Class 1 NIC
        public decimal EmployerNICSecondaryThreshold { get; set; }
        public decimal EmployerNICRate { get; set; }

        // Savings (same for all UK - always rUK rates)
        public decimal PersonalSavingsAllowanceBasic { get; set; }
        public decimal PersonalSavingsAllowanceHigher { get; set; }
        public decimal StartingRateForSavingsLimit { get; set; }

        // Marriage Allowance
        public decimal MarriageAllowanceTransfer { get; set; }

        // Blind Person's Allowance
        public decimal BlindPersonsAllowance { get; set; }

        // Dividend Allowance and Rates
        public decimal DividendAllowance { get; set; }
        public decimal DividendBasicRate { get; set; }
        public decimal DividendHigherRate { get; set; }
        public decimal DividendAdditionalRate { get; set; }

        // HMRC Flat Rate Expenses
        public decimal WorkFromHomeWeeklyRate { get; set; }
        public decimal MileageRateFirst10000 { get; set; }
        public decimal MileageRateOver10000 { get; set; }
        public decimal FlatRateUniformAllowance { get; set; }

        // Student Loan Thresholds (annual)
        public decimal StudentLoanPlan1Threshold { get; set; }
        public decimal StudentLoanPlan2Threshold { get; set; }
        public decimal StudentLoanPlan4Threshold { get; set; }
        public decimal StudentLoanPlan5Threshold { get; set; }
        public decimal StudentLoanRate { get; set; } // 9% for plans 1-5
        public decimal PostgraduateLoanThreshold { get; set; }
        public decimal PostgraduateLoanRate { get; set; } // 6%

        // High Income Child Benefit Charge
        public decimal HICBCThreshold { get; set; }
        public decimal HICBCFullChargeThreshold { get; set; }
        public decimal ChildBenefitFirstChildWeekly { get; set; }
        public decimal ChildBenefitAdditionalChildWeekly { get; set; }

        // Capital Gains Tax
        public decimal CGTAnnualExemptAmount { get; set; }
        public decimal CGTBasicRateAssets { get; set; }
        public decimal CGTHigherRateAssets { get; set; }
        public decimal CGTBasicRateProperty { get; set; }
        public decimal CGTHigherRateProperty { get; set; }

        // Property/Rental
        public decimal PropertyAllowance { get; set; }
        public decimal MortgageInterestReliefRate { get; set; } // 20% basic rate credit

        // Trading Allowance
        public decimal TradingAllowance { get; set; }

        // Pension Annual Allowance
        public decimal PensionAnnualAllowance { get; set; }

        // Investment Relief Rates
        public decimal EISReliefRate { get; set; }
        public decimal SEISReliefRate { get; set; }
        public decimal VCTReliefRate { get; set; }

        // Company Car - Fuel benefit charge multiplier
        public decimal CarFuelBenefitMultiplier { get; set; }
    }
}
