# Rebuilds the WPF app (Debug config - no code signing needed) and copies it over the live install in
# C:\Program Files\LenovoLegionToolkit, then makes sure that exe auto-elevates (no more manual
# "Run as administrator" needed). Run this from an elevated PowerShell whenever local changes need testing.
#
# Why Debug and not Release: Release's app.manifest asks for uiAccess="true", which Windows only honors for an
# Authenticode-signed exe - something this local setup has no certificate for. Debug's manifest (asInvoker,
# uiAccess="false") needs no signature; admin rights (for sensors/GodMode/etc.) come from the compatibility flag
# this script sets, not from the manifest.

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$installed = 'C:\Program Files\LenovoLegionToolkit'
$built = Join-Path $repoRoot 'BuildLLT\bin\Debug\net9.0-windows10.0.26100.0\win-x64'

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
    Write-Error "Run this from an elevated PowerShell (Program Files needs admin to write to)."
    exit 1
}

Write-Host "Building (Debug)..." -ForegroundColor Cyan
& dotnet build (Join-Path $repoRoot 'LenovoLegionToolkit.WPF\LenovoLegionToolkit.WPF.csproj') -c Debug -v:m
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed."; exit 1 }

Write-Host "Stopping the running app, if any..." -ForegroundColor Cyan
Get-Process | Where-Object { $_.Path -like "$installed*" } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "Copying the build into $installed ..." -ForegroundColor Cyan
Copy-Item (Join-Path $built '*') $installed -Recurse -Force

$exe = Join-Path $installed 'Lenovo Legion Toolkit.exe'
$regPath = 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers'
if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
New-ItemProperty -Path $regPath -Name $exe -Value 'RUNASADMIN' -PropertyType String -Force | Out-Null

Write-Host "Done. Launching..." -ForegroundColor Green
Start-Process -FilePath $exe
