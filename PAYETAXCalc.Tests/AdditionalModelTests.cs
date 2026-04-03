using System;
using PAYETAXCalc.Models;
using Xunit;

namespace PAYETAXCalc.Tests;

/// <summary>
/// Additional model tests covering CapitalGain, Employment company car fields,
/// TaxYearData new numeric fields, AppData, and TaxCalculationResult new fields.
/// </summary>
public class AdditionalModelTests
{
    // ═══════════ CapitalGain model ═══════════

    [Fact]
    public void CapitalGain_Defaults()
    {
        var cg = new CapitalGain();

        Assert.Equal("", cg.Description);
        Assert.Equal(0, cg.GainAmount);
        Assert.False(cg.IsResidentialProperty);
    }

    [Fact]
    public void CapitalGain_NaN_GainAmount_Becomes_Zero()
    {
        var cg = new CapitalGain { GainAmount = double.NaN };
        Assert.Equal(0, cg.GainAmount);
    }

    [Fact]
    public void CapitalGain_Raises_PropertyChanged_For_Description()
    {
        var cg = new CapitalGain();
        string? changedProp = null;
        cg.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        cg.Description = "Sale of shares";

        Assert.Equal(nameof(CapitalGain.Description), changedProp);
    }

    [Fact]
    public void CapitalGain_Raises_PropertyChanged_For_GainAmount()
    {
        var cg = new CapitalGain();
        string? changedProp = null;
        cg.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        cg.GainAmount = 5000;

        Assert.Equal(nameof(CapitalGain.GainAmount), changedProp);
    }

    [Fact]
    public void CapitalGain_Raises_PropertyChanged_For_IsResidentialProperty()
    {
        var cg = new CapitalGain();
        string? changedProp = null;
        cg.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        cg.IsResidentialProperty = true;

        Assert.Equal(nameof(CapitalGain.IsResidentialProperty), changedProp);
    }

    // ═══════════ Employment — company car NaN guards ═══════════

    [Fact]
    public void Employment_NaN_CarListPrice_Becomes_Zero()
    {
        var emp = new Employment { CarListPrice = double.NaN };
        Assert.Equal(0, emp.CarListPrice);
    }

    [Fact]
    public void Employment_NaN_CarFuelBenefit_Becomes_Zero()
    {
        var emp = new Employment { CarFuelBenefit = double.NaN };
        Assert.Equal(0, emp.CarFuelBenefit);
    }

    [Fact]
    public void Employment_Defaults_Include_Car_Fields()
    {
        var emp = new Employment();

        Assert.False(emp.HasCompanyCar);
        Assert.Equal(0, emp.CarListPrice);
        Assert.Equal(0, emp.CarCO2Emissions);
        Assert.False(emp.CarIsElectric);
        Assert.Equal(0, emp.CarFuelBenefit);
        Assert.False(emp.IsCombinedTaxAndNI);
    }

    [Fact]
    public void Employment_Raises_PropertyChanged_For_HasCompanyCar()
    {
        var emp = new Employment();
        string? changedProp = null;
        emp.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        emp.HasCompanyCar = true;

        Assert.Equal(nameof(Employment.HasCompanyCar), changedProp);
    }

    [Fact]
    public void Employment_Raises_PropertyChanged_For_IsCombinedTaxAndNI()
    {
        var emp = new Employment();
        string? changedProp = null;
        emp.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        emp.IsCombinedTaxAndNI = true;

        Assert.Equal(nameof(Employment.IsCombinedTaxAndNI), changedProp);
    }

    // ═══════════ TaxYearData — new field NaN guards ═══════════

    [Fact]
    public void TaxYearData_NaN_RentalIncome_Becomes_Zero()
    {
        var data = new TaxYearData { RentalIncome = double.NaN };
        Assert.Equal(0, data.RentalIncome);
    }

    [Fact]
    public void TaxYearData_NaN_RentalExpenses_Becomes_Zero()
    {
        var data = new TaxYearData { RentalExpenses = double.NaN };
        Assert.Equal(0, data.RentalExpenses);
    }

    [Fact]
    public void TaxYearData_NaN_MortgageInterest_Becomes_Zero()
    {
        var data = new TaxYearData { MortgageInterest = double.NaN };
        Assert.Equal(0, data.MortgageInterest);
    }

