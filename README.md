# Kammerlplan

Unity-2D-Projekt für SEW an der HTL Salzburg: Nachbau von Gang B und C im 3. Stock mit elektronischen Türschildern. Die Schilder zeigen Raumnummer, Lehrer, Sprechstunde und Telefonnummer. Die Daten kommen per Regex von der Lehrerseite der HTL-Homepage.

## Edge Cases

Diese 5 Fälle können mit der aktuellen Version realistisch auftreten.

### 1. Die Homepage ändert ihr HTML

Die Regex-Muster passen genau auf das heutige HTML, z.B. `<span class="text">`. Wenn die Schule die Website umbaut oder auch nur eine Klasse oder ein Leerzeichen ändert, findet `lehrerRegex` nichts mehr.

- **Folge:** In der Konsole steht „0 Lehrer gefunden“ und auf allen Schildern „Kein Lehrer eingetragen“. Es gibt keine Fehlermeldung, weil technisch alles funktioniert hat.
- **Lösung:** Wenn `alleLehrer.Count == 0`, auf den Schildern „Homepage hat sich geändert“ anzeigen.

### 2. Der Raum steht nicht oder falsch im Link

Der Raum wird aus dem Link gelesen, z.B. `...-c-205.html`. Von 268 Lehrern hatten aber nur 248 einen Raum im Link. Wer keinen Raum hat oder einen anderen Link-Aufbau (z.B. `...-c-205-1.html` bei Namensgleichheit), landet auf keinem Schild.

- **Folge:** Ein Lehrer sitzt in Wirklichkeit in B304, fehlt aber auf dem Schild.
- **Lösung:** Lehrer ohne Raum mit `Debug.Log` ausgeben, damit man sieht, wer fehlt.

### 3. Tippfehler bei der Raumnummer im Inspector

Der Vergleich `lehrer.raum == schild.raumnummer` ist exakt. Steht im Inspector `b304`, `B 304` oder `B304 ` mit Leerzeichen am Ende, passt es nicht.

- **Folge:** Das Schild zeigt „Kein Lehrer eingetragen“, obwohl dort Lehrer sitzen.
- **Lösung:** Vor dem Vergleich `schild.raumnummer.Trim().ToUpper()` verwenden.

### 4. Langsames oder abbrechendes Internet

Für jeden Lehrer wird die Detailseite einzeln und nacheinander geladen. Bei 10 Räumen sind das etwa 33 Anfragen.

- **Langsam:** Bei schlechtem Schul-WLAN dauert es lange, und die Schilder füllen sich eines nach dem anderen.
- **Abbruch:** Fällt eine einzelne Detailseite aus, fehlen bei diesem Lehrer still die Sprechstunde und die Durchwahl.
- **Kein Timeout:** Hängt eine Anfrage, bleibt die Anzeige ewig auf „Lädt ...“.
- **Lösung:** `detail.timeout = 10;` setzen, also 10 Sekunden.

### 5. Zu viel Text oder unterschiedliche Daten im Raum

- **Viel Text:** B304 und B305 haben je 5 Lehrer. Mit Sprechstunden sind das 10 oder mehr Zeilen. Durch Auto-Size wird die Schrift dann sehr klein und eventuell unlesbar.
- **Unterschiedliche Durchwahlen:** Der Code nimmt die erste Durchwahl im Raum. Haben Lehrer im selben Raum verschiedene Nummern, wird nur eine angezeigt.
- **Sonderzeichen:** Namen wie „Schneeweiß“ können im HTML als `&szlig;` stehen. Dann steht genau das auf dem Schild statt „ß“.
- **Lösung:** `System.Net.WebUtility.HtmlDecode(name)` verwenden.

**Bereits abgefangen:** Wenn auf der Homepage bei Sprechstunde oder Durchwahl nur „-“ steht, wird das als leer behandelt (siehe `Suche()` in `LehrerLader.cs`).
