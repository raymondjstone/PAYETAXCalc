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
    public void GetAvailableTaxYears_Contains_All_Defined_Years()
    {
        var years = TaxRulesProvider.GetAvailableTaxYears();

        Assert.Contains("2023/24", years);
        Assert.Contains("2024/25", years);
        Assert.Contains("2025/26", years);
    }

    [Fact]
    public void GetAvailableTaxYears_Includes_Future_Years()
    {
        var years = TaxRulesProvider.GetAvailableTaxYears();
        var defined = TaxRulesProvider.GetDefinedTaxYears();

        // Should have more years than just the defined ones (includes future)
        Assert.True(years.Count >= defined.Count);
    }

    [Fact]
    public void GetAvailableTaxYears_Sorted()
    {
        var years = TaxRulesProvider.GetAvailableTaxYears();

        for (int i = 1; i < years.Count; i++)
            Assert.True(string.Compare(years[i - 1], years[i]) < 0);
    }

    [Fact]
    public void GetCurrentTaxYear_Returns_Valid_Year()
    {
        var year = TaxRulesProvider.GetCurrentTaxYear();
        var available = TaxRulesProvider.GetAvailableTaxYears();

        Assert.Contains(year, available);
    }

    // ═══════════ GetOrEstimateRules ═══════════

    [Fact]
    public void GetOrEstimateRules_Returns_Exact_For_Known_Year()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2024/25");
        Assert.Equal("2024/25", rules.TaxYear);
    }

    [Fact]
    public void GetOrEstimateRules_Returns_Estimated_For_Future_Year()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2030/31");

        Assert.NotNull(rules);
        Assert.Equal("2030/31", rules.TaxYear);
        // Should have same structure as latest known year
        Assert.True(rules.PersonalAllowance > 0);
        Assert.True(rules.RestOfUKBands.Count > 0);
        Assert.True(rules.ScottishBands.Count > 0);
    }

    [Fact]
    public void GetOrEstimateRules_Never_Returns_Null()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2099/00");
        Assert.NotNull(rules);
    }

    // ═══════════ IsEstimated ═══════════

    [Fact]
    public void IsEstimated_False_For_Known_Years()
    {
        Assert.False(TaxRulesProvider.IsEstimated("2023/24"));
        Assert.False(TaxRulesProvider.IsEstimated("2024/25"));
        Assert.False(TaxRulesProvider.IsEstimated("2025/26"));
    }

    [Fact]
    public void IsEstimated_True_For_Unknown_Years()
    {
        Assert.True(TaxRulesProvider.IsEstimated("2030/31"));
        Assert.True(TaxRulesProvider.IsEstimated("2020/21"));
    }

    // ═══════════ GetDefinedTaxYears ═══════════

    [Fact]
    public void GetDefinedTaxYears_Returns_Exactly_Three()
    {
        var years = TaxRulesProvider.GetDefinedTaxYears();
        Assert.Equal(3, years.Count);
    }

    // ═══════════ TryParseTaxYear ═══════════

    [Theory]
    [InlineData("2024/25", true, "2024/25")]
    [InlineData("2030/31", true, "2030/31")]
    [InlineData("2027", true, "2027/28")]
    [InlineData("2099", true, "2099/00")]
    [InlineData("2020/21", true, "2020/21")]
    public void TryParseTaxYear_Valid_Inputs(string input, bool expectedResult, string expectedFormatted)
    {
        bool result = TaxRulesProvider.TryParseTaxYear(input, out string formatted);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedFormatted, formatted);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2024/26")]  // wrong second year
    [InlineData("2019/20")]  // too old
    [InlineData("2019")]     // too old
    [InlineData("")]
    public void TryParseTaxYear_Invalid_Inputs(string input)
    {
        bool result = TaxRulesProvider.TryParseTaxYear(input, out _);
        Assert.False(result);
    }

    // ═══════════ FormatTaxYear ═══════════

    [Theory]
    [InlineData(2024, "2024/25")]
    [InlineData(2099, "2099/00")]
    [InlineData(2023, "2023/24")]
    public void FormatTaxYear_Correct(int startYear, string expected)
    {
        Assert.Equal(expected, TaxRulesProvider.FormatTaxYear(startYear));
    }

    // ═══════════ Estimated rules summary ═══════════

    [Fact]
    public void GetRulesSummary_Shows_Estimated_For_Future_Year()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2030/31");
        var summary = TaxRulesProvider.GetRulesSummary(rules);

        Assert.Contains("ESTIMATED", summary);
    }

    [Fact]
    public void GetRulesSummary_No_Estimated_For_Known_Year()
    {
        var rules = TaxRulesProvider.GetRules("2024/25")!;
        var summary = TaxRulesProvider.GetRulesSummary(rules);

        Assert.DoesNotContain("ESTIMATED", summary);
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

    // ═══════════ Dividend rates ═══════════

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Dividend_Basic_Rate_Is_8_75(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.0875m, rules.DividendBasicRate);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Dividend_Higher_Rate_Is_33_75(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.3375m, rules.DividendHigherRate);
    }

    [Theory]
    [InlineData("2023/24")]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Dividend_Additional_Rate_Is_39_35(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(0.3935m, rules.DividendAdditionalRate);
    }

    [Fact]
    public void Dividend_Allowance_202324_Is_1000()
    {
        var rules = TaxRulesProvider.GetRules("2023/24")!;
        Assert.Equal(1000m, rules.DividendAllowance);
    }

    [Theory]
    [InlineData("2024/25")]
    [InlineData("2025/26")]
    public void Dividend_Allowance_202425_Onwards_Is_500(string year)
    {
        var rules = TaxRulesProvider.GetRules(year)!;
        Assert.Equal(500m, rules.DividendAllowance);
    }

    [Fact]
    public void Estimated_Rules_Clone_Dividend_Rates()
    {
        var rules = TaxRulesProvider.GetOrEstimateRules("2030/31");
        Assert.Equal(0.0875m, rules.DividendBasicRate);
        Assert.Equal(0.3375m, rules.DividendHigherRate);
        Assert.Equal(0.3935m, rules.DividendAdditionalRate);
        Assert.True(rules.DividendAllowance > 0);
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

    // ═══════════ GetCompanyCarBIKPercentage ═══════════

    [Theory]
    [InlineData("2023/24", 2)]
    [InlineData("2024/25", 2)]
    [InlineData("2025/26", 3)]
    [InlineData("2030/31", 3)]
    public void Zero_Emission_Electric_BIK_Percentage_By_Year(string year, int expected)
    {
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(0, true, year);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2023/24", 8)]
    [InlineData("2024/25", 8)]
    [InlineData("2025/26", 9)]
    public void Low_Emission_Electric_Hybrid_BIK_Percentage_By_Year(string year, int expected)
    {
        // 1-50 g/km plug-in hybrid
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(30, true, year);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Standard_Car_CO2_50_Band_Is_15_Percent()
    {
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(50, false, "2024/25");
        Assert.Equal(15, result);
    }

    [Fact]
    public void Standard_Car_CO2_110_Band_Is_28_Percent()
    {
        // 110 > 109, <= 114
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(110, false, "2024/25");
        Assert.Equal(28, result);
    }

    [Fact]
    public void Standard_Car_CO2_155_Capped_At_37_Percent()
    {
        // > 154 = 37, capped at 37
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(155, false, "2024/25");
        Assert.Equal(37, result);
    }

    [Fact]
    public void Standard_Car_High_CO2_Capped_At_37_Percent()
    {
        // Very high CO2 should still cap at 37%
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(300, false, "2024/25");
        Assert.Equal(37, result);
    }

    [Fact]
    public void Standard_Car_CO2_80_Band()
    {
        // 80 > 79, <= 84 → 22%
        int result = TaxRulesProvider.GetCompanyCarBIKPercentage(80, false, "2024/25");
        Assert.Equal(22, result);
    }
}
