using System;
using System.Collections.Generic;
using PAYETAXCalc.Models;

namespace PAYETAXCalc.Services
{
    public static class PayrollCalculatorService
    {
        /// <summary>
        /// Calculates per-period payroll figures for every period in the tax year using the
        /// PAYE cumulative basis.  Pension is treated as a salary sacrifice arrangement
        /// (reduces both taxable income and NI-able pay).
        /// </summary>
        public static List<PayrollPeriodResult> Calculate(PayrollInput input, TaxYearRules rules)
        {
            int periodsPerYear = input.Frequency switch
            {
                PayFrequency.Monthly => 12,
                PayFrequency.Weekly => 52,
                _ => throw new ArgumentOutOfRangeException(nameof(input.Frequency), input.Frequency, $"Unsupported pay frequency: {input.Frequency}"),
            };
            decimal periodGross = input.AnnualGross / periodsPerYear;

            // Derive the taxpayer's free-pay allowance from the tax code
            decimal annualAllowance = ParseTaxCode(input.TaxCode, rules.PersonalAllowance,
                out bool isBR, out bool isD0, out bool isD1, out bool isNT);

            var bands = input.IsScottish ? rules.ScottishBands
                      : input.IsWelsh    ? rules.WelshBands
                      : rules.RestOfUKBands;

            // Period NI thresholds
            decimal ptPeriod = rules.NICPrimaryThreshold / periodsPerYear;
            decimal uelPeriod = rules.NICUpperEarningsLimit / periodsPerYear;
            decimal stPeriod = rules.EmployerNICSecondaryThreshold / periodsPerYear;

            // Pension per period (salary sacrifice — reduces both taxable and NI-able pay)
            decimal empPension = ComputePensionAmount(input.EmployeePensionType, input.EmployeePensionValue, periodGross);
            decimal erPension = ComputePensionAmount(input.EmployerPensionType, input.EmployerPensionValue, periodGross);
            decimal niablePay = periodGross - empPension;

            var results = new List<PayrollPeriodResult>();
            decimal cumulativeNiable = 0m;
            decimal prevCumTax = 0m;
            decimal cumulativeEmpNI = 0m;

            for (int p = 1; p <= periodsPerYear; p++)
            {
                cumulativeNiable += niablePay;

                // PAYE cumulative basis — tax calculated on cumulative figures then differenced
                decimal cumFreePay = annualAllowance / periodsPerYear * p;
                decimal cumTaxablePay = Math.Max(0m, cumulativeNiable - cumFreePay);
                decimal cumTax = CalculateBandedTax(cumTaxablePay, rules.PersonalAllowance, bands,
                    isBR, isD0, isD1, isNT, rules.BasicRate, rules.HigherRate, rules.AdditionalRate);
                decimal taxThisPeriod = Math.Max(0m, cumTax - prevCumTax);

                // Employee Class 1 NIC (period basis, not cumulative)
                decimal empNI = 0m;
                if (niablePay > ptPeriod)
                {
                    decimal niableAbovePT = Math.Min(niablePay, uelPeriod) - ptPeriod;
                    empNI = niableAbovePT * rules.NICMainRate;
                    if (niablePay > uelPeriod)
                        empNI += (niablePay - uelPeriod) * rules.NICUpperRate;
                }

                // Employer Class 1 NIC (period basis)
                decimal erNI = Math.Max(0m, niablePay - stPeriod) * rules.EmployerNICRate;

                decimal netPay = periodGross - empPension - taxThisPeriod - empNI;
                cumulativeEmpNI += empNI;

                results.Add(new PayrollPeriodResult
                {
                    PeriodNumber = p,
                    PeriodLabel = FormatPeriodLabel(p, input.Frequency),
                    GrossPay = Math.Round(periodGross, 2),
                    EmployeeTax = Math.Round(taxThisPeriod, 2),
                    EmployeeNI = Math.Round(empNI, 2),
                    EmployeePension = Math.Round(empPension, 2),
                    EmployerNI = Math.Round(erNI, 2),
                    EmployerPension = Math.Round(erPension, 2),
                    NetPay = Math.Round(netPay, 2),
                    CumulativeGross = Math.Round(cumulativeNiable + empPension * p, 2),
                    CumulativeTax = Math.Round(cumTax, 2),
                    CumulativeEmployeeNI = Math.Round(cumulativeEmpNI, 2),
                });

                prevCumTax = cumTax;
            }

            return results;
        }

