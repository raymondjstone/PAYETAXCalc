using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PAYETAXCalc.Models;

namespace PAYETAXCalc.Services
{
    public static class DataService
    {
        private static readonly string _dataFolder;
        private static readonly string _dataFile;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        static DataService()
        {
            _dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PAYETAXCalc");
            _dataFile = Path.Combine(_dataFolder, "appdata.json");
            Directory.CreateDirectory(_dataFolder);
        }

        public static AppData Load()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    string json = File.ReadAllText(_dataFile);
                    var data = JsonSerializer.Deserialize<AppData>(json, _jsonOptions);
                    if (data != null)
                    {
                        // Ensure ObservableCollections are properly initialized
                        foreach (var ty in data.TaxYears)
                        {
                            ty.Employments ??= new ObservableCollection<Employment>();
                            ty.SavingsIncomes ??= new ObservableCollection<SavingsIncome>();
                            ty.DividendIncomes ??= new ObservableCollection<DividendIncome>();
                        }
                        return data;
                    }
                }
            }
            catch
            {
                // If data is corrupt, start fresh
            }
            return new AppData();
        }

        public static void Save(AppData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(_dataFile, json);
            }
            catch
            {
                // Silently fail - not critical
            }
        }

        public static TaxYearData CreateNewTaxYear(string taxYear, TaxYearData? previousYear)
        {
            var newYear = new TaxYearData { TaxYear = taxYear };

            if (previousYear != null)
            {
                // Carry forward non-ended employments with zero totals
                foreach (var emp in previousYear.Employments)
                {
                    if (!emp.EmploymentEnded)
                    {
                        newYear.Employments.Add(new Employment
                        {
                            EmployerName = emp.EmployerName,
                            PayeReference = emp.PayeReference,
                            IsPensionOrAnnuity = emp.IsPensionOrAnnuity,
                            GrossSalary = 0,
                            TaxPaid = 0,
                            NationalInsurancePaid = 0,
                            BenefitsInKind = 0,
                            PensionContributions = 0,
                            WorkFromHomeWeeks = 0,
                            BusinessMiles = 0,
                            ProfessionalSubscriptions = 0,
                            UniformAllowance = 0,
                            OtherExpenses = 0,
                            OtherExpensesDescription = "",
                            EmploymentEnded = false,
                        });
                    }
                }

                // Carry forward savings accounts
                foreach (var sav in previousYear.SavingsIncomes)
                {
                    newYear.SavingsIncomes.Add(new SavingsIncome
                    {
                        ProviderName = sav.ProviderName,
                        IsTaxFree = sav.IsTaxFree,
                        InterestAmount = 0,
                    });
                }

                // Carry forward dividend sources
                foreach (var div in previousYear.DividendIncomes)
                {
                    newYear.DividendIncomes.Add(new DividendIncome
                    {
                        CompanyName = div.CompanyName,
                        GrossDividend = 0,
                        TaxPaid = 0,
                    });
                }

                // Carry forward other settings
                newYear.ClaimMarriageAllowance = previousYear.ClaimMarriageAllowance;
                newYear.IsMarriageAllowanceReceiver = previousYear.IsMarriageAllowanceReceiver;
                newYear.ClaimBlindPersonsAllowance = previousYear.ClaimBlindPersonsAllowance;
                newYear.IsScottishTaxpayer = previousYear.IsScottishTaxpayer;
            }

            // Ensure at least one employment entry
            if (newYear.Employments.Count == 0)
            {
                newYear.Employments.Add(new Employment());
            }

            return newYear;
        }
    }
}
