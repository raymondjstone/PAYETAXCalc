using PAYETAXCalc.Models;
using PAYETAXCalc.Services;
using Xunit;

namespace PAYETAXCalc.Tests;

public class TaxCalculatorTests
{
    private static TaxYearRules Rules202425 => TaxRulesProvider.GetRules("2024/25")!;
    private static TaxYearRules Rules202324 => TaxRulesProvider.GetRules("2023/24")!;
    private static TaxYearRules Rules202526 => TaxRulesProvider.GetRules("2025/26")!;

    private static TaxYearData MakeData(
        double salary,
        double taxPaid = 0,
        double niPaid = 0,
        string taxYear = "2024/25",
        bool scottish = false)
    {
        var data = new TaxYearData { TaxYear = taxYear, IsScottishTaxpayer = scottish };
        data.Employments.Add(new Employment
        {
            EmployerName = "Test Employer",
            GrossSalary = salary,
            TaxPaid = taxPaid,
            NationalInsurancePaid = niPaid,
        });
        return data;
    }

    // ═══════════ Basic income tax calculation ═══════════

    [Fact]
    public void Income_Below_PA_Should_Have_Zero_Tax()
    {
        var data = MakeData(12000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalIncomeTaxDue);
        Assert.Equal(12000m, result.TotalEmploymentIncome);
    }

