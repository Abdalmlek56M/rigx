#!/usr/bin/env bash
# Copies the RIGX daily backups off the server to Google Drive, encrypted (rclone crypt), and keeps 30 days there.
# The API writes one zip a day to $BACKUP_DIR (03:00 Cairo by default); cron runs this an hour later:
#   30 4 * * * /srv/rigx/app/deploy/offsite-backup.sh >> /srv/rigx/offsite-backup.log 2>&1
# Needs rclone with a crypt remote "rigxbackup" over a Google Drive remote (docs/11-launch-runbook.md).
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/srv/rigx/backups}"
REMOTE="${RIGX_BACKUP_REMOTE:-rigxbackup:}"
KEEP_DAYS="${RIGX_BACKUP_KEEP_DAYS:-30}"

echo "$(date -Is) copying new backups to $REMOTE"
# Only finished archives (the API writes .tmp first, then renames).
rclone copy "$BACKUP_DIR" "$REMOTE" --include "rigx-backup-*.zip" --max-age 72h --immutable
rclone delete "$REMOTE" --include "rigx-backup-*.zip" --min-age "${KEEP_DAYS}d"

newest=$(rclone lsf "$REMOTE" --include "rigx-backup-*.zip" | sort | tail -n 1)
if [[ -z "$newest" ]]; then
  echo "$(date -Is) ERROR: no backup on $REMOTE" >&2
  exit 1
fi
echo "$(date -Is) done, newest off-host backup: $newest"
