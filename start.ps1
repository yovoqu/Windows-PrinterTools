# Launch WindowsPrinter (self-contained unpackaged build required).

param([switch]$Build)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$ExeName = "WindowsPrinter.exe"

function Find-LatestExe {
    Get-ChildItem (Join-Path $ProjectRoot "bin") -Recurse -Filter $ExeName -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

if ($Build -or -not (Find-LatestExe)) {
    if (Test-Path "D:\A_Tools\Dev_env\setup-env.ps1") { . "D:\A_Tools\Dev_env\setup-env.ps1" }
    Set-Location $ProjectRoot
    dotnet build -c Debug
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$exe = Find-LatestExe
if ($null -eq $exe) { throw "WindowsPrinter.exe not found." }

Write-Host "Starting: $($exe.FullName)"
Start-Process -FilePath $exe.FullName -WorkingDirectory $exe.DirectoryName
