#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CoreSourceDir,
    [Parameter(Mandatory)][string]$MelonGameDir,
    [Parameter(Mandatory)][string]$BepInExGameDir,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
if (-not $ValidateOnly) { throw 'Use -ValidateOnly. Release archives are assembled separately.' }
$root = $PSScriptRoot
$core = [IO.Path]::GetFullPath($CoreSourceDir)
if (!(Test-Path -LiteralPath (Join-Path $core 'Core\DungeonSettlersDelvers.Core.csproj') -PathType Leaf)) {
    throw 'Core 0.3.0 source checkout is required. Pass its root with -CoreSourceDir.'
}
$coreProps = [xml](Get-Content -LiteralPath (Join-Path $core 'Directory.Build.props') -Raw)
$frierenProps = [xml](Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw)
if ($coreProps.Project.PropertyGroup.Version -ne '0.3.0' -or $frierenProps.Project.PropertyGroup.Version -ne '0.3.1') {
    throw 'Frieren 0.3.1 requires the Core 0.3.0 source checkout.'
}
$nuget = Join-Path $root 'NuGet.Config'
$assets = @('Frieren_Normal.png','Frieren_Stress.png','Elfische_Erzmagierin.png','Booklover.png')
$runtimeProject = Join-Path $root 'Frieren\DungeonSettlersDelvers.Frieren.csproj'
$testProject = Join-Path $root 'Tests\Frieren\FrierenPortrait.Tests.csproj'
$previousAppData = $env:APPDATA
$previousDotnetHome = $env:DOTNET_CLI_HOME
$previousNuGetPackages = $env:NUGET_PACKAGES
$previousFirstTime = $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('Delvers-Frieren-build-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $env:APPDATA = Join-Path $tempRoot 'appdata'
    $env:DOTNET_CLI_HOME = Join-Path $tempRoot 'dotnet-home'
    if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
        $env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
    }
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    New-Item -ItemType Directory -Path $env:APPDATA,$env:DOTNET_CLI_HOME -Force | Out-Null
foreach ($loader in @('Melon','BepInEx')) {
    $game = if ($loader -eq 'Melon') { [IO.Path]::GetFullPath($MelonGameDir) } else { [IO.Path]::GetFullPath($BepInExGameDir) }
    if (!(Test-Path -LiteralPath (Join-Path $game 'GameAssembly.dll') -PathType Leaf)) { throw "Missing GameAssembly.dll for $loader" }
    $adapter = Join-Path $root "Adapters\$loader\Frieren\DungeonSettlersDelvers.Frieren.$(if($loader -eq 'Melon'){'MelonLoader'}else{'BepInEx'}).csproj"
    $properties = @("-p:LoaderProfile=$loader", "-p:GameDir=$game", "-p:DelversCoreRoot=$core", "-p:RestoreConfigFile=$nuget", '-p:NuGetAudit=false')
    foreach ($project in @($runtimeProject,$adapter,$testProject)) {
        & dotnet restore $project --configfile $nuget @properties
        if ($LASTEXITCODE -ne 0) { throw "$loader restore failed: $project" }
    }
    foreach ($project in @($runtimeProject,$adapter)) {
        & dotnet build $project -c Release --no-restore -t:Rebuild @properties
        if ($LASTEXITCODE -ne 0) { throw "$loader build failed: $project" }
    }
    & dotnet run --project $testProject -c Release --no-restore @properties
    if ($LASTEXITCODE -ne 0) { throw "$loader Frieren tests failed" }
    $bin = Join-Path $root "bin\$loader\Release\net6.0"
    $expected = @('DungeonSettlersDelvers.Frieren.dll', "DungeonSettlersDelvers.Frieren.$(if($loader -eq 'Melon'){'MelonLoader'}else{'BepInEx'}).dll")
    foreach ($name in $expected) {
        $path = Join-Path $bin $name
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing $loader artifact: $name" }
    }
    foreach ($name in $assets) {
        if (!(Test-Path -LiteralPath (Join-Path $root "Frieren\Assets\$name") -PathType Leaf)) { throw "Missing runtime PNG: $name" }
    }
    "PASS: $loader Frieren build, offline rules and six-file runtime allowlist"
}
}
finally {
    $env:APPDATA = $previousAppData
    $env:DOTNET_CLI_HOME = $previousDotnetHome
    $env:NUGET_PACKAGES = $previousNuGetPackages
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = $previousFirstTime
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    $resolvedTemp = [IO.Path]::GetFullPath($tempRoot)
    $allowedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolvedTemp.StartsWith($allowedTemp, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Build temp directory resolved outside the system temp root.'
    }
    if (Test-Path -LiteralPath $resolvedTemp) { [IO.Directory]::Delete($resolvedTemp, $true) }
}
