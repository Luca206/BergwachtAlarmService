#!/bin/bash

# This script deletes the worker service on a Linux system.

# === Configuration ===
WORKERNAME="dashboardalarmservice.service"
SERVICE_DIR="/etc/systemd/system"
INSTALLATION_DIR="/opt/dashboardalarmservice"

# === stop worker service ===
echo "Stopping worker service: $WORKERNAME"
sudo systemctl stop $WORKERNAME

# === disable worker service ===
echo "Disabling worker service: $WORKERNAME"
sudo systemctl disable $WORKERNAME

# === delete worker service file ===
echo "Deleting worker service file: $SERVICE_DIR/$WORKERNAME"
sudo rm $SERVICE_DIR/$WORKERNAME

# === re-exec systemd manager ===
echo "Re-executing systemd manager to apply changes"
sudo systemctl daemon-reexec

# === remove installation directory ===
echo "Removing installation directory: $INSTALLATION_DIR"
sudo rm -rf $INSTALLATION_DIR