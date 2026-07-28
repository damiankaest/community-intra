# Community Intranet

Eine generische, mandantenfähige Plattform für humorvolle private Intranets von
Freundesgruppen, Gaming-Communities und kleinen Vereinen. Der erste Theme-Pack-
Anwendungsfall ist eine industrielle Satisfactory-Community, der technische Kern
bleibt aber vollständig spielunabhängig.

## Aktueller Stand

Phase 1 bis 4 liefern die dokumentierte Zielarchitektur, ein startbares
Full-Stack-System sowie die ersten echten Benutzer- und Mandantendaten:

- modularer ASP.NET-Core-8-Monolith mit Minimal APIs
- ASP.NET Core Identity mit Registrierung, Login und Lockout
- kurzlebige JWT Access Tokens und gehashte Refresh-Token-Rotation
- Organisationen mit Owner-Mitgliedschaft und serverseitiger Tenant-Prüfung
- fest typisierte, serverseitig validierte und versionierte Theme Packs
- Seed-Themes `generic-corporate` und `satisfactory-ficsit`
- mehrstufiger Organisationswizard mit Theme-Vorschau und Modulauswahl
- dynamische Theme-Farben, Terminologie und Systemtexte im Frontend
- PostgreSQL über Docker Compose und eine initiale EF-Core-Migration
- Serilog, ProblemDetails, Swagger und Health Checks
- React-PWA mit Authentifizierung, Organisationsanlage und -auswahl
- Entwicklungs-Proxy von `/api` auf das Backend
- PowerShell-Skripte für Start, Stop, Status und Logs
- CI-Prüfungen für Backend, Frontend und HTTP-Smoke-Tests

Mitglieder, Einladungen und echte Abteilungen folgen in Phase 5.

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

| Dienst | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| Health | http://localhost:5080/api/health |
| Systeminfo | http://localhost:5080/api/system/info |

`start.ps1` installiert fehlende Frontend-Abhängigkeiten, startet PostgreSQL,
Backend und Frontend und schreibt Prozess-IDs nach `.runtime/processes.json`.
Für jede lokale Laufzeit wird ein temporärer JWT-Schlüssel erzeugt, sofern
keiner über eine Umgebungsvariable gesetzt wurde.

## Deployment auf einer Hetzner-VM

Der Produktions-Stack enthält PostgreSQL, Backend, Frontend und Caddy. Nur
Caddy veröffentlicht Ports `80/443`; PostgreSQL bleibt im internen
Docker-Netz. Die vollständige Anleitung steht unter
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

- noch keine Einladungen, Abteilungen oder Mitgliederverwaltung
- noch keine Seed-Benutzer oder Demo-Organisation
- noch keine Passwort-Zurücksetzen- und E-Mail-Bestätigungsstrecke

## Nächster Schritt

Phase 5 implementiert Mitglieder, sichere Einladungslinks, Abteilungen und die
Übernahme der im Theme Pack vorgeschlagenen Standardabteilungen.
