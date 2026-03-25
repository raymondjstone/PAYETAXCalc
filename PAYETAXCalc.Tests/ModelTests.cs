using PAYETAXCalc.Helpers;
using PAYETAXCalc.Models;
using Xunit;

namespace PAYETAXCalc.Tests;

public class ModelTests
{
    // ═══════════ NotifyBase / INotifyPropertyChanged ═══════════

    [Fact]
    public void Employment_Raises_PropertyChanged()
    {
        var emp = new Employment();
        string? changedProp = null;
        emp.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        emp.EmployerName = "Test";

        Assert.Equal(nameof(Employment.EmployerName), changedProp);
    }

    [Fact]
    public void Employment_Does_Not_Raise_When_Value_Same()
    {
        var emp = new Employment { EmployerName = "Test" };
        bool raised = false;
        emp.PropertyChanged += (s, e) => raised = true;

        emp.EmployerName = "Test"; // same value

        Assert.False(raised);
    }

    [Fact]
    public void TaxYearData_Raises_PropertyChanged()
    {
        var data = new TaxYearData();
        string? changedProp = null;
        data.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        data.IsScottishTaxpayer = true;

        Assert.Equal(nameof(TaxYearData.IsScottishTaxpayer), changedProp);
    }

    // ═══════════ NaN handling ═══════════

    [Fact]
    public void Employment_NaN_GrossSalary_Becomes_Zero()
    {
        var emp = new Employment { GrossSalary = double.NaN };
        Assert.Equal(0, emp.GrossSalary);
    }

    [Fact]
    public void Employment_NaN_TaxPaid_Becomes_Zero()
    {
        var emp = new Employment { TaxPaid = double.NaN };
        Assert.Equal(0, emp.TaxPaid);
    }

    [Fact]
    public void Employment_NaN_NI_Becomes_Zero()
    {
        var emp = new Employment { NationalInsurancePaid = double.NaN };
        Assert.Equal(0, emp.NationalInsurancePaid);
    }

    [Fact]
    public void Employment_NaN_BIK_Becomes_Zero()
    {
        var emp = new Employment { BenefitsInKind = double.NaN };
        Assert.Equal(0, emp.BenefitsInKind);
    }

    [Fact]
    public void Employment_NaN_PensionContributions_Becomes_Zero()
    {
        var emp = new Employment { PensionContributions = double.NaN };
        Assert.Equal(0, emp.PensionContributions);
    }

    [Fact]
    public void Employment_NaN_WFH_Becomes_Zero()
    {
        var emp = new Employment { WorkFromHomeWeeks = double.NaN };
        Assert.Equal(0, emp.WorkFromHomeWeeks);
    }

    [Fact]
    public void Employment_NaN_BusinessMiles_Becomes_Zero()
    {
        var emp = new Employment { BusinessMiles = double.NaN };
        Assert.Equal(0, emp.BusinessMiles);
    }

    [Fact]
    public void Employment_NaN_Subscriptions_Becomes_Zero()
    {
        var emp = new Employment { ProfessionalSubscriptions = double.NaN };
        Assert.Equal(0, emp.ProfessionalSubscriptions);
    }

    [Fact]
    public void Employment_NaN_Uniform_Becomes_Zero()
    {
        var emp = new Employment { UniformAllowance = double.NaN };
        Assert.Equal(0, emp.UniformAllowance);
    }

    [Fact]
    public void Employment_NaN_OtherExpenses_Becomes_Zero()
    {
        var emp = new Employment { OtherExpenses = double.NaN };
        Assert.Equal(0, emp.OtherExpenses);
    }

    [Fact]
    public void SavingsIncome_NaN_InterestAmount_Becomes_Zero()
    {
        var sav = new SavingsIncome { InterestAmount = double.NaN };
        Assert.Equal(0, sav.InterestAmount);
    }

    [Fact]
    public void TaxYearData_NaN_GiftAid_Becomes_Zero()
    {
        var data = new TaxYearData { GiftAidDonations = double.NaN };
        Assert.Equal(0, data.GiftAidDonations);
    }

    // ═══════════ Default values ═══════════

    [Fact]
    public void Employment_Defaults()
    {
        var emp = new Employment();

        Assert.Equal("", emp.EmployerName);
        Assert.Equal("", emp.PayeReference);
        Assert.False(emp.IsPensionOrAnnuity);
        Assert.Equal(0, emp.GrossSalary);
        Assert.Equal(0, emp.TaxPaid);
        Assert.False(emp.EmploymentEnded);
    }

    [Fact]
    public void SavingsIncome_Defaults()
    {
        var sav = new SavingsIncome();

        Assert.Equal("", sav.ProviderName);
        Assert.Equal(0, sav.InterestAmount);
        Assert.False(sav.IsTaxFree);
    }

    [Fact]
    public void TaxYearData_Defaults()
    {
        var data = new TaxYearData();

        Assert.Equal("", data.TaxYear);
        Assert.False(data.IsScottishTaxpayer);
        Assert.False(data.ClaimMarriageAllowance);
        Assert.False(data.IsMarriageAllowanceReceiver);
        Assert.False(data.ClaimBlindPersonsAllowance);
        Assert.Equal(0, data.GiftAidDonations);
        Assert.Empty(data.Employments);
        Assert.Empty(data.SavingsIncomes);
    }

    [Fact]
    public void WindowSettings_Defaults()
    {
        var ws = new WindowSettings();

        Assert.Equal(-1, ws.X);
        Assert.Equal(-1, ws.Y);
        Assert.Equal(1100, ws.Width);
        Assert.Equal(800, ws.Height);
    }

    [Fact]
    public void TaxCalculationResult_Defaults()
    {
        var r = new TaxCalculationResult();

        Assert.Equal(0m, r.TotalEmploymentIncome);
        Assert.Equal(0m, r.TotalIncomeTaxDue);
        Assert.Equal("", r.Summary);
        Assert.NotNull(r.TaxBreakdown);
        Assert.Empty(r.TaxBreakdown);
    }

    // ═══════════ TaxBand defaults ═══════════

    [Fact]
    public void TaxBand_ExtendsWithGiftAid_Defaults_True()
    {
        var band = new TaxBand();
        Assert.True(band.ExtendsWithGiftAid);
    }
}
