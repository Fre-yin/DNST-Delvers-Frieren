# Building Frieren from its separate source distribution

[Deutsch](BUILDING.md)

This source distribution contains only Frieren, its adapters, tests, twelve runtime PNGs, and three sounds. Core code is not duplicated. From 0.5.0 on, Delvers is available for BepInEx only; MelonLoader is no longer supported. You need the public source of Core 0.5 (0.5.0 or newer), PowerShell 7, a .NET SDK with the .NET 6 targeting pack, and an initialized local game copy with BepInEx 6 Unity IL2CPP x64.

```powershell
$CoreSourceDir = Read-Host 'Path to the source of Core 0.5 (0.5.0 or newer)'
$BepInExGameDir = Read-Host 'Path to the BepInEx game copy'
pwsh -File .\Build-Release.ps1 -ValidateOnly -CoreSourceDir $CoreSourceDir -BepInExGameDir $BepInExGameDir
```

The script checks Core and Frieren package versions, restores offline through `NuGet.Config`, builds Frieren and the BepInEx adapter with a project reference to the separate Core source, and runs the Frieren rule tests. It verifies the two Frieren DLLs, twelve PNGs, and three sounds. In the last run on the 0.5.1 state, 129 Frieren checks passed with zero build warnings or errors. `-ValidateOnly` creates no ZIP, installs nothing, and publishes nothing.

The Frieren installation package contains no Core DLL. The PNGs under `Frieren/Assets/` are the only distributed graphics. Game and loader references are read locally through `GameReferences.props` and are not distributed.

Outputs go to `bin/<Loader>/Release/net6.0/` and intermediates to `obj/<Loader>/`. Neither directory belongs in Git. An offline build is not a substitute for in-game acceptance.
