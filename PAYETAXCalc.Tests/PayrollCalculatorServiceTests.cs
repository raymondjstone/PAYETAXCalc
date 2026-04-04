using System;
using System.Linq;
using Xunit;
using PAYETAXCalc.Models;
using PAYETAXCalc.Services;

namespace PAYETAXCalc.Tests
{
    public class PayrollCalculatorServiceTests
    {
        // ─── helpers ─────────────────────────────────────────────────────────────

        private static TaxYearRules Rules2024() => TaxRulesProvider.GetOrEstimateRules("2024/25");
        private static TaxYearRules Rules2025() => TaxRulesProvider.GetOrEstimateRules("2025/26");
        private static TaxYearRules Rules2023() => TaxRulesProvider.GetOrEstimateRules("2023/24");

        private static PayrollInput BasicMonthly(decimal gross = 30000m, string code = "1257L",
            bool scottish = false, string year = "2024/25")
            => new() { TaxYear = year, Frequency = PayFrequency.Monthly, AnnualGross = gross, TaxCode = code, IsScottish = scottish };

        private static PayrollInput BasicWeekly(decimal gross = 26000m, string code = "1257L")
            => new() { TaxYear = "2024/25", Frequency = PayFrequency.Weekly, AnnualGross = gross, TaxCode = code };

        // ─── period counts ────────────────────────────────────────────────────────

