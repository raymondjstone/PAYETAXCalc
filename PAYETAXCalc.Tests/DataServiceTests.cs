using System;
using System.IO;
using PAYETAXCalc.Models;
using PAYETAXCalc.Services;
using Xunit;

namespace PAYETAXCalc.Tests;

public class DataServiceTests
{
    // ═══════════ CreateNewTaxYear ═══════════

    [Fact]
    public void CreateNewTaxYear_Sets_TaxYear()
    {
        var result = DataService.CreateNewTaxYear("2024/25", null);
        Assert.Equal("2024/25", result.TaxYear);
    }

    [Fact]
    public void CreateNewTaxYear_Without_Previous_Has_One_Empty_Employment()
    {
        var result = DataService.CreateNewTaxYear("2024/25", null);

        Assert.Single(result.Employments);
        Assert.Equal(0, result.Employments[0].GrossSalary);
        Assert.Equal("", result.Employments[0].EmployerName);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Non_Ended_Employment()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.Employments.Add(new Employment
        {
            EmployerName = "Acme Corp",
            PayeReference = "123/A456",
            GrossSalary = 50000,
            TaxPaid = 10000,
            EmploymentEnded = false,
        });

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.Single(result.Employments);
        Assert.Equal("Acme Corp", result.Employments[0].EmployerName);
        Assert.Equal("123/A456", result.Employments[0].PayeReference);
        Assert.Equal(0, result.Employments[0].GrossSalary);
        Assert.Equal(0, result.Employments[0].TaxPaid);
    }

