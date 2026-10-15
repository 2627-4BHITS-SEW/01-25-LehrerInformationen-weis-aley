using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Ein elektronisches Türschild. Hängt an jedem Raum.
// Die Raumnummer wird im Inspector eingetragen.
public class Tuerschild : MonoBehaviour
{
    public string raumnummer;   // z.B. "B304"
    public TextMeshPro text;    // das Textfeld auf dem Schild

    // Zeigt nur die Raumnummer und eine kurze Meldung, z.B. "Lädt ..."
    public void ZeigeMeldung(string meldung)
    {
        text.text = "<b>" + raumnummer + "</b>\n" + meldung;
    }

    // Schreibt alle Lehrer aus dem Raum auf das Schild
    public void ZeigeLehrer(List<Lehrer> lehrerImRaum)
    {
        string inhalt = "<b>" + raumnummer + "</b>\n";
        string durchwahl = "";

        foreach (Lehrer lehrer in lehrerImRaum)
        {
            inhalt = inhalt + lehrer.name + "\n";

            if (lehrer.sprechstunde != "")
            {
                inhalt = inhalt + "<size=80%>   " + lehrer.sprechstunde + "</size>\n";
            }

            // Alle Lehrer im Raum haben dieselbe Durchwahl, wir merken uns die erste
            if (durchwahl == "")
            {
                durchwahl = lehrer.durchwahl;
            }
        }

        if (lehrerImRaum.Count == 0)
        {
            inhalt = inhalt + "Kein Lehrer eingetragen\n";
        }

        // Zusatzaufgabe: Telefonnummer vom Raum = Nummer der Schule + Durchwahl
        if (durchwahl != "")
        {
            inhalt = inhalt + "\nTel. +43 662 453610-" + durchwahl;
        }

        text.text = inhalt;
    }
}
