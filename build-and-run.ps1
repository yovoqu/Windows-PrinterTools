# 使用 D 盘环境构建
. "D:\A_Tools\Dev_env\setup-env.ps1"
Set-Location $PSScriptRoot
dotnet build -c Debug
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run
