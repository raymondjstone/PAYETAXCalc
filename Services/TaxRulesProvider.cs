using System;
using System.Collections.Generic;
using System.Linq;
using PAYETAXCalc.Models;

namespace PAYETAXCalc.Services
{
    public static class TaxRulesProvider
    {
        private static readonly Dictionary<string, TaxYearRules> _rules = new()
        {
            ["2023/24"] = new TaxYearRules
            {
                TaxYear = "2023/24",
                PersonalAllowance = 12570m,
                PersonalAllowanceTaperThreshold = 100000m,
                BasicRateBandWidth = 37700m,
                BasicRate = 0.20m,
                HigherRate = 0.40m,
                AdditionalRateThresholdGross = 125140m,
                AdditionalRate = 0.45m,
                RestOfUKBands = new()
                {
                    new TaxBand { Name = "Basic (20%)", Rate = 0.20m, UpperGrossThreshold = 50270m },
                    new TaxBand { Name = "Higher (40%)", Rate = 0.40m, UpperGrossThreshold = 125140m },
                    new TaxBand { Name = "Additional (45%)", Rate = 0.45m, UpperGrossThreshold = 0 },
                },
                ScottishBands = new()
                {
                    new TaxBand { Name = "Starter (19%)", Rate = 0.19m, UpperGrossThreshold = 14732m, ExtendsWithGiftAid = false },
                    new TaxBand { Name = "Basic (20%)", Rate = 0.20m, UpperGrossThreshold = 25688m },
                    new TaxBand { Name = "Intermediate (21%)", Rate = 0.21m, UpperGrossThreshold = 43662m },
                    new TaxBand { Name = "Higher (42%)", Rate = 0.42m, UpperGrossThreshold = 125140m },
                    new TaxBand { Name = "Top (47%)", Rate = 0.47m, UpperGrossThreshold = 0 },
                },
                NICPrimaryThreshold = 12570m,
                NICUpperEarningsLimit = 50270m,
                NICMainRate = 0.12m,
                NICUpperRate = 0.02m,
                PersonalSavingsAllowanceBasic = 1000m,
                PersonalSavingsAllowanceHigher = 500m,
                StartingRateForSavingsLimit = 5000m,
                MarriageAllowanceTransfer = 1260m,
                BlindPersonsAllowance = 2870m,
                DividendAllowance = 1000m,
                DividendBasicRate = 0.0875m,
                DividendHigherRate = 0.3375m,
                DividendAdditionalRate = 0.3935m,
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
                // Student Loans
                StudentLoanPlan1Threshold = 22015m,
                StudentLoanPlan2Threshold = 27295m,
                StudentLoanPlan4Threshold = 27660m,
                StudentLoanPlan5Threshold = 25000m,
                StudentLoanRate = 0.09m,
                PostgraduateLoanThreshold = 21000m,
                PostgraduateLoanRate = 0.06m,
                // HICBC
                HICBCThreshold = 50000m,
                HICBCFullChargeThreshold = 60000m,
                ChildBenefitFirstChildWeekly = 24.00m,
                ChildBenefitAdditionalChildWeekly = 15.90m,
                // CGT
                CGTAnnualExemptAmount = 6000m,
                CGTBasicRateAssets = 0.10m,
                CGTHigherRateAssets = 0.20m,
                CGTBasicRateProperty = 0.18m,
                CGTHigherRateProperty = 0.28m,
                // Property/Trading
                PropertyAllowance = 1000m,
                MortgageInterestReliefRate = 0.20m,
                TradingAllowance = 1000m,
                // Pension
                PensionAnnualAllowance = 60000m,
                // Investment Relief
                EISReliefRate = 0.30m,
                SEISReliefRate = 0.50m,
                VCTReliefRate = 0.30m,
                // Company Car
                CarFuelBenefitMultiplier = 27800m,
            },
            ["2024/25"] = new TaxYearRules
            {
                TaxYear = "2024/25",
                PersonalAllowance = 12570m,
                PersonalAllowanceTaperThreshold = 100000m,
                BasicRateBandWidth = 37700m,
                BasicRate = 0.20m,
                HigherRate = 0.40m,
                AdditionalRateThresholdGross = 125140m,
                AdditionalRate = 0.45m,
                RestOfUKBands = new()
                {
                    new TaxBand { Name = "Basic (20%)", Rate = 0.20m, UpperGrossThreshold = 50270m },
                    new TaxBand { Name = "Higher (40%)", Rate = 0.40m, UpperGrossThreshold = 125140m },
                    new TaxBand { Name = "Additional (45%)", Rate = 0.45m, UpperGrossThreshold = 0 },
                },
                ScottishBands = new()
                {
                    new TaxBand { Name = "Starter (19%)", Rate = 0.19m, UpperGrossThreshold = 14876m, ExtendsWithGiftAid = false },
                    new TaxBand { Name = "Basic (20%)", Rate = 0.20m, UpperGrossThreshold = 26561m },
                    new TaxBand { Name = "Intermediate (21%)", Rate = 0.21m, UpperGrossThreshold = 43662m },
                    new TaxBand { Name = "Higher (42%)", Rate = 0.42m, UpperGrossThreshold = 75000m },
                    new TaxBand { Name = "Advanced (45%)", Rate = 0.45m, UpperGrossThreshold = 125140m },
                    new TaxBand { Name = "Top (48%)", Rate = 0.48m, UpperGrossThreshold = 0 },
                },
                NICPrimaryThreshold = 12570m,
                NICUpperEarningsLimit = 50270m,
                NICMainRate = 0.08m,
                NICUpperRate = 0.02m,
                PersonalSavingsAllowanceBasic = 1000m,
                PersonalSavingsAllowanceHigher = 500m,
                StartingRateForSavingsLimit = 5000m,
                MarriageAllowanceTransfer = 1260m,
                BlindPersonsAllowance = 3070m,
                DividendAllowance = 500m,
                DividendBasicRate = 0.0875m,
                DividendHigherRate = 0.3375m,
                DividendAdditionalRate = 0.3935m,
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
                // Student Loans
                StudentLoanPlan1Threshold = 22015m,
                StudentLoanPlan2Threshold = 27295m,
                StudentLoanPlan4Threshold = 27660m,
                StudentLoanPlan5Threshold = 25000m,
                StudentLoanRate = 0.09m,
                PostgraduateLoanThreshold = 21000m,
                PostgraduateLoanRate = 0.06m,
                // HICBC (threshold raised from £50k to £60k for 2024/25)
                HICBCThreshold = 60000m,
                HICBCFullChargeThreshold = 80000m,
                ChildBenefitFirstChildWeekly = 25.60m,
                ChildBenefitAdditionalChildWeekly = 16.95m,
                // CGT (rates changed post-Budget Oct 2024)
                CGTAnnualExemptAmount = 3000m,
                CGTBasicRateAssets = 0.18m,
                CGTHigherRateAssets = 0.24m,
                CGTBasicRateProperty = 0.18m,
                CGTHigherRateProperty = 0.24m,
                // Property/Trading
                PropertyAllowance = 1000m,
                MortgageInterestReliefRate = 0.20m,
                TradingAllowance = 1000m,
                // Pension
                PensionAnnualAllowance = 60000m,
                // Investment Relief
                EISReliefRate = 0.30m,
                SEISReliefRate = 0.50m,
                VCTReliefRate = 0.30m,
                // Company Car
                CarFuelBenefitMultiplier = 27800m,
            },
            ["2025/26"] = new TaxYearRules
            {
                TaxYear = "2025/26",
                PersonalAllowance = 12570m,
                PersonalAllowanceTaperThreshold = 100000m,
                BasicRateBandWidth = 37700m,
                BasicRate = 0.20m,
                HigherRate = 0.40m,
                AdditionalRateThresholdGross = 125140m,
                AdditionalRate = 0.45m,
                RestOfUKBands = new()
                {
                    new TaxBand { Name = "Basic (20%)", Rate = 0.20m, UpperGrossThreshold = 50270m },
                    new TaxBand { Name = "Higher (40%)", Rate = 0.40m, UpperGrossThreshold = 125140m },
                    new TaxBand { Name = "Additional (45%)", Rate = 0.45m, UpperGrossThreshold = 0 },
                },
                ScottishBands = new()
                {
                    new TaxBand { Name = "Starter (19%)", Rate = 0.19m, UpperGrossThreshold = 15397m, ExtendsWithGiftAid = false },
                    new TaxBand { Name = "Basic (20%)", Rate = 0.20m, UpperGrossThreshold = 27491m },
                    new TaxBand { Name = "Intermediate (21%)", Rate = 0.21m, UpperGrossThreshold = 43662m },
                    new TaxBand { Name = "Higher (42%)", Rate = 0.42m, UpperGrossThreshold = 75000m },
                    new TaxBand { Name = "Advanced (45%)", Rate = 0.45m, UpperGrossThreshold = 125140m },
                    new TaxBand { Name = "Top (48%)", Rate = 0.48m, UpperGrossThreshold = 0 },
                },
                NICPrimaryThreshold = 12570m,
                NICUpperEarningsLimit = 50270m,
                NICMainRate = 0.08m,
                NICUpperRate = 0.02m,
                PersonalSavingsAllowanceBasic = 1000m,
                PersonalSavingsAllowanceHigher = 500m,
                StartingRateForSavingsLimit = 5000m,
                MarriageAllowanceTransfer = 1260m,
                BlindPersonsAllowance = 3130m,
                DividendAllowance = 500m,
                DividendBasicRate = 0.0875m,
                DividendHigherRate = 0.3375m,
                DividendAdditionalRate = 0.3935m,
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
                // Student Loans
                StudentLoanPlan1Threshold = 24990m,
                StudentLoanPlan2Threshold = 27295m,
                StudentLoanPlan4Threshold = 31395m,
                StudentLoanPlan5Threshold = 25000m,
                StudentLoanRate = 0.09m,
                PostgraduateLoanThreshold = 21000m,
                PostgraduateLoanRate = 0.06m,
                // HICBC
                HICBCThreshold = 60000m,
                HICBCFullChargeThreshold = 80000m,
                ChildBenefitFirstChildWeekly = 26.05m,
                ChildBenefitAdditionalChildWeekly = 17.25m,
                // CGT
                CGTAnnualExemptAmount = 3000m,
                CGTBasicRateAssets = 0.18m,
                CGTHigherRateAssets = 0.24m,
                CGTBasicRateProperty = 0.18m,
                CGTHigherRateProperty = 0.24m,
                // Property/Trading
                PropertyAllowance = 1000m,
                MortgageInterestReliefRate = 0.20m,
                TradingAllowance = 1000m,
                // Pension
                PensionAnnualAllowance = 60000m,
                // Investment Relief
                EISReliefRate = 0.30m,
                SEISReliefRate = 0.50m,
                VCTReliefRate = 0.30m,
                // Company Car
                CarFuelBenefitMultiplier = 28200m,
            },
        };

