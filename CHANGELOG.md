# Changelog

## 0.3.0: 28.09.2026

- Frieren besitzt eine Biografie in allen zehn Spielsprachen. Leere Biografien älterer Spielstände werden ergänzt, eigene Texte bleiben erhalten.
- Save-Migrationen nutzen die Lade-Callbacks von Core API 1.3.0. Core kann die drei festen Traits von Frieren anhand des Profils wiederherstellen.
- Getrennte MelonLoader- und BepInEx-Pakete mit jeweils zwei Frieren-DLLs und vier Laufzeit-PNGs. Die öffentliche Quellfassung baut gegen die separate Core-Quelle.
