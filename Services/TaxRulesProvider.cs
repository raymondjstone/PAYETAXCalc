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
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
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
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
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
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
            },
        };

        public static TaxYearRules? GetRules(string taxYear)
        {
            return _rules.TryGetValue(taxYear, out var rules) ? rules : null;
        }

        public static IReadOnlyList<string> GetAvailableTaxYears()
        {
            return _rules.Keys.OrderBy(k => k).ToList();
        }

        public static string GetCurrentTaxYear()
        {
            var now = DateTime.Now;
            int startYear = now.Month >= 4 && now.Day >= 6 || now.Month > 4 ? now.Year : now.Year - 1;
            string key = $"{startYear}/{(startYear + 1) % 100:D2}";
            return _rules.ContainsKey(key) ? key : _rules.Keys.OrderByDescending(k => k).First();
        }

        public static string GetRulesSummary(TaxYearRules rules)
        {
            return $"Tax Year {rules.TaxYear}\n" +
                   $"Personal Allowance: £{rules.PersonalAllowance:N0} (tapers above £{rules.PersonalAllowanceTaperThreshold:N0})\n" +
                   $"Basic Rate: {rules.BasicRate:P0} on first £{rules.BasicRateBandWidth:N0} of taxable income\n" +
                   $"Higher Rate: {rules.HigherRate:P0} up to £{rules.AdditionalRateThresholdGross:N0}\n" +
                   $"Additional Rate: {rules.AdditionalRate:P0} above £{rules.AdditionalRateThresholdGross:N0}\n" +
                   $"Employee NIC: {rules.NICMainRate:P0} (£{rules.NICPrimaryThreshold:N0}-£{rules.NICUpperEarningsLimit:N0}), {rules.NICUpperRate:P0} above\n" +
                   $"WFH: £{rules.WorkFromHomeWeeklyRate}/week | Mileage: {rules.MileageRateFirst10000 * 100}p/mile (first 10k), {rules.MileageRateOver10000 * 100}p (over)\n" +
                   $"Uniform flat rate: £{rules.FlatRateUniformAllowance:N0} | PSA: £{rules.PersonalSavingsAllowanceBasic:N0} (basic) / £{rules.PersonalSavingsAllowanceHigher:N0} (higher)";
        }
    }
}