        public static TaxYearRules? GetRules(string taxYear)
        {
            return _rules.TryGetValue(taxYear, out var rules) ? rules : null;
        }

        public static TaxYearRules GetOrEstimateRules(string taxYear)
        {
            if (_rules.TryGetValue(taxYear, out var exact))
                return exact;

            var latestKey = _rules.Keys.OrderByDescending(k => k).First();
            var latest = _rules[latestKey];
            return CloneRulesForYear(latest, taxYear);
        }

        public static bool IsEstimated(string taxYear)
        {
            return !_rules.ContainsKey(taxYear);
        }

        public static IReadOnlyList<string> GetDefinedTaxYears()
        {
            return _rules.Keys.OrderBy(k => k).ToList();
        }

        public static IReadOnlyList<string> GetAvailableTaxYears()
        {
            var years = new SortedSet<string>(_rules.Keys);

            var now = DateTime.Now;
            int currentStartYear = now.Month > 4 || (now.Month == 4 && now.Day >= 6) ? now.Year : now.Year - 1;
            for (int y = currentStartYear; y <= currentStartYear + 3; y++)
            {
                years.Add(FormatTaxYear(y));
            }

            return years.ToList();
        }

        public static string FormatTaxYear(int startYear)
        {
            return $"{startYear}/{(startYear + 1) % 100:D2}";
        }

