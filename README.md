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

- **Multiple tax years** — tabs for 2023/24, 2024/25, 2025/26 with year-specific rules
- **Multiple employments & pensions** — add as many income sources as needed per year
- **Pension/annuity flag** — marks non-employment income (hides NI/expenses fields)
- **Savings interest** — with ISA tax-free flag
- **Allowable expenses** — WFH allowance, business mileage (AMAP rates), professional subscriptions, uniform allowance, other expenses
- **Tax calculation** — personal allowance (with taper), basic/higher/additional rate bands, savings starting rate, personal savings allowance
- **Marriage Allowance & Blind Person's Allowance**
- **Gift Aid** — extends basic rate band
- **NI verification** — compares paid vs expected employee Class 1 NIC
- **Carry forward** — new tax years inherit non-ended employments and savings with zero totals
- **Window position/size** — remembered between sessions
- **Auto-save** — data persisted to local app data as JSON

## Building

Requires .NET 8 SDK with Windows App SDK workload.

```
dotnet build -p:Platform=x64
```

## Tech Stack

- WinUI 3 / Windows App SDK
- .NET 8
- Mica backdrop
