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
                WorkFromHomeWeeklyRate = 6m,
                MileageRateFirst10000 = 0.45m,
                MileageRateOver10000 = 0.25m,
                FlatRateUniformAllowance = 60m,
            },
        };

        /// <summary>
        /// Returns exact rules if known, or null if no rules exist for this year.
        /// Use GetOrEstimateRules for fallback to latest known rates.
        /// </summary>
        public static TaxYearRules? GetRules(string taxYear)
        {
            return _rules.TryGetValue(taxYear, out var rules) ? rules : null;
        }

        /// <summary>
        /// Returns rules for the given tax year. If no specific rules exist,
        /// clones the latest known rules and labels them with the requested year.
        /// </summary>
        public static TaxYearRules GetOrEstimateRules(string taxYear)
        {
            if (_rules.TryGetValue(taxYear, out var exact))
                return exact;

            // Use the latest known year's rules as estimate
            var latestKey = _rules.Keys.OrderByDescending(k => k).First();
            var latest = _rules[latestKey];
            return CloneRulesForYear(latest, taxYear);
        }

        /// <summary>
        /// Returns true if the rules for this year are estimated (not specifically defined).
        /// </summary>
        public static bool IsEstimated(string taxYear)
        {
            return !_rules.ContainsKey(taxYear);
        }

        /// <summary>
        /// Returns the years that have specifically defined rules.
        /// </summary>
        public static IReadOnlyList<string> GetDefinedTaxYears()
        {
            return _rules.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Returns a list of selectable tax years: all defined years plus future years
        /// up to 3 years ahead of the current tax year.
        /// </summary>
        public static IReadOnlyList<string> GetAvailableTaxYears()
        {
            var years = new SortedSet<string>(_rules.Keys);

            // Add future years up to 3 ahead
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

            // Try "YYYY/YY" format
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

            // Try "YYYY" - assume start year
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
                   $"PSA: £{rules.PersonalSavingsAllowanceBasic:N0} (basic) / £{rules.PersonalSavingsAllowanceHigher:N0} (higher)";
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
                WorkFromHomeWeeklyRate = source.WorkFromHomeWeeklyRate,
                MileageRateFirst10000 = source.MileageRateFirst10000,
                MileageRateOver10000 = source.MileageRateOver10000,
                FlatRateUniformAllowance = source.FlatRateUniformAllowance,
            };
        }
    }
}
