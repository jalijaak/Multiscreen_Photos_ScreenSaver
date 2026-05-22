# One-time (or after clean) restore of ILRepack for single-file Release builds.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$version = '2.0.41'
$dest = Join-Path $projectRoot "packages\ILRepack.$version"
$tools = Join-Path $dest 'tools\ILRepack.exe'

if (Test-Path $tools) {
    Write-Host "ILRepack already present: $tools"
    exit 0
}

$nupkg = Join-Path $env:TEMP "ILRepack.$version.nupkg"
Write-Host "Downloading ILRepack $version..."
Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/ILRepack/$version" -OutFile $nupkg -UseBasicParsing

New-Item -ItemType Directory -Path $dest -Force | Out-Null
tar -xf $nupkg -C $dest
Write-Host "Installed ILRepack to $tools"
