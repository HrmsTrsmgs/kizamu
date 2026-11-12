#!/usr/bin/env bash
set -eu

. /etc/os-release
if [ "$ID" != ubuntu ]; then
    echo 'このスクリプトはUbuntu用です。' >&2
    exit 1
fi
if [ "$(ps -p 1 -o comm=)" != systemd ]; then
    echo 'Dockerの起動にはWSLでsystemdを有効にしてください。' >&2
    exit 1
fi

if ! dpkg-query -W -f='${Status}' docker-ce 2>/dev/null | grep -q 'install ok installed'; then
    for package in docker.io docker-doc docker-compose podman-docker containerd runc; do
        if dpkg-query -W -f='${Status}' "$package" 2>/dev/null | grep -q 'install ok installed'; then
            echo "競合する可能性のあるパッケージがインストールされています: $package" >&2
            exit 1
        fi
    done
    apt-get update
    apt-get install -y --no-upgrade ca-certificates curl
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc
    printf 'deb [arch=%s signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu %s stable\n' "$(dpkg --print-architecture)" "${UBUNTU_CODENAME:-$VERSION_CODENAME}" > /etc/apt/sources.list.d/docker.list
    apt-get update
fi

apt-get install -y --no-upgrade docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker
docker version
docker compose version
