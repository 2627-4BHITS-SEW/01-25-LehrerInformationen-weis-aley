# Requirements und Testing – Kammerlplan

Anwendung: Unity-2D-Plan von Gang B und C im 3. Stock der HTL Salzburg mit elektronischen Türschildern. Die Daten kommen von der Lehrerseite der Homepage (https://www.htl-salzburg.ac.at/lehrerinnen.html).

## 1. Funktionale Requirements

| ID | Requirement |
|---|---|
| FR-01 | Das System muss die Lehrerliste über einen Webrequest von der HTL-Homepage laden. |
| FR-02 | Das System muss jedem Lehrer mit Regex seine Raumnummer zuordnen (z.B. Link `...-c-205.html` → `C205`). |
| FR-03 | Das System muss auf jedem Türschild die Raumnummer und die Namen aller Lehrer in diesem Raum anzeigen. |
| FR-04 | Das System muss zu jedem Lehrer die Sprechstunde von seiner Detailseite laden und auf dem Türschild anzeigen. |
| FR-05 | Das System soll die Telefonnummer des Raums anzeigen (Schulnummer +43 662 453610 und Durchwahl). |

## 2. Nichtfunktionale Requirements

| ID | Requirement |
|---|---|
| NFR-01 | Das System darf bei einem Netzwerkfehler nicht abstürzen und muss eine Meldung auf den Schildern anzeigen. |
| NFR-02 | Die Türschilder müssen gut lesbar sein: Die Raumnummer ist fett, und der Text passt ins Schild. |
| NFR-03 | Alle Türschilder müssen bei normaler Internetverbindung innerhalb von 15 Sekunden nach dem Start befüllt sein. |

## 3. Akzeptanzkriterien

**FR-03 – Raumnummer und Lehrernamen anzeigen**
- Nach dem Laden steht auf dem Schild von B303 in der ersten Zeile fett „B303“.
- Darunter stehen die Namen aller Lehrer, die laut Homepage in B303 sitzen (Brenter, Schulhofer, Strobl), ohne Titel.
- Ein Raum ohne Lehrer zeigt „Kein Lehrer eingetragen“.

**FR-04 – Sprechstunde anzeigen**
- Unter jedem Lehrernamen steht dieselbe Sprechstunde wie auf seiner Detailseite, z.B. „Freitag 09:35 - 10:35 Uhr“.
- Steht auf der Homepage nur „-“, wird keine Sprechstunde angezeigt (keine leere Zeile, kein „-“).

**NFR-01 – Kein Absturz bei Netzwerkfehler**
- Ohne Internet zeigen nach dem Start alle Schilder die Raumnummer und „Keine Verbindung“.
- In der Unity-Konsole erscheint „Fehler beim Laden: …“, aber keine Exception, und das Programm läuft weiter.

## 4. Testfälle

| Test-ID | Art | Beschreibung | Erwartetes Ergebnis | Requirement |
|---|---|---|---|---|
| T-01 | Happy Path | Mit Internet Play drücken und das Schild von B303 ansehen. | Das Schild zeigt fett „B303“, darunter Brenter, Schulhofer und Strobl, jeweils mit Sprechstunde, und unten „Tel. +43 662 453610-…“. In der Konsole steht „… Lehrer gefunden“ mit einer Zahl größer als 0. | FR-01, FR-02, FR-03, FR-04, FR-05 |
| T-02 | Edge Case | Die Raumnummer eines Schildes im Inspector auf einen Raum ohne Lehrer ändern (z.B. „B399“) und Play drücken. | Das Schild zeigt „B399“ und „Kein Lehrer eingetragen“. Es gibt keine Telefonnummer und keinen Fehler. | FR-03 |
| T-03 | Edge Case | Raum mit vielen Lehrern prüfen: Play drücken und das Schild von B304 (5 Lehrer) ansehen. | Alle 5 Namen mit Sprechstunden sind sichtbar. Der Text ragt nicht aus dem Schild, weil Auto-Size die Schrift verkleinert. | FR-03, NFR-02 |
| T-04 | Error Case | Das Internet trennen (WLAN aus) und Play drücken. | Alle Schilder zeigen ihre Raumnummer und „Keine Verbindung“. In der Konsole steht „Fehler beim Laden: …“. Unity stürzt nicht ab. | NFR-01, FR-01 |
| T-05 | Error Case | In `LehrerLader.cs` die Adresse auf eine nicht existierende Seite ändern (z.B. `/lehrerinnen-falsch.html`) und Play drücken. | Der Webrequest liefert einen Fehler (404). Alle Schilder zeigen „Keine Verbindung“ und das Programm läuft weiter. | NFR-01, FR-01 |