        public static bool TryParseTaxYear(string input, out string formatted)
        {
            formatted = "";
            input = input.Trim();

            if (input.Length == 7 && input[4] == '/')
            {
                if (int.TryParse(input.Substring(0, 4), out int y1) &&
                    int.TryParse(input.Substring(5, 2), out int y2))
                {
                    if (y2 == (y1 + 1) % 100)
                    {
                        formatted = FormatTaxYear(y1);
                        return y1 >= 2020 && y1 <= 2099;
                    }
                }
            }

            if (input.Length == 4 && int.TryParse(input, out int year))
            {
                formatted = FormatTaxYear(year);
                return year >= 2020 && year <= 2099;
            }

            return false;
        }

        public static string GetCurrentTaxYear()
        {
            var now = DateTime.Now;
            int startYear = now.Month > 4 || (now.Month == 4 && now.Day >= 6) ? now.Year : now.Year - 1;
            string key = FormatTaxYear(startYear);
            return _rules.ContainsKey(key) ? key : _rules.Keys.OrderByDescending(k => k).First();
        }

        public static string GetRulesSummary(TaxYearRules rules)
        {
            string estimated = IsEstimated(rules.TaxYear)
                ? " (ESTIMATED - using latest known rates, actual rates may differ)\n"
                : "\n";
            return $"Tax Year {rules.TaxYear}{estimated}" +
                   $"Personal Allowance: £{rules.PersonalAllowance:N0} (tapers above £{rules.PersonalAllowanceTaperThreshold:N0})\n" +
                   $"rUK: {FormatBands(rules.RestOfUKBands)}\n" +
                   $"Scotland: {FormatBands(rules.ScottishBands)}\n" +
                   $"Employee NIC: {rules.NICMainRate:P0} (£{rules.NICPrimaryThreshold:N0}-£{rules.NICUpperEarningsLimit:N0}), {rules.NICUpperRate:P0} above\n" +
                   $"WFH: £{rules.WorkFromHomeWeeklyRate}/week | Mileage: {rules.MileageRateFirst10000 * 100}p first 10k, {rules.MileageRateOver10000 * 100}p over\n" +
                   $"PSA: £{rules.PersonalSavingsAllowanceBasic:N0} (basic) / £{rules.PersonalSavingsAllowanceHigher:N0} (higher)\n" +
                   $"CGT AEA: £{rules.CGTAnnualExemptAmount:N0} | Property Allowance: £{rules.PropertyAllowance:N0} | Trading Allowance: £{rules.TradingAllowance:N0}";
        }