    [Fact]
    public void Income_At_Exactly_PA_Should_Have_Zero_Tax()
    {
        var data = MakeData(12570);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalIncomeTaxDue);
    }

    [Fact]
    public void Basic_Rate_Only_Income()
    {
        // £30,000 salary: £30,000 - £12,570 = £17,430 taxable at 20%
        var data = MakeData(30000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = 17430m * 0.20m;
        Assert.Equal(expected, result.TotalIncomeTaxDue);
        Assert.Equal(17430m, result.TaxableNonSavingsIncome);
    }

    [Fact]
    public void Top_Of_Basic_Rate_Band()
    {
        // £50,270 is top of basic band for 2024/25
        var data = MakeData(50270);
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal taxable = 50270m - 12570m; // 37,700
        decimal expected = taxable * 0.20m; // 7,540
        Assert.Equal(expected, result.TotalIncomeTaxDue);
    }

    [Fact]
    public void Higher_Rate_Income()
    {
        // £70,000: basic on 37,700 + higher on (70,000-50,270)=19,730
        var data = MakeData(70000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal basicTax = 37700m * 0.20m;
        decimal higherTax = (70000m - 50270m) * 0.40m;
        Assert.Equal(basicTax + higherTax, result.TotalIncomeTaxDue);
    }

    [Fact]
    public void Additional_Rate_Income()
    {
        // £150,000: PA fully tapered away (ANI > 125,140 so PA = 0)
        // With PA=0, band boundaries are at gross thresholds:
        // Basic: 0 to 50,270 → 50,270 @ 20%, Higher: 50,270 to 125,140 → 74,870 @ 40%, Additional: 125,140+ → 24,860 @ 45%
        var data = MakeData(150000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        // PA taper: (150,000 - 100,000)/2 = 25,000 reduction. PA = max(0, 12,570-25,000) = 0
        Assert.Equal(0m, result.PersonalAllowanceUsed);

        decimal basicTax = 50270m * 0.20m;
        decimal higherTax = (125140m - 50270m) * 0.40m;
        decimal additionalTax = (150000m - 125140m) * 0.45m;
        decimal expected = basicTax + higherTax + additionalTax;
        Assert.Equal(expected, result.TotalIncomeTaxDue);
    }

    [Fact]
    public void Zero_Income_Should_Have_Zero_Tax()
    {
        var data = MakeData(0);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalIncomeTaxDue);
        Assert.Equal(0m, result.GrossIncome);
    }

    // ═══════════ Personal Allowance taper ═══════════

    [Fact]
    public void PA_Not_Tapered_Below_Threshold()
    {
        var data = MakeData(99999);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(12570m, result.PersonalAllowanceUsed);
    }

    [Fact]
    public void PA_Tapered_Above_100k()
    {
        // £110,000: PA reduced by (110,000-100,000)/2 = 5,000 → PA = 7,570
        var data = MakeData(110000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(7570m, result.PersonalAllowanceUsed);
    }

    [Fact]
    public void PA_Fully_Eliminated_At_125140()
    {
        var data = MakeData(125140);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.PersonalAllowanceUsed);
    }

    [Fact]
    public void PA_Cannot_Go_Below_Zero()
    {
        var data = MakeData(200000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.PersonalAllowanceUsed);
    }

    // ═══════════ Over/under payment ═══════════

    [Fact]
    public void Exact_Tax_Paid_Shows_Zero_Difference()
    {
        decimal expectedTax = 17430m * 0.20m; // £3,486
        var data = MakeData(30000, taxPaid: (double)expectedTax);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TaxOverUnderPayment);
        Assert.Contains("matches", result.Summary);
    }

    [Fact]
    public void Overpaid_Tax_Shows_Negative_Difference()
    {
        var data = MakeData(30000, taxPaid: 5000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxOverUnderPayment < 0);
        Assert.Contains("refund", result.Summary);
    }

    [Fact]
    public void Underpaid_Tax_Shows_Positive_Difference()
    {
        var data = MakeData(30000, taxPaid: 1000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxOverUnderPayment > 0);
        Assert.Contains("owe", result.Summary);
    }

    // ═══════════ National Insurance ═══════════

    [Fact]
    public void NI_Below_Primary_Threshold_Is_Zero()
    {
        var data = MakeData(12000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.ExpectedNI);
    }

    [Fact]
    public void NI_Main_Rate_Only_202425()
    {
        // £30,000: NI on (30,000-12,570)=17,430 at 8%
        var data = MakeData(30000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round(17430m * 0.08m, 2);
        Assert.Equal(expected, result.ExpectedNI);
    }

    [Fact]
    public void NI_With_Upper_Rate_202425()
    {
        // £60,000: main on (50,270-12,570)=37,700 at 8%, upper on (60,000-50,270)=9,730 at 2%
        var data = MakeData(60000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round(37700m * 0.08m + 9730m * 0.02m, 2);
        Assert.Equal(expected, result.ExpectedNI);
    }

    [Fact]
    public void NI_Different_Rate_For_202324()
    {
        // 2023/24 NI main rate is 12%
        var data = MakeData(30000, taxYear: "2023/24");
        var result = TaxCalculator.Calculate(data, Rules202324);

        decimal expected = Math.Round(17430m * 0.12m, 2);
        Assert.Equal(expected, result.ExpectedNI);
    }

    [Fact]
    public void NI_Skips_Pension_Employment()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            EmployerName = "Pension Provider",
            IsPensionOrAnnuity = true,
            GrossSalary = 20000,
        });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.ExpectedNI);
    }

    [Fact]
    public void NI_Calculated_Per_Employment()
    {
        // Two employments: each £25,000 → each has NI on (25,000-12,570)=12,430 at 8%
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment { GrossSalary = 25000 });
        data.Employments.Add(new Employment { GrossSalary = 25000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal perEmp = 12430m * 0.08m;
        Assert.Equal(Math.Round(perEmp * 2, 2), result.ExpectedNI);
    }

    // ═══════════ Multiple employments ═══════════

    [Fact]
    public void Multiple_Employments_Income_Summed()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment { GrossSalary = 20000, TaxPaid = 1000 });
        data.Employments.Add(new Employment { GrossSalary = 15000, TaxPaid = 500 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(35000m, result.TotalEmploymentIncome);
        Assert.Equal(1500m, result.TotalTaxPaidViaPAYE);
    }

    [Fact]
    public void Pension_Employment_Excluded_From_BIK_And_Expenses()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            IsPensionOrAnnuity = true,
            GrossSalary = 20000,
            BenefitsInKind = 5000, // should be ignored
            WorkFromHomeWeeks = 52, // should be ignored
        });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalBenefitsInKind);
        Assert.Equal(0m, result.TotalEmploymentExpenses);
    }

    // ═══════════ Benefits in Kind ═══════════

    [Fact]
    public void BIK_Added_To_Taxable_Income()
    {
        var data = MakeData(20000);
        data.Employments[0].BenefitsInKind = 3000;
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Non-savings = 20,000 + 3,000 = 23,000. Taxable = 23,000 - 12,570 = 10,430
        Assert.Equal(3000m, result.TotalBenefitsInKind);
        Assert.Equal(10430m, result.TaxableNonSavingsIncome);
    }

    // ═══════════ Pension contributions ═══════════

    [Fact]
    public void Pension_Contributions_Reduce_Taxable_Income()
    {
        var data = MakeData(30000);
        data.Employments[0].PensionContributions = 2000;
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Non-savings = 30,000 - 2,000 = 28,000. Taxable = 28,000 - 12,570 = 15,430
        Assert.Equal(2000m, result.TotalPensionContributions);
        Assert.Equal(15430m, result.TaxableNonSavingsIncome);
    }

    // ═══════════ Savings income ═══════════

    [Fact]
    public void Taxable_Savings_Interest_Included()
    {
        var data = MakeData(12570); // exactly at PA, no income tax on earnings
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 2000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(2000m, result.TotalSavingsInterest);
        Assert.Equal(14570m, result.GrossIncome);
    }

    [Fact]
    public void TaxFree_Savings_Not_Taxed()
    {
        var data = MakeData(12570);
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 5000, IsTaxFree = true });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalSavingsInterest);
        Assert.Equal(5000m, result.TotalTaxFreeSavings);
        Assert.Equal(12570m, result.GrossIncome); // ISA doesn't count
    }

    [Fact]
    public void Savings_Starting_Rate_Applies_When_NonSavings_Below_PA()
    {
        // Income exactly at PA → non-savings above PA = 0 → full £5,000 starting rate available
        var data = MakeData(12570);
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 6000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // 6,000 savings, PA fully used by salary. Starting rate covers 5,000, PSA covers 1,000 (basic rate taxpayer)
        // So all savings tax-free
        Assert.Equal(0m, result.SavingsTaxDue);
    }

    [Fact]
    public void Savings_PSA_Basic_Rate_Taxpayer()
    {
        // Basic rate taxpayer gets £1,000 PSA
        var data = MakeData(30000);
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 1000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Non-savings above PA > starting rate limit, so no starting rate
        // £1,000 savings <= £1,000 PSA → no savings tax
        Assert.Equal(0m, result.SavingsTaxDue);
    }

    [Fact]
    public void Savings_PSA_Higher_Rate_Taxpayer()
    {
        // Higher rate taxpayer gets £500 PSA
        var data = MakeData(60000);
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 1000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // £500 covered by PSA, remaining £500 taxed at 20% (basic rate band)
        Assert.Equal(500m * 0.20m, result.SavingsTaxDue);
    }

    [Fact]
    public void Savings_Remaining_PA_Applied()
    {
        // Income below PA → remaining PA applied to savings
        var data = MakeData(10000);
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 5000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Remaining PA = 12,570 - 10,000 = 2,570
        // Taxable savings = 5,000 - 2,570 = 2,430
        Assert.Equal(2430m, result.TaxableSavingsIncome);
    }

    // ═══════════ Gift Aid ═══════════

    [Fact]
    public void Gift_Aid_Grossed_Up()
    {
        var data = MakeData(30000);
        data.GiftAidDonations = 100;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(125m, result.GiftAidExtension);
    }

    [Fact]
    public void Gift_Aid_Extends_Basic_Rate_Band()
    {
        // Income just above basic band. Gift Aid should extend the band.
        // £50,370: taxable = 37,800, basic band = 37,700 → £100 at higher rate without gift aid
        var data = MakeData(50370);
        data.GiftAidDonations = 100; // grossed up = £125

        var withGiftAid = TaxCalculator.Calculate(data, Rules202425);

        var dataNoGA = MakeData(50370);
        var withoutGiftAid = TaxCalculator.Calculate(dataNoGA, Rules202425);

        // Gift aid extends basic band by £125, so less income at higher rate
        Assert.True(withGiftAid.TotalIncomeTaxDue < withoutGiftAid.TotalIncomeTaxDue);
    }

    // ═══════════ Marriage Allowance ═══════════

    [Fact]
    public void Marriage_Allowance_Receiver_Gets_Credit()
    {
        var data = MakeData(30000);
        data.ClaimMarriageAllowance = true;
        data.IsMarriageAllowanceReceiver = true;
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expectedCredit = 1260m * 0.20m; // 252
        Assert.Equal(expectedCredit, result.MarriageAllowanceCredit);
        // Credit reduces total tax
        var noMA = TaxCalculator.Calculate(MakeData(30000), Rules202425);
        Assert.Equal(noMA.TotalIncomeTaxDue - expectedCredit, result.TotalIncomeTaxDue);
    }

    [Fact]
    public void Marriage_Allowance_Transferrer_Loses_PA()
    {
        var data = MakeData(30000);
        data.ClaimMarriageAllowance = true;
        data.IsMarriageAllowanceReceiver = false;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(12570m - 1260m, result.PersonalAllowanceUsed);
    }

    [Fact]
    public void No_Marriage_Allowance_When_Not_Claimed()
    {
        var data = MakeData(30000);
        data.ClaimMarriageAllowance = false;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.MarriageAllowanceCredit);
        Assert.Equal(12570m, result.PersonalAllowanceUsed);
    }

    // ═══════════ Blind Person's Allowance ═══════════

    [Fact]
    public void Blind_Persons_Allowance_Increases_PA()
    {
        var data = MakeData(30000);
        data.ClaimBlindPersonsAllowance = true;
        var result = TaxCalculator.Calculate(data, Rules202425);

        // PA = 12,570 + 3,070 = 15,640
        Assert.Equal(15640m, result.PersonalAllowanceUsed);
        decimal taxable = 30000m - 15640m;
        Assert.Equal(taxable, result.TaxableNonSavingsIncome);
    }

    [Fact]
    public void Blind_Persons_Allowance_Values_Per_Year()
    {
        var data2324 = MakeData(30000, taxYear: "2023/24");
        data2324.ClaimBlindPersonsAllowance = true;
        var r2324 = TaxCalculator.Calculate(data2324, Rules202324);

        var data2526 = MakeData(30000, taxYear: "2025/26");
        data2526.ClaimBlindPersonsAllowance = true;
        var r2526 = TaxCalculator.Calculate(data2526, Rules202526);

        // 2023/24: 2,870, 2025/26: 3,130
        Assert.Equal(12570m + 2870m, r2324.PersonalAllowanceUsed);
        Assert.Equal(12570m + 3130m, r2526.PersonalAllowanceUsed);
    }

    // ═══════════ Employment Expenses ═══════════

    [Fact]
    public void WFH_Expense_Calculated_At_Flat_Rate()
    {
        var data = MakeData(30000);
        data.Employments[0].WorkFromHomeWeeks = 26;
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = 26m * 6m; // 156
        Assert.Equal(expected, result.TotalEmploymentExpenses);
        Assert.Contains("Working from home", result.ExpensesBreakdown);
    }

    [Fact]
    public void Mileage_Under_10000_At_45p()
    {
        var data = MakeData(30000);
        data.Employments[0].BusinessMiles = 5000;
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = 5000m * 0.45m; // 2,250
        Assert.Equal(expected, result.TotalEmploymentExpenses);
    }

    [Fact]
    public void Mileage_Over_10000_Split_Rate()
    {
        var data = MakeData(30000);
        data.Employments[0].BusinessMiles = 15000;
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = (10000m * 0.45m) + (5000m * 0.25m); // 4,500 + 1,250 = 5,750
        Assert.Equal(expected, result.TotalEmploymentExpenses);
    }

    [Fact]
    public void Mileage_Aggregated_Across_Employments()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment { GrossSalary = 20000, BusinessMiles = 8000 });
        data.Employments.Add(new Employment { GrossSalary = 20000, BusinessMiles = 4000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Total 12,000 miles: 10,000 at 45p + 2,000 at 25p
        decimal expected = (10000m * 0.45m) + (2000m * 0.25m);
        Assert.Equal(expected, result.TotalEmploymentExpenses);
    }

    [Fact]
    public void Professional_Subscriptions_Deducted()
    {
        var data = MakeData(30000);
        data.Employments[0].ProfessionalSubscriptions = 200;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(200m, result.TotalEmploymentExpenses);
        Assert.Contains("Professional subscriptions", result.ExpensesBreakdown);
    }

    [Fact]
    public void Uniform_Allowance_Deducted()
    {
        var data = MakeData(30000);
        data.Employments[0].UniformAllowance = 60;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(60m, result.TotalEmploymentExpenses);
        Assert.Contains("Uniform", result.ExpensesBreakdown);
    }

    [Fact]
    public void Other_Expenses_Deducted()
    {
        var data = MakeData(30000);
        data.Employments[0].OtherExpenses = 150;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(150m, result.TotalEmploymentExpenses);
        Assert.Contains("Other expenses", result.ExpensesBreakdown);
    }

    [Fact]
    public void Multiple_Expense_Types_Combined()
    {
        var data = MakeData(30000);
        data.Employments[0].WorkFromHomeWeeks = 10;
        data.Employments[0].ProfessionalSubscriptions = 100;
        data.Employments[0].UniformAllowance = 60;
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = (10m * 6m) + 100m + 60m; // 60 + 100 + 60 = 220
        Assert.Equal(expected, result.TotalEmploymentExpenses);
    }

    [Fact]
    public void Expenses_Reduce_Taxable_Income()
    {
        var data = MakeData(30000);
        data.Employments[0].WorkFromHomeWeeks = 52;
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal taxable = 30000m - (52m * 6m) - 12570m;
        Assert.Equal(taxable, result.TaxableNonSavingsIncome);
    }

    [Fact]
    public void Expenses_On_Pension_Employment_Ignored()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            IsPensionOrAnnuity = true,
            GrossSalary = 20000,
            WorkFromHomeWeeks = 52,
            BusinessMiles = 10000,
            ProfessionalSubscriptions = 200,
            UniformAllowance = 60,
            OtherExpenses = 100,
        });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalEmploymentExpenses);
    }

    // ═══════════ Scottish taxpayer ═══════════

    [Fact]
    public void Scottish_Uses_Scottish_Bands()
    {
        var data = MakeData(30000, scottish: true);
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Should have Scottish band names in breakdown
        Assert.Contains(result.TaxBreakdown, b => b.Label.Contains("Starter"));
    }

    [Fact]
    public void RUK_Uses_RUK_Bands()
    {
        var data = MakeData(30000, scottish: false);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains(result.TaxBreakdown, b => b.Label.Contains("Basic"));
        Assert.DoesNotContain(result.TaxBreakdown, b => b.Label.Contains("Starter"));
    }

    [Fact]
    public void Scottish_Tax_Different_From_RUK()
    {
        var dataScot = MakeData(50000, scottish: true);
        var resultScot = TaxCalculator.Calculate(dataScot, Rules202425);

        var dataRUK = MakeData(50000, scottish: false);
        var resultRUK = TaxCalculator.Calculate(dataRUK, Rules202425);

        // Scottish rates differ from rUK
        Assert.NotEqual(resultRUK.TotalIncomeTaxDue, resultScot.TotalIncomeTaxDue);
    }

    [Fact]
    public void Scottish_Starter_Rate_202425()
    {
        // £14,876 is top of Scottish starter band for 2024/25
        // Taxable at starter = 14,876 - 12,570 = 2,306 at 19%
        var data = MakeData(14876, scottish: true);
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = 2306m * 0.19m;
        Assert.Equal(expected, result.TotalIncomeTaxDue);
    }

    [Fact]
    public void Scottish_Summary_Shows_Scottish()
    {
        var data = MakeData(30000, scottish: true);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Scottish", result.Summary);
    }

    [Fact]
    public void RUK_Summary_Shows_RUK()
    {
        var data = MakeData(30000, scottish: false);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("rUK", result.Summary);
    }

    [Fact]
    public void Scottish_Higher_Income_Has_Multiple_Bands()
    {
        // £80,000 Scottish: should hit starter, basic, intermediate, higher bands
        var data = MakeData(80000, scottish: true);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxBreakdown.Count >= 4);
    }

    [Fact]
    public void Scottish_Savings_Still_Use_RUK_Rates()
    {
        // Scottish taxpayer's savings should be taxed at rUK rates
        var dataScot = MakeData(30000, scottish: true);
        dataScot.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 2000 });
        var resultScot = TaxCalculator.Calculate(dataScot, Rules202425);

        var dataRUK = MakeData(30000, scottish: false);
        dataRUK.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 2000 });
        var resultRUK = TaxCalculator.Calculate(dataRUK, Rules202425);

        // Savings tax should be the same for both
        Assert.Equal(resultRUK.SavingsTaxDue, resultScot.SavingsTaxDue);
    }

    // ═══════════ Tax breakdown ═══════════

    [Fact]
    public void Breakdown_Lines_Populated_For_RUK()
    {
        var data = MakeData(70000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxBreakdown.Count > 0);
        Assert.All(result.TaxBreakdown, line =>
        {
            Assert.False(string.IsNullOrEmpty(line.Label));
            Assert.False(string.IsNullOrEmpty(line.TaxText));
        });
    }

    [Fact]
    public void Savings_Tax_Appears_In_Breakdown_When_Due()
    {
        var data = MakeData(60000);
        data.SavingsIncomes.Add(new SavingsIncome { InterestAmount = 2000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains(result.TaxBreakdown, b => b.Label.Contains("Savings"));
    }

    // ═══════════ Edge cases ═══════════

    [Fact]
    public void NonSavings_Income_Cannot_Be_Negative()
    {
        // Expenses > income shouldn't produce negative
        var data = MakeData(500);
        data.Employments[0].WorkFromHomeWeeks = 52;
        data.Employments[0].ProfessionalSubscriptions = 500;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalIncomeTaxDue);
        Assert.Equal(0m, result.TaxableNonSavingsIncome);
    }

    [Fact]
    public void Marriage_Credit_Cannot_Make_Tax_Negative()
    {
        // Very low income where marriage credit > tax due
        var data = MakeData(13000);
        data.ClaimMarriageAllowance = true;
        data.IsMarriageAllowanceReceiver = true;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TotalIncomeTaxDue >= 0);
    }

    [Fact]
    public void NI_Difference_Noted_In_Summary()
    {
        // Pay more NI than expected
        var data = MakeData(30000, niPaid: 5000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("NI", result.Summary);
    }

    [Fact]
    public void All_Three_Tax_Years_Calculate_Without_Error()
    {
        foreach (var year in new[] { "2023/24", "2024/25", "2025/26" })
        {
            var rules = TaxRulesProvider.GetRules(year)!;
            var data = MakeData(50000, taxYear: year);
            var result = TaxCalculator.Calculate(data, rules);

            Assert.True(result.TotalIncomeTaxDue > 0);
            Assert.True(result.TaxableNonSavingsIncome > 0);
            Assert.False(string.IsNullOrEmpty(result.Summary));
        }
    }

    // ═══════════ Future / estimated years ═══════════

    [Fact]
    public void Future_Year_With_Estimated_Rules_Calculates()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2030/31");
        var data = MakeData(50000, taxYear: "2030/31");
        var result = TaxCalculator.Calculate(data, rules);

        Assert.True(result.TotalIncomeTaxDue > 0);
        Assert.True(result.TaxableNonSavingsIncome > 0);
        Assert.False(string.IsNullOrEmpty(result.Summary));
    }

    [Fact]
    public void Estimated_Rules_Produce_Same_Tax_As_Latest_Known()
    {
        var latestDefined = TaxRulesProvider.GetDefinedTaxYears();
        var latestYear = latestDefined[^1];
        var latestRules = TaxRulesProvider.GetRules(latestYear)!;

        var estimatedRules = TaxRulesProvider.GetOrEstimateRules("2030/31");

        // Same salary should produce same tax with same rates
        var data1 = MakeData(50000, taxYear: latestYear);
        var result1 = TaxCalculator.Calculate(data1, latestRules);

        var data2 = MakeData(50000, taxYear: "2030/31");
        var result2 = TaxCalculator.Calculate(data2, estimatedRules);

        Assert.Equal(result1.TotalIncomeTaxDue, result2.TotalIncomeTaxDue);
        Assert.Equal(result1.ExpectedNI, result2.ExpectedNI);
    }

    [Fact]
    public void Future_Year_Scottish_Calculates()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2028/29");
        var data = MakeData(60000, taxYear: "2028/29", scottish: true);
        var result = TaxCalculator.Calculate(data, rules);

        Assert.True(result.TotalIncomeTaxDue > 0);
        Assert.Contains(result.TaxBreakdown, b => b.Label.Contains("Starter"));
    }

    // ═══════════ Pension Tax Credit (Relief at Source) ═══════════

    [Fact]
    public void No_Relief_At_Source_No_Pension_Tax_Credit()
    {
        var data = MakeData(50000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.False(result.CanClaimPensionTaxCredit);
        Assert.Equal(0m, result.PensionTaxCreditClaimable);
        Assert.Equal(0m, result.ReliefAtSourceContributions);
    }

    [Fact]
    public void Basic_Rate_Taxpayer_No_Additional_Relief()
    {
        // £30,000 salary = basic rate taxpayer
        var data = MakeData(30000);
        data.ReliefAtSourcePensionContributions = 1000;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(1000m, result.ReliefAtSourceContributions);
        Assert.False(result.CanClaimPensionTaxCredit);
        Assert.Equal(0m, result.PensionTaxCreditClaimable);
        Assert.Contains("basic rate taxpayer", result.PensionTaxCreditInfo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Higher_Rate_Taxpayer_Gets_Additional_Relief()
    {
        // £70,000 salary = higher rate taxpayer (40%)
        var data = MakeData(70000);
        data.ReliefAtSourcePensionContributions = 800; // Net contribution
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Gross = 800 / 0.80 = 1000
        // Basic rate relief already received = 1000 * 0.20 = 200
        // Additional relief at 40% - 20% = 20% on gross = 1000 * 0.20 = 200
        decimal expectedCredit = (800m / 0.80m) * 0.20m;

        Assert.True(result.CanClaimPensionTaxCredit);
        Assert.Equal(expectedCredit, result.PensionTaxCreditClaimable);
        Assert.Contains("higher rate", result.PensionTaxCreditInfo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Additional_Rate_Taxpayer_Gets_More_Relief()
    {
        // £160,000 salary = additional rate taxpayer (45%)
        var data = MakeData(160000);
        data.ReliefAtSourcePensionContributions = 4000; // Net contribution
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Gross = 4000 / 0.80 = 5000
        // Additional relief at 45% - 20% = 25% on gross = 5000 * 0.25 = 1250
        decimal expectedCredit = (4000m / 0.80m) * 0.25m;

        Assert.True(result.CanClaimPensionTaxCredit);
        Assert.Equal(expectedCredit, result.PensionTaxCreditClaimable);
        Assert.Contains("additional rate", result.PensionTaxCreditInfo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pension_Tax_Credit_Info_Contains_Gross_Contribution()
    {
        var data = MakeData(70000);
        data.ReliefAtSourcePensionContributions = 800;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("£1,000.00", result.PensionTaxCreditInfo); // Gross contribution
        Assert.Contains("Gross Contribution", result.PensionTaxCreditInfo);
    }

    [Fact]
    public void Pension_Tax_Credit_Info_Contains_Claim_Instructions()
    {
        var data = MakeData(70000);
        data.ReliefAtSourcePensionContributions = 800;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("HOW TO CLAIM", result.PensionTaxCreditInfo);
        Assert.Contains("Self Assessment", result.PensionTaxCreditInfo);
    }

    [Fact]
    public void Pension_Tax_Credit_Warns_About_Annual_Allowance()
    {
        // £50,000 net contribution would gross up to £62,500 which exceeds £60,000 allowance
        var data = MakeData(100000);
        data.ReliefAtSourcePensionContributions = 50000;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Annual Allowance", result.PensionTaxCreditInfo);
        Assert.Contains("WARNING", result.PensionTaxCreditInfo);
    }

    [Fact]
    public void Pension_Tax_Credit_Added_To_Summary_For_Higher_Rate()
    {
        var data = MakeData(70000);
        data.ReliefAtSourcePensionContributions = 800;
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("PENSION TAX CREDIT", result.Summary);
    }

    [Fact]
    public void Scottish_Higher_Rate_Taxpayer_Gets_Correct_Relief()
    {
        // Scottish higher rate is 42% not 40%
        var data = MakeData(70000, scottish: true);
        data.ReliefAtSourcePensionContributions = 800;
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Gross = 800 / 0.80 = 1000
        // Scottish intermediate is 21%, higher is 42%
        // At £70,000 taxable = £57,430, which is in Scottish higher rate band
        // Additional relief should be at higher rate - basic rate
        Assert.True(result.CanClaimPensionTaxCredit);
        Assert.True(result.PensionTaxCreditClaimable > 0);
    }

    // ═══════════ Dividend Tax ═══════════

    [Fact]
    public void No_Dividends_No_Dividend_Tax()
    {
        var data = MakeData(50000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalDividendIncome);
        Assert.Equal(0m, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Within_Allowance_No_Tax()
    {
        // 2024/25 dividend allowance = £500
        var data = MakeData(30000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 400 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(400m, result.TotalDividendIncome);
        Assert.Equal(0m, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_At_Allowance_No_Tax()
    {
        var data = MakeData(30000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 500 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Above_Allowance_Basic_Rate()
    {
        // Basic rate taxpayer (£30k salary), £1000 dividends
        // £500 allowance at 0%, remaining £500 at 8.75%
        var data = MakeData(30000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 1000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = 500m * 0.0875m;
        Assert.Equal(expected, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Higher_Rate()
    {
        // Higher rate taxpayer (£70k salary), dividends push into higher band
        // rUK basic band width = 37700, so basic rate up to £50,270 gross
        // At £70k, taxable non-savings = 70000 - 12570 = 57430
        // This is above the basic band (37700), so dividends are in higher rate
        var data = MakeData(70000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 1000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // All £500 above allowance taxed at higher rate 33.75%
        decimal expected = 500m * 0.3375m;
        Assert.Equal(expected, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Additional_Rate()
    {
        // Additional rate taxpayer (£160k salary)
        var data = MakeData(160000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 1000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = 500m * 0.3935m;
        Assert.Equal(expected, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Included_In_Gross_Income()
    {
        var data = MakeData(30000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 5000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(35000m, result.GrossIncome);
    }

    [Fact]
    public void Dividends_Included_In_Total_Tax_Due()
    {
        var data = MakeData(30000);
        var resultNoDivs = TaxCalculator.Calculate(data, Rules202425);

        var data2 = MakeData(30000);
        data2.DividendIncomes.Add(new DividendIncome { GrossDividend = 2000 });
        var resultWithDivs = TaxCalculator.Calculate(data2, Rules202425);

        Assert.True(resultWithDivs.TotalIncomeTaxDue > resultNoDivs.TotalIncomeTaxDue);
    }

    [Fact]
    public void Dividend_Tax_Paid_Included_In_Total_Tax_Paid()
    {
        var data = MakeData(30000, taxPaid: 3000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 5000, TaxPaid = 200 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(3200m, result.TotalTaxPaidViaPAYE);
    }

    [Fact]
    public void Dividend_Tax_Appears_In_Breakdown()
    {
        var data = MakeData(30000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 2000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains(result.TaxBreakdown, b => b.Label.Contains("Dividend"));
    }

    [Fact]
    public void Multiple_Dividend_Sources_Summed()
    {
        var data = MakeData(30000);
        data.DividendIncomes.Add(new DividendIncome { CompanyName = "Company A", GrossDividend = 1000 });
        data.DividendIncomes.Add(new DividendIncome { CompanyName = "Fund B", GrossDividend = 2000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(3000m, result.TotalDividendIncome);
    }

    [Fact]
    public void Dividend_Allowance_Higher_In_202324()
    {
        // 2023/24 allowance = £1,000 vs 2024/25 = £500
        var data = MakeData(30000, taxYear: "2023/24");
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 800 });
        var result = TaxCalculator.Calculate(data, Rules202324);

        // £800 is within £1,000 allowance
        Assert.Equal(0m, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_PA_Covers_Dividends_When_Low_Income()
    {
        // If total non-savings + savings < PA, remaining PA covers dividends
        var data = MakeData(5000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 5000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // PA = 12570, non-savings = 5000, remaining PA = 7570
        // Taxable dividends = max(0, 5000 - 7570) = 0
        Assert.Equal(0m, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Partially_Covered_By_PA()
    {
        // PA partly absorbs dividends
        var data = MakeData(10000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 5000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // PA = 12570, non-savings = 10000, remaining PA = 2570
        // Taxable dividends = 5000 - 2570 = 2430
        // First £500 at 0% (allowance), then £1930 at 8.75%
        decimal expected = 1930m * 0.0875m;
        Assert.Equal(expected, result.DividendTaxDue);
    }

    [Fact]
    public void Dividends_Affect_PA_Taper()
    {
        // Dividends count toward adjusted net income for PA taper
        var data = MakeData(95000);
        data.DividendIncomes.Add(new DividendIncome { GrossDividend = 20000 });
        var result = TaxCalculator.Calculate(data, Rules202425);

        // Gross income = 95000 + 20000 = 115000, above £100k taper threshold
        Assert.True(result.PersonalAllowanceUsed < 12570m);
    }
}
