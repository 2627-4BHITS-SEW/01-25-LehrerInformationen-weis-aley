// "using" macht fertige Bibliotheken verfügbar, damit man ihre Klassen direkt verwenden kann.
using System.Collections;                 // für IEnumerator (Rückgabetyp der Coroutine)
using System.Collections.Generic;         // für List<...> (Liste, die beliebig wachsen kann)
using System.Text.RegularExpressions;     // für Regex und Match (Text mit Mustern durchsuchen)
using UnityEngine;                        // Grundfunktionen von Unity (MonoBehaviour, Debug.Log, ...)
using UnityEngine.Networking;             // für UnityWebRequest (Webseiten herunterladen)

// Lädt die Lehrer von der Homepage der HTL Salzburg und schreibt sie auf die Türschilder.
//
// ": MonoBehaviour" bedeutet: Die Klasse erbt von der Unity-Basisklasse MonoBehaviour.
// Nur dadurch kann das Skript an einem GameObject in der Szene hängen,
// Unity ruft automatisch Start() auf, und StartCoroutine() steht zur Verfügung.
public class LehrerLader : MonoBehaviour
{
    // Die Grundadresse der Homepage. Die einzelnen Seiten werden später hinten angehängt.
    string homepage = "https://www.htl-salzburg.ac.at";

    // ---------------------------------------------------------------------------------
    // REGEX-MUSTER
    // Eine Regex ist ein Suchmuster für Text. Runde Klammern ( ) bilden "Gruppen":
    // Den Text, der in einer Gruppe gefunden wird, kann man danach einzeln auslesen.
    //
    // Im C#-String gibt es zwei Besonderheiten:
    //   \"  ist ein Anführungszeichen im Text (ohne \ wäre der String dort zu Ende)
    //   \\  ist ein einzelner Backslash, \\d kommt also bei der Regex als \d an
    // ---------------------------------------------------------------------------------

    // So sieht ein Lehrer in der Lehrerliste im HTML aus:
    // <a class="value" href="/lehrerinnen-details/schweiberer-franz-prof-dipl-ing-c-205.html"><span class="text">Schweiberer Franz, Prof. Dipl.-Ing.</span></a>
    //
    // Gruppe 1 = Link, Gruppe 2 = Name
    //
    // Das Muster Stück für Stück:
    //   href="                         dieser Text muss genau so vorkommen
    //   (/lehrerinnen-details/[^"]+)   GRUPPE 1: der Link
    //                                  [^"]  = jedes Zeichen außer einem Anführungszeichen
    //                                  +     = ein oder mehrere davon
    //                                  Die Suche stoppt also beim schließenden "
    //   "><span class="text">          dieser Text muss genau so vorkommen
    //   ([^<]+)                        GRUPPE 2: der Name, alle Zeichen bis zum nächsten <
    //   </span>                        das Ende des Namens
    Regex lehrerRegex = new Regex("href=\"(/lehrerinnen-details/[^\"]+)\"><span class=\"text\">([^<]+)</span>");

    // Die Raumnummer steht am Ende vom Link: "...-c-205.html" oder "...-c205.html"
    // Gruppe 1 = Trakt (Buchstabe), Gruppe 2 = Nummer (3 Ziffern)
    //
    // Das Muster Stück für Stück:
    //   -          ein Bindestrich
    //   ([a-z])    GRUPPE 1: genau ein Kleinbuchstabe, der Trakt
    //   -?         ein optionaler Bindestrich (? = 0 oder 1 Mal), deshalb passen beide Schreibweisen
    //   (\d{3})    GRUPPE 2: genau 3 Ziffern (\d = Ziffer, {3} = genau 3 Mal)
    //   \.html     der Text ".html" (\. = ein echter Punkt, ein normaler . wäre "irgendein Zeichen")
    //   $          hier muss der Text zu Ende sein, der Raum steht also ganz am Ende vom Link
    Regex raumRegex = new Regex("-([a-z])-?(\\d{3})\\.html$");

