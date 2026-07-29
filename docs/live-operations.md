# Live Operations und Satisfactory

Phase 10 bindet optional einen Satisfactory Dedicated Server an eine
Organisation an. Das Intranet bleibt ohne Serververbindung vollständig
nutzbar.

## Was angezeigt wird

- Erreichbarkeit und API-Gesundheit
- aktive Session und Spielerzahl
- Tech-Tier und Projektphase
- aktiver Meilenstein, Laufzeit, Pause und Tickrate
- Aktualisierung alle 30 Sekunden sowie manuell

Alle aktiven Mitglieder dürfen den Status lesen. Nur Owner und Administratoren
dürfen die Verbindung testen, speichern oder entfernen. Der Chat und das
WebMCP-Werkzeug `community_get_live_server_status` greifen ausschließlich
lesend auf dieselbe autorisierte Status-API zu.

## Satisfactory vorbereiten

Die HTTPS-API läuft auf dem Spielport des Dedicated Servers, standardmäßig TCP
`7777`, unter `/api/v1`. Der Server beziehungsweise der Hoster muss eingehende
TCP-Verbindungen von der Hetzner-VM auf diesem Port erlauben.

In der Konsole des Satisfactory Dedicated Servers wird ein dauerhaftes
Application Token erzeugt:

```text
server.GenerateAPIToken
```

Das Token direkt kopieren und wie ein Passwort behandeln. Alte Application
Tokens lassen sich mit `server.InvalidateAPITokens` ungültig machen.

## Verbindung im Intranet

1. Als Owner oder Administrator `Gameserver` öffnen.
2. Einstellungen öffnen und Anzeigename, Host ohne `https://`, Port und
   Application Token eintragen.
3. `Verbindung testen` wählen.
4. Bei einem selbstsignierten Zertifikat den angezeigten SHA-256-Fingerprint
   zusätzlich beim Hoster oder auf dem Gameserver prüfen und erst dann
   übernehmen.
5. Erneut testen und `Sicher speichern` wählen.

Das Token wird im Browser nicht gespeichert und von der API nie wieder
zurückgegeben. Im Backend liegt es verschlüsselt über ASP.NET Data Protection.
Die Produktions-Keys werden im Docker-Volume `backend-data-protection`
persistiert, damit Container-Updates die Entschlüsselung nicht zerstören.

## Sicherheitsgrenzen

- Nur öffentliche IP-Adressen sind als Ziel erlaubt. Loopback-, private,
  Link-Local-, Metadaten- und Dokumentationsnetze werden gegen SSRF blockiert.
- DNS wird vor dem Verbindungsaufbau geprüft; der HTTP-Client verbindet sich
  anschließend nur mit den geprüften Adressen und folgt keinen Redirects.
- Gültige öffentliche TLS-Zertifikate funktionieren direkt.
- Selbstsignierte Zertifikate werden erst nach expliziter
  SHA-256-Fingerprint-Bestätigung akzeptiert.
- Ändert sich ein gepinntes Zertifikat, bleibt die Verbindung gesperrt, bis der
  neue Fingerprint bewusst bestätigt wurde.
- Phase 14 ergänzt ausschließlich das Auflisten und Herunterladen von
  Spielständen für die interne Analyse. Neustart, Shutdown, Save-Upload und
  Konsolenbefehle bleiben absichtlich nicht implementiert.

## Save-Import

Im Bereich `Fabriken` kann jedes inhaltlich berechtigte Mitglied eine lokale
`.sav`-Datei bis 200 MB importieren. Owner und Administratoren können
zusätzlich den neuesten Save über die konfigurierte Serververbindung laden.
Dafür benötigt das Application Token Rechte für `EnumerateSessions` und
`DownloadSaveGame`.

Beide Wege laufen durch denselben internen Parser. Die rohe Save-Datei wird
nicht dauerhaft gespeichert. Der Browser-Chat erhält über WebMCP nur die
fertige Zusammenfassung und niemals API-Token oder Save-Binärdaten.

Grundlage ist die mit dem Spiel ausgelieferte
`DedicatedServerAPIDocs.md`, die in der
[offiziellen Satisfactory-Wiki](https://satisfactory.wiki.gg/wiki/Dedicated_servers/HTTPS_API)
gespiegelt wird.
