#!/usr/bin/env bash
# Run only on the target Ubuntu/Debian server, after cloning this repository.
set -euo pipefail
if [ "$EUID" -ne 0 ]; then
    printf '%s\n' 'Run with sudo: sudo bash scripts/install-docker-server.sh' >&2
    exit 1
fi
if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    printf '%s\n' 'Docker and Compose are already installed. No packages were changed.'
    exit 0
fi
. /etc/os-release
case "$ID" in
    ubuntu) codename=${UBUNTU_CODENAME:-$VERSION_CODENAME} ;;
    debian) codename=$VERSION_CODENAME ;;
    *) printf '%s\n' 'This installer supports Ubuntu and Debian only. Use the Docker instructions for your OS.' >&2; exit 1 ;;
esac

# Do not remove or replace an existing alternative container runtime automatically.
for package in docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc; do
    if dpkg-query -W -f='${Status}' "$package" 2>/dev/null | grep -q 'install ok installed'; then
        printf 'Existing package %s conflicts with Docker CE. Follow the official installation guide for this server.\n' "$package" >&2
        exit 1
    fi
done
apt-get update
apt-get install -y ca-certificates curl
install -m 0755 -d /etc/apt/keyrings
curl -fsSL "https://download.docker.com/linux/$ID/gpg" -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
cat > /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/$ID
Suites: $codename
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker
docker version
docker compose version