        /// <summary>
        /// Returns the company car BIK percentage based on CO2 emissions.
        /// Simplified bands for 2023/24 onwards.
        /// </summary>
        public static int GetCompanyCarBIKPercentage(int co2, bool isElectric, string taxYear)
        {
            // Zero emissions (full electric)
            if (co2 == 0)
            {
                return taxYear switch
                {
                    "2023/24" => 2,
                    "2024/25" => 2,
                    "2025/26" => 3,
                    _ => 3,
                };
            }

            // 1-50 g/km electric range vehicles
            if (isElectric && co2 <= 50)
            {
                return taxYear switch
                {
                    "2023/24" => co2 <= 50 ? 8 : 12,
                    "2024/25" => co2 <= 50 ? 8 : 12,
                    "2025/26" => co2 <= 50 ? 9 : 13,
                    _ => 9,
                };
            }

            // Standard petrol/diesel CO2-based bands (2024/25)
            int basePercent;
            if (co2 <= 50) basePercent = 15;
            else if (co2 <= 54) basePercent = 16;
            else if (co2 <= 59) basePercent = 17;
            else if (co2 <= 64) basePercent = 18;
            else if (co2 <= 69) basePercent = 19;
            else if (co2 <= 74) basePercent = 20;
            else if (co2 <= 79) basePercent = 21;
            else if (co2 <= 84) basePercent = 22;
            else if (co2 <= 89) basePercent = 23;
            else if (co2 <= 94) basePercent = 24;
            else if (co2 <= 99) basePercent = 25;
            else if (co2 <= 104) basePercent = 26;
            else if (co2 <= 109) basePercent = 27;
            else if (co2 <= 114) basePercent = 28;
            else if (co2 <= 119) basePercent = 29;
            else if (co2 <= 124) basePercent = 30;
            else if (co2 <= 129) basePercent = 31;
            else if (co2 <= 134) basePercent = 32;
            else if (co2 <= 139) basePercent = 33;
            else if (co2 <= 144) basePercent = 34;
            else if (co2 <= 149) basePercent = 35;
            else if (co2 <= 154) basePercent = 36;
            else basePercent = 37;

            return Math.Min(basePercent, 37);
        }

