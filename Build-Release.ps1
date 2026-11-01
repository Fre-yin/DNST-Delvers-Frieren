#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CoreSourceDir,
    [string]$MelonGameDir = '',
    [Parameter(Mandatory)][string]$BepInExGameDir,
    [switch]$ValidateOnly,
    # 0.5.0 is BepInEx only: MelonLoader is built and checked only on this explicit switch.
    [switch]$Melon
)
$ErrorActionPreference = 'Stop'
if (-not $ValidateOnly) { throw 'Use -ValidateOnly. Release archives are assembled separately.' }
if ($Melon -and [string]::IsNullOrWhiteSpace($MelonGameDir)) { throw 'With -Melon also pass -MelonGameDir.' }
if (-not $Melon -and -not [string]::IsNullOrWhiteSpace($MelonGameDir)) { throw '-MelonGameDir is only used with -Melon (0.5.0 is BepInEx only).' }
$loaders = if ($Melon) { @('Melon', 'BepInEx') } else { @('BepInEx') }
$root = $PSScriptRoot
$core = [IO.Path]::GetFullPath($CoreSourceDir)
if (!(Test-Path -LiteralPath (Join-Path $core 'Core\DungeonSettlersDelvers.Core.csproj') -PathType Leaf)) {
    throw 'The Core source checkout of the same version is required. Pass its root with -CoreSourceDir.'
}
$coreProps = [xml](Get-Content -LiteralPath (Join-Path $core 'Directory.Build.props') -Raw)
$frierenProps = [xml](Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw)
# The version is written once, in Directory.Build.props; Frieren builds against the Core source of the same version.
$frierenVersion = @($frierenProps.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($frierenVersion) -or $coreProps.Project.PropertyGroup.Version -ne $frierenVersion) {
    throw "Frieren $frierenVersion requires the Core $frierenVersion source checkout."
}
$nuget = Join-Path $root 'NuGet.Config'
# Every PNG of the Frieren package with its exact width and height, and the sounds: the same tables as the project's
# own release script. The runtime plan, the allowlist and the size check all read these two.
$FrierenAssets = [ordered]@{
    'Frieren_Normal.png' = @(48, 48)
    'Frieren_Stress.png' = @(48, 48)
    'Elfische_Erzmagierin.png' = @(24, 24)
    'Booklover.png' = @(24, 24)
    'Zoltraak_Icon.png' = @(32, 32)
    'Zoltraak_Bruch_Symbol_16x16.png' = @(16, 16)
    'Zoltraak_Zauberkreis_32x32_5Bilder.png' = @(160, 32)
    'Zoltraak_Castimpuls_48x48_8Bilder.png' = @(384, 48)
    'Zoltraak_Strahlsegment_32x12_8Bilder.png' = @(256, 12)
    'Zoltraak_Lichtpunkt_17x17_4Bilder.png' = @(68, 17)
    'Zoltraak_Endring_8x20_4Bilder.png' = @(32, 20)
    'Zoltraak_Einschlag_64x64_4Bilder.png' = @(256, 64)
}
# All PCM 16 bit, stereo, 48 kHz. The release plays exactly these; without them Zoltraak stays silent.
$FrierenSounds = @('Zoltraak_Wirken.wav', 'Zoltraak_Abschuss.wav', 'Zoltraak_Dauerton.wav')
$runtimeProject = Join-Path $root 'Frieren\DungeonSettlersDelvers.Frieren.csproj'
$testProject = Join-Path $root 'Tests\Frieren\FrierenPortrait.Tests.csproj'

