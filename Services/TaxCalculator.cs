using System;
using System.Collections.Generic;
using PAYETAXCalc.Models;

namespace PAYETAXCalc.Services
{
    public static class TaxCalculator
    {
        public static TaxCalculationResult Calculate(TaxYearData data, TaxYearRules rules)
        {
            var result = new TaxCalculationResult();

            // 1. Sum employment income
            decimal totalSalary = 0, totalBIK = 0, totalTaxPaid = 0, totalNIPaid = 0, totalPension = 0;
            foreach (var emp in data.Employments)
            {
                totalSalary += (decimal)emp.GrossSalary;
                totalTaxPaid += (decimal)emp.TaxPaid;
                totalNIPaid += (decimal)emp.NationalInsurancePaid;

                if (!emp.IsPensionOrAnnuity)
                {
                    totalBIK += (decimal)emp.BenefitsInKind;
                    totalPension += (decimal)emp.PensionContributions;
                }
            }

            result.TotalEmploymentIncome = totalSalary;
            result.TotalBenefitsInKind = totalBIK;
            result.TotalPensionContributions = totalPension;
            result.TotalTaxPaidViaPAYE = totalTaxPaid;
            result.TotalNIPaid = totalNIPaid;

            // 2. Calculate allowable employment expenses
            decimal totalExpenses = CalculateExpenses(data, rules, result);

            // 3. Non-savings income
            decimal nonSavingsIncome = Math.Max(0, totalSalary + totalBIK - totalPension - totalExpenses);

            // 4. Savings interest
            decimal taxableSavingsInterest = 0, taxFreeSavings = 0;
            foreach (var sav in data.SavingsIncomes)
            {
                if (sav.IsTaxFree)
                    taxFreeSavings += (decimal)sav.InterestAmount;
                else
                    taxableSavingsInterest += (decimal)sav.InterestAmount;
            }
            result.TotalSavingsInterest = taxableSavingsInterest;
            result.TotalTaxFreeSavings = taxFreeSavings;

            // 5. Gift Aid
            decimal giftAidGross = (decimal)data.GiftAidDonations * 1.25m;
            result.GiftAidExtension = giftAidGross;

            // 6. Gross income and adjusted net income (for PA taper)
            decimal grossIncome = nonSavingsIncome + taxableSavingsInterest;
            result.GrossIncome = grossIncome;
            decimal adjustedNetIncome = grossIncome - giftAidGross;

            // 7. Personal Allowance (with taper)
            decimal personalAllowance = rules.PersonalAllowance;
            if (data.ClaimBlindPersonsAllowance)
                personalAllowance += rules.BlindPersonsAllowance;

            if (adjustedNetIncome > rules.PersonalAllowanceTaperThreshold)
            {
                decimal reduction = (adjustedNetIncome - rules.PersonalAllowanceTaperThreshold) / 2m;
                personalAllowance = Math.Max(0, personalAllowance - reduction);
            }

            // 8. Marriage Allowance
            decimal marriageCredit = 0;
            if (data.ClaimMarriageAllowance)
            {
                if (data.IsMarriageAllowanceReceiver)
                    marriageCredit = rules.MarriageAllowanceTransfer * rules.BasicRate;
                else
                    personalAllowance -= rules.MarriageAllowanceTransfer;
            }
            result.PersonalAllowanceUsed = personalAllowance;
            result.MarriageAllowanceCredit = marriageCredit;

            // 9. Taxable income split
            decimal taxableNonSavings = Math.Max(0, nonSavingsIncome - personalAllowance);
            decimal remainingPA = Math.Max(0, personalAllowance - nonSavingsIncome);
            decimal taxableSavings = Math.Max(0, taxableSavingsInterest - remainingPA);
            result.TaxableNonSavingsIncome = taxableNonSavings;
            result.TaxableSavingsIncome = taxableSavings;

            // 10. Tax on non-savings income using appropriate bands
            var bands = data.IsScottishTaxpayer ? rules.ScottishBands : rules.RestOfUKBands;
            decimal nonSavingsTax = CalculateBandedTax(
                taxableNonSavings, nonSavingsIncome, personalAllowance, giftAidGross,
                bands, result);

            // 11. Tax on savings income (always rUK rates, positioned after non-savings)
            decimal savingsTax = CalculateSavingsTax(
                taxableSavings, taxableNonSavings, nonSavingsIncome,
                grossIncome, personalAllowance, giftAidGross, rules);
            result.SavingsTaxDue = savingsTax;
            if (savingsTax > 0)
            {
                result.TaxBreakdown.Add(new TaxBreakdownLine
                {
                    Label = "Tax on Savings:",
                    IncomeText = $"on £{taxableSavings:N2}",
                    TaxText = $"£{savingsTax:N2}",
                });
            }

            // 12. Total tax
            decimal totalTaxDue = nonSavingsTax + savingsTax - marriageCredit;
            totalTaxDue = Math.Max(0, totalTaxDue);
            result.TotalIncomeTaxDue = totalTaxDue;

            // 13. Expected NI (per employment, pensions excluded)
            decimal expectedNI = 0;
            foreach (var emp in data.Employments)
            {
                if (emp.IsPensionOrAnnuity) continue;
                decimal salary = (decimal)emp.GrossSalary;
                if (salary > rules.NICPrimaryThreshold)
                {
                    decimal nicableAtMain = Math.Min(salary, rules.NICUpperEarningsLimit) - rules.NICPrimaryThreshold;
                    decimal nicableAtUpper = Math.Max(0, salary - rules.NICUpperEarningsLimit);
                    expectedNI += (nicableAtMain * rules.NICMainRate) + (nicableAtUpper * rules.NICUpperRate);
                }
            }
            result.ExpectedNI = Math.Round(expectedNI, 2);

            // 14. Over/under payment
            result.TaxOverUnderPayment = totalTaxDue - totalTaxPaid;

            // 15. Summary
            string regime = data.IsScottishTaxpayer ? "Scottish" : "rUK";
            if (result.TaxOverUnderPayment > 0)
                result.Summary = $"You may owe £{result.TaxOverUnderPayment:N2} in additional tax ({regime} rates).";
            else if (result.TaxOverUnderPayment < 0)
                result.Summary = $"You may be owed a refund of £{Math.Abs(result.TaxOverUnderPayment):N2} ({regime} rates).";
            else
                result.Summary = $"Your tax paid matches the calculated liability ({regime} rates).";

            if (totalExpenses > 0)
                result.Summary += $"\nEmployment expenses of £{totalExpenses:N2} deducted from taxable income.";

            decimal niDiff = totalNIPaid - expectedNI;
            if (Math.Abs(niDiff) > 1)
            {
                result.Summary += niDiff > 0
                    ? $"\nNI: You paid £{niDiff:N2} more than expected."
                    : $"\nNI: You paid £{Math.Abs(niDiff):N2} less than expected.";
            }

            return result;
        }

