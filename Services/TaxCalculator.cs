using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

            // 1a. Company car BIK calculation
            decimal totalCarBenefit = CalculateCompanyCarBenefit(data, rules, result);
            totalBIK += totalCarBenefit;

            result.TotalEmploymentIncome = totalSalary;
            result.TotalBenefitsInKind = totalBIK;
            result.TotalPensionContributions = totalPension;
            result.TotalNIPaid = totalNIPaid;

            // 2. Calculate allowable employment expenses
            decimal totalExpenses = CalculateExpenses(data, rules, result);

            // 2a. Rental income (added to non-savings)
            decimal rentalTaxable = CalculateRentalIncome(data, rules, result);

            // 2b. Trading income (added to non-savings)
            decimal tradingTaxable = CalculateTradingIncome(data, rules, result);

            // 3. Non-savings income (employment + rental + trading)
            decimal nonSavingsIncome = Math.Max(0, totalSalary + totalBIK - totalPension - totalExpenses)
                                     + rentalTaxable + tradingTaxable;

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

            // 4a. Dividend income
            decimal totalDividends = 0, totalDividendTaxPaid = 0;
            foreach (var div in data.DividendIncomes)
            {
                totalDividends += (decimal)div.GrossDividend;
                totalDividendTaxPaid += (decimal)div.TaxPaid;
            }
            result.TotalDividendIncome = totalDividends;
            result.TotalDividendTaxPaid = totalDividendTaxPaid;
            result.TotalTaxPaidViaPAYE = totalTaxPaid + totalDividendTaxPaid;

            // 5. Gift Aid
            decimal giftAidGross = (decimal)data.GiftAidDonations * 1.25m;
            result.GiftAidExtension = giftAidGross;

            // 6. Gross income and adjusted net income (for PA taper)
            decimal grossIncome = nonSavingsIncome + taxableSavingsInterest + totalDividends;
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

            // 9. Taxable income split (PA covers non-savings first, then savings, then dividends)
            decimal taxableNonSavings = Math.Max(0, nonSavingsIncome - personalAllowance);
            decimal remainingPA = Math.Max(0, personalAllowance - nonSavingsIncome);
            decimal taxableSavings = Math.Max(0, taxableSavingsInterest - remainingPA);
            decimal remainingPA2 = Math.Max(0, remainingPA - taxableSavingsInterest);
            decimal taxableDividends = Math.Max(0, totalDividends - remainingPA2);
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

            // 11a. Tax on dividends (own rates, positioned after non-savings + savings)
            decimal dividendTax = CalculateDividendTax(
                taxableDividends, taxableNonSavings, taxableSavings,
                nonSavingsIncome, personalAllowance, giftAidGross, rules, result);
            result.DividendTaxDue = dividendTax;

            // 11b. Mortgage interest relief (20% tax credit)
            decimal mortgageRelief = 0;
            if ((decimal)data.MortgageInterest > 0)
            {
                mortgageRelief = (decimal)data.MortgageInterest * rules.MortgageInterestReliefRate;
                result.MortgageInterestRelief = mortgageRelief;
            }

            // 11c. Investment reliefs (EIS/SEIS/VCT)
            decimal investmentRelief = CalculateInvestmentRelief(data, rules, result);

            // 12. Total tax
            decimal totalTaxDue = nonSavingsTax + savingsTax + dividendTax
                                - marriageCredit - mortgageRelief - investmentRelief;
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

            // 13a. Pension Tax Credit calculation (Relief at Source contributions)
            CalculatePensionTaxCredit(data, rules, taxableNonSavings, result);

            // 13b. Pension Annual Allowance Charge
            CalculatePensionAnnualAllowanceCharge(data, rules, taxableNonSavings, result);

            // 14. Student Loan Repayments
            CalculateStudentLoan(data, rules, result);

            // 15. High Income Child Benefit Charge
            CalculateChildBenefitCharge(data, rules, adjustedNetIncome, result);

            // 16. Capital Gains Tax
            CalculateCapitalGainsTax(data, rules, taxableNonSavings + taxableSavings, result);

            // 17. Tax Code Validation
            ValidateTaxCode(data, rules, personalAllowance, result);

            // 18. Prior year tax collected via this year's PAYE
            decimal priorYearTax = (decimal)data.PriorYearTaxOwed;
            result.PriorYearTaxCollected = priorYearTax;

            // 19. Over/under payment (income tax only - student loan, HICBC, CGT shown separately)
            // Tax paid via PAYE includes prior year collection, so subtract it to get this year's effective payment
            decimal effectiveTaxPaid = totalTaxPaid - priorYearTax;
            result.TaxOverUnderPayment = totalTaxDue - effectiveTaxPaid;

            // 19. Summary
            BuildSummary(data, rules, result, totalExpenses, totalNIPaid, expectedNI);

            return result;
        }

        private static void BuildSummary(TaxYearData data, TaxYearRules rules, TaxCalculationResult result,
            decimal totalExpenses, decimal totalNIPaid, decimal expectedNI)
        {
            string regime = data.IsScottishTaxpayer ? "Scottish" : "rUK";
            if (result.TaxOverUnderPayment > 0)
                result.Summary = $"You may owe £{result.TaxOverUnderPayment:N2} in additional tax ({regime} rates).";
            else if (result.TaxOverUnderPayment < 0)
                result.Summary = $"You may be owed a refund of £{Math.Abs(result.TaxOverUnderPayment):N2} ({regime} rates).";
            else
                result.Summary = $"Your tax paid matches the calculated liability ({regime} rates).";

            if (result.PriorYearTaxCollected > 0)
                result.Summary += $"\nPrior year tax of £{result.PriorYearTaxCollected:N2} collected via this year's PAYE has been accounted for.";

            if (totalExpenses > 0)
                result.Summary += $"\nEmployment expenses of £{totalExpenses:N2} deducted from taxable income.";

            decimal niDiff = totalNIPaid - expectedNI;
            if (Math.Abs(niDiff) > 1)
            {
                result.Summary += niDiff > 0
                    ? $"\nNI: You paid £{niDiff:N2} more than expected."
                    : $"\nNI: You paid £{Math.Abs(niDiff):N2} less than expected.";
            }

            if (result.CanClaimPensionTaxCredit && result.PensionTaxCreditClaimable > 0)
            {
                result.Summary += $"\n\nPENSION TAX CREDIT: You may be able to claim £{result.PensionTaxCreditClaimable:N2} " +
                    "in additional tax relief on your pension contributions.";
            }

            if (result.StudentLoanRepayment > 0)
                result.Summary += $"\nStudent Loan: Expected repayment of £{result.StudentLoanRepayment:N2}.";

            if (result.ChildBenefitCharge > 0)
                result.Summary += $"\nChild Benefit Charge: £{result.ChildBenefitCharge:N2} payable via Self Assessment.";

            if (result.CapitalGainsTax > 0)
                result.Summary += $"\nCapital Gains Tax: £{result.CapitalGainsTax:N2}.";

            if (result.PensionAnnualAllowanceCharge > 0)
                result.Summary += $"\nPension Annual Allowance Charge: £{result.PensionAnnualAllowanceCharge:N2}.";

            if (result.TotalInvestmentRelief > 0)
                result.Summary += $"\nInvestment Relief: £{result.TotalInvestmentRelief:N2} deducted from income tax.";
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
            decimal previousGrossThreshold = personalAllowance;
            decimal remaining = taxableNonSavings;

            foreach (var band in bands)
            {
                if (remaining <= 0) break;

                decimal upperGross = band.UpperGrossThreshold;

                if (band.ExtendsWithGiftAid && upperGross > 0)
                    upperGross += giftAidGross;

                decimal bandWidth;
                if (upperGross > 0)
                {
                    decimal bandUpperTaxable = Math.Max(0, upperGross - personalAllowance);
                    decimal bandLowerTaxable = Math.Max(0, previousGrossThreshold - personalAllowance);
                    bandWidth = Math.Max(0, bandUpperTaxable - bandLowerTaxable);
                    previousGrossThreshold = upperGross;
                }
                else
                {
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

            result.TaxAtBasicRate = totalTax;
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

            decimal rUKBasicBandWidth = rules.BasicRateBandWidth + giftAidGross;
            decimal rUKAdditionalThresholdTaxable = rules.AdditionalRateThresholdGross + giftAidGross - personalAllowance;
            if (rUKAdditionalThresholdTaxable < rUKBasicBandWidth)
                rUKAdditionalThresholdTaxable = rUKBasicBandWidth;

            decimal startingRateAvailable = 0;
            decimal nonSavingsAbovePA = Math.Max(0, nonSavingsIncome - rules.PersonalAllowance);
            if (nonSavingsAbovePA < rules.StartingRateForSavingsLimit)
                startingRateAvailable = rules.StartingRateForSavingsLimit - nonSavingsAbovePA;

            decimal psa;
            if (taxableNonSavings <= rules.BasicRateBandWidth)
                psa = rules.PersonalSavingsAllowanceBasic;
            else if (grossIncome <= rules.AdditionalRateThresholdGross)
                psa = rules.PersonalSavingsAllowanceHigher;
            else
                psa = 0;

            decimal savingsRemaining = taxableSavings;
            decimal savingsTax = 0;

            decimal startingUsed = Math.Min(savingsRemaining, startingRateAvailable);
            savingsRemaining -= startingUsed;

            decimal psaUsed = Math.Min(savingsRemaining, psa);
            savingsRemaining -= psaUsed;

            if (savingsRemaining > 0)
            {
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

        private static decimal CalculateDividendTax(
            decimal taxableDividends,
            decimal taxableNonSavings,
            decimal taxableSavings,
            decimal nonSavingsIncome,
            decimal personalAllowance,
            decimal giftAidGross,
            TaxYearRules rules,
            TaxCalculationResult result)
        {
            if (taxableDividends <= 0) return 0;

            decimal rUKBasicBandWidth = rules.BasicRateBandWidth + giftAidGross;
            decimal rUKAdditionalThresholdTaxable = rules.AdditionalRateThresholdGross + giftAidGross - personalAllowance;
            if (rUKAdditionalThresholdTaxable < rUKBasicBandWidth)
                rUKAdditionalThresholdTaxable = rUKBasicBandWidth;

            decimal priorTaxableIncome = taxableNonSavings + taxableSavings;

            decimal dividendAllowanceUsed = Math.Min(taxableDividends, rules.DividendAllowance);
            decimal dividendsRemaining = taxableDividends - dividendAllowanceUsed;

            decimal dividendTax = 0;

            if (dividendsRemaining > 0)
            {
                decimal priorPlusDivAllowance = priorTaxableIncome + dividendAllowanceUsed;

                decimal basicBandRemaining = Math.Max(0, rUKBasicBandWidth - priorPlusDivAllowance);
                decimal higherBandRemaining = Math.Max(0, rUKAdditionalThresholdTaxable - Math.Max(priorPlusDivAllowance, rUKBasicBandWidth));

                decimal divAtBasic = Math.Min(dividendsRemaining, basicBandRemaining);
                dividendTax += divAtBasic * rules.DividendBasicRate;
                dividendsRemaining -= divAtBasic;

                decimal divAtHigher = Math.Min(dividendsRemaining, higherBandRemaining);
                dividendTax += divAtHigher * rules.DividendHigherRate;
                dividendsRemaining -= divAtHigher;

                if (dividendsRemaining > 0)
                    dividendTax += dividendsRemaining * rules.DividendAdditionalRate;
            }

            if (taxableDividends > 0)
            {
                result.TaxBreakdown.Add(new TaxBreakdownLine
                {
                    Label = "Tax on Dividends:",
                    IncomeText = $"on £{taxableDividends:N2}",
                    TaxText = $"£{dividendTax:N2}",
                });
            }

            return dividendTax;
        }

        private static void CalculatePensionTaxCredit(
            TaxYearData data,
            TaxYearRules rules,
            decimal taxableNonSavings,
            TaxCalculationResult result)
        {
            decimal reliefAtSourceContributions = (decimal)data.ReliefAtSourcePensionContributions;
            result.ReliefAtSourceContributions = reliefAtSourceContributions;

            if (reliefAtSourceContributions <= 0)
            {
                result.CanClaimPensionTaxCredit = false;
                result.PensionTaxCreditClaimable = 0;
                result.PensionTaxCreditInfo = "";
                return;
            }

            decimal grossContribution = reliefAtSourceContributions / (1 - rules.BasicRate);

            var bands = data.IsScottishTaxpayer ? rules.ScottishBands : rules.RestOfUKBands;
            decimal marginalRate = rules.BasicRate;
            decimal remainingIncome = taxableNonSavings;

            foreach (var band in bands)
            {
                if (remainingIncome <= 0) break;

                decimal bandWidth = band.UpperGrossThreshold > 0
                    ? band.UpperGrossThreshold - rules.PersonalAllowance
                    : decimal.MaxValue;

                if (remainingIncome > bandWidth)
                {
                    remainingIncome -= bandWidth;
                }
                else
                {
                    marginalRate = band.Rate;
                    break;
                }
            }

            decimal additionalReliefRate = marginalRate - rules.BasicRate;
            decimal claimableCredit = 0;
            var infoLines = new List<string>();

            infoLines.Add($"Relief at Source Contributions: £{reliefAtSourceContributions:N2} (net)");
            infoLines.Add($"Gross Contribution: £{grossContribution:N2}");
            infoLines.Add($"Basic Rate Relief (received at source): £{grossContribution * rules.BasicRate:N2}");

            if (additionalReliefRate > 0)
            {
                claimableCredit = grossContribution * additionalReliefRate;
                result.CanClaimPensionTaxCredit = true;

                string rateDescription = marginalRate == rules.HigherRate ? "higher rate" :
                    marginalRate == rules.AdditionalRate ? "additional rate" : $"{marginalRate * 100:N0}%";

                infoLines.Add($"");
                infoLines.Add($"ELIGIBILITY CHECK: PASSED");
                infoLines.Add($"Your marginal tax rate: {marginalRate * 100:N0}% ({rateDescription})");
                infoLines.Add($"Additional relief rate: {additionalReliefRate * 100:N0}%");
                infoLines.Add($"Additional tax relief claimable: £{claimableCredit:N2}");
                infoLines.Add($"");
                infoLines.Add($"HOW TO CLAIM:");
                infoLines.Add($"  Complete a Self Assessment tax return, or");
                infoLines.Add($"  Contact HMRC to adjust your tax code, or");
                infoLines.Add($"  Write to HMRC with pension contribution evidence");
                infoLines.Add($"");
                infoLines.Add($"Deadline: 4 years from end of tax year");
            }
            else
            {
                result.CanClaimPensionTaxCredit = false;
                infoLines.Add($"");
                infoLines.Add($"ELIGIBILITY CHECK: Not applicable");
                infoLines.Add($"As a basic rate taxpayer, you have already received the full tax relief at source.");
                infoLines.Add($"No additional claim is needed.");
            }

            if (grossContribution > rules.PensionAnnualAllowance)
            {
                infoLines.Add($"");
                infoLines.Add($"WARNING: Your gross contributions (£{grossContribution:N2}) exceed the standard");
                infoLines.Add($"Annual Allowance of £{rules.PensionAnnualAllowance:N0}. You may be liable to an Annual Allowance charge.");
                infoLines.Add($"Consider consulting a tax adviser.");
            }

            result.PensionTaxCreditClaimable = claimableCredit;
            result.PensionTaxCreditInfo = string.Join("\n", infoLines);
        }

        // ═══════════ NEW CALCULATIONS ═══════════

        private static void CalculateStudentLoan(TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            decimal totalRepayment = 0;
            var infoLines = new List<string>();

            // Total gross employment income for student loan calc
            decimal grossIncome = 0;
            foreach (var emp in data.Employments)
                grossIncome += (decimal)emp.GrossSalary;

            if (data.HasStudentLoan && data.StudentLoanPlan > 0)
            {
                decimal threshold = data.StudentLoanPlan switch
                {
                    1 => rules.StudentLoanPlan1Threshold,
                    2 => rules.StudentLoanPlan2Threshold,
                    4 => rules.StudentLoanPlan4Threshold,
                    5 => rules.StudentLoanPlan5Threshold,
                    _ => 0m,
                };

                if (threshold > 0 && grossIncome > threshold)
                {
                    decimal repayment = (grossIncome - threshold) * rules.StudentLoanRate;
                    totalRepayment += repayment;
                    infoLines.Add($"Plan {data.StudentLoanPlan}: {rules.StudentLoanRate:P0} on income above £{threshold:N0}");
                    infoLines.Add($"  Repayable income: £{grossIncome - threshold:N2}");
                    infoLines.Add($"  Annual repayment: £{repayment:N2} (£{repayment / 12:N2}/month)");
                }
                else if (threshold > 0)
                {
                    infoLines.Add($"Plan {data.StudentLoanPlan}: No repayment due (income below £{threshold:N0} threshold)");
                }
            }

            if (data.HasPostgraduateLoan)
            {
                decimal pgThreshold = rules.PostgraduateLoanThreshold;
                if (grossIncome > pgThreshold)
                {
                    decimal pgRepayment = (grossIncome - pgThreshold) * rules.PostgraduateLoanRate;
                    totalRepayment += pgRepayment;
                    infoLines.Add($"Postgraduate Loan: {rules.PostgraduateLoanRate:P0} on income above £{pgThreshold:N0}");
                    infoLines.Add($"  Annual repayment: £{pgRepayment:N2} (£{pgRepayment / 12:N2}/month)");
                }
                else
                {
                    infoLines.Add($"Postgraduate Loan: No repayment due (income below £{pgThreshold:N0} threshold)");
                }
            }

            result.StudentLoanRepayment = Math.Round(totalRepayment, 2);
            result.StudentLoanInfo = string.Join("\n", infoLines);
        }

        private static void CalculateChildBenefitCharge(
            TaxYearData data, TaxYearRules rules, decimal adjustedNetIncome, TaxCalculationResult result)
        {
            if (data.NumberOfChildren <= 0) return;

            // Calculate annual child benefit if not provided
            decimal annualBenefit = (decimal)data.ChildBenefitAmount;
            if (annualBenefit <= 0 && data.NumberOfChildren > 0)
            {
                // Calculate from weekly rates
                decimal weeklyBenefit = rules.ChildBenefitFirstChildWeekly;
                if (data.NumberOfChildren > 1)
                    weeklyBenefit += (data.NumberOfChildren - 1) * rules.ChildBenefitAdditionalChildWeekly;
                annualBenefit = weeklyBenefit * 52;
            }

            var infoLines = new List<string>();
            infoLines.Add($"Children: {data.NumberOfChildren}");
            infoLines.Add($"Annual Child Benefit: £{annualBenefit:N2}");
            infoLines.Add($"Adjusted Net Income: £{adjustedNetIncome:N2}");

            decimal charge = 0;
            if (adjustedNetIncome > rules.HICBCThreshold)
            {
                decimal incomeRange = rules.HICBCFullChargeThreshold - rules.HICBCThreshold;
                decimal excessIncome = Math.Min(adjustedNetIncome - rules.HICBCThreshold, incomeRange);
                decimal chargePercent = excessIncome / incomeRange;
                charge = Math.Round(annualBenefit * chargePercent, 2);
                charge = Math.Min(charge, annualBenefit); // Can't exceed benefit

                if (adjustedNetIncome >= rules.HICBCFullChargeThreshold)
                {
                    infoLines.Add($"");
                    infoLines.Add($"Income exceeds £{rules.HICBCFullChargeThreshold:N0} - full clawback applies");
                    infoLines.Add($"Charge: 100% of benefit = £{charge:N2}");
                    infoLines.Add($"Consider opting out of receiving Child Benefit to avoid Self Assessment");
                }
                else
                {
                    decimal pct = chargePercent * 100;
                    infoLines.Add($"");
                    infoLines.Add($"Income is £{excessIncome:N0} above the £{rules.HICBCThreshold:N0} threshold");
                    infoLines.Add($"Charge: {pct:N1}% of benefit = £{charge:N2}");
                    infoLines.Add($"Must be declared via Self Assessment tax return");
                }
            }
            else
            {
                infoLines.Add($"");
                infoLines.Add($"No charge - income is below the £{rules.HICBCThreshold:N0} threshold");
            }

            result.ChildBenefitCharge = charge;
            result.ChildBenefitInfo = string.Join("\n", infoLines);
        }

        private static void CalculateCapitalGainsTax(
            TaxYearData data, TaxYearRules rules, decimal taxableIncomeBeforeCGT, TaxCalculationResult result)
        {
            decimal totalGainsAssets = 0, totalGainsProperty = 0;
            foreach (var cg in data.CapitalGains)
            {
                if (cg.IsResidentialProperty)
                    totalGainsProperty += (decimal)cg.GainAmount;
                else
                    totalGainsAssets += (decimal)cg.GainAmount;
            }

            decimal totalGains = totalGainsAssets + totalGainsProperty;
            decimal losses = Math.Max(0, (decimal)data.CapitalGainsLosses);
            decimal netGains = Math.Max(0, totalGains - losses);
            result.TotalCapitalGains = netGains;

            if (netGains <= 0) return;

            decimal taxableGains = Math.Max(0, netGains - rules.CGTAnnualExemptAmount);
            if (taxableGains <= 0)
            {
                result.CapitalGainsInfo = $"Net gains: £{netGains:N2}\nAnnual Exempt Amount: £{rules.CGTAnnualExemptAmount:N0}\nNo CGT due - gains within AEA.";
                return;
            }

            // Determine how much basic rate band is left after income
            decimal basicBandRemaining = Math.Max(0, rules.BasicRateBandWidth - taxableIncomeBeforeCGT);

            // Allocate gains proportionally between asset types
            decimal netAssets = Math.Max(0, totalGainsAssets - (totalGainsAssets > 0 && totalGains > 0 ? losses * totalGainsAssets / totalGains : 0));
            decimal netProperty = Math.Max(0, totalGainsProperty - (totalGainsProperty > 0 && totalGains > 0 ? losses * totalGainsProperty / totalGains : 0));

            // Apply AEA proportionally
            decimal aeaAssets = netAssets > 0 && netGains > 0 ? rules.CGTAnnualExemptAmount * netAssets / netGains : 0;
            decimal aeaProperty = netProperty > 0 && netGains > 0 ? rules.CGTAnnualExemptAmount * netProperty / netGains : 0;

            decimal taxableAssets = Math.Max(0, netAssets - aeaAssets);
            decimal taxableProperty = Math.Max(0, netProperty - aeaProperty);

            decimal cgtTax = 0;
            decimal basicLeft = basicBandRemaining;

            // Tax assets first
            if (taxableAssets > 0)
            {
                decimal assetsAtBasic = Math.Min(taxableAssets, basicLeft);
                decimal assetsAtHigher = taxableAssets - assetsAtBasic;
                cgtTax += assetsAtBasic * rules.CGTBasicRateAssets;
                cgtTax += assetsAtHigher * rules.CGTHigherRateAssets;
                basicLeft -= assetsAtBasic;
            }

            // Tax property
            if (taxableProperty > 0)
            {
                decimal propAtBasic = Math.Min(taxableProperty, basicLeft);
                decimal propAtHigher = taxableProperty - propAtBasic;
                cgtTax += propAtBasic * rules.CGTBasicRateProperty;
                cgtTax += propAtHigher * rules.CGTHigherRateProperty;
            }

            result.CapitalGainsTax = Math.Round(cgtTax, 2);

            var infoLines = new List<string>();
            infoLines.Add($"Total gains: £{totalGains:N2}");
            if (losses > 0) infoLines.Add($"Losses offset: -£{losses:N2}");
            infoLines.Add($"Net gains: £{netGains:N2}");
            infoLines.Add($"Annual Exempt Amount: -£{rules.CGTAnnualExemptAmount:N0}");
            infoLines.Add($"Taxable gains: £{taxableGains:N2}");
            if (taxableAssets > 0)
                infoLines.Add($"Assets: £{taxableAssets:N2} at {rules.CGTBasicRateAssets:P0}/{rules.CGTHigherRateAssets:P0}");
            if (taxableProperty > 0)
                infoLines.Add($"Property: £{taxableProperty:N2} at {rules.CGTBasicRateProperty:P0}/{rules.CGTHigherRateProperty:P0}");
            infoLines.Add($"CGT Due: £{cgtTax:N2}");
            infoLines.Add($"Report via Self Assessment or CGT on UK Property service");

            result.CapitalGainsInfo = string.Join("\n", infoLines);
        }

        private static decimal CalculateRentalIncome(TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            decimal rentalIncome = (decimal)data.RentalIncome;
            if (rentalIncome <= 0) return 0;

            decimal expenses = (decimal)data.RentalExpenses;
            decimal mortgageInterest = (decimal)data.MortgageInterest;
            decimal taxableRental;

            var infoLines = new List<string>();
            infoLines.Add($"Gross rental income: £{rentalIncome:N2}");

            if (data.UsePropertyAllowance && rentalIncome <= rules.PropertyAllowance)
            {
                taxableRental = 0;
                infoLines.Add($"Property Allowance applied: £{rules.PropertyAllowance:N0}");
                infoLines.Add($"Taxable rental income: £0.00");
            }
            else if (data.UsePropertyAllowance)
            {
                taxableRental = rentalIncome - rules.PropertyAllowance;
                infoLines.Add($"Property Allowance applied: -£{rules.PropertyAllowance:N0}");
                infoLines.Add($"Taxable rental income: £{taxableRental:N2}");
                infoLines.Add($"Note: Cannot claim expenses AND property allowance");
            }
            else
            {
                decimal profit = rentalIncome - expenses;
                taxableRental = Math.Max(0, profit);
                infoLines.Add($"Allowable expenses: -£{expenses:N2}");
                infoLines.Add($"Rental profit: £{profit:N2}");
                if (mortgageInterest > 0)
                {
                    infoLines.Add($"Mortgage interest: £{mortgageInterest:N2}");
                    infoLines.Add($"  (20% tax credit applied separately = £{mortgageInterest * rules.MortgageInterestReliefRate:N2})");
                }
            }

            result.RentalProfit = taxableRental;
            result.RentalTaxableIncome = taxableRental;
            result.RentalInfo = string.Join("\n", infoLines);
            return taxableRental;
        }

        private static decimal CalculateTradingIncome(TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            decimal tradingIncome = (decimal)data.TradingIncome;
            if (tradingIncome <= 0) return 0;

            decimal taxableTrading;
            var infoLines = new List<string>();
            infoLines.Add($"Gross trading income: £{tradingIncome:N2}");

            if (data.UseTradingAllowance)
            {
                if (tradingIncome <= rules.TradingAllowance)
                {
                    taxableTrading = 0;
                    infoLines.Add($"Trading Allowance applied: £{rules.TradingAllowance:N0}");
                    infoLines.Add($"Taxable trading income: £0.00 (fully covered by allowance)");
                }
                else
                {
                    taxableTrading = tradingIncome - rules.TradingAllowance;
                    infoLines.Add($"Trading Allowance applied: -£{rules.TradingAllowance:N0}");
                    infoLines.Add($"Taxable trading income: £{taxableTrading:N2}");
                }
            }
            else
            {
                decimal expenses = (decimal)data.TradingExpenses;
                taxableTrading = Math.Max(0, tradingIncome - expenses);
                infoLines.Add($"Trading expenses: -£{expenses:N2}");
                infoLines.Add($"Taxable trading profit: £{taxableTrading:N2}");
            }

            result.TradingTaxableIncome = taxableTrading;
            result.TradingInfo = string.Join("\n", infoLines);
            return taxableTrading;
        }

        private static void CalculatePensionAnnualAllowanceCharge(
            TaxYearData data, TaxYearRules rules, decimal taxableNonSavings, TaxCalculationResult result)
        {
            // Sum all pension contributions (workplace + personal)
            decimal totalWorkplacePension = 0;
            foreach (var emp in data.Employments)
            {
                if (!emp.IsPensionOrAnnuity)
                    totalWorkplacePension += (decimal)emp.PensionContributions;
            }

            decimal reliefAtSource = (decimal)data.ReliefAtSourcePensionContributions;
            decimal grossRAS = reliefAtSource > 0 ? reliefAtSource / (1 - rules.BasicRate) : 0;
            decimal totalGrossContributions = totalWorkplacePension + grossRAS;

            if (totalGrossContributions <= rules.PensionAnnualAllowance) return;

            decimal excess = totalGrossContributions - rules.PensionAnnualAllowance;

            // Charge is at marginal rate
            var bands = data.IsScottishTaxpayer ? rules.ScottishBands : rules.RestOfUKBands;
            decimal marginalRate = rules.BasicRate;
            decimal remainingIncome = taxableNonSavings;
            foreach (var band in bands)
            {
                if (remainingIncome <= 0) break;
                decimal bandWidth = band.UpperGrossThreshold > 0
                    ? band.UpperGrossThreshold - rules.PersonalAllowance
                    : decimal.MaxValue;
                if (remainingIncome > bandWidth)
                    remainingIncome -= bandWidth;
                else
                {
                    marginalRate = band.Rate;
                    break;
                }
            }

            decimal charge = excess * marginalRate;
            result.PensionAnnualAllowanceCharge = Math.Round(charge, 2);

            var infoLines = new List<string>();
            infoLines.Add($"Workplace pension contributions: £{totalWorkplacePension:N2}");
            if (grossRAS > 0) infoLines.Add($"Personal pension (gross): £{grossRAS:N2}");
            infoLines.Add($"Total gross contributions: £{totalGrossContributions:N2}");
            infoLines.Add($"Annual Allowance: £{rules.PensionAnnualAllowance:N0}");
            infoLines.Add($"Excess: £{excess:N2}");
            infoLines.Add($"Charge at {marginalRate:P0} marginal rate: £{charge:N2}");
            infoLines.Add($"");
            infoLines.Add($"Note: You may be able to carry forward unused allowance from the");
            infoLines.Add($"previous 3 tax years to reduce or eliminate this charge.");
            infoLines.Add($"Report via Self Assessment.");

            result.PensionAACInfo = string.Join("\n", infoLines);
        }

        private static decimal CalculateInvestmentRelief(TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            decimal eisAmount = (decimal)data.EisInvestment;
            decimal seisAmount = (decimal)data.SeisInvestment;
            decimal vctAmount = (decimal)data.VctInvestment;

            if (eisAmount <= 0 && seisAmount <= 0 && vctAmount <= 0) return 0;

            decimal eisRelief = eisAmount * rules.EISReliefRate;
            decimal seisRelief = seisAmount * rules.SEISReliefRate;
            decimal vctRelief = vctAmount * rules.VCTReliefRate;
            decimal totalRelief = eisRelief + seisRelief + vctRelief;

            var infoLines = new List<string>();
            if (eisAmount > 0)
                infoLines.Add($"EIS: £{eisAmount:N2} x {rules.EISReliefRate:P0} = £{eisRelief:N2}");
            if (seisAmount > 0)
                infoLines.Add($"SEIS: £{seisAmount:N2} x {rules.SEISReliefRate:P0} = £{seisRelief:N2}");
            if (vctAmount > 0)
                infoLines.Add($"VCT: £{vctAmount:N2} x {rules.VCTReliefRate:P0} = £{vctRelief:N2}");
            infoLines.Add($"Total relief: £{totalRelief:N2} (deducted from income tax)");
            infoLines.Add($"");
            infoLines.Add($"Shares must be held for minimum period to retain relief.");
            infoLines.Add($"EIS/SEIS: 3 years. VCT: 5 years.");

            result.TotalInvestmentRelief = totalRelief;
            result.InvestmentReliefInfo = string.Join("\n", infoLines);
            return totalRelief;
        }

        private static decimal CalculateCompanyCarBenefit(TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            decimal totalCarBenefit = 0;
            var infoLines = new List<string>();

            foreach (var emp in data.Employments)
            {
                if (emp.IsPensionOrAnnuity || !emp.HasCompanyCar) continue;

                decimal listPrice = (decimal)emp.CarListPrice;
                if (listPrice <= 0) continue;

                int bikPercent = TaxRulesProvider.GetCompanyCarBIKPercentage(
                    emp.CarCO2Emissions, emp.CarIsElectric, data.TaxYear);

                decimal carBenefit = listPrice * bikPercent / 100m;
                totalCarBenefit += carBenefit;

                infoLines.Add($"{emp.EmployerName}: {(emp.CarIsElectric ? "Electric" : "Standard")} car");
                infoLines.Add($"  List price: £{listPrice:N0} | CO2: {emp.CarCO2Emissions} g/km | BIK: {bikPercent}%");
                infoLines.Add($"  Car benefit: £{carBenefit:N2}");

                if (emp.CarFuelBenefit > 0)
                {
                    decimal fuelBenefit = rules.CarFuelBenefitMultiplier * bikPercent / 100m;
                    totalCarBenefit += fuelBenefit;
                    infoLines.Add($"  Fuel benefit: £{fuelBenefit:N2} (multiplier £{rules.CarFuelBenefitMultiplier:N0} x {bikPercent}%)");
                }
            }

            result.TotalCompanyCarBenefit = totalCarBenefit;
            result.CompanyCarInfo = string.Join("\n", infoLines);
            return totalCarBenefit;
        }

        private static void ValidateTaxCode(
            TaxYearData data, TaxYearRules rules, decimal calculatedPA, TaxCalculationResult result)
        {
            string code = (data.TaxCode ?? "").Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) return;

            var infoLines = new List<string>();
            infoLines.Add($"Tax Code: {code}");
            bool hasWarning = false;

            // Strip S (Scottish) or C (Welsh) prefix
            string coreCode = code;
            bool isScottishCode = false;
            if (coreCode.StartsWith("S"))
            {
                isScottishCode = true;
                coreCode = coreCode.Substring(1);
                infoLines.Add($"Prefix 'S' = Scottish tax rates");
            }
            else if (coreCode.StartsWith("C"))
            {
                coreCode = coreCode.Substring(1);
                infoLines.Add($"Prefix 'C' = Welsh tax rates");
            }

            // Check Scottish code matches Scottish taxpayer setting
            if (isScottishCode && !data.IsScottishTaxpayer)
            {
                infoLines.Add($"WARNING: Tax code has Scottish prefix but Scottish taxpayer is not ticked");
                hasWarning = true;
            }
            else if (!isScottishCode && data.IsScottishTaxpayer && code != "BR" && code != "D0" && code != "D1" && code != "NT")
            {
                infoLines.Add($"WARNING: Scottish taxpayer is ticked but tax code has no 'S' prefix");
                hasWarning = true;
            }

            // Decode the code
            decimal impliedPA;
            if (coreCode == "BR")
            {
                infoLines.Add($"BR = All income taxed at basic rate (no personal allowance)");
                impliedPA = 0;
            }
            else if (coreCode == "D0")
            {
                infoLines.Add($"D0 = All income taxed at higher rate");
                impliedPA = 0;
            }
            else if (coreCode == "D1")
            {
                infoLines.Add($"D1 = All income taxed at additional rate");
                impliedPA = 0;
            }
            else if (coreCode == "NT")
            {
                infoLines.Add($"NT = No tax deducted");
                impliedPA = -1; // special
            }
            else if (coreCode == "0T")
            {
                infoLines.Add($"0T = No personal allowance (may indicate underpayment collection)");
                impliedPA = 0;
            }
            else if (coreCode.StartsWith("K") && Regex.IsMatch(coreCode.Substring(1), @"^\d+$"))
            {
                // K codes - negative allowance
                int kNumber = int.Parse(coreCode.Substring(1));
                decimal kDeduction = kNumber * 10m;
                infoLines.Add($"K code = Negative allowance of £{kDeduction:N0}");
                infoLines.Add($"This means deductions/benefits exceed your Personal Allowance by £{kDeduction:N0}");
                impliedPA = -kDeduction;
            }
            else if (Regex.IsMatch(coreCode, @"^\d+[TLMN]?$"))
            {
                // Standard number + suffix
                var match = Regex.Match(coreCode, @"^(\d+)([TLMN]?)$");
                int number = int.Parse(match.Groups[1].Value);
                string suffix = match.Groups[2].Value;
                impliedPA = number * 10m;

                string suffixDesc = suffix switch
                {
                    "L" => "Standard personal allowance",
                    "T" => "Items for HMRC review (no automatic adjustments)",
                    "M" => "Marriage Allowance - receiving 10% of partner's allowance",
                    "N" => "Marriage Allowance - transferred 10% of allowance to partner",
                    _ => "",
                };

                infoLines.Add($"Implied Personal Allowance: £{impliedPA:N0}");
                if (!string.IsNullOrEmpty(suffixDesc))
                    infoLines.Add($"Suffix '{suffix}' = {suffixDesc}");
            }
            else
            {
                infoLines.Add($"Unrecognised tax code format");
                result.TaxCodeValidation = string.Join("\n", infoLines);
                result.TaxCodeHasWarning = true;
                return;
            }

            // Compare with calculated PA
            if (impliedPA >= 0)
            {
                infoLines.Add($"");
                infoLines.Add($"Calculated Personal Allowance: £{calculatedPA:N0}");
                infoLines.Add($"Tax Code Implied Allowance: £{impliedPA:N0}");

                decimal diff = calculatedPA - impliedPA;
                if (Math.Abs(diff) <= 10)
                {
                    infoLines.Add($"Tax code matches calculated allowance.");
                }
                else if (diff > 0)
                {
                    infoLines.Add($"Tax code gives LESS allowance than expected (£{diff:N0} less).");
                    infoLines.Add($"This could mean HMRC is collecting underpaid tax from a previous year,");
                    infoLines.Add($"or accounting for benefits in kind via your tax code.");
                    hasWarning = true;
                }
                else
                {
                    infoLines.Add($"Tax code gives MORE allowance than expected (£{Math.Abs(diff):N0} more).");
                    infoLines.Add($"This could mean HMRC hasn't been informed of all your income sources.");
                    hasWarning = true;
                }
            }

            result.TaxCodeValidation = string.Join("\n", infoLines);
            result.TaxCodeHasWarning = hasWarning;
        }
    }
}
