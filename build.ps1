param(
    [string]$GameRoot = (Join-Path $PSScriptRoot '..\..\..')
)

$ErrorActionPreference = 'Stop'
$gamePath = (Resolve-Path -LiteralPath $GameRoot).Path.TrimEnd('\', '/')
$gamePathForBuild = $gamePath.Replace('\', '/') + '/'
$project = Join-Path $PSScriptRoot 'GlobalLedger.csproj'

& dotnet build $project -c Release "-p:GameRoot=$gamePathForBuild"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $(Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\GlobalLedger.dll')"
