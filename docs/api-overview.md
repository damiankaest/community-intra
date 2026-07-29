# API-Übersicht

## Konventionen

- Basis: `/api`
- JSON in `camelCase`
- Fehler als `application/problem+json`
- Zeitstempel als ISO-8601 UTC
- Listen mit `page`, `pageSize`, `sort` und fachlichen Filtern
- Standardmaximum für `pageSize`: 100
- Requests und Responses verwenden DTOs, niemals EF-Entities
- schreibende Requests unterstützen später `If-Match`/Concurrency-Token
- jeder Endpunkt akzeptiert intern einen `CancellationToken`

Beispiel einer Liste:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

## Systemendpunkte (Phase 2)

| Methode | Route              |                  Auth | Zweck                                        |
| ------- | ------------------ | --------------------: | -------------------------------------------- |
| GET     | `/api/health`      |                  nein | aggregierter Anwendungs- und Datenbankstatus |
| GET     | `/api/system/info` |                  nein | Name, Version, Umgebung und Status           |
| GET     | `/swagger`         | nein, nur Development | interaktive API-Dokumentation                |

## Identity (Phase 3)

| Methode | Route                |              Auth |
| ------- | -------------------- | ----------------: |
| POST    | `/api/auth/register` |              nein |
| POST    | `/api/auth/login`    |              nein |
| POST    | `/api/auth/refresh`  |     Refresh Token |
| POST    | `/api/auth/logout`   | optionale Sitzung |
| GET     | `/api/auth/me`       |                ja |

Login, Registrierung und Refresh erhalten eigene Rate-Limit-Policies. Der
Refresh Token wird bevorzugt als `HttpOnly`, `Secure`, `SameSite`-Cookie
transportiert; der Access Token bleibt kurzlebig und wird nicht dauerhaft im
Browser gespeichert.

## Organizations (Phase 3)

| Methode | Route                                 | Permission               |
| ------- | ------------------------------------- | ------------------------ |
| GET     | `/api/organizations`                  | authentifiziert          |
| POST    | `/api/organizations`                  | authentifiziert          |
| GET     | `/api/organizations/{organizationId}` | `organization.read`      |
| PUT     | `/api/organizations/{organizationId}` | `organization.manage`    |
| DELETE  | `/api/organizations/{organizationId}` | Owner oder Administrator |

Beim Erstellen kann `themePackKey` sowie die Liste `enabledModules` angegeben
werden. Ohne Theme-Angabe wird `generic-corporate` verwendet. Antworten
enthalten Theme-Key und konkrete Version.

## Theme Packs (Phase 4)

```text
GET /api/theme-packs
GET /api/theme-packs/{key}
```

Beide Endpunkte erfordern Authentifizierung. Sie geben ausschließlich
serverseitig validierte, fest typisierte Konfiguration zurück. Theme Packs
enthalten keine Scripts, kein HTML und kein frei ausführbares CSS.

## Members und Invitations (Phase 5)

| Methode | Route                                                            | Permission               |
| ------- | ---------------------------------------------------------------- | ------------------------ |
| GET     | `/api/organizations/{organizationId}/members`                    | `members.read`           |
| GET     | `/api/organizations/{organizationId}/members/{memberId}`         | `members.read`           |
| PATCH   | `/api/organizations/{organizationId}/members/{memberId}`         | Owner oder Administrator |
| GET     | `/api/organizations/{organizationId}/departments`                | Mitglied                 |
| POST    | `/api/organizations/{organizationId}/departments`                | Owner oder Administrator |
| PUT     | `/api/organizations/{organizationId}/departments/{departmentId}` | Owner oder Administrator |
| DELETE  | `/api/organizations/{organizationId}/departments/{departmentId}` | Owner oder Administrator |
| GET     | `/api/organizations/{organizationId}/invitations`                | Owner oder Administrator |
| POST    | `/api/organizations/{organizationId}/invitations`                | `invitations.manage`     |
| POST    | `/api/invitations/resolve`                                       | nein                     |
| POST    | `/api/invitations/accept`                                        | ja                       |
| DELETE  | `/api/organizations/{organizationId}/invitations/{invitationId}` | `invitations.manage`     |

Einladungstokens werden bei `resolve` und `accept` im Request-Body übertragen.
Der öffentliche Browser-Link verwendet ein URL-Fragment (`/invite#token`), das
nicht an Caddy oder das Backend übertragen wird. Der Klartext-Token wird nur in
der Erstellungsantwort ausgegeben.

