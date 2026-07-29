# Community Intranet

Eine generische, mandantenfähige Plattform für humorvolle private Intranets von
Freundesgruppen, Gaming-Communities und kleinen Vereinen. Der erste Theme-Pack-
Anwendungsfall ist eine industrielle Satisfactory-Community, der technische Kern
bleibt aber vollständig spielunabhängig.

## Aktueller Stand

Phase 1 bis 8 liefern die dokumentierte Zielarchitektur, ein startbares
Full-Stack-System sowie echte Benutzer-, Mandanten- und Fachdaten:

- modularer ASP.NET-Core-8-Monolith mit Minimal APIs
- ASP.NET Core Identity mit Registrierung, Login und Lockout
- kurzlebige JWT Access Tokens und gehashte Refresh-Token-Rotation
- Organisationen mit Owner-Mitgliedschaft und serverseitiger Tenant-Prüfung
- fest typisierte, serverseitig validierte und versionierte Theme Packs
- Seed-Themes `generic-corporate` und `satisfactory-ficsit`
- mehrstufiger Organisationswizard mit Theme-Vorschau und Modulauswahl
- dynamische Theme-Farben, Terminologie und Systemtexte im Frontend
- Mitgliederverwaltung mit technischen Rollen, sichtbaren Titeln und Status
- Theme-basierte Abteilungen inklusive Verwaltung und Bestands-Backfill
- sichere, widerrufbare Einladungslinks mit Ablauf und Maximalnutzung
- öffentlicher Einladungs-Check und Beitritt nach Login oder Registrierung
- Projekte und Aufgaben mit Filterung, Zuweisung und Statusübergängen
- Incident Reports mit Untersuchung, Auflösung und Lessons Learned
- Theme-basierte Auszeichnungen für Mitglieder
- strukturierter, lokalisierbarer Activity Feed
- Dashboard-Kennzahlen, aktuelle Auszeichnung und Schnellaktionen
- ausklappbare KI-Arbeitsplanung mit Theme- und Normal-Ton
- prüfbare Projektentwürfe mit Ressourcen, Aufgaben und Abnahmekriterien
- explizite Bestätigung vor dem atomaren Anlegen von Projekt und Aufgaben
- WebMCP-Tools als dünne Adapter auf dieselben autorisierten APIs
- persistenter Community-Chat mit echter Streaming-Antwort
- KI-Lesezugriff auf vorhandene Projekte und Aufgaben
- kleine bestätigungspflichtige Chat-Aktionen statt automatischer Mammutpläne
- anklickbare Projekt- und Aufgabenansichten
- Subtasks, Kommentare und Screenshot-Anhänge an Aufgaben
- PostgreSQL über Docker Compose und eine initiale EF-Core-Migration
- Serilog, ProblemDetails, Swagger und Health Checks
- React-PWA mit Authentifizierung, Organisationsanlage und -auswahl
- Entwicklungs-Proxy von `/api` auf das Backend
- PowerShell-Skripte für Start, Stop, Status und Logs
- CI-Prüfungen für Backend, Frontend und HTTP-Smoke-Tests

## Architektur in Kürze

Das Backend ist ein modularer Monolith. Jedes Fachmodul besitzt sein eigenes
Projekt und später seine eigenen Endpunkte, Anwendungsfälle, Domain-Typen sowie
EF-Core-Konfigurationen. Alle Module laufen in einem Prozess und verwenden eine
PostgreSQL-Datenbank mit getrennten Schemas.

Organisationen sind die Mandanten. Jede mandantenbezogene Entity trägt eine
`OrganizationId`; der Server ermittelt und prüft die Mitgliedschaft für jeden
fachlichen Request. Sichtbare humoristische Titel sind reine Darstellung und
werden niemals für Berechtigungen verwendet.

Ausführliche Entscheidungen stehen unter [`docs/`](docs/architecture.md).

## Voraussetzungen

- .NET SDK 8
- Node.js 24 und npm 11
- Docker mit Docker Compose
- PowerShell 7 (`pwsh`)

## Lokale Einrichtung

```powershell
Copy-Item .env.example .env
./dev/start.ps1
```

Danach sind verfügbar:

| Dienst     | URL                                   |
| ---------- | ------------------------------------- |
| Frontend   | http://localhost:5173                 |
| Backend    | http://localhost:5080                 |
| Swagger    | http://localhost:5080/swagger         |
| Health     | http://localhost:5080/api/health      |
| Systeminfo | http://localhost:5080/api/system/info |

