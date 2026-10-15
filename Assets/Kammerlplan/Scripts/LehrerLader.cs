using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

// Lädt die Lehrer von der Homepage der HTL Salzburg und schreibt sie auf die Türschilder.
public class LehrerLader : MonoBehaviour
{
    string homepage = "https://www.htl-salzburg.ac.at";

    // So sieht ein Lehrer in der Lehrerliste im HTML aus:
    // <a class="value" href="/lehrerinnen-details/schweiberer-franz-prof-dipl-ing-c-205.html"><span class="text">Schweiberer Franz, Prof. Dipl.-Ing.</span></a>
    // Gruppe 1 = Link, Gruppe 2 = Name
    Regex lehrerRegex = new Regex("href=\"(/lehrerinnen-details/[^\"]+)\"><span class=\"text\">([^<]+)</span>");

    // Die Raumnummer steht am Ende vom Link: "...-c-205.html" oder "...-c205.html"
    // Gruppe 1 = Trakt (Buchstabe), Gruppe 2 = Nummer (3 Ziffern)
    Regex raumRegex = new Regex("-([a-z])-?(\\d{3})\\.html$");

    // So sehen Sprechstunde und Durchwahl auf der Detailseite aus:
    // <div class="label">Sprechstunde:</div>
    // <div class="value"> <span class="text">Freitag 09:35 - 10:35 Uhr</span></div>
    Regex sprechstundeRegex = new Regex("Sprechstunde:</div>\\s*<div class=\"value\">\\s*<span class=\"text\">([^<]*)</span>");
    Regex durchwahlRegex = new Regex("Tel-DW:</div>\\s*<div class=\"value\">\\s*<span class=\"text\">([^<]*)</span>");

    void Start()
    {
        // Eine Coroutine kann auf den Download warten, ohne dass das Programm hängen bleibt
        StartCoroutine(Laden());
    }

    IEnumerator Laden()
    {
        // Alle Türschilder in der Szene suchen
        Tuerschild[] schilder = FindObjectsByType<Tuerschild>(FindObjectsSortMode.None);
        foreach (Tuerschild schild in schilder)
        {
            schild.ZeigeMeldung("Lädt ...");
        }

        // 1. Lehrerliste herunterladen
        UnityWebRequest anfrage = UnityWebRequest.Get(homepage + "/lehrerinnen.html");
        yield return anfrage.SendWebRequest();

        if (anfrage.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Fehler beim Laden: " + anfrage.error);
            foreach (Tuerschild schild in schilder)
            {
                schild.ZeigeMeldung("Keine Verbindung");
            }
            yield break;
        }

        string html = anfrage.downloadHandler.text;
        anfrage.Dispose();

        // 2. Mit Regex alle Lehrer aus dem HTML herausholen
        List<Lehrer> alleLehrer = new List<Lehrer>();
        foreach (Match treffer in lehrerRegex.Matches(html))
        {
            Lehrer lehrer = new Lehrer();
            lehrer.link = treffer.Groups[1].Value;
            lehrer.name = treffer.Groups[2].Value.Split(',')[0];   // Titel nach dem Beistrich weglassen

            Match raum = raumRegex.Match(lehrer.link);
            if (raum.Success)
            {
                lehrer.raum = raum.Groups[1].Value.ToUpper() + raum.Groups[2].Value;   // "c" + "205" -> "C205"
            }

            alleLehrer.Add(lehrer);
        }
        Debug.Log(alleLehrer.Count + " Lehrer gefunden");

        // 3. Für jedes Türschild die passenden Lehrer suchen
        foreach (Tuerschild schild in schilder)
        {
            List<Lehrer> lehrerImRaum = new List<Lehrer>();
            foreach (Lehrer lehrer in alleLehrer)
            {
                if (lehrer.raum == schild.raumnummer)
                {
                    lehrerImRaum.Add(lehrer);
                }
            }

            // 4. Für diese Lehrer die Detailseite laden (Sprechstunde und Durchwahl)
            foreach (Lehrer lehrer in lehrerImRaum)
            {
                UnityWebRequest detail = UnityWebRequest.Get(homepage + lehrer.link);
                yield return detail.SendWebRequest();

                if (detail.result == UnityWebRequest.Result.Success)
                {
                    string detailHtml = detail.downloadHandler.text;
                    lehrer.sprechstunde = Suche(sprechstundeRegex, detailHtml);
                    lehrer.durchwahl = Suche(durchwahlRegex, detailHtml);
                }
                detail.Dispose();
            }

            // 5. Alles auf das Schild schreiben
            schild.ZeigeLehrer(lehrerImRaum);
        }
    }

    // Gibt den Text aus Gruppe 1 zurück, oder "" wenn nichts gefunden wurde
    string Suche(Regex regex, string text)
    {
        Match treffer = regex.Match(text);
        if (treffer.Success)
        {
            string wert = treffer.Groups[1].Value.Trim();
            if (wert == "-")
            {
                return "";   // "-" bedeutet auf der Homepage: nichts eingetragen
            }
            return wert;
        }
        return "";
    }
}
