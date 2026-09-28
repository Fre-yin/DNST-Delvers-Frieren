# Dungeon Settlers Delvers: Frieren

**Deutsch** | [English](README.en.md)

Frieren ist ein zusätzliches Charakterpaket für Dungeon Settlers Delvers. Du kannst sie im Modus „Eigene Expedition“ und über die Gilde als einzigartige Elfenmagierin rekrutieren. Sie hat eine eigene Biografie, Porträts, den legendären Hintergrund „Elfische Erzmagierin“ und die Eigenschaft „Bücherwurm“. Ihre Geschichte verbindet die Suche nach seltenen Zaubern mit einer Expedition zum erwachten Dungeonkern.

**Version 0.3.1 benötigt Delvers Core 0.3.0 mit API 1.3.0.** Installiere Core zuerst und verwende für beide Mods denselben Loader. Unter [Releases](../../releases) findest du `DelversFrieren-0.3.1-MelonLoader.zip` und `DelversFrieren-0.3.1-BepInEx.zip`. Lade nur das Paket für deinen Loader herunter. Die automatisch von GitHub erzeugten Quellarchive lassen sich nicht als Mod installieren.

## Frieren im Spiel

Frieren erscheint in „Eigene Expedition“ und im Gildenpool. In der Gilde bezahlst du ihre Rekrutierung mit Technikbüchern. Der Preis entspricht einem Buch je angefangene 150 Gold des normalen Gildenpreises, mindestens aber einem Buch. Core verhindert, dass Frieren durch erneutes Auswürfeln mehrfach als einzigartige Kandidatin erscheint. Wenn du sie in einer Kampagne bereits rekrutiert hast, bleibt das auch nach ihrem Tod vermerkt. Sie wird dann nicht noch einmal erzeugt.

Der legendäre Hintergrund „Elfische Erzmagierin“ ersetzt ihren gewöhnlichen Magierhintergrund. Damit kann sie Feuermagie, Wassermagie und Naturmagie mit den passenden Stäben einsetzen. Ihre maximale Energie steigt um 50 und ihre Cooldownreduktion um 20. Dafür verursacht sie 40 weniger physischen Schaden. Ihre Hauptskillbäume sind Feuermagie, Wassermagie und Naturmagie. Als Nebenbäume stehen Burst, Tide und Harmony zur Verfügung. Frieren nutzt die Zauber des Spiels. Das Paket enthält keine kopierten Zaubergrafiken.

Frieren beginnt je nach Kandidatenstufe auf Level 1 bis 8. Ihre Grundwerte sind Intelligenz 17, Willenskraft 16, Stärke 11, Konstitution 12, Beweglichkeit 12 und Wahrnehmung 13. Intelligenz und Willenskraft haben Genius Talente, Stärke Poor und die übrigen Werte Moderate. Auf höheren Startleveln verwendet sie feste Würfe über den normalen Levelaufstieg des Spiels. Kleidung, Kopfschutz und Feuerstab passen sich über drei Ausrüstungsstufen an. Anschließend berechnet das Spiel ihren Preis in der Gilde.

„Bücherwurm“ ist eine gewöhnliche individuelle Eigenschaft. Benutzt eine Figur mit dieser Eigenschaft ein Technikbuch aus ihrem Inventar, wird das Buch verbraucht. Sie erhält dafür zwei Hauptskillpunkte, zwei Sekundärskillpunkte und für 20 Minuten vier Punkte bessere Laune. Die Eigenschaft kann auch bei Figuren der sieben ursprünglichen Völker erscheinen. Sie gibt keine zusätzlichen Punkte im Schlaf und keinen dauerhaften Bonus auf Skillpunkte.

Das Paket enthält vier PNG Dateien: zwei Porträts sowie die Bilder für den legendären Hintergrund und „Bücherwurm“. Frierens Figur in der Spielwelt verwendet vorhandene Grafiken des installierten Spiels. Diese Grafiken und die Arbeitsdateien sind nicht im Paket. Neue Kandidatinnen erhalten die Biografie sofort. Bei älteren Spielständen ergänzt Frieren nur eine leere Biografie; selbst geschriebener Text bleibt erhalten.

## Voraussetzungen und Installation

1. Du brauchst Windows x64, Dungeon Settlers DS_B.0.4.23 mit der geprüften Signatur der Steam Versionen 25269660 oder 25284551 und Delvers Core 0.3.0 mit API 1.3.0.
2. Installiere MelonLoader 0.7.3 oder BepInEx 6 für Unity IL2CPP x64. Core und Frieren müssen denselben Loader verwenden. Installiere nicht beide Loader gleichzeitig.
3. Falls du eine ältere Version von Frieren benutzt hast, entferne `DungeonSettlersFrierenPortrait.dll` aus `Mods/`, `UserLibs/` oder `BepInEx/plugins/`.
4. Sichere deine Spielstände und entpacke das passende ZIP in den Spielordner. Bei MelonLoader liegt der Adapter danach in `Mods/`, die Frieren DLL in `UserLibs/` und die Bilder in `Mods/DungeonSettlersDelvers/Frieren/`. Bei BepInEx liegen beide DLLs in `BepInEx/plugins/DungeonSettlersDelvers/` und die Bilder im Unterordner `Frieren/`.

Extended Hotbar 1.0.1 ist optional. Normale Kampagnen wurden mit Core, Frieren und dieser Version der Hotbar unter beiden Loadern geladen. Andere Kombinationen von Mods wurden damit nicht allgemein bestätigt.

## Prüfung und Rechte

Die getrennte Quellfassung wurde mit Core für MelonLoader und BepInEx ohne Warnungen oder Fehler gebaut. Je Loader liefen 39 Prüfungen für Frierens Regeln. Unter MelonLoader und BepInEx wurden eine neue „Eigene Expedition“ mit Frieren als Startfigur und das Laden einer bestehenden Kampagne mit Frieren im Spiel geprüft. Ihre Biografie wurde im Rekrutierungsfenster unter MelonLoader geprüft. Unter BepInEx ließ sich eine normale Kampagne laden; dabei wurde auch eine ältere, leere Frieren Biografie ergänzt. Quickload und weitere Fälle bei der Übernahme älterer Spielstände sind noch nicht vollständig im Spiel geprüft.

Eigener Code und eigene Anleitungen stehen unter MIT. Frieren, Dungeon Settlers, Figuren, Designs, Vorlagen aus dem Spiel und Marken sind davon ausgenommen. Einzelheiten stehen in [LICENSING.txt](LICENSING.txt) und [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Dies ist ein inoffizielles Communityprojekt und stammt nicht von CanOpener. Eine Anleitung zum Bauen der Quellen steht in [BUILDING.md](BUILDING.md).
