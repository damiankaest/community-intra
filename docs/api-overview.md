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

| Methode | Route | Auth | Zweck |
|---|---|---:|---|
| GET | `/api/health` | nein | aggregierter Anwendungs- und Datenbankstatus |
| GET | `/api/system/info` | nein | Name, Version, Umgebung und Status |
| GET | `/swagger` | nein, nur Development | interaktive API-Dokumentation |

## Identity (Phase 3)

| Methode | Route | Auth |
|---|---|---:|
| POST | `/api/auth/register` | nein |
| POST | `/api/auth/login` | nein |
| POST | `/api/auth/refresh` | Refresh Token |
| POST | `/api/auth/logout` | optionale Sitzung |
| GET | `/api/auth/me` | ja |

Login, Registrierung und Refresh erhalten eigene Rate-Limit-Policies. Der
Refresh Token wird bevorzugt als `HttpOnly`, `Secure`, `SameSite`-Cookie
transportiert; der Access Token bleibt kurzlebig und wird nicht dauerhaft im
Browser gespeichert.

## Organizations (Phase 3)

| Methode | Route | Permission |
|---|---|---|
| GET | `/api/organizations` | authentifiziert |
| POST | `/api/organizations` | authentifiziert |
| GET | `/api/organizations/{organizationId}` | `organization.read` |
| PUT | `/api/organizations/{organizationId}` | `organization.manage` |
| DELETE | `/api/organizations/{organizationId}` | Owner oder Administrator |

## Members und Invitations (Phase 5)

| Methode | Route | Permission |
|---|---|---|
| GET | `/api/organizations/{organizationId}/members` | `members.read` |
| GET | `/api/organizations/{organizationId}/members/{memberId}` | `members.read` |
| PATCH | `/api/organizations/{organizationId}/members/{memberId}` | `members.manage` oder eigene erlaubte Felder |
| POST | `/api/organizations/{organizationId}/invitations` | `invitations.manage` |
| GET | `/api/invitations/{token}` | nein |
| POST | `/api/invitations/{token}/accept` | ja |
| DELETE | `/api/organizations/{organizationId}/invitations/{invitationId}` | `invitations.manage` |

## Fachmodule (Phase 6)

Alle Collections liegen unter
`/api/organizations/{organizationId}/{resource}`:

- `/projects`
- `/tasks`
- `/incidents`
- `/awards`
- `/activities`
- `/dashboard`

CRUD nutzt `GET`, `POST`, `PUT`/`PATCH` und `DELETE` konsistent. Fachliche
Transitionen, die mehr als Feldänderungen ausdrücken, erhalten sprechende
Subressourcen, zum Beispiel:

```text
POST /api/organizations/{organizationId}/incidents/{incidentId}/resolve
POST /api/organizations/{organizationId}/awards
PATCH /api/organizations/{organizationId}/tasks/{taskId}/status
```

## Statuscodes

| Code | Verwendung |
|---:|---|
| 200 | erfolgreiche Abfrage oder Änderung |
| 201 | Ressource erstellt, inklusive `Location` |
| 204 | erfolgreiche Aktion ohne Body |
| 400 | syntaktisch/fachlich ungültiger Request |
| 401 | nicht authentifiziert |
| 403 | authentifiziert, aber keine Permission |
| 404 | Ressource nicht vorhanden oder für fremden Tenant verborgen |
| 409 | Eindeutigkeits- oder Concurrency-Konflikt |
| 422 | optionale spätere Nutzung für komplexe Fachvalidierung |
| 429 | Rate Limit erreicht |
| 500 | unerwarteter, intern protokollierter Fehler |
| 503 | Health Check nicht gesund |

Bei Tenant-fremden IDs wird in der Regel `404` statt `403` geliefert, damit die
Existenz fremder Ressourcen nicht bestätigt wird.

## ProblemDetails

Jede Fehlerantwort enthält mindestens `type`, `title`, `status`, `detail`,
`instance` und `traceId`. Validierungsfehler ergänzen ein `errors`-Objekt.
Interne Exception-Texte, SQL-Details, Tokens und Secrets werden nie ausgegeben.