`start.ps1` installiert fehlende Frontend-Abhängigkeiten, startet PostgreSQL,
Backend und Frontend und schreibt Prozess-IDs nach `.runtime/processes.json`.
Für jede lokale Laufzeit wird ein temporärer JWT-Schlüssel erzeugt, sofern
keiner über eine Umgebungsvariable gesetzt wurde.

Für die KI-Arbeitsplanung muss zusätzlich `AiAssistant__ApiKey` in der lokalen
`.env` gesetzt werden. Der Schlüssel bleibt ausschließlich im Backend. Ohne
Schlüssel bleibt das übrige Intranet vollständig nutzbar und der Chat zeigt
seinen nicht konfigurierten Status an.

## Deployment auf einer Hetzner-VM

Der Produktions-Stack enthält PostgreSQL, Backend, Frontend und Caddy. Nur
Caddy veröffentlicht Ports `80/443`; PostgreSQL bleibt im internen
Docker-Netz. Der manuell gestartete GitHub-Workflow baut versionierte Images,
überträgt die Laufzeitkonfiguration per SSH und prüft anschließend die
öffentliche HTTPS-URL. Die VM benötigt keinen GitHub-Repository-Zugriff.

Nach der einmaligen Einrichtung von Docker, DNS und SSH müssen nur noch die
Secrets im GitHub-Environment `production` hinterlegt werden. Die vollständige
Anleitung und Secret-Liste stehen unter
[`docs/deployment-hetzner.md`](docs/deployment-hetzner.md).

Firebase ist dafür nicht erforderlich. Identity, Sitzungen und Organisationen
laufen vollständig selbst gehostet über ASP.NET Core und PostgreSQL. Firebase
kann später optional für Push-Nachrichten oder externe Login-Anbieter ergänzt
werden.

## Entwicklungsbefehle

```powershell
./dev/status.ps1
./dev/logs.ps1 backend
./dev/logs.ps1 frontend
./dev/stop.ps1
./dev/stop.ps1 -StopDatabase
```

PostgreSQL bleibt beim normalen Stoppen aktiv, damit der nächste Start schneller
ist.

## Manuelle Prüfungen

```powershell
dotnet restore CommunityIntranet.sln
dotnet build CommunityIntranet.sln --no-restore
dotnet test CommunityIntranet.sln --no-build

Set-Location frontend
npm install
npm run lint
npm run test
npm run build
```

## Datenbank und Migrationen

Die lokale Verbindung wird ausschließlich aus `.env` geladen. `.env` ist
ignoriert und darf nicht committed werden. Neue Migrationen werden so erzeugt:

```powershell
dotnet ef migrations add NameDerMigration `
  --project backend/CommunityIntranet.Infrastructure `
  --startup-project backend/CommunityIntranet.Api
```

Migrationen werden lokal und im dokumentierten Produktions-Stack beim Start
automatisch angewendet.

## Projektstruktur

```text
backend/
  CommunityIntranet.Api/                 # Composition Root und HTTP
  CommunityIntranet.BuildingBlocks/      # kleine, stabile Querschnittstypen
  CommunityIntranet.Infrastructure/      # EF Core und technische Adapter
  CommunityIntranet.Modules.*/           # fachliche Modulgrenzen
  CommunityIntranet.Api.Tests/           # Backend-Testbasis
frontend/                                # React-PWA
docs/                                    # Architekturentscheidungen
dev/                                     # lokale PowerShell-Werkzeuge
```

## Theme Packs

Theme Packs sind validierte Daten, kein ausführbarer Code. Sie liefern Farben,
Terminologie, Titelvorschläge, Abteilungen, Kategorien und Systemtexte. Die
fest typisierte Konfiguration wird als JSONB gespeichert; die Seed-Packs
`generic-corporate` und `satisfactory-ficsit` werden beim Datenbankstart
versioniert angelegt. Offizielle Spiel-Assets werden nicht verwendet.

## Bekannte Einschränkungen

- noch keine Seed-Benutzer oder Demo-Organisation
- noch keine Passwort-Zurücksetzen- und E-Mail-Bestätigungsstrecke
- noch kein Versand von Einladungen per E-Mail; Links werden bewusst kopiert
- WebMCP benötigt einen Browser beziehungsweise Agenten mit experimenteller
  WebMCP-Unterstützung
- WebMCP ist optional; der eingebaute Chat arbeitet unabhängig davon über die
  Backend-API

## Nächster Schritt

Als Nächstes folgen Agent-Evaluierungen, Bildkomprimierung, feinere
Benachrichtigungen und weitere bestätigungspflichtige Chat-Aktionen.