## Fachmodule (Phase 6)

Alle Collections liegen unter
`/api/organizations/{organizationId}/{resource}`:

- `/projects`
- `/tasks`
- `/incidents`
- `/awards`
- `/activities`
- `/dashboard`

Die implementierten Endpunkte sind:

| Methode        | Route                                                | Zweck                                              |
| -------------- | ---------------------------------------------------- | -------------------------------------------------- |
| GET/POST       | `/projects`                                          | Projekte filtern oder erstellen                    |
| GET/PUT/DELETE | `/projects/{projectId}`                              | Projekt lesen, bearbeiten oder abbrechen           |
| GET/POST       | `/tasks`                                             | Aufgaben filtern oder erstellen                    |
| GET/PUT/DELETE | `/tasks/{taskId}`                                    | Aufgabe lesen, bearbeiten oder abbrechen           |
| PATCH          | `/tasks/{taskId}/status`                             | Aufgabenstatus ändern                              |
| GET            | `/tasks/{taskId}/details`                            | Subtasks, Kommentare und Screenshot-Metadaten      |
| POST           | `/tasks/{taskId}/comments`                           | Kommentar hinzufügen                               |
| POST           | `/tasks/{taskId}/attachments`                        | Screenshot bis 5 MB hochladen                      |
| GET            | `/tasks/{taskId}/attachments/{attachmentId}/content` | Screenshot laden                                   |
| GET/POST       | `/incidents`                                         | Incidents filtern oder melden                      |
| GET/PUT        | `/incidents/{incidentId}`                            | Incident lesen oder bearbeiten                     |
| POST           | `/incidents/{incidentId}/resolve`                    | Lösung dokumentieren                               |
| GET/POST       | `/awards`                                            | Auszeichnungen lesen oder vergeben                 |
| GET            | `/awards/templates`                                  | Theme-Pack-Vorlagen lesen                          |
| GET            | `/activities`                                        | strukturierte Aktivitäten lesen                    |
| GET            | `/dashboard`                                         | organisationsweite Kennzahlen und Schnellübersicht |

Schreibende Updates verwenden einen `concurrencyToken`; veraltete Änderungen
antworten mit `409 Conflict`. Projekt-, Mitglieds- und Zuweisungsreferenzen
werden zusätzlich gegen den aktiven Tenant geprüft.

## KI-Assistent und WebMCP (Phase 7)

| Methode | Route                                                                     | Zweck                                      |
| ------- | ------------------------------------------------------------------------- | ------------------------------------------ |
| GET     | `/organizations/{organizationId}/assistant/availability`                  | serverseitige KI-Konfiguration prüfen      |
| POST    | `/organizations/{organizationId}/assistant/work-plan-drafts`              | unverbindlichen Projektentwurf vorbereiten |
| POST    | `/organizations/{organizationId}/assistant/work-plan-drafts/{id}/confirm` | Projekt und Aufgaben verbindlich anlegen   |

Ein Entwurf enthält einen Theme- oder Neutral-Ton, Ressourcen, ein bis zwölf
Aufgaben und Abnahmekriterien. Er läuft standardmäßig nach 30 Minuten ab und ist
an das erstellende Mitglied und die Organisation gebunden. Die Bestätigung
benötigt den aktuellen `concurrencyToken`. Wiederholte Bestätigungen erzeugen
keine Duplikate, sondern geben das bereits angelegte Projekt zurück.

Diese Endpunkte bleiben für bestehende Phase-7-Clients verfügbar. Die sichtbare
Oberfläche und die aktuellen WebMCP-Werkzeuge verwenden ab Phase 8 den
feingranularen Chat- und Aktionsfluss.

## Community-Chat und interaktive Aufgaben (Phase 8)

| Methode | Route                                                            | Zweck                                     |
| ------- | ---------------------------------------------------------------- | ----------------------------------------- |
| GET     | `/organizations/{organizationId}/assistant/chat`                 | letzte Unterhaltung und Aktionen laden    |
| POST    | `/organizations/{organizationId}/assistant/chat/messages`        | Nachricht senden; NDJSON-Antwort streamen |
| POST    | `/organizations/{organizationId}/assistant/actions/{id}/confirm` | vorgeschlagene Änderung bestätigen        |