# The checks below are the ones of the project's own release script (Assert-PngDimensions with the size check repaired),
# so a public build refuses what that one is meant to refuse.
function Assert-NoPrivateBuildPath([byte[]]$Bytes, [string]$Artifact) {
    foreach ($encoding in @([Text.Encoding]::Latin1, [Text.Encoding]::Unicode, [Text.Encoding]::BigEndianUnicode)) {
        $text = $encoding.GetString($Bytes)
        foreach ($marker in @(('C:' + '\' + 'Users' + '\'), 'Game Copies')) {
            if ($text.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Local user or game-copy path found in package file: $Artifact"
            }
        }
    }
}

function Assert-NoForbiddenPackageEntry([string]$EntryName) {
    $normalized = $EntryName.Replace('\', '/')
    $forbidden = '(?i)(^|/)(GameAssembly\.dll|global-metadata\.dat|Assembly-CSharp\.dll|Il2Cppmscorlib\.dll|Il2CppSystem\.Core\.dll|Il2CppNewtonsoft\.Json\.dll|Newtonsoft\.Json\.dll|MelonLoader\.dll|BepInEx[^/]*\.dll|0Harmony\.dll|Il2CppInterop\.Runtime\.dll|UnityEngine[^/]*\.dll|Unity\.TextMeshPro\.dll|SemanticVersioning\.dll|DungeonSettlersFrierenPortrait\.dll|[^/]+\.pdb|[^/]+\.deps\.json|[^/]+\.dev\.cfg)$|(^|/)(Steam|steamapps|userdata|UserData|Saves|SaveGames)(/|$)|\.(sav|save|savegame|log|dmp|crash)$'
    if ($normalized -match $forbidden) { throw "Forbidden game, loader, legacy, or user-data package entry: $normalized" }
}

function Assert-PngDimensions([string]$Path, [int]$Width, [int]$Height) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $signature = [byte[]](137,80,78,71,13,10,26,10)
    if ($bytes.Length -lt 24) { throw "Runtime PNG is too short: $Path" }
    $hasSignature = $true
    for ($index = 0; $index -lt $signature.Length; $index++) {
        if ($bytes[$index] -ne $signature[$index]) { $hasSignature = $false; break }
    }
    # PowerShell variables ignore case: the file's size must not be called $width or $height, they would overwrite the parameters.
    $actualWidth = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes,16))
    $actualHeight = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes,20))
    if (-not $hasSignature -or $actualWidth -ne $Width -or $actualHeight -ne $Height) {
        throw "Runtime PNG is invalid or has unexpected dimensions ($Width x $Height): $Path"
    }
}

function Assert-WavFormat([string]$Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $ascii = [Text.Encoding]::ASCII
    if ($bytes.Length -lt 44 -or $ascii.GetString($bytes, 0, 4) -ne 'RIFF' -or $ascii.GetString($bytes, 8, 4) -ne 'WAVE') {
        throw "Runtime WAV is not a RIFF WAVE file: $Path"
    }
    $offset = 12
    while ($offset + 8 -le $bytes.Length) {
        $id = $ascii.GetString($bytes, $offset, 4)
        $size = [BitConverter]::ToInt32($bytes, $offset + 4)
        if ($id -eq 'fmt ') {
            $format = [BitConverter]::ToUInt16($bytes, $offset + 8)
            $channels = [BitConverter]::ToUInt16($bytes, $offset + 10)
            $rate = [BitConverter]::ToInt32($bytes, $offset + 12)
            $bits = [BitConverter]::ToUInt16($bytes, $offset + 22)
            if ($format -ne 1 -or $channels -ne 2 -or $rate -ne 48000 -or $bits -ne 16) {
                throw "Runtime WAV must be PCM 16 bit, stereo, 48 kHz (format $format, $channels ch, $rate Hz, $bits bit): $Path"
            }
            return
        }
        if ($size -lt 0) { break }
        $offset += 8 + $size + ($size % 2)
    }
    throw "Runtime WAV has no fmt chunk: $Path"
}