    // So sehen Sprechstunde und Durchwahl auf der Detailseite aus:
    // <div class="label">Sprechstunde:</div>
    // <div class="value"> <span class="text">Freitag 09:35 - 10:35 Uhr</span></div>
    //
    // Das Muster Stück für Stück:
    //   Sprechstunde:</div>         dieser Text muss genau so vorkommen
    //   \s*                         beliebig viele Leerzeichen oder Zeilenumbrüche (\s = Leerraum, * = 0 oder mehr)
    //   <div class="value">         dieser Text muss genau so vorkommen
    //   \s*                         wieder beliebig viel Leerraum
    //   <span class="text">         dieser Text muss genau so vorkommen
    //   ([^<]*)                     GRUPPE 1: der gesuchte Wert, alle Zeichen bis zum nächsten <
    //                               * statt +, damit auch ein leeres Feld passt
    //   </span>                     das Ende des Werts
    Regex sprechstundeRegex = new Regex("Sprechstunde:</div>\\s*<div class=\"value\">\\s*<span class=\"text\">([^<]*)</span>");

    // Genau derselbe Aufbau wie oben, nur mit "Tel-DW:" (Telefon-Durchwahl) am Anfang.
    Regex durchwahlRegex = new Regex("Tel-DW:</div>\\s*<div class=\"value\">\\s*<span class=\"text\">([^<]*)</span>");

    // Start() wird von Unity automatisch einmal aufgerufen, wenn die Szene startet.
    void Start()
    {
        // Eine Coroutine kann auf den Download warten, ohne dass das Programm hängen bleibt.
        // Wichtig: Sie muss mit StartCoroutine() gestartet werden.
        // Ein normaler Aufruf Laden(); würde den Code darin nicht ausführen.
        StartCoroutine(Laden());
    }