    [Fact]
    public void CreateNewTaxYear_Does_Not_Carry_Ended_Employment()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.Employments.Add(new Employment
        {
            EmployerName = "Old Job",
            EmploymentEnded = true,
            GrossSalary = 30000,
        });
        prev.Employments.Add(new Employment
        {
            EmployerName = "Current Job",
            EmploymentEnded = false,
            GrossSalary = 40000,
        });

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.Single(result.Employments);
        Assert.Equal("Current Job", result.Employments[0].EmployerName);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Pension_Flag()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.Employments.Add(new Employment
        {
            EmployerName = "Pension Fund",
            IsPensionOrAnnuity = true,
        });

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.True(result.Employments[0].IsPensionOrAnnuity);
    }

    [Fact]
    public void CreateNewTaxYear_Zeros_All_Financial_Fields()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.Employments.Add(new Employment
        {
            GrossSalary = 50000,
            TaxPaid = 10000,
            NationalInsurancePaid = 4000,
            BenefitsInKind = 2000,
            PensionContributions = 3000,
            WorkFromHomeWeeks = 26,
            BusinessMiles = 5000,
            ProfessionalSubscriptions = 200,
            UniformAllowance = 60,
            OtherExpenses = 100,
        });

        var result = DataService.CreateNewTaxYear("2024/25", prev);
        var emp = result.Employments[0];

        Assert.Equal(0, emp.GrossSalary);
        Assert.Equal(0, emp.TaxPaid);
        Assert.Equal(0, emp.NationalInsurancePaid);
        Assert.Equal(0, emp.BenefitsInKind);
        Assert.Equal(0, emp.PensionContributions);
        Assert.Equal(0, emp.WorkFromHomeWeeks);
        Assert.Equal(0, emp.BusinessMiles);
        Assert.Equal(0, emp.ProfessionalSubscriptions);
        Assert.Equal(0, emp.UniformAllowance);
        Assert.Equal(0, emp.OtherExpenses);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Savings()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.SavingsIncomes.Add(new SavingsIncome
        {
            ProviderName = "Bank ISA",
            IsTaxFree = true,
            InterestAmount = 500,
        });
        prev.SavingsIncomes.Add(new SavingsIncome
        {
            ProviderName = "Regular Saver",
            IsTaxFree = false,
            InterestAmount = 200,
        });

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.Equal(2, result.SavingsIncomes.Count);
        Assert.Equal("Bank ISA", result.SavingsIncomes[0].ProviderName);
        Assert.True(result.SavingsIncomes[0].IsTaxFree);
        Assert.Equal(0, result.SavingsIncomes[0].InterestAmount);
        Assert.Equal("Regular Saver", result.SavingsIncomes[1].ProviderName);
        Assert.False(result.SavingsIncomes[1].IsTaxFree);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Marriage_Allowance()
    {
        var prev = new TaxYearData
        {
            TaxYear = "2023/24",
            ClaimMarriageAllowance = true,
            IsMarriageAllowanceReceiver = true,
        };

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.True(result.ClaimMarriageAllowance);
        Assert.True(result.IsMarriageAllowanceReceiver);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Blind_Persons_Allowance()
    {
        var prev = new TaxYearData
        {
            TaxYear = "2023/24",
            ClaimBlindPersonsAllowance = true,
        };

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.True(result.ClaimBlindPersonsAllowance);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Scottish_Flag()
    {
        var prev = new TaxYearData
        {
            TaxYear = "2023/24",
            IsScottishTaxpayer = true,
        };

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.True(result.IsScottishTaxpayer);
    }

    [Fact]
    public void CreateNewTaxYear_All_Ended_Gets_Default_Employment()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.Employments.Add(new Employment { EmploymentEnded = true });

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        // Should still have at least one employment entry
        Assert.Single(result.Employments);
        Assert.Equal("", result.Employments[0].EmployerName);
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Dividends()
    {
        var prev = new TaxYearData { TaxYear = "2023/24" };
        prev.DividendIncomes.Add(new DividendIncome
        {
            CompanyName = "Big Corp plc",
            GrossDividend = 5000,
            TaxPaid = 200,
        });

        var result = DataService.CreateNewTaxYear("2024/25", prev);

        Assert.Single(result.DividendIncomes);
        Assert.Equal("Big Corp plc", result.DividendIncomes[0].CompanyName);
        Assert.Equal(0, result.DividendIncomes[0].GrossDividend);
        Assert.Equal(0, result.DividendIncomes[0].TaxPaid);
    }

    // ═══════════ Save and Load round-trip ═══════════

    [Fact]
    public void Save_And_Load_Roundtrip()
    {
        var appData = new AppData();
        appData.Window = new WindowSettings { X = 100, Y = 200, Width = 1200, Height = 900 };
        var ty = new TaxYearData { TaxYear = "2024/25", IsScottishTaxpayer = true };
        ty.Employments.Add(new Employment { EmployerName = "TestCo", GrossSalary = 45000 });
        ty.SavingsIncomes.Add(new SavingsIncome { ProviderName = "TestBank", InterestAmount = 500 });
        appData.TaxYears.Add(ty);

        DataService.Save(appData);
        var loaded = DataService.Load();

        Assert.Single(loaded.TaxYears);
        Assert.Equal("2024/25", loaded.TaxYears[0].TaxYear);
        Assert.True(loaded.TaxYears[0].IsScottishTaxpayer);
        Assert.Single(loaded.TaxYears[0].Employments);
        Assert.Equal("TestCo", loaded.TaxYears[0].Employments[0].EmployerName);
        Assert.Equal(45000, loaded.TaxYears[0].Employments[0].GrossSalary);
        Assert.Single(loaded.TaxYears[0].SavingsIncomes);
        Assert.Equal(100, loaded.Window.X);
        Assert.Equal(200, loaded.Window.Y);
    }

    // ═══════════ ExportBackup ═══════════

    [Fact]
    public void ExportBackup_Creates_File_With_Content()
    {
        var appData = new AppData();
        var ty = new TaxYearData { TaxYear = "2024/25" };
        ty.Employments.Add(new Employment { EmployerName = "Backup Corp", GrossSalary = 60000 });
        appData.TaxYears.Add(ty);

        string path = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_backup_{Guid.NewGuid():N}.json");
        try
        {
            DataService.ExportBackup(appData, path);

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ExportBackup_Produces_Valid_Json_That_Round_Trips()
    {
        var appData = new AppData();
        appData.BuyMeCoffeeClicked = true;
        var ty = new TaxYearData { TaxYear = "2023/24", IsScottishTaxpayer = true };
        ty.Employments.Add(new Employment { EmployerName = "Round Trip Ltd", GrossSalary = 50000 });
        ty.DividendIncomes.Add(new DividendIncome { CompanyName = "ACME plc", GrossDividend = 3000 });
        appData.TaxYears.Add(ty);

        string path = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_backup_{Guid.NewGuid():N}.json");
        try
        {
            DataService.ExportBackup(appData, path);

            string json = File.ReadAllText(path);
            Assert.Contains("Round Trip Ltd", json);
            Assert.Contains("2023/24", json);
            Assert.Contains("ACME plc", json);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Save_And_Load_Preserves_BuyMeCoffeeClicked()
    {
        var appData = new AppData { BuyMeCoffeeClicked = true };
        appData.TaxYears.Add(new TaxYearData { TaxYear = "2024/25" });
        appData.TaxYears[0].Employments.Add(new Employment());

        DataService.Save(appData);
        var loaded = DataService.Load();

        Assert.True(loaded.BuyMeCoffeeClicked);
    }

    [Fact]
    public void Save_And_Load_Preserves_CapitalGains()
    {
        var appData = new AppData();
        var ty = new TaxYearData { TaxYear = "2024/25" };
        ty.Employments.Add(new Employment { GrossSalary = 30000 });
        ty.CapitalGains.Add(new CapitalGain { Description = "Property sale", GainAmount = 50000, IsResidentialProperty = true });
        appData.TaxYears.Add(ty);

        DataService.Save(appData);
        var loaded = DataService.Load();

        Assert.Single(loaded.TaxYears[0].CapitalGains);
        Assert.Equal("Property sale", loaded.TaxYears[0].CapitalGains[0].Description);
        Assert.Equal(50000, loaded.TaxYears[0].CapitalGains[0].GainAmount);
        Assert.True(loaded.TaxYears[0].CapitalGains[0].IsResidentialProperty);
    }

    // ═══════════ ImportBackup ═══════════

    [Fact]
    public void ImportBackup_Returns_Null_For_Invalid_File()
    {
        string path = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_invalid_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "this is not valid json {{{");
            var result = DataService.ImportBackup(path);
            Assert.Null(result);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ImportBackup_Returns_Null_For_Missing_File()
    {
        var result = DataService.ImportBackup(Path.Combine(Path.GetTempPath(), "no_such_file_xyz.json"));
        Assert.Null(result);
    }

    [Fact]
    public void ImportBackup_Round_Trips_Tax_Data()
    {
        var appData = new AppData();
        var ty = new TaxYearData
        {
            TaxYear = "2024/25",
            IsScottishTaxpayer = true,
            GiftAidDonations = 250,
        };
        ty.Employments.Add(new Employment { EmployerName = "Import Co", GrossSalary = 55000, TaxPaid = 12000 });
        ty.SavingsIncomes.Add(new SavingsIncome { ProviderName = "Import Bank", InterestAmount = 800 });
        appData.TaxYears.Add(ty);

        string path = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_import_{Guid.NewGuid():N}.json");
        try
        {
            DataService.ExportBackup(appData, path);
            var imported = DataService.ImportBackup(path);

            Assert.NotNull(imported);
            Assert.Single(imported!.TaxYears);
            Assert.Equal("2024/25", imported.TaxYears[0].TaxYear);
            Assert.True(imported.TaxYears[0].IsScottishTaxpayer);
            Assert.Equal(250, imported.TaxYears[0].GiftAidDonations);
            Assert.Single(imported.TaxYears[0].Employments);
            Assert.Equal("Import Co", imported.TaxYears[0].Employments[0].EmployerName);
            Assert.Equal(55000, imported.TaxYears[0].Employments[0].GrossSalary);
            Assert.Single(imported.TaxYears[0].SavingsIncomes);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ImportBackup_Initialises_Null_Collections()
    {
        // Simulate a minimal JSON without collections (as might come from an old backup)
        string json = "{\"taxYears\":[{\"taxYear\":\"2024/25\"}]}";
        string path = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_null_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, json);
            var imported = DataService.ImportBackup(path);

            Assert.NotNull(imported);
            Assert.Single(imported!.TaxYears);
            Assert.NotNull(imported.TaxYears[0].Employments);
            Assert.NotNull(imported.TaxYears[0].SavingsIncomes);
            Assert.NotNull(imported.TaxYears[0].DividendIncomes);
            Assert.NotNull(imported.TaxYears[0].CapitalGains);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Save_And_Load_Preserves_CoffeePromptTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        var appData = new AppData
        {
            BuyMeCoffeeClicked = false,
            LastCoffeePrompt = now,
            FirstAppUse = now.AddDays(-7),
        };
        appData.TaxYears.Add(new TaxYearData { TaxYear = "2024/25" });
        appData.TaxYears[0].Employments.Add(new Employment());

        DataService.Save(appData);
        var loaded = DataService.Load();

        Assert.False(loaded.BuyMeCoffeeClicked);
        Assert.NotNull(loaded.LastCoffeePrompt);
        Assert.NotNull(loaded.FirstAppUse);
        // Round-trip preserves to at least second precision
        Assert.Equal(now.Date, loaded.LastCoffeePrompt!.Value.Date);
        Assert.Equal(now.AddDays(-7).Date, loaded.FirstAppUse!.Value.Date);
    }

    [Fact]
    public void ImportBackup_Preserves_Welsh_Taxpayer_Flag()
    {
        var appData = new AppData();
        appData.TaxYears.Add(new TaxYearData { TaxYear = "2025/26", IsWelshTaxpayer = true });
        appData.TaxYears[0].Employments.Add(new Employment { GrossSalary = 30000 });

        string path = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_welsh_{Guid.NewGuid():N}.json");
        try
        {
            DataService.ExportBackup(appData, path);
            var imported = DataService.ImportBackup(path);

            Assert.NotNull(imported);
            Assert.True(imported!.TaxYears[0].IsWelshTaxpayer);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void CreateNewTaxYear_Carries_Forward_Welsh_Flag()
    {
        var prev = new TaxYearData { TaxYear = "2024/25", IsWelshTaxpayer = true };

        var result = DataService.CreateNewTaxYear("2025/26", prev);

        Assert.True(result.IsWelshTaxpayer);
    }
}
