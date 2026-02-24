FROM ubuntu:22.04

# Minimale Runtime-Abhaengigkeiten fuer Unity Server Build
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libgcc-s1 \
    libstdc++6 \
    && rm -rf /var/lib/apt/lists/*

# Non-root User fuer Sicherheit
RUN useradd -m -s /bin/bash gameserver

WORKDIR /app

# Server Build Output kopieren
COPY Builds/Server/ .

# Server-Binary ausfuehrbar machen
RUN chmod +x ./GameKit_HDRP

# Zu non-root User wechseln
USER gameserver

# FishNet Tugboat UDP Port
EXPOSE 7770/udp

# Konfiguration ueber Environment-Variablen (gelesen von ServerBootstrap)
ENV PORT=7770
ENV MAX_PLAYERS=100
ENV ADDRESS=0.0.0.0

# Unity Dedicated Server im Batch-Modus starten
# -logFile /dev/stdout leitet Unity-Logs an Docker-Logs weiter
ENTRYPOINT ["./GameKit_HDRP", "-batchmode", "-nographics", "-logFile", "/dev/stdout"]
