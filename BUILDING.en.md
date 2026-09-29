# Building Frieren from its separate source distribution

[Deutsch](BUILDING.md)

This source distribution contains only Frieren, its adapters, tests, and four runtime PNGs. Core code is not duplicated. You need the public Core 0.3.0 source with API 1.3.0, PowerShell 7, a .NET SDK with the .NET 6 targeting pack, and initialized local game copies for MelonLoader 0.7.3 and BepInEx 6 Unity IL2CPP x64.

```powershell
$CoreSourceDir = Read-Host 'Path to the Core 0.3.0 source'
$MelonGameDir = Read-Host 'Path to the MelonLoader game copy'
$BepInExGameDir = Read-Host 'Path to the BepInEx game copy'
pwsh -File .\Build-Release.ps1 -ValidateOnly `
  -CoreSourceDir $CoreSourceDir `
  -MelonGameDir $MelonGameDir `
  -BepInExGameDir $BepInExGameDir
```

The script checks Core and Frieren package versions, restores offline through `NuGet.Config`, builds Frieren and both adapters with a project reference to the separate Core source, and runs the Frieren rule tests for both loaders. It verifies the two Frieren DLLs and four PNGs. The Release build passed 43 of 43 Frieren checks per loader with zero build warnings or errors. `-ValidateOnly` creates no ZIP, installs nothing, and publishes nothing.

The Frieren installation package contains no Core DLL. The PNGs under `Frieren/Assets/` are the only distributed graphics. Game and loader references are read locally through `GameReferences.props` and are not distributed.

Outputs go to `bin/<Loader>/Release/net6.0/` and intermediates to `obj/<Loader>/`. Neither directory belongs in Git. An offline build is not a substitute for in-game acceptance.