        private static decimal CalculateBandedTax(
            decimal taxableNonSavings,
            decimal nonSavingsGross,
            decimal personalAllowance,
            decimal giftAidGross,
            List<TaxBand> bands,
            TaxCalculationResult result)
        {
            decimal totalTax = 0;
            decimal previousGrossThreshold = personalAllowance; // bands start above PA
            decimal remaining = taxableNonSavings;

            foreach (var band in bands)
            {
                if (remaining <= 0) break;

                decimal upperGross = band.UpperGrossThreshold;

                // Apply Gift Aid extension to eligible bands
                if (band.ExtendsWithGiftAid && upperGross > 0)
                    upperGross += giftAidGross;

                decimal bandWidth;
                if (upperGross > 0)
                {
                    // Taxable threshold = gross threshold - PA
                    decimal bandUpperTaxable = Math.Max(0, upperGross - personalAllowance);
                    decimal bandLowerTaxable = Math.Max(0, previousGrossThreshold - personalAllowance);
                    bandWidth = Math.Max(0, bandUpperTaxable - bandLowerTaxable);
                    previousGrossThreshold = upperGross;
                }
                else
                {
                    // Unlimited top band
                    bandWidth = remaining;
                }

                decimal incomeInBand = Math.Min(remaining, bandWidth);
                decimal taxInBand = incomeInBand * band.Rate;

                if (incomeInBand > 0 || band == bands[^1])
                {
                    result.TaxBreakdown.Add(new TaxBreakdownLine
                    {
                        Label = $"{band.Name}:",
                        IncomeText = $"on £{incomeInBand:N2}",
                        TaxText = $"£{taxInBand:N2}",
                    });
                }

                totalTax += taxInBand;
                remaining -= incomeInBand;
            }

            // Also populate legacy fields for rUK compatibility
            result.TaxAtBasicRate = totalTax; // simplified - total non-savings tax
            result.IncomeAtBasicRate = taxableNonSavings;

            return totalTax;
        }

