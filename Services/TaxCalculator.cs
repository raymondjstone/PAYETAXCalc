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
            decimal totalExpenses = 0;
            var expenseLines = new List<string>();

            // Working from home allowance (per employment)
            decimal totalWfh = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity && emp.WorkFromHomeWeeks > 0)
                {
                    decimal wfh = (decimal)emp.WorkFromHomeWeeks * rules.WorkFromHomeWeeklyRate;
                    totalWfh += wfh;
                }
            }
            if (totalWfh > 0)
            {
                totalExpenses += totalWfh;
                expenseLines.Add($"Working from home: £{totalWfh:N2}");
            }

            // Business mileage (AMAP rates - 10,000 mile threshold is per tax year across all employments)
            decimal totalMiles = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity)
                    totalMiles += (decimal)emp.BusinessMiles;
            }
            if (totalMiles > 0)
            {
                decimal mileageAllowance;
                if (totalMiles <= 10000)
                {
                    mileageAllowance = totalMiles * rules.MileageRateFirst10000;
                }
                else
                {
                    mileageAllowance = (10000m * rules.MileageRateFirst10000)
                                     + ((totalMiles - 10000m) * rules.MileageRateOver10000);
                }
                totalExpenses += mileageAllowance;
                expenseLines.Add($"Business mileage ({totalMiles:N0} miles): £{mileageAllowance:N2}");
            }

            // Professional subscriptions (per employment, actual amounts)
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

            // Uniform/work clothing allowance (per employment)
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

            // Other expenses (per employment)
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
            result.ExpensesBreakdown = expenseLines.Count > 0
                ? string.Join("\n", expenseLines)
                : "";

            // 3. Calculate non-savings income (salary + BIK - pension contributions - expenses)
            decimal nonSavingsIncome = totalSalary + totalBIK - totalPension - totalExpenses;
            nonSavingsIncome = Math.Max(0, nonSavingsIncome);

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

            // 5. Gift Aid - extends basic rate band
            decimal giftAidGross = (decimal)data.GiftAidDonations * 1.25m;
            result.GiftAidExtension = giftAidGross;
            decimal extendedBasicRateBand = rules.BasicRateBandWidth + giftAidGross;
            decimal extendedAdditionalThreshold = rules.AdditionalRateThresholdGross + giftAidGross;

            // 6. Calculate adjusted net income (for PA taper)
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
                {
                    marriageCredit = rules.MarriageAllowanceTransfer * rules.BasicRate;
                }
                else
                {
                    personalAllowance -= rules.MarriageAllowanceTransfer;
                }
            }
            result.PersonalAllowanceUsed = personalAllowance;
            result.MarriageAllowanceCredit = marriageCredit;

            // 9. Allocate PA against non-savings income first
            decimal taxableNonSavings = Math.Max(0, nonSavingsIncome - personalAllowance);
            decimal remainingPA = Math.Max(0, personalAllowance - nonSavingsIncome);
            decimal taxableSavings = Math.Max(0, taxableSavingsInterest - remainingPA);

            result.TaxableNonSavingsIncome = taxableNonSavings;
            result.TaxableSavingsIncome = taxableSavings;

            // 10. Tax on non-savings income using bands
            decimal additionalThresholdTaxable = extendedAdditionalThreshold - personalAllowance;
            if (additionalThresholdTaxable < extendedBasicRateBand)
                additionalThresholdTaxable = extendedBasicRateBand;

            decimal basicRateTax = 0, higherRateTax = 0, additionalRateTax = 0;
            decimal basicIncome = 0, higherIncome = 0, additionalIncome = 0;

            if (taxableNonSavings > 0)
            {
                basicIncome = Math.Min(taxableNonSavings, extendedBasicRateBand);
                basicRateTax = basicIncome * rules.BasicRate;

                if (taxableNonSavings > extendedBasicRateBand)
                {
                    higherIncome = Math.Min(taxableNonSavings - extendedBasicRateBand,
                                            additionalThresholdTaxable - extendedBasicRateBand);
                    higherIncome = Math.Max(0, higherIncome);
                    higherRateTax = higherIncome * rules.HigherRate;

                    if (taxableNonSavings > additionalThresholdTaxable)
                    {
                        additionalIncome = taxableNonSavings - additionalThresholdTaxable;
                        additionalRateTax = additionalIncome * rules.AdditionalRate;
                    }
                }
            }

            result.IncomeAtBasicRate = basicIncome;
            result.TaxAtBasicRate = basicRateTax;
            result.IncomeAtHigherRate = higherIncome;
            result.TaxAtHigherRate = higherRateTax;
            result.IncomeAtAdditionalRate = additionalIncome;
            result.TaxAtAdditionalRate = additionalRateTax;

            // 11. Tax on savings income
            decimal savingsTax = 0;
            if (taxableSavings > 0)
            {
                decimal totalNonSavingsTaxable = taxableNonSavings;

                decimal startingRateAvailable = 0;
                decimal nonSavingsAbovePA = Math.Max(0, nonSavingsIncome - rules.PersonalAllowance);
                if (nonSavingsAbovePA < rules.StartingRateForSavingsLimit)
                {
                    startingRateAvailable = rules.StartingRateForSavingsLimit - nonSavingsAbovePA;
                }

                decimal psa;
                if (totalNonSavingsTaxable <= rules.BasicRateBandWidth)
                    psa = rules.PersonalSavingsAllowanceBasic;
                else if (grossIncome <= rules.AdditionalRateThresholdGross)
                    psa = rules.PersonalSavingsAllowanceHigher;
                else
                    psa = 0;

                decimal savingsRemaining = taxableSavings;

                decimal startingRateUsed = Math.Min(savingsRemaining, startingRateAvailable);
                savingsRemaining -= startingRateUsed;

                decimal psaUsed = Math.Min(savingsRemaining, psa);
                savingsRemaining -= psaUsed;

                if (savingsRemaining > 0)
                {
                    decimal bandUsedByNonSavings = taxableNonSavings;
                    decimal basicBandRemaining = Math.Max(0, extendedBasicRateBand - bandUsedByNonSavings);
                    decimal higherBandRemaining = Math.Max(0, additionalThresholdTaxable - Math.Max(bandUsedByNonSavings, extendedBasicRateBand));

                    decimal savingsAtBasic = Math.Min(savingsRemaining, basicBandRemaining);
                    savingsTax += savingsAtBasic * rules.BasicRate;
                    savingsRemaining -= savingsAtBasic;

                    decimal savingsAtHigher = Math.Min(savingsRemaining, higherBandRemaining);
                    savingsTax += savingsAtHigher * rules.HigherRate;
                    savingsRemaining -= savingsAtHigher;

                    if (savingsRemaining > 0)
                    {
                        savingsTax += savingsRemaining * rules.AdditionalRate;
                    }
                }
            }
            result.SavingsTaxDue = savingsTax;

            // 12. Total tax
            decimal totalTaxDue = basicRateTax + higherRateTax + additionalRateTax + savingsTax - marriageCredit;
            totalTaxDue = Math.Max(0, totalTaxDue);
            result.TotalIncomeTaxDue = totalTaxDue;

            // 13. Expected NI (per employment - pensions/annuities don't pay NI)
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
            if (result.TaxOverUnderPayment > 0)
                result.Summary = $"You may owe £{result.TaxOverUnderPayment:N2} in additional tax.";
            else if (result.TaxOverUnderPayment < 0)
                result.Summary = $"You may be owed a refund of £{Math.Abs(result.TaxOverUnderPayment):N2}.";
            else
                result.Summary = "Your tax paid matches the calculated liability.";

            if (totalExpenses > 0)
                result.Summary += $"\nEmployment expenses of £{totalExpenses:N2} have been deducted from taxable income.";

            decimal niDiff = totalNIPaid - expectedNI;
            if (Math.Abs(niDiff) > 1)
            {
                if (niDiff > 0)
                    result.Summary += $"\nNI: You paid £{niDiff:N2} more than expected.";
                else
                    result.Summary += $"\nNI: You paid £{Math.Abs(niDiff):N2} less than expected.";
            }

            return result;
        }
    }
}
