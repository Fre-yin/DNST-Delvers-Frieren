# Dungeon Settlers Delvers: Frieren

**Deutsch** | [English](README.en.md)

Frieren ist ein zusätzliches Charakterpaket für Dungeon Settlers Delvers. Du kannst sie im Modus „Eigene Expedition“ und über die Gilde als einzigartige Elfenmagierin rekrutieren. Sie hat eine eigene Biografie, Porträts, den legendären Hintergrund „Elfische Erzmagierin“ und die Eigenschaft „Bücherwurm“. Ihre Geschichte verbindet die Suche nach seltenen Zaubern mit einer Expedition zum erwachten Dungeonkern.

**Version 0.5.0 benötigt Delvers Core 0.5.0.** Installiere beide zusammen und Core zuerst. Unter [Releases](../../releases) findest du `DelversFrieren-0.5.0-BepInEx.zip`. Version 0.5.0 gibt es nur für BepInEx; die MelonLoader-Fassungen bis 0.4.1 werden archiviert. Die automatisch von GitHub erzeugten Quellarchive lassen sich nicht als Mod installieren.

## Frieren im Spiel

Frieren erscheint in „Eigene Expedition“ und im Gildenpool. In der Gilde bezahlst du ihre Rekrutierung mit Technikbüchern. Der Preis entspricht einem Buch je angefangene 150 Gold des normalen Gildenpreises, mindestens aber einem Buch. Core verhindert, dass Frieren durch erneutes Auswürfeln mehrfach als einzigartige Kandidatin erscheint. Wenn du sie in einer Kampagne bereits rekrutiert hast, bleibt das auch nach ihrem Tod vermerkt. Sie wird dann nicht noch einmal erzeugt.

Der legendäre Hintergrund „Elfische Erzmagierin“ ersetzt ihren gewöhnlichen Magierhintergrund. Damit kann sie Feuermagie, Wassermagie und Naturmagie mit den passenden Stäben einsetzen. Ihre maximale Energie steigt um 50 und ihre Cooldownreduktion um 20. Dafür verursacht sie 40 weniger physischen Schaden. Ihre Hauptskillbäume sind Feuermagie, Wassermagie und Naturmagie. Als Nebenbäume stehen Burst, Tide und Harmony zur Verfügung. Diese Zauber stammen aus dem Spiel. Das Paket enthält keine kopierten Zaubergrafiken.

Dazu kommt Frierens eigener Zauber Zoltraak. Aus einem sich drehenden Zauberkreis kanalisiert sie drei Sekunden lang einen Strahl, der mit 15 Treffern zu je 40 % ihres Magieangriffs den ersten Gegner in einer Linie trifft. Ein Teil jedes Treffers geht sicher auf den Kopf. Ein Hindernis davor fängt den Strahl ab; zerstörbare Hindernisse nehmen dabei Schaden, auch eigene Gebäude, Bäume und Erz. Jeder Treffer gibt einen Stapel Zoltraakbruch, der die physische und die magische Resistenz für 12 Sekunden senkt, höchstens 30 Stapel. Betäubung oder Bewusstlosigkeit brechen den Zauber ab. Danach braucht er 10 Sekunden, bis er wieder bereit ist. Wirken, Abschuss und Strahl haben eigene Töne. Frieren lernt Zoltraak auch in bestehenden Kampagnen beim nächsten Laden.

Frieren beginnt je nach Kandidatenstufe auf Level 1 bis 8. Ihre Grundwerte sind Intelligenz 17, Willenskraft 16, Stärke 11, Konstitution 12, Beweglichkeit 12 und Wahrnehmung 13. Intelligenz und Willenskraft haben Genius Talente, Stärke Poor und die übrigen Werte Moderate. Auf höheren Startleveln verwendet sie feste Würfe über den normalen Levelaufstieg des Spiels. Kleidung, Kopfschutz und Feuerstab passen sich über drei Ausrüstungsstufen an. Anschließend berechnet das Spiel ihren Preis in der Gilde.

„Bücherwurm“ ist eine gewöhnliche individuelle Eigenschaft. Benutzt eine Figur mit dieser Eigenschaft ein Technikbuch aus ihrem Inventar, wird das Buch verbraucht. Sie erhält dafür zwei Hauptskillpunkte, zwei Sekundärskillpunkte und für 20 Minuten vier Punkte bessere Laune. Die Eigenschaft kann auch bei Figuren der sieben ursprünglichen Völker erscheinen. Sie gibt keine zusätzlichen Punkte im Schlaf und keinen dauerhaften Bonus auf Skillpunkte.