Der Chat kann Projekte und Aufgaben lesen sowie genau eine
Aufgaben-/Projektänderung vorbereiten. Schreibende KI-Aktionen bleiben bis zur
Bestätigung im Status `Pending`. Nachrichten und Aktionen sind an Organisation
und Mitglied gebunden.

Aufgaben unterstützen eine Ebene Subtasks, Kommentare und bis zu 20
Screenshot-Anhänge mit jeweils maximal 5 MB. Erlaubt sind PNG, JPEG, WebP und
GIF. Binärdaten liegen in PostgreSQL und werden nur nach erneuter
Mitgliedschaftsprüfung ausgeliefert.

## Zusammenarbeit im Alltag (Phase 9)

| Methode | Route                                                   | Zweck                              |
| ------- | ------------------------------------------------------- | ---------------------------------- |
| GET     | `/organizations/{organizationId}/notifications`         | eigene Benachrichtigungen laden    |
| GET     | `/organizations/{organizationId}/notifications/summary` | Zahl ungelesener Meldungen laden   |
| POST    | `/organizations/{organizationId}/notifications/{id}/read` | Meldung als gelesen markieren    |
| POST    | `/organizations/{organizationId}/notifications/read-all` | alle Meldungen als gelesen markieren |

Zuweisungen, Kommentare, `@Person`-Erwähnungen und Statusänderungen erzeugen
mandantengebundene In-App-Benachrichtigungen. Der Empfänger wird immer aus der
aktiven Mitgliedschaft abgeleitet; Eigenbenachrichtigungen werden verworfen.

Screenshot-Uploads dürfen zusätzlich ein maximal 512 KB großes Vorschaubild
enthalten. Das Frontend verkleinert übliche Bilder auf maximal 1920 Pixel und
erzeugt eine Vorschau mit maximal 480 Pixel. GIF-Dateien bleiben unverändert.
Die Vorschau ist über
`/tasks/{taskId}/attachments/{attachmentId}/thumbnail` abrufbar.

## Live Operations (Phase 10)

Alle Routen liegen unter
`/organizations/{organizationId}/live-operations/server`:

| Methode | Route            | Permission               | Zweck                                  |
| ------- | ---------------- | ------------------------ | -------------------------------------- |
| GET     | `/status`        | aktives Mitglied         | gecachten Gameserver-Status laden      |
| GET     | `/configuration` | Owner oder Administrator | maskierte Konfiguration laden          |
| PUT     | `/configuration` | Owner oder Administrator | Verbindung verschlüsselt speichern     |
| DELETE  | `/configuration` | Owner oder Administrator | Verbindung aus dem Intranet entfernen  |
| POST    | `/test`          | Owner oder Administrator | Verbindung ohne Speicherung überprüfen |

Für Owner und Administratoren umgeht `status?forceRefresh=true` den kurzen
Status-Cache. API-Tokens werden niemals zurückgegeben. Selbstsignierte
TLS-Zertifikate benötigen einen bewusst bestätigten SHA-256-Fingerprint.

## Statuscodes

| Code | Verwendung                                                  |
| ---: | ----------------------------------------------------------- |
|  200 | erfolgreiche Abfrage oder Änderung                          |
|  201 | Ressource erstellt, inklusive `Location`                    |
|  204 | erfolgreiche Aktion ohne Body                               |
|  400 | syntaktisch/fachlich ungültiger Request                     |
|  401 | nicht authentifiziert                                       |
|  403 | authentifiziert, aber keine Permission                      |
|  404 | Ressource nicht vorhanden oder für fremden Tenant verborgen |
|  409 | Eindeutigkeits- oder Concurrency-Konflikt                   |
|  410 | Einladung ungültig, abgelaufen, widerrufen oder verbraucht  |
|  422 | optionale spätere Nutzung für komplexe Fachvalidierung      |
|  429 | Rate Limit erreicht                                         |
|  500 | unerwarteter, intern protokollierter Fehler                 |
|  503 | Health Check nicht gesund oder KI-Dienst nicht verfügbar    |

Bei Tenant-fremden IDs wird in der Regel `404` statt `403` geliefert, damit die
Existenz fremder Ressourcen nicht bestätigt wird.

## ProblemDetails

Jede Fehlerantwort enthält mindestens `type`, `title`, `status`, `detail`,
`instance` und `traceId`. Validierungsfehler ergänzen ein `errors`-Objekt.
Interne Exception-Texte, SQL-Details, Tokens und Secrets werden nie ausgegeben.
