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

# === 2. Copy executable to /opt ===
echo "Copying build to $TARGET_DIR ..."
sudo mkdir -p "$TARGET_DIR"
sudo rsync -av --exclude='*.pdb' "$PUBLISH_PATH"/ "$TARGET_DIR"/
sudo chmod +x "$TARGET_DIR/$EXECUTABLE_NAME"

# === 3. Create systemd service ===
echo "Copying systemd service file..."
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}"
sudo cp "$SOLUTION_DIR/systemd-service/$SERVICE_NAME" "$SERVICE_FILE"

# === 4. Enable service ===
echo "Enabling and starting service..."
sudo systemctl daemon-reexec
sudo systemctl enable $SERVICE_NAME
sudo systemctl restart $SERVICE_NAME

# === 5. Show status ===
echo "Status of $SERVICE_NAME:"
sudo systemctl status $SERVICE_NAME --no-pager

# === 6. Logging hint ===
echo "View logs with: journalctl -u $SERVICE_NAME -f"
