# Community Intranet

Eine generische, mandantenfähige Plattform für humorvolle private Intranets von
Freundesgruppen, Gaming-Communities und kleinen Vereinen. Der erste Theme-Pack-
Anwendungsfall ist eine industrielle Satisfactory-Community, der technische Kern
bleibt aber vollständig spielunabhängig.

## Aktueller Stand

Phase 1 und 2 liefern die dokumentierte Zielarchitektur und ein startbares
Full-Stack-Grundgerüst:

- modularer ASP.NET-Core-8-Monolith mit Minimal APIs
- PostgreSQL über Docker Compose und EF-Core-Grundkonfiguration
- Serilog, ProblemDetails, Swagger und Health Checks
- React 19, TypeScript, Vite, Tailwind CSS 4 und PWA-Basis
- Entwicklungs-Proxy von `/api` auf das Backend
- PowerShell-Skripte für Start, Stop, Status und Logs
- CI-Prüfungen für Backend, Frontend und HTTP-Smoke-Tests

Authentifizierung, Organisationen und echte Fachdaten beginnen in Phase 3. Die
Landingpage zeigt deshalb bewusst nur Systemstatus und Roadmap.

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
ignoriert und darf nicht committed werden. Neue Migrationen werden ab Phase 3
erzeugt:

```powershell
dotnet ef migrations add InitialIdentityAndOrganizations `
  --project backend/CommunityIntranet.Infrastructure `
  --startup-project backend/CommunityIntranet.Api
```

Migrationen werden im Development-Modus beim Start automatisch angewendet,
sobald die erste Migration vorhanden ist.

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
fest typisierte Konfiguration sowie die Seed-Packs `generic-corporate` und
`satisfactory-ficsit` folgen in Phase 4. Offizielle Spiel-Assets werden nicht
verwendet.

## Seed-Daten

Noch nicht aktiv. Die Demo-Organisation „Rheinische FICSIT-Niederlassung“ und
lokale Seed-Benutzer werden erst zusammen mit Identity und Organisationsdaten in
Phase 3/4 eingeführt, damit keine provisorische Authentifizierung entsteht.

## Bekannte Einschränkungen

- noch keine Registrierung oder Anmeldung
- noch keine Organisations- oder Tenant-Daten
- noch keine Theme-Pack-Persistenz
- PostgreSQL-Schema noch ohne Fachmigrationen
- die Modulprojekte definieren in Phase 2 nur die Kompilierungsgrenzen

## Nächster Schritt

Phase 3 implementiert ASP.NET Core Identity, JWT Access Tokens, gehashte und
rotierende Refresh Tokens, Registrierung/Login sowie Organisationsanlage und
Tenant-Zugriffsprüfung.
