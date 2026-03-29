using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PAYETAXCalc.Models;
using QuestDocument = QuestPDF.Fluent.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using WordColor = DocumentFormat.OpenXml.Wordprocessing.Color;

namespace PAYETAXCalc.Services
{
    public static class ExportService
    {
        static ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ═══════════ EXCEL ═══════════

        public static void ExportToExcel(string path, TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            using var wb = new XLWorkbook();

            // Employments sheet
            var wsEmp = wb.Worksheets.Add("Employments");
            wsEmp.Cell(1, 1).Value = $"UK PAYE Tax Calculator - Tax Year {data.TaxYear}";
            wsEmp.Range(1, 1, 1, 8).Merge();
            wsEmp.Cell(1, 1).Style.Font.Bold = true;
            wsEmp.Cell(1, 1).Style.Font.FontSize = 14;

            if (TaxRulesProvider.IsEstimated(data.TaxYear))
            {
                wsEmp.Cell(2, 1).Value = "NOTE: Tax rates for this year are ESTIMATED based on the latest known rates.";
                wsEmp.Cell(2, 1).Style.Font.FontColor = XLColor.Red;
                wsEmp.Cell(2, 1).Style.Font.Bold = true;
            }

            int row = TaxRulesProvider.IsEstimated(data.TaxYear) ? 4 : 3;
            var headers = new[] { "Employer", "PAYE Ref", "Type", "Gross Income", "Tax Paid", "NI Paid", "Benefits in Kind", "Pension Contributions" };
            for (int i = 0; i < headers.Length; i++)
            {
                wsEmp.Cell(row, i + 1).Value = headers[i];
                wsEmp.Cell(row, i + 1).Style.Font.Bold = true;
                wsEmp.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            row++;

            foreach (var emp in data.Employments)
            {
                wsEmp.Cell(row, 1).Value = emp.EmployerName;
                wsEmp.Cell(row, 2).Value = emp.PayeReference;
                wsEmp.Cell(row, 3).Value = emp.IsPensionOrAnnuity ? "Pension/Annuity" : "Employment";
                wsEmp.Cell(row, 4).Value = emp.GrossSalary;
                wsEmp.Cell(row, 5).Value = emp.TaxPaid;
                wsEmp.Cell(row, 6).Value = emp.NationalInsurancePaid;
                wsEmp.Cell(row, 7).Value = emp.IsPensionOrAnnuity ? 0 : emp.BenefitsInKind;
                wsEmp.Cell(row, 8).Value = emp.IsPensionOrAnnuity ? 0 : emp.PensionContributions;
                for (int c = 4; c <= 8; c++)
                    wsEmp.Cell(row, c).Style.NumberFormat.Format = "£#,##0.00";
                row++;
            }

            // Expenses sub-table
            if (data.Employments.Any(e => !e.IsPensionOrAnnuity && (e.WorkFromHomeWeeks > 0 || e.BusinessMiles > 0 ||
                e.ProfessionalSubscriptions > 0 || e.UniformAllowance > 0 || e.OtherExpenses > 0)))
            {
                row += 2;
                wsEmp.Cell(row, 1).Value = "Allowable Expenses";
                wsEmp.Cell(row, 1).Style.Font.Bold = true;
                row++;
                var expHeaders = new[] { "Employer", "WFH Weeks", "Business Miles", "Prof. Subscriptions", "Uniform", "Other Expenses" };
                for (int i = 0; i < expHeaders.Length; i++)
                {
                    wsEmp.Cell(row, i + 1).Value = expHeaders[i];
                    wsEmp.Cell(row, i + 1).Style.Font.Bold = true;
                    wsEmp.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                row++;
                foreach (var emp in data.Employments.Where(e => !e.IsPensionOrAnnuity))
                {
                    wsEmp.Cell(row, 1).Value = emp.EmployerName;
                    wsEmp.Cell(row, 2).Value = emp.WorkFromHomeWeeks;
                    wsEmp.Cell(row, 3).Value = emp.BusinessMiles;
                    wsEmp.Cell(row, 4).Value = emp.ProfessionalSubscriptions;
                    wsEmp.Cell(row, 5).Value = emp.UniformAllowance;
                    wsEmp.Cell(row, 6).Value = emp.OtherExpenses;
                    for (int c = 4; c <= 6; c++)
                        wsEmp.Cell(row, c).Style.NumberFormat.Format = "£#,##0.00";
                    row++;
                }
            }

            wsEmp.Columns().AdjustToContents();

            // Savings sheet
            if (data.SavingsIncomes.Count > 0)
            {
                var wsSav = wb.Worksheets.Add("Savings");
                wsSav.Cell(1, 1).Value = "Savings Interest";
                wsSav.Cell(1, 1).Style.Font.Bold = true;
                wsSav.Cell(1, 1).Style.Font.FontSize = 14;
                wsSav.Cell(3, 1).Value = "Provider";
                wsSav.Cell(3, 2).Value = "Interest Earned";
                wsSav.Cell(3, 3).Value = "Tax Free (ISA)";
                for (int c = 1; c <= 3; c++)
                {
                    wsSav.Cell(3, c).Style.Font.Bold = true;
                    wsSav.Cell(3, c).Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                int sRow = 4;
                foreach (var sav in data.SavingsIncomes)
                {
                    wsSav.Cell(sRow, 1).Value = sav.ProviderName;
                    wsSav.Cell(sRow, 2).Value = sav.InterestAmount;
                    wsSav.Cell(sRow, 2).Style.NumberFormat.Format = "£#,##0.00";
                    wsSav.Cell(sRow, 3).Value = sav.IsTaxFree ? "Yes" : "No";
                    sRow++;
                }
                wsSav.Columns().AdjustToContents();
            }

            // Dividends sheet
            if (data.DividendIncomes.Count > 0)
            {
                var wsDiv = wb.Worksheets.Add("Dividends");
                wsDiv.Cell(1, 1).Value = "Dividend Income";
                wsDiv.Cell(1, 1).Style.Font.Bold = true;
                wsDiv.Cell(1, 1).Style.Font.FontSize = 14;
                wsDiv.Cell(3, 1).Value = "Company / Fund";
                wsDiv.Cell(3, 2).Value = "Gross Dividend";
                wsDiv.Cell(3, 3).Value = "Tax Paid";
                for (int c = 1; c <= 3; c++)
                {
                    wsDiv.Cell(3, c).Style.Font.Bold = true;
                    wsDiv.Cell(3, c).Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                int dRow = 4;
                foreach (var div in data.DividendIncomes)
                {
                    wsDiv.Cell(dRow, 1).Value = div.CompanyName;
                    wsDiv.Cell(dRow, 2).Value = div.GrossDividend;
                    wsDiv.Cell(dRow, 2).Style.NumberFormat.Format = "£#,##0.00";
                    wsDiv.Cell(dRow, 3).Value = div.TaxPaid;
                    wsDiv.Cell(dRow, 3).Style.NumberFormat.Format = "£#,##0.00";
                    dRow++;
                }
                wsDiv.Columns().AdjustToContents();
            }

            // Calculation Results sheet
            var wsRes = wb.Worksheets.Add("Tax Calculation");
            WriteResultsSheet(wsRes, data, rules, result);

            wb.SaveAs(path);
        }

        private static void WriteResultsSheet(IXLWorksheet ws, TaxYearData data, TaxYearRules rules, TaxCalculationResult r)
        {
            ws.Cell(1, 1).Value = $"Tax Calculation - {data.TaxYear}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 3).Merge();

            if (TaxRulesProvider.IsEstimated(data.TaxYear))
            {
                ws.Cell(2, 1).Value = "ESTIMATED RATES - actual rates for this year may differ";
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.Red;
                ws.Cell(2, 1).Style.Font.Bold = true;
            }

            string regime = data.IsScottishTaxpayer ? "Scottish" : "Rest of UK";
            int row = TaxRulesProvider.IsEstimated(data.TaxYear) ? 4 : 3;

            void AddRow(string label, decimal value, bool bold = false, string format = "£#,##0.00")
            {
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 2).Value = (double)value;
                ws.Cell(row, 2).Style.NumberFormat.Format = format;
                if (bold)
                {
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 2).Style.Font.Bold = true;
                }
                row++;
            }

            void AddTextRow(string label, string value, bool bold = false)
            {
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 2).Value = value;
                if (bold)
                {
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 2).Style.Font.Bold = true;
                }
                row++;
            }

            ws.Cell(row, 1).Value = "Tax Regime:";
            ws.Cell(row, 2).Value = regime;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row += 2;

            ws.Cell(row, 1).Value = "Income Summary";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            AddRow("Total Employment/Pension Income", r.TotalEmploymentIncome);
            AddRow("Total Benefits in Kind", r.TotalBenefitsInKind);
            if (r.TotalCompanyCarBenefit > 0)
                AddRow("  (incl. Company Car BIK)", r.TotalCompanyCarBenefit);
            if (r.TotalPensionContributions > 0)
                AddRow("Pension Contributions (deducted)", -r.TotalPensionContributions);
            if (r.TotalEmploymentExpenses > 0)
                AddRow("Allowable Employment Expenses", -r.TotalEmploymentExpenses);
            if (r.RentalTaxableIncome > 0)
                AddRow("Rental/Property Income (taxable)", r.RentalTaxableIncome);
            if (r.TradingTaxableIncome > 0)
                AddRow("Trading Income (taxable)", r.TradingTaxableIncome);
            AddRow("Taxable Savings Interest", r.TotalSavingsInterest);
            if (r.TotalTaxFreeSavings > 0)
                AddRow("Tax-Free Savings (ISA)", r.TotalTaxFreeSavings);
            if (r.TotalDividendIncome > 0)
                AddRow("Dividend Income", r.TotalDividendIncome);
            AddRow("Gross Taxable Income", r.GrossIncome, bold: true);
            AddRow("Personal Allowance Used", r.PersonalAllowanceUsed);
            if (r.GiftAidExtension > 0)
                AddRow("Gift Aid Band Extension", r.GiftAidExtension);
            if (r.MarriageAllowanceCredit > 0)
                AddRow("Marriage Allowance Credit", -r.MarriageAllowanceCredit);
            if (r.MortgageInterestRelief > 0)
                AddRow("Mortgage Interest Relief (20%)", -r.MortgageInterestRelief);
            if (r.TotalInvestmentRelief > 0)
                AddRow("Investment Relief (EIS/SEIS/VCT)", -r.TotalInvestmentRelief);

            row++;
            ws.Cell(row, 1).Value = $"Tax Breakdown ({regime} rates)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            foreach (var line in r.TaxBreakdown)
            {
                ws.Cell(row, 1).Value = line.Label;
                ws.Cell(row, 2).Value = line.IncomeText;
                ws.Cell(row, 3).Value = line.TaxText;
                row++;
            }

            row++;
            AddRow("Total Income Tax Due", r.TotalIncomeTaxDue, bold: true);
            AddRow("Total Tax Paid (PAYE)", r.TotalTaxPaidViaPAYE);
            if (r.PriorYearTaxCollected > 0)
            {
                AddRow("Less: Prior Year Tax Collected via PAYE", -r.PriorYearTaxCollected);
                AddRow("Effective Tax Paid (this year)", r.TotalTaxPaidViaPAYE - r.PriorYearTaxCollected);
            }
            AddRow("Difference (Over/Under Payment)", r.TaxOverUnderPayment, bold: true);

            row++;
            ws.Cell(row, 1).Value = "National Insurance";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            row++;
            AddRow("Total NI Paid", r.TotalNIPaid);
            AddRow("Expected NI", r.ExpectedNI);

            // Student Loan
            if (r.StudentLoanRepayment > 0)
            {
                row++;
                ws.Cell(row, 1).Value = "Student Loan Repayments";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                row++;
                AddRow("Annual Repayment", r.StudentLoanRepayment);
            }

            // HICBC
            if (r.ChildBenefitCharge > 0)
            {
                row++;
                ws.Cell(row, 1).Value = "High Income Child Benefit Charge";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                row++;
                AddRow("HICBC Charge", r.ChildBenefitCharge);
            }

            // CGT
            if (r.CapitalGainsTax > 0)
            {
                row++;
                ws.Cell(row, 1).Value = "Capital Gains Tax";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                row++;
                AddRow("Total Gains", r.TotalCapitalGains);
                AddRow("CGT Due", r.CapitalGainsTax);
            }

            // Pension AAC
            if (r.PensionAnnualAllowanceCharge > 0)
            {
                row++;
                ws.Cell(row, 1).Value = "Pension Annual Allowance Charge";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                row++;
                AddRow("AAC Charge", r.PensionAnnualAllowanceCharge);
            }

            // Tax Code
            if (!string.IsNullOrEmpty(r.TaxCodeValidation))
            {
                row++;
                AddTextRow("Tax Code Validation", r.TaxCodeValidation);
            }

            row += 2;
            ws.Cell(row, 1).Value = r.Summary;
            ws.Cell(row, 1).Style.Font.Italic = true;

            row += 2;
            ws.Cell(row, 1).Value = "DISCLAIMER: This is an estimate only. No warranty or guarantee of accuracy. Not financial advice.";
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Red;
            ws.Cell(row, 1).Style.Font.FontSize = 9;

            ws.Columns().AdjustToContents();
        }

