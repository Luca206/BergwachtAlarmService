1. Vorbereitung und Hinzufügen des Docker GPG-Schlüssels
# Update der Paketliste und Installation notwendiger Tools
sudo apt update
sudo apt install ca-certificates curl gnupg

# Erstellen des Verzeichnisses für den Schlüssel
sudo install -m 0755 -d /etc/apt/keyrings

# Hinzufügen des offiziellen GPG-Schlüssels von Docker
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

# Korrekte Dateiberechtigung setzen
sudo chmod a+r /etc/apt/keyrings/docker.gpg

2. Einrichten des Docker-Repositories
# Hinzufügen des Docker-Repositories zur APT-Quellliste
echo \
  "deb [arch=\"$(dpkg --print-architecture)\" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  \"$(. /etc/os-release && echo "$VERSION_CODENAME")\" stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

3. Installation der Docker-Pakete
# Paketliste erneut aktualisieren, um das neue Repository einzubinden
sudo apt update

# Installation der Docker Engine, CLI und Container-Runtime
sudo apt install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

4. Nach der Docker-Installation (Wichtig)
# Fügen Sie Ihren aktuellen Benutzer zur 'docker'-Gruppe hinzu
sudo usermod -aG docker $USER 

# System neustarten

# Überprüfen Sie die Installation (nach dem Neustart oder einer Neuanmeldung!)
docker run hello-world