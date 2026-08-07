# Deployment auf einer Hetzner-VM

Die Anwendung läuft als Docker-Compose-Stack hinter Caddy. Caddy stellt
automatisch HTTPS bereit. Backend und Frontend werden in GitHub Actions gebaut
und unter der jeweiligen Commit-ID in der GitHub Container Registry
veröffentlicht. Die VM benötigt deshalb keinen Zugriff auf das Repository und
das Deployment funktioniert auch nach einer Umstellung auf ein privates Repo.

PostgreSQL, Backend und Frontend sind nur im internen Docker-Netz erreichbar.
Lediglich Caddy veröffentlicht die Ports `80/443`.

## Einmalige Vorbereitung

Vor dem ersten GitHub-Deployment sind auf der VM und im DNS vier Schritte nötig:

1. Docker Engine mit aktuellem Docker-Compose-Plugin nach der
   [offiziellen Docker-Anleitung](https://docs.docker.com/engine/install/)
   installieren.
2. Einen SSH-Benutzer für das Deployment vorbereiten. Er muss Docker ohne
   interaktive `sudo`-Abfrage ausführen dürfen.
3. Das Verzeichnis `/opt/community-intra` für diesen Benutzer anlegen.
4. Einen `A`-Record der gewünschten Domain auf die öffentliche IPv4-Adresse der
   VM setzen.

Beispiel für einen bereits vorhandenen Benutzer `deploy`:

```bash
sudo usermod -aG docker deploy
sudo install -d -m 700 -o deploy -g deploy /opt/community-intra
```

Nach der Gruppenänderung muss sich der Benutzer einmal neu anmelden. Folgender
Befehl muss danach ohne `sudo` funktionieren:

```bash
docker info
docker compose version
```

In der Hetzner-Firewall müssen TCP `22`, `80` und `443` freigegeben werden.
UDP `443` ist optional für HTTP/3. PostgreSQL `5432` darf nicht öffentlich
freigegeben werden.

## Eigener SSH-Schlüssel für GitHub Actions

Auf dem eigenen Rechner einen separaten Schlüssel ohne Passphrase erzeugen:

```bash
ssh-keygen \
  -t ed25519 \
  -C "community-intra-github-deploy" \
  -f community-intra-deploy
```

Den öffentlichen Schlüssel `community-intra-deploy.pub` beim Deployment-
Benutzer der VM in `~/.ssh/authorized_keys` eintragen. Der private Schlüssel
`community-intra-deploy` wird später als GitHub Secret gespeichert.

Den Host-Key der VM erfassen:

```bash
ssh-keyscan -H <VM-IP>
```

Der ausgegebene Fingerabdruck sollte einmal direkt auf der VM mit dem
Fingerabdruck unter `/etc/ssh/ssh_host_ed25519_key.pub` verglichen werden. Erst
danach gehört die vollständige `ssh-keyscan`-Zeile in GitHub.

## GitHub-Environment und Secrets

Unter `Settings → Environments` ein Environment namens `production` erstellen
und dort diese Secrets hinterlegen. GitHub beschreibt Environment-Secrets in
der
[offiziellen Anleitung](https://docs.github.com/actions/deployment/targeting-different-environments/using-environments-for-deployment).

| Secret                    | Inhalt                                                  |
| ------------------------- | ------------------------------------------------------- |
| `APP_DOMAIN`              | Domain ohne Protokoll, zum Beispiel `intra.example.com` |
| `HETZNER_HOST`            | öffentliche IPv4-Adresse oder SSH-Hostname              |
| `HETZNER_USER`            | SSH-Benutzer, zum Beispiel `deploy`                     |
| `HETZNER_SSH_PRIVATE_KEY` | vollständiger privater Deployment-Schlüssel             |
| `HETZNER_KNOWN_HOSTS`     | verifizierte vollständige `ssh-keyscan`-Ausgabe         |
| `POSTGRES_PASSWORD`       | langes zufälliges Datenbankpasswort                     |
| `JWT_SIGNING_KEY`         | zufälliger Schlüssel mit mindestens 32 Zeichen          |
| `OPENAI_API_KEY`          | API-Schlüssel für die serverseitige KI-Arbeitsplanung   |
| `SPOTIFY_CLIENT_ID`       | Client-ID der Spotify Developer App                     |
| `SPOTIFY_CLIENT_SECRET`   | Client-Secret der Spotify Developer App                 |

Geeignete zufällige Werte:

```bash
openssl rand -base64 36
openssl rand -base64 48
```

Bei einem abweichenden SSH-Port kann zusätzlich unter
`Settings → Secrets and variables → Actions → Variables` die Variable
`HETZNER_SSH_PORT` gesetzt werden. Ohne Variable wird Port `22` verwendet.

Optional kann dort außerdem `AI_MODEL` gesetzt werden. Ohne diese Variable
verwendet das Backend `gpt-5.6`.

Das Datenbankpasswort darf nach dem ersten Start nicht einfach im GitHub Secret
geändert werden: Bei einem bestehenden PostgreSQL-Volume muss das Kennwort
zusätzlich in PostgreSQL rotiert werden. Ein neuer JWT-Schlüssel meldet alle
aktiven Sitzungen ab.

Eine Änderung von `OPENAI_API_KEY` oder `AI_MODEL` erfordert lediglich ein
erneutes Deployment; die PostgreSQL-Daten bleiben unverändert.

Für die Party-Musikfunktion muss in der Spotify Developer App zusätzlich exakt
`https://<APP_DOMAIN>/api/parties/spotify/callback` als Redirect URI hinterlegt
sein. Danach verbindet der Party-Admin seinen Spotify-Premium-Account direkt in
der Party-Oberfläche. Refresh-Tokens werden serverseitig verschlüsselt gespeichert;
Gäste erhalten weder Spotify-Zugangsdaten noch Tokens.

Der Produktions-Stack persistiert zusätzlich die ASP.NET-Data-Protection-Keys
im Volume `backend-data-protection`. Dieses Volume wird für die verschlüsselte
Satisfactory-Serverkonfiguration und Spotify-Refresh-Tokens benötigt und darf bei normalen Deployments
nicht gelöscht werden. Für die Live-Anbindung ist kein weiteres GitHub Secret
erforderlich; Host, Port, API-Token und bestätigter Zertifikat-Fingerprint
werden später durch einen Owner in der Intranet-Oberfläche eingetragen.

## Deployment starten

Unter `Actions → Deploy production → Run workflow` den Workflow auf `main`
starten. Der Workflow:

1. prüft, ob alle Secrets vorhanden sind,
2. baut Backend und Frontend,
3. veröffentlicht beide Images mit der aktuellen Commit-ID in GHCR,
4. überträgt Caddy-, Compose- und Laufzeitkonfiguration per SSH,
5. lädt auf der VM exakt diese Images,
6. startet den Stack inklusive Migrationen,
7. wartet auf die Container-Healthchecks und
8. prüft Frontend und `/api/health` über die öffentliche HTTPS-Domain.

Das Deployment ist zunächst absichtlich manuell. So führt nicht jeder Merge
sofort eine Produktionsänderung aus. Nach erfolgreicher Validierung kann der
Workflow später zusätzlich bei jedem erfolgreichen Merge auf `main` gestartet
werden.

## Betrieb und Diagnose

Auf der VM:

```bash
cd /opt/community-intra

docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  ps

docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  logs -f --tail=200
```

Die Datei `deploy/.env.production` enthält Secrets, ist nur für den
Deployment-Benutzer lesbar und darf nicht aus der VM kopiert oder committed
werden.

## Datenbank sichern

```bash
cd /opt/community-intra

docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  exec -T postgres pg_dump \
  -U community_intranet \
  -d community_intranet \
  -Fc > community-intranet-$(date +%F).dump
```

Backups müssen regelmäßig auf ein getrenntes Ziel kopiert und testweise
wiederhergestellt werden. Das benannte Docker-Volume `postgres-data` bleibt
bei neuen Deployments erhalten.

## Manuelles Deployment als Rückfalloption

Wenn GitHub Actions nicht verfügbar ist, kann das öffentliche Repository auf
der VM geklont und dort gebaut werden:

```bash
git clone https://github.com/damiankaest/community-intra.git
cd community-intra
cp deploy/.env.production.example deploy/.env.production

docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  up -d --build --wait
```

## Firebase

Firebase ist für diesen Stack nicht erforderlich. Benutzer, Organisationen und
Sitzungen werden durch ASP.NET Core Identity, JWT und PostgreSQL verwaltet.
Firebase kann später optional für Push-Benachrichtigungen oder externe
Anmeldeanbieter ergänzt werden.
