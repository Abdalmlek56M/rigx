#!/usr/bin/env bash
# One-time server preparation for RIGX (safe to run again: it never overwrites an existing value or file).
# - creates the backup and secrets folders,
# - creates deploy/.env from production.env.example with random database passwords,
# - creates the Data Protection certificate (sign-in cookies survive restarts) and its password.
# Prints no secret. Run from the repository root: bash deploy/init-server.sh
set -euo pipefail

APP_UID=1654   # the "app" user inside the .NET runtime image
ROOT=/srv/rigx
ENV_FILE="$(dirname "$0")/.env"
PFX="$ROOT/secrets/dataprotection.pfx"

sudo mkdir -p "$ROOT/backups" "$ROOT/secrets"
sudo chown "$APP_UID:$APP_UID" "$ROOT/backups"
sudo chmod 750 "$ROOT/backups"
sudo chmod 700 "$ROOT/secrets"

if [[ ! -f "$ENV_FILE" ]]; then
  cp "$(dirname "$0")/production.env.example" "$ENV_FILE"
  echo "created deploy/.env"
fi
chmod 600 "$ENV_FILE"

fill() {  # fill KEY with a random value when it is still empty
  local key=$1
  if grep -qE "^${key}=$" "$ENV_FILE"; then
    sed -i "s|^${key}=$|${key}=$(openssl rand -hex 24)|" "$ENV_FILE"
    echo "generated $key"
  fi
}
fill POSTGRES_PASSWORD
fill APP_DB_PASSWORD
fill DATAPROTECTION_CERT_PASSWORD

if ! sudo test -f "$PFX"; then
  password=$(grep -E '^DATAPROTECTION_CERT_PASSWORD=' "$ENV_FILE" | cut -d= -f2-)
  work=$(mktemp -d)
  openssl req -x509 -newkey rsa:3072 -sha256 -days 3650 -nodes -subj "/CN=rigx-dataprotection" \
    -keyout "$work/key.pem" -out "$work/cert.pem" 2>/dev/null
  DP_PASS="$password" openssl pkcs12 -export -inkey "$work/key.pem" -in "$work/cert.pem" -out "$work/dp.pfx" -passout env:DP_PASS
  sudo mv "$work/dp.pfx" "$PFX"
  rm -rf "$work"
  echo "created the Data Protection certificate"
fi
sudo chown "$APP_UID:$APP_UID" "$PFX"
sudo chmod 400 "$PFX"

chmod +x "$(dirname "$0")/offsite-backup.sh"
echo "done. Still to fill in deploy/.env: SMTP_APP_PASSWORD, GOOGLE_CLIENT_ID, SENTRY_DSN_API, SENTRY_DSN_WEB, GA_ID, META_PIXEL_ID"