        private static string FormatBands(List<TaxBand> bands)
        {
            return string.Join(", ", bands.Select(b => $"{b.Rate:P0}" + (b.UpperGrossThreshold > 0 ? $" to £{b.UpperGrossThreshold:N0}" : "+")));
        }

        private static TaxYearRules CloneRulesForYear(TaxYearRules source, string taxYear)
        {
            return new TaxYearRules
            {
                TaxYear = taxYear,
                PersonalAllowance = source.PersonalAllowance,
                PersonalAllowanceTaperThreshold = source.PersonalAllowanceTaperThreshold,
                BasicRateBandWidth = source.BasicRateBandWidth,
                BasicRate = source.BasicRate,
                HigherRate = source.HigherRate,
                AdditionalRateThresholdGross = source.AdditionalRateThresholdGross,
                AdditionalRate = source.AdditionalRate,
                RestOfUKBands = source.RestOfUKBands.Select(b => new TaxBand
                {
                    Name = b.Name, Rate = b.Rate,
                    UpperGrossThreshold = b.UpperGrossThreshold,
                    ExtendsWithGiftAid = b.ExtendsWithGiftAid,
                }).ToList(),
                ScottishBands = source.ScottishBands.Select(b => new TaxBand
                {
                    Name = b.Name, Rate = b.Rate,
                    UpperGrossThreshold = b.UpperGrossThreshold,
                    ExtendsWithGiftAid = b.ExtendsWithGiftAid,
                }).ToList(),
                NICPrimaryThreshold = source.NICPrimaryThreshold,
                NICUpperEarningsLimit = source.NICUpperEarningsLimit,
                NICMainRate = source.NICMainRate,
                NICUpperRate = source.NICUpperRate,
                PersonalSavingsAllowanceBasic = source.PersonalSavingsAllowanceBasic,
                PersonalSavingsAllowanceHigher = source.PersonalSavingsAllowanceHigher,
                StartingRateForSavingsLimit = source.StartingRateForSavingsLimit,
                MarriageAllowanceTransfer = source.MarriageAllowanceTransfer,
                BlindPersonsAllowance = source.BlindPersonsAllowance,
                DividendAllowance = source.DividendAllowance,
                DividendBasicRate = source.DividendBasicRate,
                DividendHigherRate = source.DividendHigherRate,
                DividendAdditionalRate = source.DividendAdditionalRate,
                WorkFromHomeWeeklyRate = source.WorkFromHomeWeeklyRate,
                MileageRateFirst10000 = source.MileageRateFirst10000,
                MileageRateOver10000 = source.MileageRateOver10000,
                FlatRateUniformAllowance = source.FlatRateUniformAllowance,
                // New fields
                StudentLoanPlan1Threshold = source.StudentLoanPlan1Threshold,
                StudentLoanPlan2Threshold = source.StudentLoanPlan2Threshold,
                StudentLoanPlan4Threshold = source.StudentLoanPlan4Threshold,
                StudentLoanPlan5Threshold = source.StudentLoanPlan5Threshold,
                StudentLoanRate = source.StudentLoanRate,
                PostgraduateLoanThreshold = source.PostgraduateLoanThreshold,
                PostgraduateLoanRate = source.PostgraduateLoanRate,
                HICBCThreshold = source.HICBCThreshold,
                HICBCFullChargeThreshold = source.HICBCFullChargeThreshold,
                ChildBenefitFirstChildWeekly = source.ChildBenefitFirstChildWeekly,
                ChildBenefitAdditionalChildWeekly = source.ChildBenefitAdditionalChildWeekly,
                CGTAnnualExemptAmount = source.CGTAnnualExemptAmount,
                CGTBasicRateAssets = source.CGTBasicRateAssets,
                CGTHigherRateAssets = source.CGTHigherRateAssets,
                CGTBasicRateProperty = source.CGTBasicRateProperty,
                CGTHigherRateProperty = source.CGTHigherRateProperty,
                PropertyAllowance = source.PropertyAllowance,
                MortgageInterestReliefRate = source.MortgageInterestReliefRate,
                TradingAllowance = source.TradingAllowance,
                PensionAnnualAllowance = source.PensionAnnualAllowance,
                EISReliefRate = source.EISReliefRate,
                SEISReliefRate = source.SEISReliefRate,
                VCTReliefRate = source.VCTReliefRate,
                CarFuelBenefitMultiplier = source.CarFuelBenefitMultiplier,
            };
        }
    }
}
