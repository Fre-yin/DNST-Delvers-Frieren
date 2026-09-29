# Changelog

## 0.4.0: 29.09.2026

- Frierens gespeicherte Keys tragen jetzt einheitlich das Präfix `Delvers`, zum Beispiel `AFFECTER_DelversElfArchmage`. Ältere Spielstände werden beim Laden automatisch umgestellt. Dafür meldet Frieren die Umbenennung bei Core an.
- Wichtig: Ein Spielstand, der mit 0.4.0 gespeichert wurde, lässt sich mit Frieren 0.3.x nicht mehr vollständig laden. Sichere deine Spielstände vor dem Update.
- Behoben: Unter BepInEx konnte die Übernahme älterer Frieren-Spielstände fehlerhafte Trait-Einträge schreiben. MelonLoader war nicht betroffen.
- Benötigt Delvers Core 0.4.0 mit API 1.4.0.
- Geprüft: 47 Frieren-Prüfungen je Loader ohne Buildwarnungen. Unter MelonLoader und BepInEx wurden Spielstände mit den alten Keys und mit fehlenden festen Traits geladen und gespeichert. Danach standen nur noch die neuen Keys und alle drei festen Traits im Spielstand.

## 0.3.2: 29.09.2026

- Behoben: Benutzte eine Figur mit „Bücherwurm“ ein anderes Item als ein Technikbuch, zum Beispiel Essen, schrieb Frieren eine unnötige Warnung ins Log. Jetzt wird nur ein benutztes Technikbuch für die Belohnung vorgemerkt. Am Spielverhalten ändert sich nichts.

## 0.3.1: 28.09.2026

- Behoben: Unter BepInEx 6 stürzte das Spiel ab, sobald Frieren als neue Figur erschien, zum Beispiel als Startfigur einer „Eigenen Expedition“. Ihre Startausrüstung legt jetzt das Spiel selbst an. MelonLoader war nicht betroffen.
- Frieren prüft ihre Startausrüstung, bevor sie übernommen wird. Gelingt das nicht, behält sie die vom Spiel erzeugte Ausrüstung und meldet das im Log.
- Ein Build aus einem Git-Klon erzeugt jetzt dieselben DLL-Bytes wie das Release.

## 0.3.0: 28.09.2026

- Frieren besitzt eine Biografie in allen zehn Spielsprachen. Leere Biografien älterer Spielstände werden ergänzt, eigene Texte bleiben erhalten.
- Save-Migrationen nutzen die Lade-Callbacks von Core API 1.3.0. Core kann die drei festen Traits von Frieren anhand des Profils wiederherstellen.
- Getrennte MelonLoader- und BepInEx-Pakete mit jeweils zwei Frieren-DLLs und vier Laufzeit-PNGs. Die öffentliche Quellfassung baut gegen die separate Core-Quelle.
