#!/bin/bash

# === Configuration ===
SOLUTION_DIR="/home/luca/GitHub/Luca206/BergwachtDashboardMonitor"
PROJECT_PATH="$SOLUTION_DIR/src/AlarmService"
PUBLISH_PATH="$SOLUTION_DIR/deploy/AlarmService"
TARGET_DIR="/opt/dashboardalarmservice"
SERVICE_NAME="dashboardalarmservice.service"
EXECUTABLE_NAME="AlarmService"
RUNTIME_ID="linux-x64"
ENVIRONMENT="Production"

# === 1. Publish the Project ===
echo "Publishing .NET WorkerService..."
dotnet publish "$PROJECT_PATH" \
    -c Release \
    -r $RUNTIME_ID \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeAllContentForSelfExtract=true \
    -o "$PUBLISH_PATH"

if [ $? -ne 0 ]; then
    echo "Publish failed."
    exit 1
fi