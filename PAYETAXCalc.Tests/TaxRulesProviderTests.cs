using PAYETAXCalc.Services;
using Xunit;

namespace PAYETAXCalc.Tests;

public class TaxRulesProviderTests
{
    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void GetRules_Returns_Rules_For_Known_Years(string year)
    {
        var rules = TaxRulesProvider.GetRules(year);

        Assert.NotNull(rules);
        Assert.Equal(year, rules.TaxYear);
    }

    [Fact]
    public void GetRules_Returns_Null_For_Unknown_Year()
    {
        Assert.Null(TaxRulesProvider.GetRules("2020/21"));
    }

    [Fact]
    public void GetAvailableTaxYears_Returns_All_Three()
    {
        var years = TaxRulesProvider.GetAvailableTaxYears();

        Assert.Equal(3, years.Count);
        Assert.Contains("2023/24", years);
        Assert.Contains("2024/25", years);
        Assert.Contains("2025/26", years);
    }

    [Fact]
    public void GetAvailableTaxYears_Sorted()
    {
        var years = TaxRulesProvider.GetAvailableTaxYears();

        Assert.Equal("2023/24", years[0]);
        Assert.Equal("2024/25", years[1]);
        Assert.Equal("2025/26", years[2]);
    }

    [Fact]
    public void GetCurrentTaxYear_Returns_Valid_Year()
    {
        var year = TaxRulesProvider.GetCurrentTaxYear();
        var available = TaxRulesProvider.GetAvailableTaxYears();

        Assert.Contains(year, available);
    }

    // ═══════════ Personal Allowance ═══════════

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void PA_Is_12570_For_All_Years(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(12570m, rules.PersonalAllowance);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void PA_Taper_Threshold_Is_100k(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(100000m, rules.PersonalAllowanceTaperThreshold);
    }

    // ═══════════ rUK Bands ═══════════

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void RUK_Has_Three_Bands(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(3, rules.RestOfUKBands.Count);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void RUK_Basic_Rate_Is_20_Percent(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.20m, rules.RestOfUKBands[0].Rate);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void RUK_Top_Band_Has_No_Upper_Limit(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        var topBand = rules.RestOfUKBands[^1];
        Assert.Equal(0m, topBand.UpperGrossThreshold);
    }

    // ═══════════ Scottish Bands ═══════════

    [Fact]
    public void Scottish_202324_Has_Five_Bands()
    {
        var rules = TaxRulesProvider.GetRules("2023/24")!;
        Assert.Equal(5, rules.ScottishBands.Count);
    }

    [Theory]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Scottish_202425_And_202526_Have_Six_Bands(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(6, rules.ScottishBands.Count);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Scottish_Starter_Rate_Is_19_Percent(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.19m, rules.ScottishBands[0].Rate);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Scottish_Starter_Band_Does_Not_Extend_With_Gift_Aid(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.False(rules.ScottishBands[0].ExtendsWithGiftAid);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Scottish_Top_Band_Has_No_Upper_Limit(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0m, rules.ScottishBands[^1].UpperGrossThreshold);
    }

    // ═══════════ NIC rates ═══════════

    [Fact]
    public void NIC_Main_Rate_Changed_From_202324_To_202425()
    {
        var r2324 = TaxRulesProvider.GetRules("2023/24")!;
        var r2425 = TaxRulesProvider.GetRules("2024/25")!;

        Assert.Equal(0.12m, r2324.NICMainRate);
        Assert.Equal(0.08m, r2425.NICMainRate);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void NIC_Upper_Rate_Is_2_Percent(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.02m, rules.NICUpperRate);
    }

    // ═══════════ Other allowances ═══════════

    [Theory]
    [InlineData("2023/24", 2870)]
    [InlineData("2024/25", 3070)]
    [InlineData("2025/26", 3130)]
    public void Blind_Persons_Allowance_Correct_Per_Year(string year, int expected)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal((decimal)expected, rules.BlindPersonsAllowance);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void WFH_Rate_Is_6_Per_Week(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(6m, rules.WorkFromHomeWeeklyRate);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Mileage_Rates_Correct(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.45m, rules.MileageRateFirst10000);
        Assert.Equal(0.25m, rules.MileageRateOver10000);
    }

    [Fact]
    public void GetRulesSummary_Returns_NonEmpty_String()
    {
        var rules = TaxRulesProvider.GetRules("2024/25")!;
        var summary = TaxRulesProvider.GetRulesSummary(rules);

        Assert.False(string.IsNullOrWhiteSpace(summary));
        Assert.Contains("2024/25", summary);
        Assert.Contains("Personal Allowance", summary);
    }
}
