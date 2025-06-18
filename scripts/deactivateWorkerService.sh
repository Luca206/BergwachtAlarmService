#!/bin/bash

# This script deactivates the worker service on a Linux system.

# === Configuration ===
WORKERNAME="dashboardalarmservice.service"

# === stop worker service ===
echo "Stopping worker service: $WORKERNAME"
sudo systemctl stop $WORKERNAME

# === disable worker service ===
echo "Disabling worker service: $WORKERNAME"
sudo systemctl disable $WORKERNAME

# === check status of worker service ===
echo "Checking status of worker service: $WORKERNAME"
sudo systemctl status $WORKERNAME