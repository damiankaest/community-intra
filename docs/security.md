# Sicherheitskonzept

## Vertrauensgrenzen

Browserdaten sind grundsätzlich unvertrauenswürdig. Organisation, Rolle,
Permission, sichtbarer Titel und Theme-Werte werden serverseitig aufgelöst oder
validiert. PostgreSQL ist nicht direkt aus dem Internet erreichbar.

## Authentifizierung

ASP.NET Core Identity übernimmt Passwort-Hashing, Normalisierung, Lockout und
Sicherheitsstempel. Implementiert sind:

- Access Token als signiertes JWT mit kurzer Laufzeit
- Refresh Token mit hoher Entropie und längerer, begrenzter Laufzeit
- nur der SHA-256-Hash des Refresh Tokens wird persistiert
- konkurrenzsichere Rotation bei jeder Verwendung
- Token-Familie zur Erkennung wiederverwendeter Vorgänger
- Widerruf der Familie bei Reuse-Verdacht sowie Widerruf des aktuellen Tokens
  beim Logout
- keine Tokens, Passwörter oder Authorization-Header in Logs

Der JWT enthält Benutzer-ID und minimale globale Claims. Organisationsrollen
werden nicht dauerhaft in den Token kopiert, da ein Benutzer mehreren
Organisationen angehört und Rollenänderungen zeitnah gelten müssen.

## Tenant Isolation

Für eine organisationsbezogene Operation gelten gleichzeitig:

1. authentifizierte Benutzer-ID aus dem validierten JWT
2. `organizationId` aus der Route
3. aktive Mitgliedschaft aus der Datenbank
4. benötigte benannte Permission
5. expliziter `OrganizationId`-Filter in der Fachabfrage

Eine `OrganizationId` im JSON-Body wird ignoriert beziehungsweise abgelehnt.
Referenzen auf Projekt, Mitglied oder Department werden darauf geprüft, dass
sie zum gleichen Tenant gehören. Fremde Ressourcen antworten mit `404`, um
deren Existenz nicht offenzulegen.

## Permission-Modell

Rollen werden serverseitig auf benannte Permissions abgebildet:

```text
organization.read
organization.manage
members.read
members.manage
invitations.manage
projects.read
projects.create
projects.manage
tasks.read
tasks.create
tasks.manage
incidents.read
incidents.create
incidents.manage
awards.read
awards.grant
```

`VisibleTitle` und Theme-Titel werden an keiner Stelle in der
Autorisierungsentscheidung verwendet.

## Einladungen

- Tokens werden mit einem CSPRNG erzeugt
- im Speicher liegt nur der Hash; Klartext erscheint einmal im Link
- kurze, konfigurierbare Laufzeit und optionale Maximalnutzung
- Annahme prüft Ablauf, Widerruf, Nutzung und aktive Mitgliedschaft atomar
- Endpunkte erhalten Rate Limits
- Tokenwerte werden aus Logs und ProblemDetails entfernt

## Theme-Pack-Sicherheit

- fest typisiertes und größenbegrenztes Schema
- keine Scripts, Event-Handler, HTML-Fragmente oder externe Stylesheets
- Text wird von React normal escaped gerendert
- Farben und Icons werden gegen Format bzw. Allowlist geprüft
- Bild-URLs werden zunächst nicht frei unterstützt; spätere Assets laufen über
  kontrollierten Upload, Content-Type-Prüfung und eigene Auslieferungsdomain

## Web-Sicherheit

- CORS enthält nur konfigurierte Frontend-Origins
- HTTPS und sichere Cookies in produktiven Umgebungen
- `HttpOnly`, `Secure` und angemessenes `SameSite` für Refresh-Cookies
- CSP, `X-Content-Type-Options`, Referrer-Policy und Frame-Policy am Edge
- Rate Limits für Authentifizierung und Einladung
- keine API-Antworten im PWA-Cache
- Begrenzung von Body-, Seiten- und Textgrößen

## KI- und WebMCP-Sicherheit

- der OpenAI-API-Schlüssel liegt nur im Backend und wird nie an den Browser
  ausgeliefert
- Requests an die Responses API verwenden `store: false`
- das Modell erhält den begrenzten eigenen Gesprächsverlauf; Workspace-Daten
  werden ausschließlich durch organisationsgefilterte Lesewerkzeuge geladen