Das Paket enthält zwölf PNG Dateien und drei Töne: zwei Porträts, die Bilder für den legendären Hintergrund und „Bücherwurm“, acht eigene Bilder für Zoltraak, nämlich Symbol, Symbol des Bruchs, Zauberkreis, Lichtimpuls, Strahl, Ringe, Lichtpunkt und Einschlag, sowie die Töne für Wirken, Abschuss und Strahl. Frierens Figur in der Spielwelt verwendet vorhandene Grafiken des installierten Spiels. Diese Grafiken und die Arbeitsdateien sind nicht im Paket. Neue Kandidatinnen erhalten die Biografie sofort. Bei älteren Spielständen ergänzt Frieren nur eine leere Biografie; selbst geschriebener Text bleibt erhalten.

## Voraussetzungen und Installation

1. Du brauchst Windows x64, Dungeon Settlers DS_B.0.4.29 mit der geprüften Signatur der Steam Version 25846389 und Delvers Core 0.5.0.
2. Installiere BepInEx 6 für Unity IL2CPP x64 (getestet mit 6.0.0-be.788). Hast du bisher MelonLoader benutzt, entferne zuerst die alten Core- und Frieren-Dateien aus `Mods/` und `UserLibs/`.
3. Falls du eine ältere Version von Frieren benutzt hast, entferne `DungeonSettlersFrierenPortrait.dll` aus `BepInEx/plugins/`, `Mods/` oder `UserLibs/`.
4. Sichere deine Spielstände und entpacke das passende ZIP in den Spielordner. Bei BepInEx liegen beide DLLs danach in `BepInEx/plugins/DungeonSettlersDelvers/` und die Bilder und Töne im Unterordner `Frieren/`.

Ab 0.4.0 tragen Frierens gespeicherte Keys ein neues Präfix. Ältere Spielstände stellt Frieren beim Laden automatisch um. Sobald du einen Spielstand mit 0.4.0 gespeichert hast, lässt er sich mit Frieren 0.3.x nicht mehr vollständig laden. Ab 0.5.0 steht Zoltraak im Spielstand. Ein so gespeicherter Spielstand lässt sich mit Frieren 0.4.x oder ohne Frieren nicht mehr sauber laden, und Core sperrt dann das Speichern.

Extended Hotbar 1.0.2 ist optional. Normale Kampagnen wurden mit Core, Frieren und dieser Version der Hotbar unter beiden Loadern geladen. Andere Kombinationen von Mods wurden damit nicht allgemein bestätigt.

## Prüfung und Rechte

Die getrennte Quellfassung wurde mit Core für BepInEx ohne Warnungen oder Fehler gebaut, und 127 Prüfungen für Frierens Regeln bestanden. Für 0.5.0 wurden unter BepInEx auf DS_B.0.4.29 Testspielstände geladen, gespeichert und erneut geladen, zusammen mit den Daten von Core und Extended Hotbar 1.0.2. Dabei hat Frieren Zoltraak gewirkt: Der Strahl erschien, verschwand nach dem Kanal wieder, und die Abklingzeit begann. Für 0.4.1 wurden unter beiden Loadern auf DS_B.0.4.29 Testspielstände mit alten Keys und fehlenden festen Traits geladen, gespeichert und erneut geladen. Geprüft wurden dabei auch Frierens Startausrüstung, Extended Hotbar 1.0.2 und der Schutz von Core vor beschädigten Spielständen. Für 0.4.0 wurden unter beiden Loadern Spielstände mit den alten Keys und mit fehlenden festen Traits geladen und gespeichert; danach waren nur noch die neuen Keys und alle drei festen Traits gespeichert. Unter MelonLoader und BepInEx wurden eine neue „Eigene Expedition“ mit Frieren als Startfigur und das Laden einer bestehenden Kampagne mit Frieren im Spiel geprüft. Ihre Biografie wurde im Rekrutierungsfenster unter MelonLoader geprüft. Unter BepInEx ließ sich eine normale Kampagne laden; dabei wurde auch eine ältere, leere Frieren Biografie ergänzt. Unter beiden Loadern hat außerdem eine Figur mit „Bücherwurm“ ein Technikbuch gelesen und einen Hartkeks gegessen, beides ohne Warnung im Log. Quickload und weitere Fälle bei der Übernahme älterer Spielstände sind noch nicht vollständig im Spiel geprüft.

Eigener Code und eigene Anleitungen stehen unter MIT. Frieren, Dungeon Settlers, Figuren, Designs, Vorlagen aus dem Spiel und Marken sind davon ausgenommen. Einzelheiten stehen in [LICENSING.txt](LICENSING.txt) und [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Dies ist ein inoffizielles Communityprojekt und stammt nicht von CanOpener. Eine Anleitung zum Bauen der Quellen steht in [BUILDING.md](BUILDING.md).
