# Community-Chat und WebMCP

## Nutzerfluss

Der Assistent ist ab Phase 8 ein dauerhafter Chat pro Organisation und
Mitglied:

1. Die eigene Nachricht erscheint sofort im Verlauf.
2. Das Backend sendet die OpenAI-Antwort als NDJSON-Stream an den Browser.
3. Text-Deltas werden während der Generierung sichtbar.
4. Der Assistent lädt Projekte und Aufgaben über klar begrenzte Lesewerkzeuge.
5. Gewünschte Änderungen erscheinen als kompakte Vorschau.
6. Erst ein sichtbarer Bestätigungsklick führt die Änderung mit den normalen
   Rollen- und Tenant-Prüfungen aus.

Eine einfache Frage bleibt eine kurze Antwort. Der Assistent darf nur dann
mehrere Aufgaben oder ein Projekt vorschlagen, wenn ausdrücklich ein Plan oder
mehrere Schritte gewünscht sind.

## Tonfall

- **Theme-Pack:** leichter Humor passend zur aktuellen Intranet-Welt
- **Klar & normal:** freundlich, direkt und ohne Rollenspiel

Der Tonfall verändert nur den Begleittext. Aufgabentitel und -beschreibungen
müssen in beiden Modi Ziel, konkrete Arbeit und ein erkennbares
Fertig-Kriterium enthalten.

## OpenAI Responses API

Das Backend nutzt `stream: true` und verarbeitet insbesondere
`response.output_text.delta` und `response.output_item.done`.
Werkzeugaufrufe verwenden strikte JSON-Schemas, `parallel_tool_calls: false`
und höchstens vier Werkzeugrunden. `store: false` bleibt gesetzt.

Folgende serverseitige Werkzeuge stehen dem Modell zur Verfügung:

- `list_projects`
- `list_tasks`
- `list_members`
- `get_task_details`
- `propose_create_task`
- `propose_update_task`
- `propose_create_project`
- `propose_add_task_comment`

Lesewerkzeuge geben nur Daten der aktiven Organisation zurück. Schreibwerkzeuge
speichern ausschließlich eine `AssistantAction` im Status `Pending`. Die
Ausführung erfolgt über einen getrennten Bestätigungsendpunkt.

Weiterführende offizielle Dokumentation:

- [Streaming API responses](https://developers.openai.com/api/docs/guides/streaming-responses)
- [Function calling](https://developers.openai.com/api/docs/guides/function-calling)

## WebMCP

`frontend/src/webmcp/assistantTools.ts` registriert optional:

- `community_list_projects`
- `community_list_tasks`
- `community_list_members`
- `community_get_task`
- `community_create_task`
- `community_change_task_status`
- `community_assign_task`
- `community_add_task_comment`

Alle schreibenden Werkzeuge zeigen vor dem Schreiben eine Browser-Bestätigung.
Mitglieder werden vor Zuweisungen oder Erwähnungen über das Lesewerkzeug
aufgelöst. Alle Werkzeuge verwenden dieselben REST-Endpunkte wie die sichtbare
Oberfläche; WebMCP enthält keine zweite Geschäftslogik.

WebMCP ist eine experimentelle Browser-Schnittstelle. Fehlt
`document.modelContext`, funktioniert der eingebaute Chat trotzdem vollständig
über das Backend. Die UI zeigt deshalb keinen Fehlerzustand für fehlendes
WebMCP.

Weiterführende Spezifikation:

- [WebMCP Imperative API](https://webmachinelearning.github.io/webmcp/#imperative-api)
- [WebMCP Security and Privacy](https://webmachinelearning.github.io/webmcp/#security-and-privacy)

## Legacy-Arbeitsplan

Die Phase-7-Endpunkte für große, bestätigungspflichtige Arbeitspläne bleiben
vorerst kompatibel. Die normale Chat-Oberfläche verwendet ab Phase 8 jedoch den
feingranularen Chat- und Aktionsfluss.