    // IEnumerator als Rückgabetyp macht die Methode zu einer Coroutine:
    // Sie kann bei "yield return" pausieren und später an derselben Stelle weitermachen.
    // Unity läuft währenddessen normal weiter, das Fenster friert also nicht ein.
    IEnumerator Laden()
    {
        // Alle Türschilder in der Szene suchen.
        // <Tuerschild> gibt an, welche Komponente gesucht wird.
        // FindObjectsSortMode.None bedeutet: Die Reihenfolge ist egal, das ist am schnellsten.
        Tuerschild[] schilder = FindObjectsByType<Tuerschild>(FindObjectsSortMode.None);

        // Auf jedes Schild "Lädt ..." schreiben, damit man sieht, dass etwas passiert.
        foreach (Tuerschild schild in schilder)
        {
            schild.ZeigeMeldung("Lädt ...");
        }

        // 1. Lehrerliste herunterladen

        // Die Anfrage wird hier nur vorbereitet, es wird noch nichts geschickt.
        // Die Adresse ergibt zusammen: https://www.htl-salzburg.ac.at/lehrerinnen.html
        UnityWebRequest anfrage = UnityWebRequest.Get(homepage + "/lehrerinnen.html");

        // SendWebRequest() schickt die Anfrage ab.
        // "yield return" pausiert die Coroutine hier, bis die Antwort da ist.
        yield return anfrage.SendWebRequest();

        // Hat es geklappt? Ohne Internet oder bei einer falschen Adresse (z.B. 404) ist das nicht Success.
        if (anfrage.result != UnityWebRequest.Result.Success)
        {
            // Den Fehler in die Unity-Konsole schreiben, das Programm läuft trotzdem weiter.
            Debug.Log("Fehler beim Laden: " + anfrage.error);

            // Auf allen Schildern die Fehlermeldung anzeigen.
            foreach (Tuerschild schild in schilder)
            {
                schild.ZeigeMeldung("Keine Verbindung");
            }

            // "yield break" beendet die Coroutine sofort, ohne Daten geht es nicht weiter.
            yield break;
        }

        // Der gesamte HTML-Text der Seite als ein einziger langer String.
        string html = anfrage.downloadHandler.text;

        // Die Anfrage wird nicht mehr gebraucht, also ihren Speicher wieder freigeben.
        anfrage.Dispose();

        // 2. Mit Regex alle Lehrer aus dem HTML herausholen

        // Eine leere Liste, in die alle gefundenen Lehrer kommen.
        // Eine Liste passt hier besser als ein Array, weil die Anzahl vorher nicht bekannt ist.
        List<Lehrer> alleLehrer = new List<Lehrer>();

        // Matches() liefert alle Stellen im HTML, auf die das Muster passt.
        // Die Schleife läuft also einmal pro Lehrer in der Liste.
        foreach (Match treffer in lehrerRegex.Matches(html))
        {
            // Ein neues, noch leeres Lehrer-Objekt anlegen.
            Lehrer lehrer = new Lehrer();

            // Groups[1] ist die erste Klammer im Muster, also der Link.
            lehrer.link = treffer.Groups[1].Value;

            // Groups[2] ist die zweite Klammer, also z.B. "Schweiberer Franz, Prof. Dipl.-Ing."
            // Split(',') teilt den Text beim Beistrich in mehrere Teile.
            // [0] nimmt den ersten Teil: "Schweiberer Franz". Der Titel fällt damit weg.
            lehrer.name = treffer.Groups[2].Value.Split(',')[0];

            // Im Link nach der Raumnummer suchen. Match() liefert nur den ersten Treffer.
            Match raum = raumRegex.Match(lehrer.link);

            // Success ist false, wenn kein Raum im Link steht.
            // Dann bleibt lehrer.raum leer, und der Lehrer passt später auf kein Schild.
            if (raum.Success)
            {
                // ToUpper() macht aus "c" ein "C", dann wird die Nummer angehängt: "c" + "205" -> "C205"
                // So sieht die Raumnummer genauso aus wie im Inspector.
                lehrer.raum = raum.Groups[1].Value.ToUpper() + raum.Groups[2].Value;
            }

            // Den fertigen Lehrer in die Liste geben.
            alleLehrer.Add(lehrer);
        }

        // Kontrollausgabe in der Konsole, z.B. "268 Lehrer gefunden".
        // Steht hier 0, passt die Regex wahrscheinlich nicht mehr zum HTML der Homepage.
        Debug.Log(alleLehrer.Count + " Lehrer gefunden");

        // 3. Für jedes Türschild die passenden Lehrer suchen
        foreach (Tuerschild schild in schilder)
        {
            // Für jedes Schild eine eigene, neue Liste.
            List<Lehrer> lehrerImRaum = new List<Lehrer>();

            // Alle Lehrer durchgehen und die behalten, deren Raum zum Schild passt.
            foreach (Lehrer lehrer in alleLehrer)
            {
                // Der Vergleich ist exakt: "b304" oder "B304 " im Inspector würde nicht passen.
                if (lehrer.raum == schild.raumnummer)
                {
                    lehrerImRaum.Add(lehrer);
                }
            }

            // 4. Für diese Lehrer die Detailseite laden (Sprechstunde und Durchwahl)
            // Nur für die Lehrer im Raum, das spart viele Anfragen gegenüber allen Lehrern.
            foreach (Lehrer lehrer in lehrerImRaum)
            {
                // lehrer.link beginnt mit "/lehrerinnen-details/...", die Adresse ist damit vollständig.
                UnityWebRequest detail = UnityWebRequest.Get(homepage + lehrer.link);

                // Wieder pausieren, bis diese eine Seite da ist.
                // Die Seiten werden also nacheinander geladen, nicht gleichzeitig.
                yield return detail.SendWebRequest();

                // Schlägt nur diese eine Seite fehl, bricht nicht alles ab.
                // Sprechstunde und Durchwahl bleiben dann einfach leer ("").
                if (detail.result == UnityWebRequest.Result.Success)
                {
                    string detailHtml = detail.downloadHandler.text;

                    // Die Hilfsmethode Suche() steht ganz unten in dieser Klasse.
                    lehrer.sprechstunde = Suche(sprechstundeRegex, detailHtml);
                    lehrer.durchwahl = Suche(durchwahlRegex, detailHtml);
                }

                // Auch diese Anfrage wieder freigeben, egal ob sie geklappt hat oder nicht.
                detail.Dispose();
            }

            // 5. Alles auf das Schild schreiben
            // Den Text baut das Türschild selbst zusammen (siehe Tuerschild.cs).
            // Da das hier in der Schleife passiert, füllen sich die Schilder eines nach dem anderen.
            schild.ZeigeLehrer(lehrerImRaum);
        }
    }

    // Gibt den Text aus Gruppe 1 zurück, oder "" wenn nichts gefunden wurde.
    // Eine eigene Methode, weil Sprechstunde und Durchwahl genau gleich gesucht werden.
    // So muss derselbe Code nicht zweimal dastehen.
    string Suche(Regex regex, string text)
    {
        // Den ersten Treffer im Text suchen.
        Match treffer = regex.Match(text);

        if (treffer.Success)
        {
            // Trim() entfernt Leerzeichen und Zeilenumbrüche am Anfang und am Ende.
            string wert = treffer.Groups[1].Value.Trim();

            if (wert == "-")
            {
                return "";   // "-" bedeutet auf der Homepage: nichts eingetragen
            }

            return wert;
        }

        // Das Muster wurde nicht gefunden, also leer zurückgeben.
        return "";
    }
}