    [Fact]
    public void TaxYearData_NaN_TradingIncome_Becomes_Zero()
    {
        var data = new TaxYearData { TradingIncome = double.NaN };
        Assert.Equal(0, data.TradingIncome);
    }

    [Fact]
    public void TaxYearData_NaN_TradingExpenses_Becomes_Zero()
    {
        var data = new TaxYearData { TradingExpenses = double.NaN };
        Assert.Equal(0, data.TradingExpenses);
    }

    [Fact]
    public void TaxYearData_NaN_CapitalGainsLosses_Becomes_Zero()
    {
        var data = new TaxYearData { CapitalGainsLosses = double.NaN };
        Assert.Equal(0, data.CapitalGainsLosses);
    }

    [Fact]
    public void TaxYearData_NaN_ChildBenefitAmount_Becomes_Zero()
    {
        var data = new TaxYearData { ChildBenefitAmount = double.NaN };
        Assert.Equal(0, data.ChildBenefitAmount);
    }

    [Fact]
    public void TaxYearData_NaN_EisInvestment_Becomes_Zero()
    {
        var data = new TaxYearData { EisInvestment = double.NaN };
        Assert.Equal(0, data.EisInvestment);
    }

    [Fact]
    public void TaxYearData_NaN_SeisInvestment_Becomes_Zero()
    {
        var data = new TaxYearData { SeisInvestment = double.NaN };
        Assert.Equal(0, data.SeisInvestment);
    }

    [Fact]
    public void TaxYearData_NaN_VctInvestment_Becomes_Zero()
    {
        var data = new TaxYearData { VctInvestment = double.NaN };
        Assert.Equal(0, data.VctInvestment);
    }

    [Fact]
    public void TaxYearData_NaN_PriorYearTaxOwed_Becomes_Zero()
    {
        var data = new TaxYearData { PriorYearTaxOwed = double.NaN };
        Assert.Equal(0, data.PriorYearTaxOwed);
    }

    [Fact]
    public void TaxYearData_NaN_ReliefAtSourcePensionContributions_Becomes_Zero()
    {
        var data = new TaxYearData { ReliefAtSourcePensionContributions = double.NaN };
        Assert.Equal(0, data.ReliefAtSourcePensionContributions);
    }

    // ═══════════ TaxYearData — new field defaults ═══════════

    [Fact]
    public void TaxYearData_Extended_Defaults()
    {
        var data = new TaxYearData();

        Assert.Equal(0, data.RentalIncome);
        Assert.Equal(0, data.RentalExpenses);
        Assert.Equal(0, data.MortgageInterest);
        Assert.False(data.UsePropertyAllowance);
        Assert.Equal(0, data.TradingIncome);
        Assert.Equal(0, data.TradingExpenses);
        Assert.False(data.UseTradingAllowance);
        Assert.Equal(0, data.EisInvestment);
        Assert.Equal(0, data.SeisInvestment);
        Assert.Equal(0, data.VctInvestment);
        Assert.Equal(0, data.PriorYearTaxOwed);
        Assert.Equal("", data.TaxCode);
        Assert.False(data.HasStudentLoan);
        Assert.Equal(0, data.StudentLoanPlan);
        Assert.False(data.HasPostgraduateLoan);
        Assert.Equal(0, data.NumberOfChildren);
        Assert.Equal(0, data.ChildBenefitAmount);
        Assert.Equal(0, data.CapitalGainsLosses);
        Assert.Empty(data.CapitalGains);
    }

    [Fact]
    public void TaxYearData_TaxCode_Null_Becomes_Empty_String()
    {
        var data = new TaxYearData();
        data.TaxCode = null!;
        Assert.Equal("", data.TaxCode);
    }

    [Fact]
    public void TaxYearData_Raises_PropertyChanged_For_RentalIncome()
    {
        var data = new TaxYearData();
        string? changedProp = null;
        data.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        data.RentalIncome = 5000;

        Assert.Equal(nameof(TaxYearData.RentalIncome), changedProp);
    }

    [Fact]
    public void TaxYearData_Raises_PropertyChanged_For_TradingIncome()
    {
        var data = new TaxYearData();
        string? changedProp = null;
        data.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        data.TradingIncome = 3000;

        Assert.Equal(nameof(TaxYearData.TradingIncome), changedProp);
    }