        /// <summary>
        /// Parses a PAYE tax code into an annual free-pay allowance.
        /// Handles L/M/N/T/P/V/Y suffix codes, BR, D0, D1, NT, 0T, K codes,
        /// and S/C country prefixes plus W1/M1/X non-cumulative suffixes (treated as cumulative).
        /// </summary>
        public static decimal ParseTaxCode(string code, decimal defaultAllowance,
            out bool isBR, out bool isD0, out bool isD1, out bool isNT)
        {
            isBR = false; isD0 = false; isD1 = false; isNT = false;

            if (string.IsNullOrWhiteSpace(code))
                return defaultAllowance;

            string upper = code.Trim().ToUpperInvariant();

            // Strip country prefix (S = Scottish, C = Welsh)
            if (upper.StartsWith("S") || upper.StartsWith("C"))
                upper = upper[1..];

            // Strip non-cumulative suffix W1 / M1 / X
            foreach (var suffix in new[] { "W1", "M1", "X" })
            {
                if (upper.EndsWith(suffix))
                {
                    upper = upper[..^suffix.Length].Trim();
                    break;
                }
            }

            if (upper == "BR") { isBR = true; return 0m; }
            if (upper == "D0") { isD0 = true; return 0m; }
            if (upper == "D1") { isD1 = true; return 0m; }
            if (upper == "NT") { isNT = true; return 0m; }
            if (upper == "0T") return 0m;

            // K codes: deductions exceed allowances — negative free pay
            if (upper.StartsWith("K"))
            {
                if (decimal.TryParse(upper[1..], out decimal kNum))
                    return -(kNum * 10m);
                return defaultAllowance;
            }

            // Standard codes: numeric prefix multiplied by 10, letter suffix stripped
            string numStr = upper.TrimEnd('L', 'M', 'N', 'T', 'P', 'V', 'Y');
            if (decimal.TryParse(numStr, out decimal num))
                return num * 10m;

            return defaultAllowance;
        }

        /// <summary>
        /// Applies tax bands to a cumulative taxable pay amount.
        /// Band widths are computed using the standard personal allowance (as defined in the rules)
        /// so the band structure is independent of any K-code or zero-allowance adjustments.
        /// </summary>
        private static decimal CalculateBandedTax(decimal taxablePay, decimal standardPA,
            List<TaxBand> bands, bool isBR, bool isD0, bool isD1, bool isNT,
            decimal basicRate, decimal higherRate, decimal additionalRate)
        {
            if (isNT || taxablePay <= 0m) return 0m;
            if (isBR) return taxablePay * basicRate;
            if (isD0) return taxablePay * higherRate;
            if (isD1) return taxablePay * additionalRate;

            decimal tax = 0m;
            decimal remaining = taxablePay;
            decimal prevGrossThreshold = standardPA;

            foreach (var band in bands)
            {
                if (remaining <= 0m) break;

                decimal bandWidth = band.UpperGrossThreshold == 0m
                    ? decimal.MaxValue / 2m
                    : band.UpperGrossThreshold - prevGrossThreshold;

                if (bandWidth <= 0m)
                {
                    if (band.UpperGrossThreshold > 0m)
                        prevGrossThreshold = band.UpperGrossThreshold;
                    continue;
                }

                decimal inBand = Math.Min(remaining, bandWidth);
                tax += inBand * band.Rate;
                remaining -= inBand;

                if (band.UpperGrossThreshold > 0m)
                    prevGrossThreshold = band.UpperGrossThreshold;
            }

            return tax;
        }

        private static decimal ComputePensionAmount(PensionContributionType type, decimal value, decimal periodGross)
        {
            if (value <= 0m) return 0m;
            return type == PensionContributionType.PercentOfGross
                ? periodGross * (value / 100m)
                : value;
        }

        private static string FormatPeriodLabel(int periodNumber, PayFrequency frequency)
        {
            if (frequency == PayFrequency.Monthly)
            {
                string[] months = { "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Jan", "Feb", "Mar" };
                return $"Month {periodNumber} ({months[periodNumber - 1]})";
            }
            return $"Week {periodNumber}";
        }
    }
}
