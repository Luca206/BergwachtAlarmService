#!/bin/bash

# This script activates the worker service on a Linux system.

# === Configuration ===
WORKERNAME="dashboardalarmservice.service"

# === re-exec systemd manager ===
echo "Re-executing systemd manager to apply changes"
sudo systemctl daemon-reexec

# === enable worker service ===
echo "Enabling worker service: $WORKERNAME"
sudo systemctl enable $WORKERNAME

# === start worker service ===
echo "Starting worker service: $WORKERNAME"
sudo systemctl start $WORKERNAME

# === check status of worker service ===
echo "Checking status of worker service: $WORKERNAME"
sudo systemctl status $WORKERNAME