# Build MSI installer for PAYETAXCalc
# This script publishes the app and creates an MSI installer using WiX v5
# Adapted from CryptoTax2026\BuildMSI.ps1

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

# Ensure we're running from the script's own directory so all relative paths resolve correctly.
# This matters when VS Publish invokes the script via MSBuild Exec — the working directory
# may differ from the project root.
Set-Location $PSScriptRoot

# Ensure dotnet global tools (including wix) are in PATH.
# VS launches MSBuild in-process and its PATH may not include %USERPROFILE%\.dotnet\tools.
$dotnetToolsPath = Join-Path $env:USERPROFILE ".dotnet\tools"
if ((Test-Path $dotnetToolsPath) -and ($env:PATH -notlike "*$dotnetToolsPath*")) {
    $env:PATH = "$dotnetToolsPath;$env:PATH"
}

$PublishDir = "bin\publish\msi-$Platform"

Write-Host "Building PAYETAXCalc MSI Installer ($Platform)..." -ForegroundColor Green

if (-not $SkipPublish) {
    # Step 1: Clean previous builds
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $PublishDir) {
        Remove-Item -Recurse -Force $PublishDir
    }
    if (Test-Path "bin\installer") {
        Remove-Item -Recurse -Force "bin\installer"
    }
    if (Test-Path "bin\buildmsi-temp") {
        Remove-Item -Recurse -Force "bin\buildmsi-temp"
    }

    # Step 2: Build to generate resources.pri (requires MSIX tooling active)
    Write-Host "Building to generate resources.pri..." -ForegroundColor Yellow
    dotnet build "PAYETAXCalc.csproj" -c $Configuration -p:Platform=$Platform

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build application"
    }

    # Step 3: Publish the application (MSIX tooling disabled to avoid packaging)
    Write-Host "Publishing application..." -ForegroundColor Yellow
    dotnet publish "PAYETAXCalc.csproj" -c $Configuration -r "win-$Platform" --self-contained true -o $PublishDir -p:GenerateAppxPackageOnBuild=false -p:EnableMsixTooling=false -p:Platform=$Platform

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish application"
    }
}

# Step 2b: Copy the PRI resource file into the publish output.
# dotnet publish does not include the .pri file for WinUI 3 apps when MSIX tooling
# is disabled (it is normally bundled by MSIX packaging). For unpackaged/MSI
# deployment we must copy it manually. The file may be named resources.pri or
# <AppName>.pri depending on the build configuration.
Write-Host "Copying PRI resource file..." -ForegroundColor Yellow
$priSearchDirs = @(
    "bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\win-$Platform",
    "bin\$Platform\$Configuration\net8.0-windows10.0.19041.0"
)
$priFile = $null
foreach ($dir in $priSearchDirs) {
    $found = Get-ChildItem -Path $dir -Filter "*.pri" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { $priFile = $found; break }
}
if ($priFile) {
    Copy-Item $priFile.FullName "$PublishDir\$($priFile.Name)" -Force
    Write-Host "Copied $($priFile.Name) from $($priFile.FullName)" -ForegroundColor Cyan

    # WinUI 3 MRT Core resolves XAML resources at startup by looking for 'resources.pri'
    # in the exe directory. When MSIX tooling is disabled the build names the file after
    # the project (e.g. PAYETAXCalc.pri) instead of resources.pri, so the runtime can't
    # find it and crashes inside InitializeComponent() before any managed handler fires.
    # Always ensure a copy named resources.pri exists alongside the original.
    if ($priFile.Name -ne "resources.pri") {
        Copy-Item $priFile.FullName "$PublishDir\resources.pri" -Force
        Write-Host "Also written as resources.pri for MRT Core compatibility" -ForegroundColor Cyan
    }
} else {
    Write-Warning "PRI resource file not found - the installed app will crash at startup"
}

# Step 3: Generate HarvestedFiles.wxs with proper subdirectory support
Write-Host "Generating file list for installer..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "bin\installer" | Out-Null
New-Item -ItemType Directory -Force -Path "bin\buildmsi-temp" | Out-Null

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <ComponentGroup Id="HarvestedFiles" Directory="INSTALLFOLDER">')

$publishRoot = (Get-Item $PublishDir).FullName
$fileCount = 0
Get-ChildItem -Path $PublishDir -File -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($publishRoot.Length).TrimStart('\')
    $relativePathForward = $relativePath.Replace('\', '/')
    $fileName = $_.Name
    $componentId = "File_$fileCount"

    $cleanFileName = $fileName -replace '[^A-Za-z0-9_.]', '_' -replace '__+', '_'
    if ($cleanFileName.Length -gt 50) {
        $cleanFileName = $cleanFileName.Substring(0, 50)
    }
    $fileId = "File_${fileCount}_$cleanFileName"

    $keyPath = ''
    if ($fileName -eq "PAYETAXCalc.exe") { $keyPath = ' KeyPath="yes"' }

    $subDir = Split-Path $relativePath -Parent
    $subDirAttr = ''
    if ($subDir) {
        $subDirAttr = " Subdirectory=`"$subDir`""
    }

    [void]$sb.AppendLine("      <Component Id=`"$componentId`" Guid=`"*`"$subDirAttr>")
    [void]$sb.AppendLine("        <File Id=`"$fileId`" Source=`"$PublishDir\$relativePathForward`"$keyPath />")
    [void]$sb.AppendLine("      </Component>")
    $fileCount++
}

[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('</Wix>')

$sb.ToString() | Out-File -FilePath "bin\buildmsi-temp\HarvestedFiles.wxs" -Encoding UTF8

# Step 4: Build the MSI using wix.exe directly
Write-Host "Building MSI package..." -ForegroundColor Yellow
$outputName = "PAYETAXCalc-Setup-$Platform.msi"
[xml]$manifest = Get-Content "Package.appxmanifest"
$productVersion = $manifest.Package.Identity.Version
Write-Host "Product version: $productVersion" -ForegroundColor Cyan

$utilCA = if ($Platform -eq "x86") { "Wix4UtilCA_X86" } else { "Wix4UtilCA_X64" }

wix build "Installer.wxs" "bin\buildmsi-temp\HarvestedFiles.wxs" `
    -o "bin\installer\$outputName" `
    -arch $Platform.ToLower() `
    -d "ProductVersion=$productVersion" `
    -d "SourceDir=$PublishDir" `
    -d "UtilCA=$utilCA" `
    -ext WixToolset.Util.wixext `
    -pdbtype none

if ($LASTEXITCODE -ne 0) {
    throw "Failed to build MSI"
}

Write-Host "MSI installer created: bin\installer\$outputName" -ForegroundColor Green
Write-Host "Installer size: $([math]::Round((Get-Item "bin\installer\$outputName").Length / 1MB, 1)) MB" -ForegroundColor Cyan
