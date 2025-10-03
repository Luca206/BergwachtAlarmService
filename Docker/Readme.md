# Create Docker-Container
cd BergwachtDashboardMonitor/src

docker build \
  --build-arg NUGET_USER="GH Username" \
  --build-arg NUGET_PAT="Github PAT" \
  -t my-alarm-service:latest \
  -f AlarmService/Dockerfile . 

# Run Docker-Container
docker run -d --name alarm-worker \
  -v ${HOST_CONFIG_PATH}:/configs \
  my-alarm-service:latest

# Read logs
docker logs alarm-worker

# Remove Docker-Container
docker rm alarm-worker

---
# SystemD Service einrichten
## systemd-service datei kopieren
sudo cp {Path}/systemd-service/bergwacht-docker-worker.service /etc/systemd/system/'

# docker container bauen
docker build \
  --build-arg NUGET_USER="GH Username" \
  --build-arg NUGET_PAT="Github PAT" \
  -t my-alarm-service:latest \
  -f AlarmService/Dockerfile . 

# systemd neu laden, um die neue .service-Datei zu erkennen
sudo systemctl daemon-reload

# Dienst aktivieren (startet beim Booten)
sudo systemctl enable bergwacht-worker.service

# Dienst starten
sudo systemctl start bergwacht-worker.service

# Status prüfen
systemctl status bergwacht-worker.service