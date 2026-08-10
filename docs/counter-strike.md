# CouchClash Counter-Strike

Der Bereich `/cs2` ist ein eigenes Fachmodul innerhalb des bestehenden
modularen Monolithen. Organisation, Benutzer, Rollen, Feed, PostgreSQL,
Deployment und Secrets bleiben gemeinsame Plattformdienste.

## Demo-Analyse

Uploads werden außerhalb des Webroots gespeichert, anhand des Demo-Headers
und eines konfigurierbaren Größenlimits validiert sowie per SHA-256 dedupliziert.
Ein begrenzter In-Memory-Kanal übergibt Imports an einen einzelnen Background
Worker. Nach einem Neustart nimmt der Worker unterbrochene Imports wieder auf.

`ICounterStrikeDemoAnalyzer` kapselt die Parser-Engine. Die aktuelle
Implementierung startet `csda` ohne Shell und übergibt Dateipfade ausschließlich
über `ProcessStartInfo.ArgumentList`. Laufzeit, Parallelität und Fehlerausgabe
sind begrenzt. Das JSON ist ein Import-DTO und wird in das eigene relationale
Modell gemappt; Rohdaten liegen optional als Import-Artefakt vor.

Verwendetes Upstream-Projekt:

- `akiver/cs-demo-analyzer`
- Lizenz: MIT
- Standardversion im Container: `v1.10.5`
- Linux-x64-Archiv wird vor dem Entpacken per SHA-256 verifiziert
- Lizenztext: `third-party/cs-demo-analyzer-LICENSE.md`

Bei einem Versionsupdate sind CLI-Parameter, JSON-Schema, Release-Checksum und
Lizenz erneut zu prüfen.

## Konfiguration

- `CounterStrike__StorageRoot`
- `CounterStrike__AnalyzerExecutable`
- `CounterStrike__MaximumDemoMegabytes`
- `CounterStrike__ParserTimeoutSeconds`
- `CounterStrike__QueueCapacity`

## Externe Accounts

Google und Discord werden nur registriert, wenn Client-ID und Client-Secret
vorliegen. Redirect-URIs sind
`https://<domain>/api/auth/external/google-signin` und
`https://<domain>/api/auth/external/discord-signin`. Steam nutzt OpenID 2.0 ausschließlich als
verknüpften Account; ein Steam Web API Key ist nur für Profilname und Avatar
erforderlich.
