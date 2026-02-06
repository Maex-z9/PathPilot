#!/usr/bin/env pwsh
# Build-Script: Publishes PathPilot as self-contained Windows x64 application

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$project = Join-Path $repoRoot "src/PathPilot.Desktop/PathPilot.Desktop.csproj"
$publishDir = Join-Path $scriptDir "publish"

Write-Host "Publishing PathPilot for win-x64..." -ForegroundColor Cyan

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

$fileCount = (Get-ChildItem $publishDir -Recurse -File).Count
Write-Host "Publish complete: $fileCount files in $publishDir" -ForegroundColor Green
