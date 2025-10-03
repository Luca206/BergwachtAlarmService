# ------------------------------------------------
# STAGE 1: BUILD
# ------------------------------------------------
# Verwenden des .NET 9 SDK-Images für den Build-Prozess
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Die Build Arguments (ARGs) für die NuGet-Authentifizierung definieren.
# Diese sind NICHT im finalen Image enthalten und werden beim 'docker build' übergeben.
ARG NUGET_USER
ARG NUGET_PAT

# 1. Projektmappendatei (.sln) und .csproj-Dateien in den Container kopieren
# Wir müssen zwei Ebenen nach oben gehen, um die .sln zu erreichen.
# Von /src/AlarmService/Dockerfile aus:
# Kopiert die .sln-Datei, die sich in '../' befindet, in das WORKDIR '/src'.
COPY ../BergwachtDashboardMonitor.sln .

# Kopiert die .csproj-Datei und ihren gesamten Ordner in das WORKDIR '/src'.
# Der Pfad zur .csproj innerhalb des Containers wird: /src/AlarmService/AlarmService.csproj
COPY AlarmService/ AlarmService/
COPY AlarmServiceTests/ AlarmServiceTests/

# 2. Authentifizierung für den privaten NuGet-Feed hinzufügen
# Nutzen Sie den 'dotnet nuget add source'-Befehl, um die Credentials sicher zu übergeben.
# Der Username ist meist beliebig, das Personal Access Token (PAT) ist das 'Passwort'.
RUN dotnet nuget add source "https://nuget.pkg.github.com/bergwacht-bayern/index.json" \
    --name "bergwacht-bayern" \
    --username "${NUGET_USER}" \
    --password "${NUGET_PAT}" \
    --store-password-in-clear-text

# 3. Nuget-Pakete wiederherstellen
# Führt 'dotnet restore' für die gesamte Projektmappe durch
RUN dotnet restore "BergwachtDashboardMonitor.sln"

# 4. Restliche Quelldateien kopieren
# Der Kopiervorgang ist auf der Build-Ebene des WORKDIR /src
COPY . .

# 5. Anwendung veröffentlichen
# Wichtig: Der Pfad zur .csproj muss relativ zur .sln (im WORKDIR /src) sein: AlarmService/AlarmService.csproj
RUN dotnet publish "AlarmService/AlarmService.csproj" -c Release -o /app/publish /p:SelfContained=false /p:UseAppHost=false

# ------------------------------------------------
# STAGE 2: FINAL (RUNTIME)
# ------------------------------------------------
# Verwenden des schlanken .NET 9 Runtime-Images
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Die veröffentlichten Dateien von der 'build'-Stage kopieren
COPY --from=build /app/publish .

# Der .NET WorkerService als Einstiegspunkt definieren
ENTRYPOINT ["dotnet", "AlarmService.dll"]