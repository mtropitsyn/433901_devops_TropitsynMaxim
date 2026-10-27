#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."

if [ -e .env ]; then
    printf '%s\n' '.env already exists; credentials and settings were preserved.'
    exit 0
fi

umask 077
new_password() { od -An -N32 -tx1 /dev/urandom | tr -d ' \n'; }
{
    printf '%s\n' 'COMPOSE_PROJECT_NAME=tropitsyn16' 'STUDENT_NUMBER=16' 'APP_PORT=5316' 'DB_PORT=53316'
    printf 'MARIADB_PASSWORD=%s\n' "$(new_password)"
    printf 'MARIADB_ROOT_PASSWORD=%s\n' "$(new_password)"
} > .env
printf '%s\n' 'Created .env with random passwords; website port 5316, database loopback port 53316.'
