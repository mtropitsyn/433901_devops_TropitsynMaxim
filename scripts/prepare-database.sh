#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
compose=(docker compose)
if [ "$#" -gt 0 ]; then compose+=(-p "$1"); fi

"${compose[@]}" exec -T db sh -c \
    'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" exec mariadb --user=root' < database/prepare.sql
printf '%s\n' 'Database 16-WebCalculatorDb is ready; calculator has access. Existing data was preserved.'
