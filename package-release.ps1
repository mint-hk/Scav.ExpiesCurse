param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Casualties Unknown Demo",
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
$package = Join-Path $dist "Scav.ExpiesCurse-$Version"

$project = Join-Path $root "Scav.ExpiesCurse.csproj"
dotnet restore $project
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet msbuild $project /p:Configuration=Release /p:GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Remove-Item -Recurse -Force $package -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $package | Out-Null

Copy-Item (Join-Path $root "bin\Release\Scav.ExpiesCurse.dll") $package
Copy-Item (Join-Path $root "install-expies-curse.cmd") $dist

$zip = "$package.zip"
Remove-Item -Force $zip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $package "*") -DestinationPath $zip

Write-Host "Created $zip"
