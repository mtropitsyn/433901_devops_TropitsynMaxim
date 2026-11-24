#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."

if [ -e .env ]; then
    printf '%s\n' '.env exists; settings preserved.'
    exit 0
fi

umask 077
password=$(od -An -N24 -tx1 /dev/urandom | tr -d ' \n')
printf '%s\n' 'APP_PORT=5316' 'PROMETHEUS_PORT=19016' 'GRAFANA_PORT=13016' \
    'MONITORING_SUBNET=10.216.16.0/24' "GRAFANA_ADMIN_PASSWORD=$password" > .env
printf '%s\n' 'Created .env. Grafana login: admin; password stored in .env.'
