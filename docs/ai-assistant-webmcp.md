# KI-Arbeitsplanung und WebMCP

## Nutzerfluss

1. Ein Mitglied öffnet den schwebenden Assistenten.
2. Es beschreibt das gewünschte Ergebnis und wählt den Tonfall.
3. Das Backend lässt einen strukturierten Arbeitsplan erzeugen.
4. Der Entwurf zeigt Management-Text, Materialliste und Aufgaben.
5. Erst nach einer ausdrücklichen Bestätigung werden Projekt und Aufgaben
   gespeichert.
6. Eine wiederholte Bestätigung liefert dasselbe Ergebnis und erzeugt keine
   Duplikate.

Entwürfe sind 30 Minuten gültig und an Organisation sowie Ersteller gebunden.

## Tonfall

- **Theme-Pack:** Formuliert die Anforderungen passend zur aktuellen
  Intranet-Welt, etwa bewusst schwammig und mit Fabrik-Humor.
- **Normal:** Formuliert sachlich, konkret und ohne Rollenspiel.

Der Tonfall verändert nur die Darstellung. Pflichtfelder, Grenzen und
serverseitige Prüfungen gelten in beiden Modi identisch.

## OpenAI

Das Backend nutzt die Responses API mit einem strikten JSON-Schema. Das Modell
ist über `AiAssistant__Model` konfigurierbar und standardmäßig `gpt-5.6`.
Speicherung beim Anbieter ist deaktiviert (`store: false`); der API-Schlüssel
bleibt ausschließlich im Backend.

Weiterführende offizielle Dokumentation:

- [Function Calling](https://developers.openai.com/api/docs/guides/function-calling)
- [Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs)

## WebMCP

`frontend/src/webmcp/assistantTools.ts` registriert zwei Werkzeuge:

- `prepare_work_plan` erzeugt einen Entwurf über dieselbe REST-API wie der
  sichtbare Chat.
- `confirm_current_work_plan` wird nur registriert, wenn ein aktueller Entwurf
  vorliegt. Vor der schreibenden Aktion erscheint zusätzlich eine
  Browser-Bestätigung.

Die Werkzeuge enthalten keine zweite Geschäftslogik. Rollen, Mandantengrenzen,
Validierung und Idempotenz werden ausschließlich im Backend erzwungen. Browser
ohne WebMCP-Unterstützung nutzen weiterhin den vollständigen sichtbaren
Assistenten.

Weiterführende offizielle Dokumentation:

- [WebMCP Imperative API](https://webmachinelearning.github.io/webmcp/#imperative-api)
- [WebMCP Security and Privacy](https://webmachinelearning.github.io/webmcp/#security-and-privacy)

## Nächste Ausbaustufen

- Integrationstests für organisationsübergreifende Zugriffsversuche
- Eval-Datensatz für Satisfactory- und Normal-Ton
- Entwurf vor dem Speichern bearbeiten
- Aufgaben direkt Mitgliedern zuweisen
- Lesende Werkzeuge für Projekte, Aufgaben und Vorfälle
- Weitere schreibende Werkzeuge nur mit Vorschau und Bestätigung
