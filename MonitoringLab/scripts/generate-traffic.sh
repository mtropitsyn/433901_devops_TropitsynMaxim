#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."

port=${APP_PORT:-}
if [[ -z $port && -r .env ]]; then
    port=$(sed -n 's/^APP_PORT=//p' .env | tr -d '\r')
fi
port=${port:-5316}
case "${1:-normal}" in
    normal) path=/ ;;
    slow) path=/monitoring/slow ;;
    errors) path=/monitoring/error ;;
    *) printf '%s\n' 'Usage: bash scripts/generate-traffic.sh [normal|slow|errors]'; exit 1 ;;
esac

for ((i=1; i<=60; i++)); do
    code=$(curl --silent --show-error --max-time 5 --output /dev/null \
        --write-out '%{http_code}' "http://127.0.0.1:$port$path")
    printf '%s %s HTTP %s\n' "$i/60" "$path" "$code"
    sleep 1
done
