# UK PAYE Tax Calculator

A Windows desktop application for estimating UK income tax liability across multiple tax years.

## IMPORTANT DISCLAIMER

**This application is provided for estimation purposes only.**

- There is **no warranty** of any kind, express or implied
- There is **no support** provided
- Calculations **may be wrong, incomplete, or out of date**
- Tax rules are complex and change frequently — this tool does not guarantee accuracy
- **Do not rely on this application for filing tax returns or making financial decisions**
- Always consult HMRC or a qualified tax professional for definitive guidance
- The authors accept **no liability** for any errors, omissions, or consequences arising from use of this software

Use entirely at your own risk.

## Features

### Income & Employment
- **Multiple tax years** — tabs for 2023/24, 2024/25, 2025/26 with year-specific rules
- **Multiple employments & pensions** — add as many income sources as needed per year
- **Pension/annuity flag** — marks non-employment income (hides NI/expenses fields)
- **Savings interest** — with ISA tax-free flag
- **Scottish taxpayer support** — uses Scottish income tax bands when selected

### Allowances & Reliefs
- **Personal Allowance** — with automatic taper above £100,000
- **Marriage Allowance** — transfer or receive allowance from spouse
- **Blind Person's Allowance**
- **Gift Aid donations** — extends basic rate band

### Allowable Expenses
- **Working from home allowance** — HMRC flat rate (£6/week)
- **Business mileage** — AMAP rates (45p first 10,000 miles, 25p thereafter)
- **Professional subscriptions** — HMRC-approved bodies only
- **Uniform/work clothing allowance** — flat rate or actual cost
- **Other allowable expenses** — with description field

### Pension Tax Credits
- **Workplace pension contributions** — deducted from taxable income (Net Pay/AVC schemes)
- **Personal pension contributions (Relief at Source)** — calculates additional tax relief claimable:
  - Automatically grosses up net contributions
  - Identifies higher/additional rate taxpayers eligible for extra relief
  - Shows claimable amount with detailed breakdown
  - Provides guidance on how to claim (Self Assessment, tax code adjustment, or writing to HMRC)
  - Warns if contributions exceed the Annual Allowance (£60,000)

### Tax Calculation
- **Income tax bands** — basic (20%), higher (40%), additional (45%) rates
- **Scottish bands** — starter (19%), basic (20%), intermediate (21%), higher (42%), advanced (45%), top (48%)
- **Savings income** — starting rate for savings, personal savings allowance
- **NI verification** — compares paid vs expected employee Class 1 NIC

### User Experience
- **Formatted numbers** — thousand separators and 2 decimal places for currency
- **Carry forward** — new tax years inherit non-ended employments and savings with zero totals
- **Window position/size** — remembered between sessions
- **Auto-save** — data persisted to local app data as JSON
- **Export options** — Excel (.xlsx), PDF, Word (.docx)

## Pension Contributions Guide

The app handles two different types of pension contributions:

| Type | Where to Enter | How Tax Relief Works |
|------|----------------|---------------------|
| **Workplace pension** (deducted from salary) | Employment section → "Pension Contributions" | Full relief automatic via PAYE |
| **Personal pension/SIPP** (paid from bank account) | Other Details → "Personal Pension Contributions" | 20% added by provider; higher rates claim back |

### Example: Personal Pension (Relief at Source)
1. You pay £800 from your bank account to a SIPP
2. Pension provider adds £200 (20% basic rate relief) → £1,000 gross
3. If you're a 40% taxpayer, you can claim back an additional £200 from HMRC
4. The app calculates this automatically and shows how to claim

## Building

Requires .NET 8 SDK with Windows App SDK workload.

```
dotnet build PAYETAXCalc.csproj -p:Platform=x64
```

## Running Tests

```
dotnet test PAYETAXCalc.Tests\PAYETAXCalc.Tests.csproj
```

## MSI Installer

WiX Toolset v5 installer projects are included for x64 and x86 builds.

**Build an MSI locally:**
```
dotnet build PAYETAXCalc.Installer.x64.wixproj -c Release
```
Output: `bin\installer\PAYETAXCalc-Setup-x64.msi`

The installer project automatically publishes the app self-contained, harvests all output files using HeatWave, and packages them into a single MSI with Start Menu and Desktop shortcuts.

## CI / CD

GitHub Actions (`.github/workflows/ci.yml`) runs on every push and pull request to `master`:

| Job | Trigger | What it does |
|-----|---------|-------------|
| **Build & Test** | All pushes / PRs | Restores, builds, and runs all xUnit tests; uploads TRX results as an artifact |
| **Build & Release MSI** | Push to `master` only | Builds the x64 MSI and publishes a GitHub Release tagged with the version from `Package.appxmanifest` |

**To release a new version:**
1. Update `Version` in `Package.appxmanifest` (e.g. `1.2.0.0`)
2. Push to `master`
3. GitHub Actions builds the MSI and creates a release tagged `v1.2.0.0` automatically

## Tech Stack

- WinUI 3 / Windows App SDK
- .NET 8
- Mica backdrop
- WiX Toolset v5 (MSI installer)
- xUnit for testing
- GitHub Actions for CI/CD

## License

See LICENSE file for details.
