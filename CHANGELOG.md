# Changelog

## 0.3.1: 28.09.2026

- Behoben: Unter BepInEx 6 stürzte das Spiel ab, sobald Frieren als neue Figur erschien, zum Beispiel als Startfigur einer „Eigenen Expedition“. Ihre Startausrüstung legt jetzt das Spiel selbst an. MelonLoader war nicht betroffen.
- Frieren prüft ihre Startausrüstung, bevor sie übernommen wird. Gelingt das nicht, behält sie die vom Spiel erzeugte Ausrüstung und meldet das im Log.
- Ein Build aus einem Git-Klon erzeugt jetzt dieselben DLL-Bytes wie das Release.

## 0.3.0: 28.09.2026

- Frieren besitzt eine Biografie in allen zehn Spielsprachen. Leere Biografien älterer Spielstände werden ergänzt, eigene Texte bleiben erhalten.
- Save-Migrationen nutzen die Lade-Callbacks von Core API 1.3.0. Core kann die drei festen Traits von Frieren anhand des Profils wiederherstellen.
- Getrennte MelonLoader- und BepInEx-Pakete mit jeweils zwei Frieren-DLLs und vier Laufzeit-PNGs. Die öffentliche Quellfassung baut gegen die separate Core-Quelle.
