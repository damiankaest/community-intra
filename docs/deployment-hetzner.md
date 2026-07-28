# Deployment auf einer Hetzner-VM

Die Anwendung läuft als Docker-Compose-Stack hinter Caddy. Caddy terminiert
HTTPS und erneuert das TLS-Zertifikat automatisch. Frontend, Backend und
PostgreSQL teilen sich ein internes Docker-Netz; nur Caddy veröffentlicht
Ports auf der VM.

## Voraussetzungen

- eine Linux-VM mit Docker Engine und Docker Compose Plugin
- eine Domain oder Subdomain
- ein `A`-Record auf die öffentliche IPv4-Adresse der VM
- optional ein `AAAA`-Record, wenn IPv6 auf der VM korrekt konfiguriert ist
- eingehend freigegebene TCP-Ports `22`, `80` und `443`
- optional UDP `443` für HTTP/3

PostgreSQL-Port `5432` darf in der Hetzner-Firewall nicht öffentlich
freigegeben werden.

## Erster Start

Repository auf die VM klonen und in das Projekt wechseln:

```bash
git clone https://github.com/damiankaest/community-intra.git
cd community-intra
cp deploy/.env.production.example deploy/.env.production
```

In `deploy/.env.production` müssen Domain, Datenbankpasswort und JWT-Schlüssel
ersetzt werden. Ein geeigneter Schlüssel lässt sich auf der VM erzeugen:

```bash
openssl rand -base64 48
```

`APP_DOMAIN` und `JWT_ISSUER` müssen dieselbe öffentliche HTTPS-Domain
verwenden. Danach:

```bash
docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  up -d --build
```

Die Anwendung ist nach DNS-Auflösung und Zertifikatsausstellung unter
`https://<APP_DOMAIN>` erreichbar.

## Betrieb

Status und Logs:

```bash
docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  ps

docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  logs -f --tail=200
```

Aktualisieren:

```bash
git pull --ff-only
docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  up -d --build
```

Datenbank sichern:

```bash
docker compose \
  --env-file deploy/.env.production \
  -f deploy/docker-compose.production.yml \
  exec -T postgres pg_dump \
  -U community_intranet \
  -d community_intranet \
  -Fc > community-intranet-$(date +%F).dump
```

Wenn `POSTGRES_USER` oder `POSTGRES_DB` geändert wurden, müssen die beiden
Werte im Backup-Befehl entsprechend angepasst werden. Backups sollten
regelmäßig von der VM auf ein getrenntes Ziel kopiert und testweise
wiederhergestellt werden.

## Secrets und Firebase

`deploy/.env.production` bleibt ausschließlich auf der VM und wird von Git
ignoriert. Firebase ist für diesen Stack nicht erforderlich: Benutzer,
Organisationen und Sitzungen werden durch ASP.NET Core Identity, JWT und
PostgreSQL verwaltet.

Firebase kann später optional für Push-Benachrichtigungen oder externe
Anmeldeanbieter ergänzt werden. Das Firebase-Projekt und seine Credentials
werden dann manuell im Firebase-Dashboard erstellt und als Secrets auf der VM
hinterlegt; Credentials gehören niemals in das Repository.
