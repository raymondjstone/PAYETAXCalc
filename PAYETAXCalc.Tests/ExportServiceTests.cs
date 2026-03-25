using System;
using System.IO;
using PAYETAXCalc.Models;
using PAYETAXCalc.Services;
using Xunit;

namespace PAYETAXCalc.Tests;

public class ExportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PAYETAXCalc_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private static (TaxYearData data, TaxYearRules rules, TaxCalculationResult result) CreateTestData()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment
        {
            EmployerName = "Test Corp",
            PayeReference = "123/A456",
            GrossSalary = 45000,
            TaxPaid = 6500,
            NationalInsurancePaid = 3000,
            BenefitsInKind = 1000,
            PensionContributions = 2000,
            WorkFromHomeWeeks = 20,
            BusinessMiles = 5000,
        });
        data.Employments.Add(new Employment
        {
            EmployerName = "State Pension",
            IsPensionOrAnnuity = true,
            GrossSalary = 10000,
            TaxPaid = 1000,
        });
        data.SavingsIncomes.Add(new SavingsIncome
        {
            ProviderName = "Big Bank",
            InterestAmount = 800,
        });
        data.SavingsIncomes.Add(new SavingsIncome
        {
            ProviderName = "ISA Account",
            InterestAmount = 500,
            IsTaxFree = true,
        });
        data.GiftAidDonations = 100;

        var rules = TaxRulesProvider.GetRules("2024/25")!;
        var result = TaxCalculator.Calculate(data, rules);

        return (data, rules, result);
    }

    // ═══════════ Excel ═══════════

    [Fact]
    public void ExportToExcel_Creates_File()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.xlsx");

        ExportService.ExportToExcel(path, data, rules, result);

        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
    }

    [Fact]
    public void ExportToExcel_File_Is_Valid_Xlsx()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.xlsx");

        ExportService.ExportToExcel(path, data, rules, result);

        // ClosedXML can re-read it
        using var wb = new ClosedXML.Excel.XLWorkbook(path);
        Assert.True(wb.Worksheets.Count >= 2); // Employments + Tax Calculation at minimum
    }

    [Fact]
    public void ExportToExcel_Contains_Employment_Data()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.xlsx");

        ExportService.ExportToExcel(path, data, rules, result);

        using var wb = new ClosedXML.Excel.XLWorkbook(path);
        var ws = wb.Worksheet("Employments");
        Assert.NotNull(ws);
        // Should contain the employer name somewhere
        bool found = false;
        foreach (var cell in ws.CellsUsed())
        {
            if (cell.GetString().Contains("Test Corp"))
            {
                found = true;
                break;
            }
        }
        Assert.True(found);
    }

    [Fact]
    public void ExportToExcel_Contains_Results_Sheet()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.xlsx");

        ExportService.ExportToExcel(path, data, rules, result);

        using var wb = new ClosedXML.Excel.XLWorkbook(path);
        Assert.True(wb.TryGetWorksheet("Tax Calculation", out _));
    }

    [Fact]
    public void ExportToExcel_Contains_Savings_Sheet()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.xlsx");

        ExportService.ExportToExcel(path, data, rules, result);

        using var wb = new ClosedXML.Excel.XLWorkbook(path);
        Assert.True(wb.TryGetWorksheet("Savings", out _));
    }

    [Fact]
    public void ExportToExcel_Estimated_Year_Shows_Warning()
    {
        var data = new TaxYearData { TaxYear = "2030/31" };
        data.Employments.Add(new Employment { GrossSalary = 30000 });
        var rules = TaxRulesProvider.GetOrEstimateRules("2030/31");
        var result = TaxCalculator.Calculate(data, rules);
        string path = Path.Combine(_tempDir, "estimated.xlsx");

        ExportService.ExportToExcel(path, data, rules, result);

        using var wb = new ClosedXML.Excel.XLWorkbook(path);
        var ws = wb.Worksheet("Employments");
        bool hasEstimated = false;
        foreach (var cell in ws.CellsUsed())
        {
            if (cell.GetString().Contains("ESTIMATED"))
            {
                hasEstimated = true;
                break;
            }
        }
        Assert.True(hasEstimated);
    }

    // ═══════════ PDF ═══════════

    [Fact]
    public void ExportToPdf_Creates_File()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.pdf");

        ExportService.ExportToPdf(path, data, rules, result);

        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
    }

    [Fact]
    public void ExportToPdf_File_Starts_With_PDF_Header()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.pdf");

        ExportService.ExportToPdf(path, data, rules, result);

        byte[] header = new byte[5];
        using var fs = File.OpenRead(path);
        fs.Read(header, 0, 5);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(header));
    }

    [Fact]
    public void ExportToPdf_Scottish_Taxpayer_Works()
    {
        var data = new TaxYearData { TaxYear = "2024/25", IsScottishTaxpayer = true };
        data.Employments.Add(new Employment { GrossSalary = 50000, TaxPaid = 8000 });
        var rules = TaxRulesProvider.GetRules("2024/25")!;
        var result = TaxCalculator.Calculate(data, rules);
        string path = Path.Combine(_tempDir, "scottish.pdf");

        ExportService.ExportToPdf(path, data, rules, result);

        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
    }

    // ═══════════ Word ═══════════

    [Fact]
    public void ExportToWord_Creates_File()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.docx");

        ExportService.ExportToWord(path, data, rules, result);

        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
    }

    [Fact]
    public void ExportToWord_File_Is_Valid_Docx()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.docx");

        ExportService.ExportToWord(path, data, rules, result);

        // OpenXml can re-read it
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path, false);
        Assert.NotNull(doc.MainDocumentPart);
        Assert.NotNull(doc.MainDocumentPart!.Document.Body);
    }

    [Fact]
    public void ExportToWord_Contains_Tax_Year()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.docx");

        ExportService.ExportToWord(path, data, rules, result);

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path, false);
        string allText = doc.MainDocumentPart!.Document.Body!.InnerText;
        Assert.Contains("2024/25", allText);
    }

    [Fact]
    public void ExportToWord_Contains_Employer_Name()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.docx");

        ExportService.ExportToWord(path, data, rules, result);

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path, false);
        string allText = doc.MainDocumentPart!.Document.Body!.InnerText;
        Assert.Contains("Test Corp", allText);
    }

    [Fact]
    public void ExportToWord_Contains_Disclaimer()
    {
        var (data, rules, result) = CreateTestData();
        string path = Path.Combine(_tempDir, "test.docx");

        ExportService.ExportToWord(path, data, rules, result);

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path, false);
        string allText = doc.MainDocumentPart!.Document.Body!.InnerText;
        Assert.Contains("DISCLAIMER", allText);
    }

    // ═══════════ Edge cases ═══════════

    [Fact]
    public void Export_With_No_Savings_Works()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment { GrossSalary = 30000 });
        var rules = TaxRulesProvider.GetRules("2024/25")!;
        var result = TaxCalculator.Calculate(data, rules);

        string xlsxPath = Path.Combine(_tempDir, "nosav.xlsx");
        string pdfPath = Path.Combine(_tempDir, "nosav.pdf");
        string docxPath = Path.Combine(_tempDir, "nosav.docx");

        ExportService.ExportToExcel(xlsxPath, data, rules, result);
        ExportService.ExportToPdf(pdfPath, data, rules, result);
        ExportService.ExportToWord(docxPath, data, rules, result);

        Assert.True(File.Exists(xlsxPath));
        Assert.True(File.Exists(pdfPath));
        Assert.True(File.Exists(docxPath));
    }

    [Fact]
    public void Export_With_Zero_Income_Works()
    {
        var data = new TaxYearData { TaxYear = "2024/25" };
        data.Employments.Add(new Employment());
        var rules = TaxRulesProvider.GetRules("2024/25")!;
        var result = TaxCalculator.Calculate(data, rules);

        string path = Path.Combine(_tempDir, "zero.xlsx");
        ExportService.ExportToExcel(path, data, rules, result);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Export_Future_Year_Estimated_Works()
    {
        var data = new TaxYearData { TaxYear = "2030/31" };
        data.Employments.Add(new Employment { GrossSalary = 50000, TaxPaid = 8000 });
        var rules = TaxRulesProvider.GetOrEstimateRules("2030/31");
        var result = TaxCalculator.Calculate(data, rules);

        string xlsxPath = Path.Combine(_tempDir, "future.xlsx");
        string pdfPath = Path.Combine(_tempDir, "future.pdf");
        string docxPath = Path.Combine(_tempDir, "future.docx");

        ExportService.ExportToExcel(xlsxPath, data, rules, result);
        ExportService.ExportToPdf(pdfPath, data, rules, result);
        ExportService.ExportToWord(docxPath, data, rules, result);

        Assert.True(File.Exists(xlsxPath));
        Assert.True(File.Exists(pdfPath));
        Assert.True(File.Exists(docxPath));
    }
}