    [Fact]
    public void TaxYearData_Raises_PropertyChanged_For_HasStudentLoan()
    {
        var data = new TaxYearData();
        string? changedProp = null;
        data.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        data.HasStudentLoan = true;

        Assert.Equal(nameof(TaxYearData.HasStudentLoan), changedProp);
    }

    [Fact]
    public void TaxYearData_Raises_PropertyChanged_For_NumberOfChildren()
    {
        var data = new TaxYearData();
        string? changedProp = null;
        data.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        data.NumberOfChildren = 2;

        Assert.Equal(nameof(TaxYearData.NumberOfChildren), changedProp);
    }

    // ═══════════ AppData defaults ═══════════

    [Fact]
    public void AppData_Defaults()
    {
        var appData = new AppData();

        Assert.NotNull(appData.Window);
        Assert.NotNull(appData.TaxYears);
        Assert.Empty(appData.TaxYears);
        Assert.False(appData.BuyMeCoffeeClicked);
        Assert.Null(appData.LastCoffeePrompt);
        Assert.Null(appData.FirstAppUse);
    }

    [Fact]
    public void AppData_BuyMeCoffeeClicked_Can_Be_Set()
    {
        var appData = new AppData { BuyMeCoffeeClicked = true };
        Assert.True(appData.BuyMeCoffeeClicked);
    }

    [Fact]
    public void AppData_LastCoffeePrompt_Can_Be_Set()
    {
        var now = DateTimeOffset.UtcNow;
        var appData = new AppData { LastCoffeePrompt = now };
        Assert.Equal(now, appData.LastCoffeePrompt);
    }

    [Fact]
    public void AppData_FirstAppUse_Can_Be_Set()
    {
        var now = DateTimeOffset.UtcNow;
        var appData = new AppData { FirstAppUse = now };
        Assert.Equal(now, appData.FirstAppUse);
    }

    // ═══════════ TaxCalculationResult — new field defaults ═══════════

    [Fact]
    public void TaxCalculationResult_Extended_Defaults()
    {
        var r = new TaxCalculationResult();

        Assert.Equal(0m, r.RentalProfit);
        Assert.Equal(0m, r.RentalTaxableIncome);
        Assert.Equal("", r.RentalInfo);
        Assert.Equal(0m, r.TradingTaxableIncome);
        Assert.Equal("", r.TradingInfo);
        Assert.Equal(0m, r.CapitalGainsTax);
        Assert.Equal("", r.CapitalGainsInfo);
        Assert.Equal(0m, r.TotalCapitalGains);
        Assert.Equal(0m, r.StudentLoanRepayment);
        Assert.Equal("", r.StudentLoanInfo);
        Assert.Equal(0m, r.ChildBenefitCharge);
        Assert.Equal("", r.ChildBenefitInfo);
        Assert.Equal(0m, r.PensionAnnualAllowanceCharge);
        Assert.Equal("", r.PensionAACInfo);
        Assert.Equal(0m, r.TotalInvestmentRelief);
        Assert.Equal("", r.InvestmentReliefInfo);
        Assert.Equal(0m, r.TotalCompanyCarBenefit);
        Assert.Equal("", r.CompanyCarInfo);
        Assert.Equal(0m, r.PriorYearTaxCollected);
        Assert.Equal(0m, r.MortgageInterestRelief);
        Assert.Equal("", r.TaxCodeValidation);
        Assert.False(r.TaxCodeHasWarning);
        Assert.False(r.HasSeparateNIFigures);
        Assert.Equal("", r.NIEstimationInfo);
    }

    [Fact]
    public void TaxCalculationResult_Raises_PropertyChanged_For_CapitalGainsTax()
    {
        var r = new TaxCalculationResult();
        string? changedProp = null;
        r.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        r.CapitalGainsTax = 1500m;

        Assert.Equal(nameof(TaxCalculationResult.CapitalGainsTax), changedProp);
    }

    [Fact]
    public void TaxCalculationResult_Raises_PropertyChanged_For_StudentLoanRepayment()
    {
        var r = new TaxCalculationResult();
        string? changedProp = null;
        r.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        r.StudentLoanRepayment = 500m;

        Assert.Equal(nameof(TaxCalculationResult.StudentLoanRepayment), changedProp);
    }
}
