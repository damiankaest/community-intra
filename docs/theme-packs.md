# Theme Packs

## Ziel

Theme Packs verwandeln dieselben generischen Module in ein neutrales
Community-Portal oder ein thematisches, humorvolles Intranet. Sie enthalten
ausschließlich validierte Konfiguration. Es werden weder .NET-Assemblies noch
JavaScript, HTML oder frei eingebettetes CSS ausgeführt.

## Fest typisiertes Modell

Die implementierte Konfiguration besteht aus:

```text
ThemePackConfiguration
├── Key, Name, Description, Version, Author
├── Visuals
│   ├── PrimaryColor, SecondaryColor, AccentColor
│   ├── BackgroundColor, SurfaceColor, TextColor
│   ├── DangerColor, WarningColor, SuccessColor
│   ├── LogoIcon
│   └── Style
├── Terminology
│   ├── Organization, Member(s), Department
│   ├── Project, Task, Incident, Award
│   └── ActivityFeed
├── SuggestedTitles[]
├── SuggestedDepartments[]
├── IncidentCategories[]
├── AwardTemplates[]
├── StatusMessages[]
└── Messages
    ├── Welcome
    ├── EmptyProjects
    ├── EmptyTasks
    ├── EmptyIncidents
    └── EmptyActivityFeed
```

Die JSON-Spalte ist das Persistenzformat, nicht das Programmiermodell.
Serialisierung und Deserialisierung erfolgen in genau diesen Typ.

## Validierungsregeln

- `Key`: Kleinbuchstaben, Zahlen und Bindestrich, maximal 64 Zeichen
- `Version`: SemVer
- alle Namen und Texte besitzen feste Längenlimits
- Farben: ausschließlich `#RRGGBB`
- Icons: Schlüssel aus einer serverseitigen Lucide-Allowlist
- Style: definierte Enum/Allowlist
- Listen besitzen Obergrenzen und keine leeren Einträge
- keine HTML-Tags, URLs mit nicht erlaubten Schemes oder Steuerzeichen
- serialisierte Gesamtgröße ist begrenzt
- Key/Version-Kombination ist unveränderlich und eindeutig

## CSS-Anwendung

Das Frontend mappt die validierten Farben zentral auf:

```css
--theme-primary
--theme-secondary
--theme-accent
--theme-background
--theme-surface
--theme-text
--theme-muted
--theme-danger
--theme-warning
--theme-success
```

Theme-Werte werden als Properties gesetzt, niemals als kompletter Style- oder
HTML-String eingefügt.

## Versionierung

Eine publizierte Version wird nicht in-place verändert. Korrekturen erzeugen
eine neue Version. Organisationen referenzieren eine konkrete Version und
können ein Upgrade mit Vorschau durchführen. Das ermöglicht reproduzierbare
Darstellung und sichere Rollbacks.

## Seeds

Phase 4 liefert:

- `generic-corporate`: neutral, freundlich und für beliebige Gruppen geeignet
- `satisfactory-ficsit`: eigenständige industrielle Optik mit humorvoller
  Konzernsprache, ohne offizielle Logos, Grafiken oder kopierte Spiel-Assets

Theme Packs enthalten Standardabteilungen und Vorlagen. Beim Erstellen einer
Organisation werden sie im Wizard bereits angezeigt. Die Übernahme in echte
organisationsbezogene Abteilungen folgt gemeinsam mit der
Mitgliederverwaltung in Phase 5. Spätere Theme-Updates überschreiben
benutzerdefinierte Abteilungen oder Titel nicht automatisch.

## API und Anwendung

Authentifizierte Nutzer können die verfügbaren Themes über
`GET /api/theme-packs` und ein einzelnes Theme über
`GET /api/theme-packs/{key}` abrufen. Organisationen speichern die konkrete
Theme-Pack-ID und geben zusätzlich Key und Version zurück.

Beim Start nach einer Migration werden beide System-Themes idempotent angelegt.
Bestehende Organisationen ohne Theme erhalten `generic-corporate`. Der
Erstellungswizard zeigt Farben, Beispielbegriffe, Titel und vorgeschlagene
Abteilungen. Nach der Gründung setzt das Frontend ausschließlich die erlaubten
CSS-Variablen und verwendet die validierte Terminologie und Systemtexte.

## Activity-Texte

Theme Packs dürfen Renderer-Texte für bekannte Activity-Typen bereitstellen.
Die Datenbank speichert weiterhin strukturierte Werte:

```json
{
  "activityType": "award.granted",
  "schemaVersion": 1,
  "data": {
    "awardName": "Chief Spaghetti Officer",
    "targetMemberName": "Kevin"
  }
}
```

Unbekannte Typen oder Versionen erhalten eine neutrale Fallback-Darstellung.
