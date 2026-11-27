# Frieren aus getrennter Quellfassung bauen

[English](BUILDING.en.md)

Diese Quellfassung enthält nur Frieren, seine Adapter, Tests, zwölf Laufzeit-PNGs und drei Töne. Der Core-Code wird nicht dupliziert. Ab 0.5.0 gibt es Delvers nur noch für BepInEx, MelonLoader wird nicht mehr unterstützt. Benötigt werden die öffentliche Quelle von Core 0.5 (0.5.0 oder neuer), PowerShell 7, ein .NET SDK mit .NET-6-Targeting-Pack und eine initialisierte lokale Spielkopie mit BepInEx 6 Unity IL2CPP x64.

```powershell
$CoreSourceDir = Read-Host 'Pfad zur Quellfassung von Core 0.5 (0.5.0 oder neuer)'
$BepInExGameDir = Read-Host 'Pfad zur BepInEx-Spielkopie'
pwsh -File .\Build-Release.ps1 -ValidateOnly -CoreSourceDir $CoreSourceDir -BepInExGameDir $BepInExGameDir
```

Das Skript prüft die Core- und Frieren-Paketversion, restauriert offline über `NuGet.Config`, baut Frieren und den BepInEx-Adapter mit einem Projektverweis auf die separate Core-Quelle und führt die Frieren-Regeltests aus. Es prüft die beiden Frieren-DLLs, zwölf PNGs und drei Töne. Beim letzten Lauf am Stand 0.5.1 bestanden 129 Frieren-Prüfungen ohne Buildwarnungen oder Fehler. `-ValidateOnly` erstellt kein ZIP, installiert nichts und veröffentlicht nichts.

Frieren enthält keine Core-DLL im Installationspaket. Die PNGs unter `Frieren/Assets/` sind die einzigen mitgelieferten Grafiken. Spiel- und Loader-Referenzen werden lokal über `GameReferences.props` gelesen und nicht verteilt.

Build-Ausgaben liegen in `bin/<Loader>/Release/net6.0/`; Zwischenstände in `obj/<Loader>/`. Diese Ordner gehören nicht ins Git-Repository. Ein Offline-Build ersetzt keine Ingame-Abnahme.
