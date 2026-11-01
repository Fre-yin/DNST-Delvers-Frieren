# Changelog

## 0.5.0: 01.11.2026

- Benötigt Delvers Core 0.5.0. Version 0.5.0 gibt es nur für BepInEx; die MelonLoader-Fassungen bis 0.4.1 werden archiviert. Mit Core 0.4.1 bleibt Frieren aus und meldet das im Log.
- Neu: Frierens eigener Zauber Zoltraak. Aus einem sich drehenden Zauberkreis kanalisiert Frieren drei Sekunden lang einen Strahl, der mit 15 Treffern zu je 40 % ihres Magieangriffs den ersten Gegner in einer Linie trifft. Ein Hindernis davor fängt ihn ab; zerstörbare Hindernisse nehmen dabei Schaden, auch eigene Gebäude, Bäume und Erz. Jeder Treffer gibt einen Stapel Zoltraakbruch, der die physische und magische Resistenz senkt. Der Strahl läuft in einen Lichtpunkt aus, und Wirken, Abschuss und Strahl haben eigene Töne. Frieren erhält den Zauber auch in bestehenden Kampagnen beim nächsten Laden.
- Wichtig: Ein Spielstand, der mit 0.5.0 gespeichert wurde, enthält Zoltraak. Mit Frieren 0.4.x oder ganz ohne Frieren lässt er sich nicht mehr sauber laden, und Core sperrt dann das Speichern. Sichere deine Spielstände vor dem Update.
- Behoben: Wer beim Start einer neuen Kampagne Werte oder Traits einer Figur sperrte und neu würfelte, konnte Frieren erhalten, und ihre festen Werte überschrieben dann die Sperre. Bei einer aktiven Sperre wird Frieren jetzt nicht mehr gezogen, außer der Platz ist auf ihr Profil festgelegt.
- Behoben: Frieren konnte nach einem Absturz beim Rekrutieren für immer verschwinden. Dafür braucht Frieren Core 0.5.0.
- Neu: Beim ersten Rekrutieren schreibt Frieren ins Log, welche anderen Mods dieselben Stellen des Spiels verändern. Das hilft bei Fehlerberichten.
- Behoben: Schlug nach dem Laden der Abgleich von Frierens Zaubern fehl, blieb das Speichern bis zum Neustart gesperrt. Der Fehler wird jetzt nur gemeldet.
- Frierens Name erscheint in jeder Spielsprache in der dort üblichen Schrift: 프리렌, Фрирен, 芙莉莲, 芙莉蓮 und フリーレン. In den übrigen Sprachen heißt sie weiterhin Frieren.
- Leistung: Frieren prüft ihre Registrierung nicht mehr in jedem Frame, sondern nur noch etwa alle zwei Sekunden als Rückfall. Im Leerlauf legt sie damit keinen Speicher mehr an.
- Leistung: Frieren hängt sich nicht mehr in jede Textabfrage des Spiels ein. Ihre Texte stehen ohnehin in der Texttabelle. Umgeleitet werden nur noch drei Abfragen, alle für Frierens Namen.
- Robuster bei Spielupdates: Frieren liest die Werte aus Spielmethoden jetzt über deren Namen statt über ihre Position. Schiebt ein Update dort neue Werte ein, greift sie nicht mehr unbemerkt auf den falschen Wert zu.
- Geprüft unter BepInEx 6.0.0-be.788 auf DS_B.0.4.29, zusammen mit Extended Hotbar 1.0.2: 127 Frieren-Prüfungen ohne Buildwarnungen. Im Spiel wurden Testspielstände geladen, gespeichert und erneut geladen, und Frieren hat Zoltraak gewirkt.

## 0.4.1: 10.10.2026

- Angepasst an das Spielupdate DS_B.0.4.29 (Steam Version 25846389). Benötigt Delvers Core 0.4.1 mit API 1.4.0. Installiere beide zusammen.
- Behoben: Unter BepInEx lud Frieren 0.4.0 nur zusammen mit genau Core 0.4.0. Frieren verlangt jetzt Core ab Version 0.4.1, damit spätere Core Patches ohne neues Frieren laufen. MelonLoader war nicht betroffen.
- Die Startmeldung im Log nennt jetzt die Spielversion, die der installierte Core geprüft hat.
- Geprüft unter MelonLoader 0.7.3 und BepInEx 6.0.0-be.788 auf DS_B.0.4.29: 47 Frieren-Prüfungen je Loader ohne Buildwarnungen. Im Spiel wurden Testspielstände mit alten Keys und fehlenden festen Traits geladen, gespeichert und erneut geladen, zusammen mit Extended Hotbar 1.0.2. Frierens Startausrüstung und der Schutz von Core vor beschädigten Spielständen wurden ebenfalls geprüft.

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