        private static decimal CalculateSavingsTax(
            decimal taxableSavings,
            decimal taxableNonSavings,
            decimal nonSavingsIncome,
            decimal grossIncome,
            decimal personalAllowance,
            decimal giftAidGross,
            TaxYearRules rules)
        {
            if (taxableSavings <= 0) return 0;

            // Savings are always taxed at rUK rates (20/40/45) even for Scottish taxpayers
            decimal rUKBasicBandWidth = rules.BasicRateBandWidth + giftAidGross;
            decimal rUKAdditionalThresholdTaxable = rules.AdditionalRateThresholdGross + giftAidGross - personalAllowance;
            if (rUKAdditionalThresholdTaxable < rUKBasicBandWidth)
                rUKAdditionalThresholdTaxable = rUKBasicBandWidth;

            // Starting rate for savings
            decimal startingRateAvailable = 0;
            decimal nonSavingsAbovePA = Math.Max(0, nonSavingsIncome - rules.PersonalAllowance);
            if (nonSavingsAbovePA < rules.StartingRateForSavingsLimit)
                startingRateAvailable = rules.StartingRateForSavingsLimit - nonSavingsAbovePA;

            // PSA depends on taxpayer band
            decimal psa;
            if (taxableNonSavings <= rules.BasicRateBandWidth)
                psa = rules.PersonalSavingsAllowanceBasic;
            else if (grossIncome <= rules.AdditionalRateThresholdGross)
                psa = rules.PersonalSavingsAllowanceHigher;
            else
                psa = 0;

            decimal savingsRemaining = taxableSavings;
            decimal savingsTax = 0;

            // Starting rate (0%)
            decimal startingUsed = Math.Min(savingsRemaining, startingRateAvailable);
            savingsRemaining -= startingUsed;

            // PSA (0%)
            decimal psaUsed = Math.Min(savingsRemaining, psa);
            savingsRemaining -= psaUsed;

            if (savingsRemaining > 0)
            {
                // Position savings in rUK bands after non-savings income
                decimal basicBandRemaining = Math.Max(0, rUKBasicBandWidth - taxableNonSavings);
                decimal higherBandRemaining = Math.Max(0, rUKAdditionalThresholdTaxable - Math.Max(taxableNonSavings, rUKBasicBandWidth));

                decimal savingsAtBasic = Math.Min(savingsRemaining, basicBandRemaining);
                savingsTax += savingsAtBasic * rules.BasicRate;
                savingsRemaining -= savingsAtBasic;

                decimal savingsAtHigher = Math.Min(savingsRemaining, higherBandRemaining);
                savingsTax += savingsAtHigher * rules.HigherRate;
                savingsRemaining -= savingsAtHigher;

                if (savingsRemaining > 0)
                    savingsTax += savingsRemaining * rules.AdditionalRate;
            }

            return savingsTax;
        }

        private static decimal CalculateExpenses(TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            decimal totalExpenses = 0;
            var expenseLines = new List<string>();

            decimal totalWfh = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity && emp.WorkFromHomeWeeks > 0)
                    totalWfh += (decimal)emp.WorkFromHomeWeeks * rules.WorkFromHomeWeeklyRate;
            }
            if (totalWfh > 0)
            {
                totalExpenses += totalWfh;
                expenseLines.Add($"Working from home: £{totalWfh:N2}");
            }

            decimal totalMiles = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity)
                    totalMiles += (decimal)emp.BusinessMiles;
            }
            if (totalMiles > 0)
            {
                decimal mileageAllowance = totalMiles <= 10000
                    ? totalMiles * rules.MileageRateFirst10000
                    : (10000m * rules.MileageRateFirst10000) + ((totalMiles - 10000m) * rules.MileageRateOver10000);
                totalExpenses += mileageAllowance;
                expenseLines.Add($"Business mileage ({totalMiles:N0} miles): £{mileageAllowance:N2}");
            }

            decimal totalSubs = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity)
                    totalSubs += (decimal)emp.ProfessionalSubscriptions;
            }
            if (totalSubs > 0)
            {
                totalExpenses += totalSubs;
                expenseLines.Add($"Professional subscriptions: £{totalSubs:N2}");
            }

            decimal totalUniform = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity)
                    totalUniform += (decimal)emp.UniformAllowance;
            }
            if (totalUniform > 0)
            {
                totalExpenses += totalUniform;
                expenseLines.Add($"Uniform/clothing: £{totalUniform:N2}");
            }

            decimal totalOther = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity)
                    totalOther += (decimal)emp.OtherExpenses;
            }
            if (totalOther > 0)
            {
                totalExpenses += totalOther;
                expenseLines.Add($"Other expenses: £{totalOther:N2}");
            }

            result.TotalEmploymentExpenses = totalExpenses;
            result.ExpensesBreakdown = expenseLines.Count > 0 ? string.Join("\n", expenseLines) : "";
            return totalExpenses;
        }
    }
}