        [Fact]
        public void Monthly_Returns12Periods()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(), Rules2024());
            Assert.Equal(12, results.Count);
        }

        [Fact]
        public void Weekly_Returns52Periods()
        {
            var results = PayrollCalculatorService.Calculate(BasicWeekly(), Rules2024());
            Assert.Equal(52, results.Count);
        }

        // ─── period labels ────────────────────────────────────────────────────────

        [Fact]
        public void Monthly_Period1Label_IsMonth1Apr()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(), Rules2024());
            Assert.Equal("Month 1 (Apr)", results[0].PeriodLabel);
        }

        [Fact]
        public void Monthly_Period12Label_IsMonth12Mar()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(), Rules2024());
            Assert.Equal("Month 12 (Mar)", results[11].PeriodLabel);
        }

        [Fact]
        public void Weekly_Period1Label_IsWeek1()
        {
            var results = PayrollCalculatorService.Calculate(BasicWeekly(), Rules2024());
            Assert.Equal("Week 1", results[0].PeriodLabel);
        }

        // ─── gross pay per period ─────────────────────────────────────────────────

        [Fact]
        public void Monthly_GrossPayPerPeriod_IsAnnualDividedBy12()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(36000m), Rules2024());
            Assert.All(results, r => Assert.Equal(3000m, r.GrossPay));
        }

        [Fact]
        public void Weekly_GrossPayPerPeriod_IsAnnualDividedBy52()
        {
            var results = PayrollCalculatorService.Calculate(BasicWeekly(26000m), Rules2024());
            Assert.All(results, r => Assert.Equal(500m, r.GrossPay));
        }

        // ─── PAYE tax — standard code ─────────────────────────────────────────────

        [Fact]
        public void Monthly_BasicRateTaxpayer_CorrectTaxPerPeriod()
        {
            // Gross £30,000  PA £12,570  taxable £17,430  annual tax £3,486  monthly £290.50
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            Assert.Equal(290.50m, results[0].EmployeeTax);
            // All periods equal for constant pay
            Assert.All(results, r => Assert.Equal(290.50m, r.EmployeeTax));
        }

        [Fact]
        public void Monthly_AnnualTax_MatchesManualCalculation()
        {
            // Annual taxable = 30000 - 12570 = 17430 → 17430 × 0.20 = 3486
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            decimal totalTax = results.Sum(r => r.EmployeeTax);
            Assert.Equal(3486m, totalTax);
        }

        [Fact]
        public void HighEarner_LastPeriod_CumulativeTaxMatchesAnnual()
        {
            // £70,000 gross: basic 37700×0.20=7540, higher (57430-37700)×0.40=7892 → total £15,432
            var results = PayrollCalculatorService.Calculate(BasicMonthly(70000m), Rules2024());
            Assert.Equal(15432m, results.Last().CumulativeTax);
        }

        // ─── PAYE tax — special codes ─────────────────────────────────────────────

        [Fact]
        public void BRCode_AllTaxAtBasicRate_NoPersonalAllowance()
        {
            // BR: every period: 2500 × 0.20 = 500
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "BR"), Rules2024());
            Assert.All(results, r => Assert.Equal(500m, r.EmployeeTax));
        }

        [Fact]
        public void NTCode_NoTax()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "NT"), Rules2024());
            Assert.All(results, r => Assert.Equal(0m, r.EmployeeTax));
        }

        [Fact]
        public void D0Code_AllTaxAtHigherRate()
        {
            // D0: 2500 × 0.40 = 1000
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "D0"), Rules2024());
            Assert.All(results, r => Assert.Equal(1000m, r.EmployeeTax));
        }

        [Fact]
        public void D1Code_AllTaxAtAdditionalRate()
        {
            // D1: 2500 × 0.45 = 1125
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "D1"), Rules2024());
            Assert.All(results, r => Assert.Equal(1125m, r.EmployeeTax));
        }

        [Fact]
        public void ZeroTCode_NoPersonalAllowance_StandardBands()
        {
            // 0T: PA = 0, taxable = gross, bands still 37700/74870
            // Annual gross 30000: all taxable, 30000 × 0.20 = 6000 annual tax
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "0T"), Rules2024());
            decimal totalTax = results.Sum(r => r.EmployeeTax);
            Assert.Equal(6000m, totalTax);
        }

        [Fact]
        public void KCode_NegativeAllowance_MoreTaxThanStandard()
        {
            // K500 → annual allowance = -5000 → more income taxable than standard
            var stdResults = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "1257L"), Rules2024());
            var kResults = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "K500"), Rules2024());
            // K code should produce more tax than standard allowance
            Assert.True(kResults.Sum(r => r.EmployeeTax) > stdResults.Sum(r => r.EmployeeTax));
        }

        [Fact]
        public void KCode_Period1Tax_CorrectValue()
        {
            // K500: annualAllowance=-5000, monthly cumFreePay=-416.67
            // cumTaxable(P1) = 2500-(-416.67)=2916.67  → 2916.67×0.20=583.33
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "K500"), Rules2024());
            Assert.Equal(583.33m, results[0].EmployeeTax);
        }

        [Fact]
        public void SPrefix_TaxCode_WorksSameAsNoPrefix()
        {
            // S1257L should parse identically to 1257L (Scottish prefix stripped)
            var r1 = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "1257L"), Rules2024());
            var r2 = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "S1257L"), Rules2024());
            Assert.Equal(r1[0].EmployeeTax, r2[0].EmployeeTax);
        }

        [Fact]
        public void M1Suffix_TaxCode_Stripped()
        {
            // 1257L M1 should strip the M1 and behave as 1257L
            var r1 = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "1257L"), Rules2024());
            var r2 = PayrollCalculatorService.Calculate(BasicMonthly(30000m, "1257L M1"), Rules2024());
            Assert.Equal(r1[0].EmployeeTax, r2[0].EmployeeTax);
        }

        // ─── Scottish taxpayer ────────────────────────────────────────────────────

        [Fact]
        public void ScottishTaxpayer_Period1_LowerTaxThanRUK()
        {
            // Scottish starter rate (19%) vs rUK basic rate (20%) for first band
            var ruk = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            var scot = PayrollCalculatorService.Calculate(BasicMonthly(30000m, scottish: true), Rules2024());
            Assert.True(scot[0].EmployeeTax < ruk[0].EmployeeTax);
        }

        [Fact]
        public void ScottishHighEarner_HigherTaxThanRUK()
        {
            // At £60k, Scottish advanced rate (45%) exceeds rUK higher rate (40%)
            var ruk = PayrollCalculatorService.Calculate(BasicMonthly(60000m), Rules2024());
            var scot = PayrollCalculatorService.Calculate(BasicMonthly(60000m, scottish: true), Rules2024());
            Assert.True(scot.Sum(r => r.EmployeeTax) > ruk.Sum(r => r.EmployeeTax));
        }

        // ─── Employee NI ──────────────────────────────────────────────────────────

        [Fact]
        public void Monthly_EmployeeNI_BasicRateTaxpayer_2024()
        {
            // 2024/25: rate 8%.  ptPeriod=1047.50, gross=2500
            // empNI = (2500-1047.50) × 0.08 = 1452.50 × 0.08 = 116.20
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            Assert.Equal(116.20m, results[0].EmployeeNI);
        }

        [Fact]
        public void Monthly_EmployeeNI_2023_HigherRate()
        {
            // 2023/24: main rate 12%
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m, year: "2023/24"), Rules2023());
            // empNI = 1452.50 × 0.12 = 174.30
            Assert.Equal(174.30m, results[0].EmployeeNI);
        }

        [Fact]
        public void HighEarner_EmployeeNI_UsesBothRates()
        {
            // Gross £60k: monthly £5000.  UEL = 50270/12 = 4189.17
            // mainRate portion: (4189.17 - 1047.50) × 0.08 = 3141.67 × 0.08 = 251.33
            // upperRate portion: (5000 - 4189.17) × 0.02 = 810.83 × 0.02 = 16.22
            // total = 267.55
            var results = PayrollCalculatorService.Calculate(BasicMonthly(60000m), Rules2024());
            Assert.Equal(267.55m, results[0].EmployeeNI);
        }

        [Fact]
        public void BelowPrimaryThreshold_NoEmployeeNI()
        {
            // Annual £9,000 → monthly £750 < ptPeriod £1047.50 → NI = 0
            var results = PayrollCalculatorService.Calculate(BasicMonthly(9000m), Rules2024());
            Assert.All(results, r => Assert.Equal(0m, r.EmployeeNI));
        }

        // ─── Employer NI ──────────────────────────────────────────────────────────

        [Fact]
        public void Monthly_EmployerNI_2024_CorrectRate()
        {
            // 2024/25: stPeriod=9100/12=758.33, rate=13.8%
            // erNI = (2500-758.33)×0.138 = 1741.67×0.138 = 240.35
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            Assert.Equal(240.35m, results[0].EmployerNI);
        }

        [Fact]
        public void Monthly_EmployerNI_2025_HigherRateLowerThreshold()
        {
            // 2025/26: stPeriod=5000/12=416.67, rate=15%
            // erNI = (2500-416.67)×0.15 = 2083.33×0.15 = 312.50
            var input = new PayrollInput { TaxYear = "2025/26", Frequency = PayFrequency.Monthly, AnnualGross = 30000m, TaxCode = "1257L" };
            var results = PayrollCalculatorService.Calculate(input, Rules2025());
            Assert.Equal(312.50m, results[0].EmployerNI);
        }

        [Fact]
        public void BelowSecondaryThreshold_NoEmployerNI()
        {
            // Annual £7,200 → monthly £600 < stPeriod £758.33 → employer NI = 0
            var results = PayrollCalculatorService.Calculate(BasicMonthly(7200m), Rules2024());
            Assert.All(results, r => Assert.Equal(0m, r.EmployerNI));
        }

        // ─── Employee pension ─────────────────────────────────────────────────────

        [Fact]
        public void EmployeePension_PercentOfGross_ReducesTaxAndNI()
        {
            // 5% employee pension on £30k monthly (£2500): pension = £125 → niable = £2375
            // cumTaxable(P1) = 2375-1047.50=1327.50  tax=1327.50×0.20=265.50
            // empNI = (2375-1047.50)×0.08=106.20
            var input = BasicMonthly(30000m) with
            {
                EmployeePensionType = PensionContributionType.PercentOfGross,
                EmployeePensionValue = 5m,
            };
            var results = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.Equal(125m, results[0].EmployeePension);
            Assert.Equal(265.50m, results[0].EmployeeTax);
            Assert.Equal(106.20m, results[0].EmployeeNI);
        }

        [Fact]
        public void EmployeePension_FixedAmount_CorrectDeduction()
        {
            // Fixed £200/month pension
            var input = BasicMonthly(30000m) with
            {
                EmployeePensionType = PensionContributionType.FixedAmount,
                EmployeePensionValue = 200m,
            };
            var results = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.Equal(200m, results[0].EmployeePension);
            Assert.All(results, r => Assert.Equal(200m, r.EmployeePension));
        }

        [Fact]
        public void EmployeePension_ReducesNetPay()
        {
            var noPension = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            var input = BasicMonthly(30000m) with
            {
                EmployeePensionType = PensionContributionType.PercentOfGross,
                EmployeePensionValue = 5m,
            };
            var withPension = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.True(withPension[0].NetPay < noPension[0].NetPay);
        }

        // ─── Employer pension ─────────────────────────────────────────────────────

        [Fact]
        public void EmployerPension_PercentOfGross_CorrectAmount()
        {
            // 3% employer pension on £2500 = £75
            var input = BasicMonthly(30000m) with
            {
                EmployerPensionType = PensionContributionType.PercentOfGross,
                EmployerPensionValue = 3m,
            };
            var results = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.Equal(75m, results[0].EmployerPension);
            Assert.All(results, r => Assert.Equal(75m, r.EmployerPension));
        }

        [Fact]
        public void EmployerPension_FixedAmount_CorrectDeduction()
        {
            var input = BasicMonthly(30000m) with
            {
                EmployerPensionType = PensionContributionType.FixedAmount,
                EmployerPensionValue = 150m,
            };
            var results = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.Equal(150m, results[0].EmployerPension);
        }

        [Fact]
        public void EmployerPension_DoesNotAffectNetPay()
        {
            // Employer pension doesn't come off the employee's net pay
            var noPension = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            var input = BasicMonthly(30000m) with
            {
                EmployerPensionType = PensionContributionType.PercentOfGross,
                EmployerPensionValue = 5m,
            };
            var withPension = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.Equal(noPension[0].NetPay, withPension[0].NetPay);
        }

        // ─── Net pay ──────────────────────────────────────────────────────────────

        [Fact]
        public void NetPay_EqualsGrossMinusTaxMinusNIMinusPension()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            var r = results[0];
            decimal expected = r.GrossPay - r.EmployeeTax - r.EmployeeNI - r.EmployeePension;
            Assert.Equal(Math.Round(expected, 2), r.NetPay);
        }

        // ─── Cumulative figures ───────────────────────────────────────────────────

        [Fact]
        public void CumulativeTax_LastPeriod_EqualsSumOfAllPeriodTax()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            decimal sumTax = results.Sum(r => r.EmployeeTax);
            Assert.Equal(sumTax, results.Last().CumulativeTax);
        }

        [Fact]
        public void CumulativeEmpNI_LastPeriod_EqualsSumOfAllPeriodNI()
        {
            var results = PayrollCalculatorService.Calculate(BasicMonthly(30000m), Rules2024());
            decimal sumNI = results.Sum(r => r.EmployeeNI);
            Assert.Equal(sumNI, results.Last().CumulativeEmployeeNI);
        }

        // ─── ParseTaxCode ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData("1257L", 12570)]
        [InlineData("500L", 5000)]
        [InlineData("0L", 0)]
        [InlineData("1257M", 12570)]
        [InlineData("1257N", 12570)]
        public void ParseTaxCode_StandardLCodes(string code, decimal expected)
        {
            decimal result = PayrollCalculatorService.ParseTaxCode(code, 12570m,
                out _, out _, out _, out _);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ParseTaxCode_BR_SetsFlag()
        {
            PayrollCalculatorService.ParseTaxCode("BR", 12570m, out bool isBR, out _, out _, out _);
            Assert.True(isBR);
        }

        [Fact]
        public void ParseTaxCode_NT_SetsFlag()
        {
            PayrollCalculatorService.ParseTaxCode("NT", 12570m, out _, out _, out _, out bool isNT);
            Assert.True(isNT);
        }

        [Fact]
        public void ParseTaxCode_D0_SetsFlag()
        {
            PayrollCalculatorService.ParseTaxCode("D0", 12570m, out _, out bool isD0, out _, out _);
            Assert.True(isD0);
        }

        [Fact]
        public void ParseTaxCode_D1_SetsFlag()
        {
            PayrollCalculatorService.ParseTaxCode("D1", 12570m, out _, out _, out bool isD1, out _);
            Assert.True(isD1);
        }

        [Fact]
        public void ParseTaxCode_K500_ReturnsNegative5000()
        {
            decimal result = PayrollCalculatorService.ParseTaxCode("K500", 12570m,
                out _, out _, out _, out _);
            Assert.Equal(-5000m, result);
        }

        [Fact]
        public void ParseTaxCode_SPrefix_Stripped()
        {
            decimal r1 = PayrollCalculatorService.ParseTaxCode("1257L", 12570m, out _, out _, out _, out _);
            decimal r2 = PayrollCalculatorService.ParseTaxCode("S1257L", 12570m, out _, out _, out _, out _);
            Assert.Equal(r1, r2);
        }

        [Fact]
        public void ParseTaxCode_Invalid_ReturnsDefault()
        {
            decimal result = PayrollCalculatorService.ParseTaxCode("BADCODE", 12570m, out _, out _, out _, out _);
            Assert.Equal(12570m, result);
        }

        // ─── Edge cases ───────────────────────────────────────────────────────────

        [Fact]
        public void ZeroGross_AllFieldsZero()
        {
            var input = BasicMonthly(0m);
            var results = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.All(results, r =>
            {
                Assert.Equal(0m, r.EmployeeTax);
                Assert.Equal(0m, r.EmployeeNI);
                Assert.Equal(0m, r.EmployerNI);
            });
        }

        [Fact]
        public void ZeroPensionValue_PensionIsZero()
        {
            var input = BasicMonthly(30000m) with
            {
                EmployeePensionType = PensionContributionType.PercentOfGross,
                EmployeePensionValue = 0m,
            };
            var results = PayrollCalculatorService.Calculate(input, Rules2024());
            Assert.All(results, r => Assert.Equal(0m, r.EmployeePension));
        }

        // ─── Welsh taxpayer ───────────────────────────────────────────────────────

        [Fact]
        public void Welsh_Tax_Matches_RUK_For_Same_Income()
        {
            // Welsh rates = rUK rates for all defined years, so results should be identical
            var ruk = BasicMonthly(40000m);
            var welsh = ruk with { IsWelsh = true };
            var rules = Rules2024();

            var rukResults = PayrollCalculatorService.Calculate(ruk, rules);
            var welshResults = PayrollCalculatorService.Calculate(welsh, rules);

            Assert.Equal(rukResults[0].EmployeeTax, welshResults[0].EmployeeTax);
            Assert.Equal(rukResults[0].NetPay, welshResults[0].NetPay);
        }

        [Fact]
        public void Welsh_And_Scottish_Are_Mutually_Exclusive_In_Input()
        {
            // PayrollInput allows IsWelsh and IsScottish independently; calculator uses Welsh if IsWelsh=true and IsScottish=false
            var welsh = new PayrollInput
            {
                TaxYear = "2024/25",
                Frequency = PayFrequency.Monthly,
                AnnualGross = 30000m,
                TaxCode = "1257L",
                IsScottish = false,
                IsWelsh = true,
            };
            var rules = Rules2024();
            var results = PayrollCalculatorService.Calculate(welsh, rules);
            Assert.Equal(12, results.Count);
        }

        [Fact]
        public void InvalidFrequency_ThrowsArgumentOutOfRange()
        {
            var input = new PayrollInput
            {
                TaxYear = "2024/25",
                Frequency = (PayFrequency)999,
                AnnualGross = 30000m,
                TaxCode = "1257L",
            };
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PayrollCalculatorService.Calculate(input, Rules2024()));
        }

        [Fact]
        public void ParseTaxCode_CPrefix_Stripped()
        {
            // 'C' prefix denotes Welsh taxpayer — should be stripped like 'S'
            decimal r1 = PayrollCalculatorService.ParseTaxCode("1257L", 12570m, out _, out _, out _, out _);
            decimal r2 = PayrollCalculatorService.ParseTaxCode("C1257L", 12570m, out _, out _, out _, out _);
            Assert.Equal(r1, r2);
        }
    }
}