        // ═══════════ PDF ═══════════

        public static void ExportToPdf(string path, TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            QuestDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"UK PAYE Tax Calculation - Tax Year {data.TaxYear}")
                            .FontSize(18).Bold();
                        string regime = data.IsScottishTaxpayer ? "Scottish" : "Rest of UK";
                        col.Item().Text($"Tax Regime: {regime}").FontSize(11);
                        if (TaxRulesProvider.IsEstimated(data.TaxYear))
                            col.Item().Text("ESTIMATED RATES - actual rates for this year may differ")
                                .FontColor(Colors.Red.Medium).Bold();
                        col.Item().PaddingBottom(10).LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        // Employments
                        col.Item().PaddingTop(5).Text("Employments & Pensions").FontSize(13).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Employer").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Type").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Gross Income").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Tax Paid").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("NI Paid").Bold();
                            });

                            foreach (var emp in data.Employments)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(emp.EmployerName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(emp.IsPensionOrAnnuity ? "Pension" : "Employment");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"£{emp.GrossSalary:N2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"£{emp.TaxPaid:N2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"£{emp.NationalInsurancePaid:N2}");
                            }
                        });

                        // Savings
                        if (data.SavingsIncomes.Count > 0)
                        {
                            col.Item().PaddingTop(15).Text("Savings Interest").FontSize(13).Bold();
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(4);
                                    cd.RelativeColumn(2);
                                    cd.RelativeColumn(2);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Provider").Bold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Interest").Bold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Tax Free").Bold();
                                });
                                foreach (var sav in data.SavingsIncomes)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(sav.ProviderName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"£{sav.InterestAmount:N2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(sav.IsTaxFree ? "Yes (ISA)" : "No");
                                }
                            });
                        }

                        // Dividends
                        if (data.DividendIncomes.Count > 0)
                        {
                            col.Item().PaddingTop(15).Text("Dividend Income").FontSize(13).Bold();
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(4);
                                    cd.RelativeColumn(2);
                                    cd.RelativeColumn(2);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Company / Fund").Bold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Gross Dividend").Bold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Tax Paid").Bold();
                                });
                                foreach (var div in data.DividendIncomes)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(div.CompanyName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"£{div.GrossDividend:N2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"£{div.TaxPaid:N2}");
                                }
                            });
                        }

                        // Results
                        col.Item().PaddingTop(15).Text("Tax Calculation Summary").FontSize(13).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(5);
                                cd.RelativeColumn(3);
                            });

                            void ResultRow(string label, string value, bool bold = false)
                            {
                                var cellL = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3);
                                var cellR = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight();
                                if (bold) { cellL.Text(label).Bold(); cellR.Text(value).Bold(); }
                                else { cellL.Text(label); cellR.Text(value); }
                            }

                            ResultRow("Total Employment/Pension Income", $"£{result.TotalEmploymentIncome:N2}");
                            ResultRow("Total Benefits in Kind", $"£{result.TotalBenefitsInKind:N2}");
                            if (result.TotalPensionContributions > 0)
                                ResultRow("Pension Contributions", $"-£{result.TotalPensionContributions:N2}");
                            if (result.TotalEmploymentExpenses > 0)
                                ResultRow("Allowable Expenses", $"-£{result.TotalEmploymentExpenses:N2}");
                            if (result.RentalTaxableIncome > 0)
                                ResultRow("Rental Income (taxable)", $"£{result.RentalTaxableIncome:N2}");
                            if (result.TradingTaxableIncome > 0)
                                ResultRow("Trading Income (taxable)", $"£{result.TradingTaxableIncome:N2}");
                            ResultRow("Taxable Savings Interest", $"£{result.TotalSavingsInterest:N2}");
                            if (result.TotalDividendIncome > 0)
                                ResultRow("Dividend Income", $"£{result.TotalDividendIncome:N2}");
                            ResultRow("Gross Taxable Income", $"£{result.GrossIncome:N2}", bold: true);
                            ResultRow("Personal Allowance Used", $"£{result.PersonalAllowanceUsed:N2}");
                            if (result.MortgageInterestRelief > 0)
                                ResultRow("Mortgage Interest Relief", $"-£{result.MortgageInterestRelief:N2}");
                            if (result.TotalInvestmentRelief > 0)
                                ResultRow("Investment Relief", $"-£{result.TotalInvestmentRelief:N2}");
                        });

                        // Tax breakdown
                        col.Item().PaddingTop(10).Text($"Tax Breakdown ({(data.IsScottishTaxpayer ? "Scottish" : "rUK")} rates)")
                            .FontSize(11).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(4);
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(2);
                            });
                            foreach (var line in result.TaxBreakdown)
                            {
                                table.Cell().Padding(2).Text(line.Label);
                                table.Cell().Padding(2).Text(line.IncomeText);
                                table.Cell().Padding(2).AlignRight().Text(line.TaxText).Bold();
                            }
                        });

                        // Totals
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(5);
                                cd.RelativeColumn(3);
                            });

                            void TotalRow(string label, string value, bool bold = false)
                            {
                                var cellL = table.Cell().Padding(3);
                                var cellR = table.Cell().Padding(3).AlignRight();
                                if (bold) { cellL.Text(label).Bold().FontSize(12); cellR.Text(value).Bold().FontSize(12); }
                                else { cellL.Text(label); cellR.Text(value); }
                            }

                            TotalRow("Total Income Tax Due", $"£{result.TotalIncomeTaxDue:N2}", bold: true);
                            TotalRow("Total Tax Paid (PAYE)", $"£{result.TotalTaxPaidViaPAYE:N2}");
                            if (result.PriorYearTaxCollected > 0)
                            {
                                TotalRow("Less: Prior Year Tax via PAYE", $"-£{result.PriorYearTaxCollected:N2}");
                                TotalRow("Effective Tax Paid (this year)", $"£{result.TotalTaxPaidViaPAYE - result.PriorYearTaxCollected:N2}");
                            }

                            string diffLabel = result.TaxOverUnderPayment > 0 ? "Tax Underpaid" :
                                              result.TaxOverUnderPayment < 0 ? "Tax Overpaid (refund due)" : "Difference";
                            TotalRow(diffLabel, $"£{Math.Abs(result.TaxOverUnderPayment):N2}", bold: true);
                        });

                        // NI
                        col.Item().PaddingTop(10).Text("National Insurance").FontSize(11).Bold();
                        col.Item().Text($"NI Paid: £{result.TotalNIPaid:N2}  |  Expected NI: £{result.ExpectedNI:N2}");

                        // Additional sections
                        if (result.StudentLoanRepayment > 0)
                        {
                            col.Item().PaddingTop(10).Text("Student Loan Repayments").FontSize(11).Bold();
                            col.Item().Text($"Annual Repayment: £{result.StudentLoanRepayment:N2}");
                        }

                        if (result.ChildBenefitCharge > 0)
                        {
                            col.Item().PaddingTop(10).Text("High Income Child Benefit Charge").FontSize(11).Bold();
                            col.Item().Text($"HICBC Charge: £{result.ChildBenefitCharge:N2}");
                        }

                        if (result.CapitalGainsTax > 0)
                        {
                            col.Item().PaddingTop(10).Text("Capital Gains Tax").FontSize(11).Bold();
                            col.Item().Text($"CGT Due: £{result.CapitalGainsTax:N2}");
                        }

                        if (result.PensionAnnualAllowanceCharge > 0)
                        {
                            col.Item().PaddingTop(10).Text("Pension Annual Allowance Charge").FontSize(11).Bold();
                            col.Item().Text($"AAC Charge: £{result.PensionAnnualAllowanceCharge:N2}");
                        }

                        // Summary
                        col.Item().PaddingTop(15).Background(Colors.Grey.Lighten4).Padding(10).Text(result.Summary).Italic();
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().PaddingTop(10).LineHorizontal(1);
                        col.Item().PaddingTop(5).Text("DISCLAIMER: This is an estimate only. No warranty or guarantee of accuracy. Not financial advice.")
                            .FontSize(8).FontColor(Colors.Red.Medium);
                        col.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(path);
        }

        // ═══════════ WORD ═══════════

        public static void ExportToWord(string path, TaxYearData data, TaxYearRules rules, TaxCalculationResult result)
        {
            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new WordDocument(new Body());
            var body = mainPart.Document.Body!;

            // Title
            body.Append(CreateParagraph($"UK PAYE Tax Calculation - Tax Year {data.TaxYear}",
                bold: true, fontSize: 28));

            string regime = data.IsScottishTaxpayer ? "Scottish" : "Rest of UK";
            body.Append(CreateParagraph($"Tax Regime: {regime}", fontSize: 22));

            if (TaxRulesProvider.IsEstimated(data.TaxYear))
                body.Append(CreateParagraph(
                    "ESTIMATED RATES - actual rates for this year may differ",
                    bold: true, color: "FF0000"));

            body.Append(CreateParagraph(""));

            // Employments
            body.Append(CreateParagraph("Employments & Pensions", bold: true, fontSize: 24));
            foreach (var emp in data.Employments)
            {
                string type = emp.IsPensionOrAnnuity ? "Pension/Annuity" : "Employment";
                body.Append(CreateParagraph(
                    $"{emp.EmployerName} ({type}) - PAYE: {emp.PayeReference}",
                    bold: true));
                body.Append(CreateParagraph(
                    $"  Gross Income: £{emp.GrossSalary:N2}  |  Tax Paid: £{emp.TaxPaid:N2}  |  NI Paid: £{emp.NationalInsurancePaid:N2}"));
                if (!emp.IsPensionOrAnnuity)
                {
                    if (emp.BenefitsInKind > 0 || emp.PensionContributions > 0)
                        body.Append(CreateParagraph(
                            $"  BIK: £{emp.BenefitsInKind:N2}  |  Pension Contributions: £{emp.PensionContributions:N2}"));
                    if (emp.HasCompanyCar && emp.CarListPrice > 0)
                        body.Append(CreateParagraph(
                            $"  Company Car: List £{emp.CarListPrice:N0}, CO2 {emp.CarCO2Emissions}g/km"));
                    if (emp.WorkFromHomeWeeks > 0 || emp.BusinessMiles > 0 ||
                        emp.ProfessionalSubscriptions > 0 || emp.UniformAllowance > 0 || emp.OtherExpenses > 0)
                        body.Append(CreateParagraph(
                            $"  Expenses: WFH {emp.WorkFromHomeWeeks} weeks, {emp.BusinessMiles:N0} miles, " +
                            $"Subs £{emp.ProfessionalSubscriptions:N2}, Uniform £{emp.UniformAllowance:N2}, Other £{emp.OtherExpenses:N2}"));
                }
            }

            // Savings
            if (data.SavingsIncomes.Count > 0)
            {
                body.Append(CreateParagraph(""));
                body.Append(CreateParagraph("Savings Interest", bold: true, fontSize: 24));
                foreach (var sav in data.SavingsIncomes)
                {
                    string taxFree = sav.IsTaxFree ? " (Tax Free ISA)" : "";
                    body.Append(CreateParagraph($"{sav.ProviderName}: £{sav.InterestAmount:N2}{taxFree}"));
                }
            }

            // Dividends
            if (data.DividendIncomes.Count > 0)
            {
                body.Append(CreateParagraph(""));
                body.Append(CreateParagraph("Dividend Income", bold: true, fontSize: 24));
                foreach (var div in data.DividendIncomes)
                {
                    string taxInfo = div.TaxPaid > 0 ? $"  |  Tax Paid: £{div.TaxPaid:N2}" : "";
                    body.Append(CreateParagraph($"{div.CompanyName}: £{div.GrossDividend:N2}{taxInfo}"));
                }
            }

            // Other details
            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph("Other Details", bold: true, fontSize: 24));
            if (data.ClaimMarriageAllowance)
                body.Append(CreateParagraph($"Marriage Allowance: {(data.IsMarriageAllowanceReceiver ? "Receiving" : "Transferring")}"));
            if (data.ClaimBlindPersonsAllowance)
                body.Append(CreateParagraph("Blind Person's Allowance: Claimed"));
            if (data.GiftAidDonations > 0)
                body.Append(CreateParagraph($"Gift Aid Donations: £{data.GiftAidDonations:N2}"));
            if (data.RentalIncome > 0)
                body.Append(CreateParagraph($"Rental Income: £{data.RentalIncome:N2} (Taxable: £{result.RentalTaxableIncome:N2})"));
            if (data.TradingIncome > 0)
                body.Append(CreateParagraph($"Trading Income: £{data.TradingIncome:N2} (Taxable: £{result.TradingTaxableIncome:N2})"));

            // Results
            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph("Tax Calculation Summary", bold: true, fontSize: 24));
            body.Append(CreateParagraph($"Total Employment/Pension Income: £{result.TotalEmploymentIncome:N2}"));
            body.Append(CreateParagraph($"Total Benefits in Kind: £{result.TotalBenefitsInKind:N2}"));
            if (result.TotalPensionContributions > 0)
                body.Append(CreateParagraph($"Pension Contributions: -£{result.TotalPensionContributions:N2}"));
            if (result.TotalEmploymentExpenses > 0)
            {
                body.Append(CreateParagraph($"Allowable Employment Expenses: -£{result.TotalEmploymentExpenses:N2}"));
                if (!string.IsNullOrEmpty(result.ExpensesBreakdown))
                    body.Append(CreateParagraph($"  ({result.ExpensesBreakdown.Replace("\n", ", ")})",
                        color: "666666"));
            }
            body.Append(CreateParagraph($"Taxable Savings Interest: £{result.TotalSavingsInterest:N2}"));
            if (result.TotalTaxFreeSavings > 0)
                body.Append(CreateParagraph($"Tax-Free Savings (ISA): £{result.TotalTaxFreeSavings:N2}"));
            if (result.TotalDividendIncome > 0)
                body.Append(CreateParagraph($"Dividend Income: £{result.TotalDividendIncome:N2}"));
            body.Append(CreateParagraph($"Gross Taxable Income: £{result.GrossIncome:N2}", bold: true));
            body.Append(CreateParagraph($"Personal Allowance Used: £{result.PersonalAllowanceUsed:N2}"));
            if (result.GiftAidExtension > 0)
                body.Append(CreateParagraph($"Gift Aid Band Extension: £{result.GiftAidExtension:N2}"));

            // Tax breakdown
            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph($"Tax Breakdown ({regime} rates)", bold: true, fontSize: 22));
            foreach (var line in result.TaxBreakdown)
            {
                body.Append(CreateParagraph($"{line.Label}  {line.IncomeText}  =  {line.TaxText}"));
            }

            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph($"Total Income Tax Due: £{result.TotalIncomeTaxDue:N2}", bold: true, fontSize: 24));
            body.Append(CreateParagraph($"Total Tax Paid (PAYE): £{result.TotalTaxPaidViaPAYE:N2}", fontSize: 22));
            if (result.PriorYearTaxCollected > 0)
            {
                body.Append(CreateParagraph($"Less: Prior Year Tax Collected via PAYE: -£{result.PriorYearTaxCollected:N2}", color: "666666"));
                body.Append(CreateParagraph($"Effective Tax Paid (this year): £{result.TotalTaxPaidViaPAYE - result.PriorYearTaxCollected:N2}", fontSize: 22));
            }

            string diffLabel = result.TaxOverUnderPayment > 0 ? "Tax Underpaid" :
                              result.TaxOverUnderPayment < 0 ? "Tax Overpaid (refund due)" : "Difference";
            body.Append(CreateParagraph($"{diffLabel}: £{Math.Abs(result.TaxOverUnderPayment):N2}",
                bold: true, fontSize: 24,
                color: result.TaxOverUnderPayment > 0 ? "FF4500" : result.TaxOverUnderPayment < 0 ? "228B22" : null));

            // NI
            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph("National Insurance", bold: true, fontSize: 22));
            body.Append(CreateParagraph($"NI Paid: £{result.TotalNIPaid:N2}  |  Expected NI: £{result.ExpectedNI:N2}"));

            // Additional sections
            if (result.StudentLoanRepayment > 0)
            {
                body.Append(CreateParagraph(""));
                body.Append(CreateParagraph("Student Loan Repayments", bold: true, fontSize: 22));
                body.Append(CreateParagraph($"Annual Repayment: £{result.StudentLoanRepayment:N2}"));
            }

            if (result.ChildBenefitCharge > 0)
            {
                body.Append(CreateParagraph(""));
                body.Append(CreateParagraph("High Income Child Benefit Charge", bold: true, fontSize: 22));
                body.Append(CreateParagraph($"HICBC Charge: £{result.ChildBenefitCharge:N2}"));
            }

            if (result.CapitalGainsTax > 0)
            {
                body.Append(CreateParagraph(""));
                body.Append(CreateParagraph("Capital Gains Tax", bold: true, fontSize: 22));
                body.Append(CreateParagraph($"CGT Due: £{result.CapitalGainsTax:N2}"));
            }

            if (result.PensionAnnualAllowanceCharge > 0)
            {
                body.Append(CreateParagraph(""));
                body.Append(CreateParagraph("Pension Annual Allowance Charge", bold: true, fontSize: 22));
                body.Append(CreateParagraph($"AAC Charge: £{result.PensionAnnualAllowanceCharge:N2}"));
            }

            // Summary
            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph(result.Summary, italic: true));

            // Disclaimer
            body.Append(CreateParagraph(""));
            body.Append(CreateParagraph(
                "DISCLAIMER: This is an estimate only. No warranty or guarantee of accuracy. Not financial advice.",
                color: "FF0000", fontSize: 16));
            body.Append(CreateParagraph($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}", color: "999999"));
        }

        private static Paragraph CreateParagraph(string text,
            bool bold = false, bool italic = false, string? color = null, int fontSize = 20)
        {
            var run = new Run();
            var runProps = new RunProperties();

            if (bold) runProps.Append(new Bold());
            if (italic) runProps.Append(new Italic());
            if (color != null)
                runProps.Append(new WordColor { Val = color });
            runProps.Append(new FontSize { Val = fontSize.ToString() });

            run.Append(runProps);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            return new Paragraph(run);
        }
    }
}