- Modellantworten müssen einem strengen JSON-Schema entsprechen und werden
  anschließend erneut serverseitig auf Enum-, Anzahl- und Textgrenzen geprüft
- generierter Text bleibt unvertrauenswürdig und wird durch React ohne
  HTML-Ausführung dargestellt
- schreibende Modellwerkzeuge erzeugen ausschließlich eine
  bestätigungspflichtige `AssistantAction`, keine unmittelbare Fachänderung
- Entwürfe sind kurzlebig, organisations- und mitgliedsgebunden sowie durch
  einen Concurrency-Token geschützt
- erst der getrennte Bestätigungsendpunkt legt Projekt und Aufgaben atomar an
- die Bestätigung ist idempotent und erzeugt bei Wiederholung keine Duplikate
- WebMCP enthält keine Business-Logik und verwendet die normale JWT-Sitzung
- WebMCP-Tools werden nicht an fremde Origins exponiert und kennzeichnen
  Schreibzugriffe mit `readOnlyHint: false`
- der bestätigende WebMCP-Handler fordert zusätzlich eine sichtbare
  Nutzerbestätigung an
- der Assistent besitzt ein eigenes Rate Limit
- Werkzeugaufrufe verwenden strikte Schemas und deaktivieren parallele
  Funktionsaufrufe
- Chatnachrichten und Aktionen sind zusätzlich an das aktive Mitglied gebunden

## Aufgabenbilder und Kommentare

- Uploads erlauben nur PNG, JPEG, WebP und GIF; SVG und frei ausführbare
  Dokumentformate sind ausgeschlossen
- maximal 5 MB pro Bild und maximal 20 Bilder pro Aufgabe
- Dateinamen werden auf den Basename und 240 Zeichen reduziert
- Bildinhalte werden erst nach JWT-, Mitgliedschafts- und Tenant-Prüfung
  ausgeliefert
- Caddy setzt `X-Content-Type-Options: nosniff`
- Kommentare sind auf 2000 Zeichen begrenzt und werden von React als Text
  gerendert

## Fehler und Logging

Unerwartete Fehler erhalten einen Trace-Identifier und werden intern durch
Serilog protokolliert. Antworten enthalten keine Stacktraces, SQL-Texte,
Connection Strings oder Secrets. Request Logging erfasst Route, Status und
Dauer, aber filtert sensible Header und Request-Bodies.

## Sicherheitsrelevante Tests

- Nutzer A kann keine Organisation-B-Ressource lesen, ändern oder über IDs
  referenzieren
- sichtbare Titel verleihen keine Permissions
- abgelaufene, widerrufene und wiederverwendete Refresh Tokens schlagen fehl
- Einladungs-Maximalnutzung ist auch bei parallelen Requests korrekt
- archivierte Organisationen sind schreibgeschützt
- Theme Packs mit HTML, Scripts, ungültigen Farben oder zu großen Listen werden
  abgelehnt
- Theme Keys und Versionen werden validiert; Farben müssen `#RRGGBB` verwenden
- Icons und Layout-Stile stammen aus festen serverseitigen Allowlists
- unbekannte JSON-Felder sowie Konfigurationen über 128 KiB werden abgelehnt
- das Frontend setzt ausschließlich bekannte CSS-Variablen und rendert Texte
  ohne HTML-Injektion

## Secret Management

`.env` ist ignoriert und nur für lokale Entwicklung. Das Repository enthält
lediglich `.env.example` mit nicht produktiven Platzhaltern. Produktion liefert
Connection String, Token-Schlüssel und andere Secrets über den jeweiligen
Secret Store beziehungsweise sichere Umgebungsvariablen.

`OPENAI_API_KEY` wird als GitHub-Environment-Secret übertragen und auf der VM
nur in der zugriffsgeschützten Produktions-Environment-Datei gespeichert.

Satisfactory-Application-Tokens werden pro Organisation mit ASP.NET Data
Protection verschlüsselt in PostgreSQL gespeichert. Die Schlüssel liegen in
Produktion in einem separaten Docker-Volume und werden weder an das Frontend
noch an den Chat zurückgegeben. Die Serveranbindung blockiert interne
Zielnetze, Redirects und nicht bestätigte selbstsignierte Zertifikate.

Die lokale Entwicklungsroutine und GitHub Actions erzeugen flüchtige
JWT-Schlüssel zur Laufzeit. Im Repository liegt kein verwendbarer
JWT-Signaturschlüssel.