# The runtime files of one loader: the two DLLs from the build output, then the PNGs and sounds from Frieren\Assets,
# each with the entry name it has in the package. The count of the allowlist is the length of this plan, written nowhere else.
function New-RuntimePlan([string]$Loader) {
    $bin = Join-Path $root "bin\$Loader\Release\net6.0"
    $adapter = "DungeonSettlersDelvers.Frieren.$(if ($Loader -eq 'Melon') { 'MelonLoader' } else { 'BepInEx' }).dll"
    if ($Loader -eq 'Melon') {
        $files = @(
            [pscustomobject]@{ Source = Join-Path $bin $adapter; Entry = "Mods/$adapter" }
            [pscustomobject]@{ Source = Join-Path $bin 'DungeonSettlersDelvers.Frieren.dll'; Entry = 'UserLibs/DungeonSettlersDelvers.Frieren.dll' }
        )
        $assetRoot = 'Mods/DungeonSettlersDelvers/Frieren'
    }
    else {
        $pluginRoot = 'BepInEx/plugins/DungeonSettlersDelvers'
        $files = @(
            [pscustomobject]@{ Source = Join-Path $bin $adapter; Entry = "$pluginRoot/$adapter" }
            [pscustomobject]@{ Source = Join-Path $bin 'DungeonSettlersDelvers.Frieren.dll'; Entry = "$pluginRoot/DungeonSettlersDelvers.Frieren.dll" }
        )
        $assetRoot = "$pluginRoot/Frieren"
    }
    foreach ($name in $FrierenAssets.Keys) {
        $files += [pscustomobject]@{ Source = Join-Path $root "Frieren\Assets\$name"; Entry = "$assetRoot/$name" }
    }
    foreach ($name in $FrierenSounds) {
        $files += [pscustomobject]@{ Source = Join-Path $root "Frieren\Assets\Sounds\$name"; Entry = "$assetRoot/Sounds/$name" }
    }
    return ,$files
}

# Every planned file exists and is allowed, PNGs have their size and sounds their format, the build output holds no PDB,
# and a staged copy of the plan holds nothing outside the allowlist.
function Assert-RuntimePlan([object[]]$Plan, [string]$Loader) {
    $entries = @($Plan | ForEach-Object { $_.Entry })
    if (@($entries | Select-Object -Unique).Count -ne $Plan.Count) { throw "$Loader runtime allowlist contains duplicate entries." }
    $pdbFiles = @(Get-ChildItem -LiteralPath (Join-Path $root "bin\$Loader\Release\net6.0") -Filter '*.pdb' -File -Recurse -ErrorAction SilentlyContinue)
    if ($pdbFiles.Count -gt 0) { throw "$Loader Release output unexpectedly contains PDB files: $($pdbFiles.Name -join ', ')" }
    foreach ($file in $Plan) {
        Assert-NoForbiddenPackageEntry $file.Entry
        if (!(Test-Path -LiteralPath $file.Source -PathType Leaf)) { throw "Missing $Loader runtime file: $($file.Source)" }
        Assert-NoPrivateBuildPath ([IO.File]::ReadAllBytes($file.Source)) $file.Entry
        if ($file.Entry -like '*.png') {
            $name = $file.Entry.Substring($file.Entry.LastIndexOf('/') + 1)
            if (-not $FrierenAssets.Contains($name)) { throw "$Loader runtime plan has a PNG without a known size: $($file.Entry)" }
            Assert-PngDimensions $file.Source $FrierenAssets[$name][0] $FrierenAssets[$name][1]
        }
        elseif ($file.Entry -like '*.wav') {
            Assert-WavFormat $file.Source
        }
    }
    $stage = Join-Path $tempRoot "staged-$Loader"
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    foreach ($file in $Plan) {
        $destination = Join-Path $stage $file.Entry.Replace('/', '\')
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.Source -Destination $destination
    }
    $actual = @(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object { $_.FullName.Substring($stage.Length + 1).Replace('\', '/') })
    if ($actual.Count -ne $Plan.Count -or @($actual | Where-Object { $_ -notin $entries }).Count -gt 0) {
        throw "$Loader runtime staging contains files outside its exact allowlist."
    }
}
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
foreach ($loader in $loaders) {
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
    $plan = New-RuntimePlan $loader
    Assert-RuntimePlan $plan $loader
    "PASS: $loader Frieren build, offline rules and $($plan.Count)-file runtime allowlist (files, PNG sizes, WAV format, no extra files, no PDB, private-path scan)"
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
