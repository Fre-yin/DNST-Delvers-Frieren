# Frieren aus getrennter Quellfassung bauen

[English](BUILDING.en.md)

Diese Quellfassung enthält nur Frieren, seine Adapter, Tests und vier Laufzeit-PNGs. Der Core-Code wird nicht dupliziert. Benötigt werden die öffentliche Core-0.4.0-Quelle mit API 1.4.0, PowerShell 7, ein .NET SDK mit .NET-6-Targeting-Pack sowie je eine initialisierte lokale Spielkopie für MelonLoader 0.7.3 und BepInEx 6 Unity IL2CPP x64.

```powershell
$CoreSourceDir = Read-Host 'Pfad zur Core-0.4.0-Quellfassung'
$MelonGameDir = Read-Host 'Pfad zur MelonLoader-Spielkopie'
$BepInExGameDir = Read-Host 'Pfad zur BepInEx-Spielkopie'
pwsh -File .\Build-Release.ps1 -ValidateOnly `
  -CoreSourceDir $CoreSourceDir `
  -MelonGameDir $MelonGameDir `
  -BepInExGameDir $BepInExGameDir
```

Das Skript prüft die Core- und Frieren-Paketversion, restauriert offline über `NuGet.Config`, baut Frieren und beide Adapter mit einem Projektverweis auf die separate Core-Quelle und führt die Frieren-Regeltests für beide Loader aus. Es prüft die beiden Frieren-DLLs und vier PNGs. Im Release-Build wurden 47 von 47 Frieren-Prüfungen je Loader ohne Buildwarnungen oder Fehler bestanden. `-ValidateOnly` erstellt kein ZIP, installiert nichts und veröffentlicht nichts.

Frieren enthält keine Core-DLL im Installationspaket. Die PNGs unter `Frieren/Assets/` sind die einzigen mitgelieferten Grafiken. Spiel- und Loader-Referenzen werden lokal über `GameReferences.props` gelesen und nicht verteilt.

Build-Ausgaben liegen in `bin/<Loader>/Release/net6.0/`; Zwischenstände in `obj/<Loader>/`. Diese Ordner gehören nicht ins Git-Repository. Ein Offline-Build ersetzt keine Ingame-Abnahme.
