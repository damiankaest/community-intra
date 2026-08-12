# Raspberry Pi / Heimnetz

Dieser Compose-Stack baut Community Intranet ohne den optionalen x64-CS2-Demo-
Analyzer und läuft dadurch auch auf einem 64-Bit-Raspberry-Pi-OS.

```bash
cd deploy/raspberry-pi
cp .env.example .env
# Passwörter, LAN_HOST und optional OPENAI_API_KEY in .env setzen
docker compose up -d --build
```

Danach ist das Mystery-Spiel unter
`http://raspberrypi.local:8080/mistery` erreichbar. Ohne `OPENAI_API_KEY` wird
der lokale Demo-Fall verwendet; der Schlüssel liegt ausschließlich in der
Backend-Container-Umgebung.
