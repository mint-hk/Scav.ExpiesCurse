param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Casualties Unknown Demo",
    [string]$Version = "0.1.2"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"

$project = Join-Path $root "Scav.ExpiesCurse.csproj"
dotnet restore $project
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet msbuild $project /p:Configuration=Release /p:GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path $dist | Out-Null

Copy-Item -Force (Join-Path $root "bin\Release\Scav.ExpiesCurse.dll") $dist
Copy-Item (Join-Path $root "install-expies-curse.cmd") $dist

Write-Host "Created $(Join-Path $dist 'Scav.ExpiesCurse.dll')"
