using PAYETAXCalc.Models;
using PAYETAXCalc.Services;
using Xunit;

namespace PAYETAXCalc.Tests;

/// <summary>
/// Tests for advanced TaxCalculator features not covered by TaxCalculatorTests:
/// rental income, trading income, capital gains, student loans, HICBC,
/// investment reliefs, pension annual allowance charge, company car,
/// tax code validation, combined tax+NI, prior year tax.
/// </summary>
public class TaxCalculatorAdvancedTests
{
    private static readonly TaxYearRules Rules202425 = TaxRulesProvider.GetRules("2024/25")!;

    private static TaxYearData MakeData(double salary, bool scottish = false, double taxPaid = 0, string taxYear = "2024/25")
    {
        var data = new TaxYearData { TaxYear = taxYear, IsScottishTaxpayer = scottish };
        data.Employments.Add(new Employment { GrossSalary = salary, TaxPaid = taxPaid });
        return data;
    }

    // ═══════════ Rental Income ═══════════

    [Fact]
    public void Rental_Within_Property_Allowance_Zero_Tax()
    {
        var data = MakeData(0);
        data.RentalIncome = 800;
        data.UsePropertyAllowance = true;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.RentalProfit);
        Assert.Equal(0m, result.RentalTaxableIncome);
    }

    [Fact]
    public void Rental_Above_Property_Allowance_Taxable_Reduced()
    {
        var data = MakeData(0);
        data.RentalIncome = 5000;
        data.UsePropertyAllowance = true;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // 5000 - 1000 property allowance = 4000 taxable
        Assert.Equal(4000m, result.RentalTaxableIncome);
    }

    [Fact]
    public void Rental_Expenses_Deducted_When_Not_Using_Allowance()
    {
        var data = MakeData(0);
        data.RentalIncome = 5000;
        data.RentalExpenses = 2000;
        data.UsePropertyAllowance = false;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(3000m, result.RentalTaxableIncome);
    }

    [Fact]
    public void Rental_Income_Adds_To_Tax_Liability()
    {
        var data = MakeData(30000);
        data.RentalIncome = 5000;
        data.RentalExpenses = 0;
        data.UsePropertyAllowance = false;

        var resultWithRental = TaxCalculator.Calculate(data, Rules202425);
        var resultNoRental = TaxCalculator.Calculate(MakeData(30000), Rules202425);

        Assert.True(resultWithRental.TotalIncomeTaxDue > resultNoRental.TotalIncomeTaxDue);
    }

    [Fact]
    public void Mortgage_Interest_Relief_Applied_As_20_Percent_Credit()
    {
        var data = MakeData(20000);
        data.RentalIncome = 5000;
        data.MortgageInterest = 2000;
        data.UsePropertyAllowance = false;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // Relief = 2000 * 20% = 400
        Assert.Equal(400m, result.MortgageInterestRelief);
    }

    [Fact]
    public void Rental_Info_Populated_When_Rental_Income_Present()
    {
        var data = MakeData(0);
        data.RentalIncome = 3000;
        data.UsePropertyAllowance = false;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.False(string.IsNullOrEmpty(result.RentalInfo));
        Assert.Contains("rental income", result.RentalInfo, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════ Trading Income ═══════════

    [Fact]
    public void Trading_Within_Trading_Allowance_Zero_Taxable()
    {
        var data = MakeData(0);
        data.TradingIncome = 800;
        data.UseTradingAllowance = true;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TradingTaxableIncome);
    }

    [Fact]
    public void Trading_Above_Trading_Allowance_Reduces_Taxable()
    {
        var data = MakeData(0);
        data.TradingIncome = 5000;
        data.UseTradingAllowance = true;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // 5000 - 1000 trading allowance = 4000
        Assert.Equal(4000m, result.TradingTaxableIncome);
    }

    [Fact]
    public void Trading_Expenses_Deducted_When_Not_Using_Allowance()
    {
        var data = MakeData(0);
        data.TradingIncome = 5000;
        data.TradingExpenses = 2000;
        data.UseTradingAllowance = false;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(3000m, result.TradingTaxableIncome);
    }

    [Fact]
    public void Trading_Income_Adds_To_Tax_Liability()
    {
        var data = MakeData(30000);
        data.TradingIncome = 5000;
        data.UseTradingAllowance = false;

        var resultWithTrading = TaxCalculator.Calculate(data, Rules202425);
        var resultNoTrading = TaxCalculator.Calculate(MakeData(30000), Rules202425);

        Assert.True(resultWithTrading.TotalIncomeTaxDue > resultNoTrading.TotalIncomeTaxDue);
    }

    // ═══════════ Capital Gains Tax ═══════════

    [Fact]
    public void No_Capital_Gains_No_CGT()
    {
        var data = MakeData(30000);

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalCapitalGains);
        Assert.Equal(0m, result.CapitalGainsTax);
    }

    [Fact]
    public void Gains_Within_AEA_No_CGT_Due()
    {
        // 2024/25 AEA = £3,000
        var data = MakeData(30000);
        data.CapitalGains.Add(new CapitalGain { Description = "Shares", GainAmount = 2000 });

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(2000m, result.TotalCapitalGains);
        Assert.Equal(0m, result.CapitalGainsTax);
        Assert.Contains("No CGT due", result.CapitalGainsInfo);
    }

    [Fact]
    public void Basic_Rate_Taxpayer_Asset_Gain_At_Basic_Rate()
    {
        // Salary = 20000, taxableNonSavings = 7430, basicBandRemaining = 30270
        var data = MakeData(20000);
        data.CapitalGains.Add(new CapitalGain { Description = "Shares", GainAmount = 10000, IsResidentialProperty = false });

        var result = TaxCalculator.Calculate(data, Rules202425);

        // netGains = 10000, AEA = 3000, taxableGains = 7000
        // All in basic band at 18%
        Assert.Equal(7000m * 0.18m, result.CapitalGainsTax);
    }

    [Fact]
    public void Higher_Rate_Taxpayer_Asset_Gain_At_Higher_Rate()
    {
        // Salary = 70000, taxableNonSavings = 57430, no basic band remaining
        var data = MakeData(70000);
        data.CapitalGains.Add(new CapitalGain { Description = "Shares", GainAmount = 10000, IsResidentialProperty = false });

        var result = TaxCalculator.Calculate(data, Rules202425);

        // AEA = 3000, taxableGains = 7000, all at 24%
        Assert.Equal(7000m * 0.24m, result.CapitalGainsTax);
    }

    [Fact]
    public void Losses_Reduce_Net_Gains()
    {
        var data = MakeData(20000);
        data.CapitalGains.Add(new CapitalGain { Description = "Shares", GainAmount = 15000 });
        data.CapitalGainsLosses = 3000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // netGains = 15000 - 3000 = 12000, AEA = 3000, taxableGains = 9000
        Assert.Equal(12000m, result.TotalCapitalGains);
        Assert.Equal(9000m * 0.18m, result.CapitalGainsTax);
    }

    [Fact]
    public void Capital_Gains_Tax_Appears_In_Summary()
    {
        var data = MakeData(70000);
        data.CapitalGains.Add(new CapitalGain { Description = "Property", GainAmount = 30000, IsResidentialProperty = true });

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.CapitalGainsTax > 0);
        Assert.Contains("Capital Gains Tax", result.Summary);
    }

    // ═══════════ Student Loan Repayments ═══════════

    [Fact]
    public void No_Student_Loan_Flag_No_Repayment()
    {
        var data = MakeData(40000);
        data.HasStudentLoan = false;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.StudentLoanRepayment);
    }

    [Fact]
    public void Student_Loan_Plan1_Below_Threshold_No_Repayment()
    {
        var data = MakeData(20000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 1; // threshold 22015

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.StudentLoanRepayment);
    }

    [Fact]
    public void Student_Loan_Plan1_Above_Threshold()
    {
        var data = MakeData(30000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 1; // threshold 22015

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round((30000m - 22015m) * 0.09m, 2);
        Assert.Equal(expected, result.StudentLoanRepayment);
    }

    [Fact]
    public void Student_Loan_Plan2_Above_Threshold()
    {
        var data = MakeData(35000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 2; // threshold 27295

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round((35000m - 27295m) * 0.09m, 2);
        Assert.Equal(expected, result.StudentLoanRepayment);
    }

    [Fact]
    public void Student_Loan_Plan4_Above_Threshold()
    {
        var data = MakeData(35000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 4; // threshold 27660

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round((35000m - 27660m) * 0.09m, 2);
        Assert.Equal(expected, result.StudentLoanRepayment);
    }

    [Fact]
    public void Student_Loan_Plan5_Above_Threshold()
    {
        var data = MakeData(35000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 5; // threshold 25000

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round((35000m - 25000m) * 0.09m, 2);
        Assert.Equal(expected, result.StudentLoanRepayment);
    }

    [Fact]
    public void Postgraduate_Loan_Only_Above_Threshold()
    {
        var data = MakeData(25000);
        data.HasPostgraduateLoan = true; // threshold 21000, rate 6%

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal expected = Math.Round((25000m - 21000m) * 0.06m, 2);
        Assert.Equal(expected, result.StudentLoanRepayment);
    }

    [Fact]
    public void Plan2_And_Postgraduate_Loan_Combined()
    {
        var data = MakeData(35000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 2;
        data.HasPostgraduateLoan = true;

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal planRepay = Math.Round((35000m - 27295m) * 0.09m, 2);
        decimal pgRepay = Math.Round((35000m - 21000m) * 0.06m, 2);
        Assert.Equal(Math.Round(planRepay + pgRepay, 2), result.StudentLoanRepayment);
    }

    [Fact]
    public void Student_Loan_Repayment_Appears_In_Summary()
    {
        var data = MakeData(35000);
        data.HasStudentLoan = true;
        data.StudentLoanPlan = 2;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Student Loan", result.Summary);
    }

    // ═══════════ High Income Child Benefit Charge ═══════════

    [Fact]
    public void No_Children_No_HICBC()
    {
        var data = MakeData(80000);
        data.NumberOfChildren = 0;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.ChildBenefitCharge);
    }

    [Fact]
    public void Income_Below_HICBC_Threshold_No_Charge()
    {
        // 2024/25 threshold = £60,000
        var data = MakeData(50000);
        data.NumberOfChildren = 1;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.ChildBenefitCharge);
    }

    [Fact]
    public void HICBC_Partial_Charge_One_Child_Calculated_From_Weekly_Rates()
    {
        // 2024/25: threshold 60000, full charge 80000
        // Salary = 70000 → excessIncome = 10000, incomeRange = 20000, chargePercent = 50%
        var data = MakeData(70000);
        data.NumberOfChildren = 1;
        // ChildBenefitAmount = 0, so calculated from weekly rates: 25.60 * 52 = 1331.20

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal annualBenefit = 25.60m * 52m;
        decimal expected = Math.Round(annualBenefit * 0.5m, 2);
        Assert.Equal(expected, result.ChildBenefitCharge);
    }

    [Fact]
    public void HICBC_Full_Charge_At_Full_Charge_Threshold()
    {
        // income >= 80000 = full clawback
        var data = MakeData(90000);
        data.NumberOfChildren = 1;
        data.ChildBenefitAmount = 1500; // custom amount

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(1500m, result.ChildBenefitCharge);
    }

    [Fact]
    public void HICBC_Two_Children_Calculated_From_Weekly_Rates()
    {
        // 2024/25 full charge with 2 children
        var data = MakeData(90000);
        data.NumberOfChildren = 2;
        // 2 children: (25.60 + 16.95) * 52 = 2212.60

        var result = TaxCalculator.Calculate(data, Rules202425);

        decimal annualBenefit = (25.60m + 16.95m) * 52m;
        Assert.Equal(Math.Round(annualBenefit, 2), result.ChildBenefitCharge);
    }

    [Fact]
    public void HICBC_Charge_Appears_In_Summary()
    {
        var data = MakeData(70000);
        data.NumberOfChildren = 1;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.ChildBenefitCharge > 0);
        Assert.Contains("Child Benefit Charge", result.Summary);
    }

    // ═══════════ Investment Reliefs (EIS / SEIS / VCT) ═══════════

    [Fact]
    public void No_Investments_No_Relief()
    {
        var data = MakeData(50000);

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalInvestmentRelief);
    }

    [Fact]
    public void EIS_Relief_Calculated_At_30_Percent()
    {
        var data = MakeData(50000);
        data.EisInvestment = 5000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(1500m, result.TotalInvestmentRelief);
    }

    [Fact]
    public void SEIS_Relief_Calculated_At_50_Percent()
    {
        var data = MakeData(50000);
        data.SeisInvestment = 2000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(1000m, result.TotalInvestmentRelief);
    }

    [Fact]
    public void VCT_Relief_Calculated_At_30_Percent()
    {
        var data = MakeData(50000);
        data.VctInvestment = 3000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(900m, result.TotalInvestmentRelief);
    }

    [Fact]
    public void All_Three_Investment_Reliefs_Combined()
    {
        var data = MakeData(50000);
        data.EisInvestment = 5000;
        data.SeisInvestment = 2000;
        data.VctInvestment = 3000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // 1500 + 1000 + 900 = 3400
        Assert.Equal(3400m, result.TotalInvestmentRelief);
    }

    [Fact]
    public void Investment_Relief_Reduces_Income_Tax_Due()
    {
        var baseline = MakeData(50000);
        var withRelief = MakeData(50000);
        withRelief.EisInvestment = 5000;

        var resultBaseline = TaxCalculator.Calculate(baseline, Rules202425);
        var resultWithRelief = TaxCalculator.Calculate(withRelief, Rules202425);

        Assert.Equal(resultBaseline.TotalIncomeTaxDue - 1500m, resultWithRelief.TotalIncomeTaxDue);
    }

    [Fact]
    public void Investment_Relief_Appears_In_Summary()
    {
        var data = MakeData(50000);
        data.EisInvestment = 5000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Investment Relief", result.Summary);
    }

    [Fact]
    public void InvestmentReliefInfo_Populated_With_EIS_SEIS_VCT()
    {
        var data = MakeData(50000);
        data.EisInvestment = 5000;
        data.SeisInvestment = 2000;
        data.VctInvestment = 3000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("EIS", result.InvestmentReliefInfo);
        Assert.Contains("SEIS", result.InvestmentReliefInfo);
        Assert.Contains("VCT", result.InvestmentReliefInfo);
        Assert.Contains("Total relief", result.InvestmentReliefInfo);
    }

    // ═══════════ Pension Annual Allowance Charge ═══════════

    [Fact]
    public void Pension_Contributions_Below_Allowance_No_Charge()
    {
        var data = MakeData(70000);
        data.Employments[0].PensionContributions = 40000; // < 60000 allowance

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.PensionAnnualAllowanceCharge);
    }

    [Fact]
    public void Pension_Contributions_Above_Allowance_Charge_At_Marginal_Rate()
    {
        // Salary = 120000, pension = 65000 → excess = 5000 over £60,000 allowance
        // After pension deduction: nonSavings = 55000, taxableNonSavings = 42430 → higher rate band
        var data = MakeData(120000);
        data.Employments[0].PensionContributions = 65000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // excess = 5000, marginal rate = 40% (taxableNonSavings 42430 > basicBandWidth 37700)
        Assert.Equal(2000m, result.PensionAnnualAllowanceCharge);
    }

    [Fact]
    public void Pension_Annual_Allowance_Charge_Appears_In_Summary()
    {
        var data = MakeData(70000);
        data.Employments[0].PensionContributions = 70000;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Pension Annual Allowance Charge", result.Summary);
    }

    // ═══════════ Company Car BIK ═══════════

    [Fact]
    public void Company_Car_Adds_BIK_To_Income()
    {
        var data = MakeData(50000);
        data.Employments[0].HasCompanyCar = true;
        data.Employments[0].CarListPrice = 30000;
        data.Employments[0].CarCO2Emissions = 110; // bikPercent = 28 for 2024/25
        data.Employments[0].CarIsElectric = false;

        var resultWithCar = TaxCalculator.Calculate(data, Rules202425);
        var resultNoCar = TaxCalculator.Calculate(MakeData(50000), Rules202425);

        // car benefit = 30000 * 28% = 8400 → higher BIK → higher tax
        Assert.Equal(8400m, resultWithCar.TotalCompanyCarBenefit);
        Assert.True(resultWithCar.TotalIncomeTaxDue > resultNoCar.TotalIncomeTaxDue);
    }

    [Fact]
    public void Electric_Car_Uses_Low_BIK_Percentage()
    {
        // Electric car 2024/25 = 2%
        var data = MakeData(50000);
        data.Employments[0].HasCompanyCar = true;
        data.Employments[0].CarListPrice = 40000;
        data.Employments[0].CarCO2Emissions = 0;
        data.Employments[0].CarIsElectric = true;

        var result = TaxCalculator.Calculate(data, Rules202425);

        // 40000 * 2% = 800
        Assert.Equal(800m, result.TotalCompanyCarBenefit);
    }

    [Fact]
    public void Car_Fuel_Benefit_Added_When_Fuel_Benefit_Set()
    {
        var data = MakeData(50000);
        data.Employments[0].HasCompanyCar = true;
        data.Employments[0].CarListPrice = 30000;
        data.Employments[0].CarCO2Emissions = 110; // bikPercent = 28
        data.Employments[0].CarIsElectric = false;
        data.Employments[0].CarFuelBenefit = 1; // any positive value enables fuel benefit

        var result = TaxCalculator.Calculate(data, Rules202425);

        // carBenefit = 8400, fuelBenefit = 27800 * 28/100 = 7784
        Assert.Equal(8400m + 7784m, result.TotalCompanyCarBenefit);
    }

    [Fact]
    public void Pension_Annuity_Employment_No_Car_Benefit()
    {
        var data = MakeData(30000);
        data.Employments[0].IsPensionOrAnnuity = true;
        data.Employments[0].HasCompanyCar = true;
        data.Employments[0].CarListPrice = 30000;
        data.Employments[0].CarCO2Emissions = 110;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.TotalCompanyCarBenefit);
    }

    // ═══════════ Tax Code Validation ═══════════

    [Fact]
    public void Empty_Tax_Code_No_Validation()
    {
        var data = MakeData(30000);
        data.TaxCode = "";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal("", result.TaxCodeValidation);
        Assert.False(result.TaxCodeHasWarning);
    }

    [Fact]
    public void BR_Tax_Code_Identified()
    {
        var data = MakeData(30000);
        data.TaxCode = "BR";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("BR", result.TaxCodeValidation);
        Assert.Contains("basic rate", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void D0_Tax_Code_Identified()
    {
        var data = MakeData(30000);
        data.TaxCode = "D0";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("D0", result.TaxCodeValidation);
        Assert.Contains("higher rate", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void D1_Tax_Code_Identified()
    {
        var data = MakeData(30000);
        data.TaxCode = "D1";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("D1", result.TaxCodeValidation);
        Assert.Contains("additional rate", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NT_Tax_Code_No_Warning()
    {
        var data = MakeData(30000);
        data.TaxCode = "NT";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("NT", result.TaxCodeValidation);
        Assert.False(result.TaxCodeHasWarning);
    }

    [Fact]
    public void Zero_T_Tax_Code_Identified()
    {
        var data = MakeData(30000);
        data.TaxCode = "0T";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("0T", result.TaxCodeValidation);
        Assert.Contains("No personal allowance", result.TaxCodeValidation);
    }

    [Fact]
    public void K_Tax_Code_Shows_Negative_Allowance()
    {
        var data = MakeData(30000);
        data.TaxCode = "K100";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("K code", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("£1,000", result.TaxCodeValidation);
    }

    [Fact]
    public void Standard_Tax_Code_Matching_PA_No_Warning()
    {
        // 1257L implies PA = 12570 which matches default calculated PA
        var data = MakeData(30000);
        data.TaxCode = "1257L";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("1257L", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("matches", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.TaxCodeHasWarning);
    }

    [Fact]
    public void Tax_Code_Less_Allowance_Than_Expected_Warns()
    {
        // 1000L implies PA = 10000, calculated PA = 12570 → diff > 10 → warning
        var data = MakeData(30000);
        data.TaxCode = "1000L";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxCodeHasWarning);
        Assert.Contains("LESS allowance", result.TaxCodeValidation);
    }

    [Fact]
    public void Tax_Code_More_Allowance_Than_Expected_Warns()
    {
        // 1500L implies PA = 15000, calculated PA = 12570 → diff < -10 → warning
        var data = MakeData(30000);
        data.TaxCode = "1500L";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxCodeHasWarning);
        Assert.Contains("MORE allowance", result.TaxCodeValidation);
    }

    [Fact]
    public void Scottish_Prefix_Without_Scottish_Taxpayer_Warns()
    {
        var data = MakeData(30000, scottish: false);
        data.TaxCode = "S1257L";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxCodeHasWarning);
        Assert.Contains("WARNING", result.TaxCodeValidation);
    }

    [Fact]
    public void Scottish_Taxpayer_Without_S_Prefix_Warns()
    {
        var data = MakeData(30000, scottish: true);
        data.TaxCode = "1257L";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxCodeHasWarning);
        Assert.Contains("WARNING", result.TaxCodeValidation);
    }

    [Fact]
    public void Welsh_Prefix_C_Identified()
    {
        var data = MakeData(30000);
        data.TaxCode = "C1257L";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Welsh", result.TaxCodeValidation);
    }

    [Fact]
    public void Unrecognised_Tax_Code_Sets_Warning()
    {
        var data = MakeData(30000);
        data.TaxCode = "XXXYYY";

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.True(result.TaxCodeHasWarning);
        Assert.Contains("Unrecognised", result.TaxCodeValidation);
    }

    [Fact]
    public void Tax_Code_Suffix_M_Marriage_Receiving_Described()
    {
        var data = MakeData(30000);
        data.TaxCode = "1383M"; // receiving 10% of partner's allowance

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Marriage Allowance", result.TaxCodeValidation);
        Assert.Contains("receiving", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tax_Code_Suffix_N_Marriage_Transferred_Described()
    {
        var data = MakeData(30000);
        data.TaxCode = "1131N"; // transferred 10% to partner

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Marriage Allowance", result.TaxCodeValidation);
        Assert.Contains("transferred", result.TaxCodeValidation, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════ Combined Tax + NI ═══════════

    [Fact]
    public void Combined_TaxAndNI_Non_Pension_Estimates_NI()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            EmployerName = "Test Corp",
            GrossSalary = 30000,
            TaxPaid = 5000, // combined figure
            IsCombinedTaxAndNI = true,
            IsPensionOrAnnuity = false,
        });

        var result = TaxCalculator.Calculate(data, Rules202425);

        // NI estimated from salary, splits the combined figure
        Assert.False(string.IsNullOrEmpty(result.NIEstimationInfo));
        Assert.Contains("Combined", result.NIEstimationInfo);
        // estimated NI = (30000 - 12570) * 8% = 17430 * 0.08 = 1394.40
        Assert.Contains("£1,394.40", result.NIEstimationInfo);
    }

    [Fact]
    public void Combined_TaxAndNI_Pension_All_Treated_As_Tax()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            EmployerName = "State Pension",
            GrossSalary = 10000,
            TaxPaid = 500,
            IsCombinedTaxAndNI = true,
            IsPensionOrAnnuity = true,
        });

        var result = TaxCalculator.Calculate(data, Rules202425);

        // Pension has no NI so combined = all tax
        Assert.Equal(500m, result.TotalTaxPaidViaPAYE);
        Assert.Equal(0m, result.TotalNIPaid);
    }

    [Fact]
    public void Combined_TaxAndNI_Summary_Notes_Estimation()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            GrossSalary = 30000,
            TaxPaid = 5000,
            IsCombinedTaxAndNI = true,
        });

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Estimated from combined", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════ Prior Year Tax ═══════════

    [Fact]
    public void Prior_Year_Tax_Owed_Is_Collected_From_Effective_Payment()
    {
        // Without prior year: tax 3486, paid 5000 → refund 1514
        // With prior year 500: effectivePaid = 5000 - 500 = 4500 → refund 1014
        var data = MakeData(30000, taxPaid: 5000);
        data.PriorYearTaxOwed = 500;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(500m, result.PriorYearTaxCollected);
        // Less refund than without prior year
        Assert.Equal(result.TotalIncomeTaxDue - (5000m - 500m), result.TaxOverUnderPayment);
    }

    [Fact]
    public void Prior_Year_Tax_Zero_No_Effect()
    {
        var dataA = MakeData(30000, taxPaid: 5000);
        var dataB = MakeData(30000, taxPaid: 5000);
        dataB.PriorYearTaxOwed = 0;

        var resultA = TaxCalculator.Calculate(dataA, Rules202425);
        var resultB = TaxCalculator.Calculate(dataB, Rules202425);

        Assert.Equal(resultA.TaxOverUnderPayment, resultB.TaxOverUnderPayment);
        Assert.Equal(0m, resultB.PriorYearTaxCollected);
    }

    [Fact]
    public void Prior_Year_Tax_Appears_In_Summary()
    {
        var data = MakeData(30000, taxPaid: 5000);
        data.PriorYearTaxOwed = 500;

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("Prior year tax", result.Summary);
        Assert.Contains("£500.00", result.Summary);
    }

    // ═══════════ Summary content ═══════════

    [Fact]
    public void Summary_Refund_Message_When_Overpaid()
    {
        // Pay more than needed → refund message
        var data = MakeData(30000, taxPaid: 10000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("refund", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Summary_Owe_Message_When_Underpaid()
    {
        // Pay less than needed → owe message
        var data = MakeData(50000, taxPaid: 1000);
        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("owe", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Summary_NI_Less_Than_Expected_Noted()
    {
        // Pay less NI than expected (by more than £1)
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            GrossSalary = 30000,
            TaxPaid = 3000,
            NationalInsurancePaid = 100, // much less than expected ~1394
        });

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("NI", result.Summary);
        Assert.Contains("less than expected", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Summary_Expenses_Total_Noted_When_Present()
    {
        var data = MakeData(30000);
        data.Employments[0].WorkFromHomeWeeks = 10; // 10 * £6 = £60 expenses

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Contains("expenses", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════ Postgraduate loan below threshold ═══════════

    [Fact]
    public void Postgraduate_Loan_Below_Threshold_No_Repayment()
    {
        var data = MakeData(15000);
        data.HasPostgraduateLoan = true; // threshold 21000

        var result = TaxCalculator.Calculate(data, Rules202425);

        Assert.Equal(0m, result.StudentLoanRepayment);
        Assert.Contains("No repayment due", result.StudentLoanInfo);
    }
}
