# RIGX — Running the project

## Requirements
.NET SDK 10, Docker (PostgreSQL + tests use Testcontainers), Node 24 (frontend, phase 4).

## Local setup
```bash
cp .env.example .env            # then set local passwords in .env
docker compose up -d postgres   # PostgreSQL on 127.0.0.1:5433
dotnet tool restore             # dotnet-ef
dotnet build HARDWARE_STORE.slnx
```

## Database migrations
Each module has its own DbContext and migrations folder (`src/Modules/<Module>/**/Migrations`).
Apply all of them, in order, with one command (uses `ConnectionStrings:Migrations` if set, else `Default`):
```bash
dotnet run --project src/Host/HardwareStore.Api -- --migrate
```
Add a migration:
```bash
dotnet ef migrations add <Name> --project src/Modules/<Module> --startup-project src/Host/HardwareStore.Api
```

## First owner account
One-time interactive command (see `src/Host/HardwareStore.Api/OwnerBootstrapCommand.cs`): run the API with `--bootstrap-owner` and `OwnerBootstrap:Enabled=true`. It asks for username, email and full name only; the owner then signs in with a 6-digit code sent to that email (or Google once configured). There are no passwords.

## Tests
```bash
dotnet test HARDWARE_STORE.slnx            # needs Docker running (real PostgreSQL)
dotnet test --filter "FullyQualifiedName~Retail"   # one area only
```

## Demo data (Development)
The preview initializer can fill a local database with a realistic demo store for testing the storefront and the admin:
~32 published stock items (`RX-…`, new and used, four of them sold out), 3 published builds (`BLD-…`), 10 customers with
saved addresses, 12 online orders covering every customer status, 5 paid POS sales, 6 expenses, the five published policies
and six general FAQs. Shipping rates, the pickup address and the demo payment methods are filled only while those settings
are still empty.

It runs through the real module services (stock, invoices, warranties, money, audit), so it behaves like real use.
It is idempotent: calling it again creates nothing and reports 0 for the parts that already exist.

Enable it in Development only (never production):
```bash
Preview__Enabled=true
Preview__BootstrapKey=<a local key you choose>
Preview__OwnerEmail=owner@example.com
Preview__OwnerUsername=owner
Preview__OwnerFullName="RIGX Owner"
```
The app also needs `ConnectionStrings:Migrations` (same host, port and database as `Default`, any username) because the
initializer applies every module migration first. Then, with the API running:
```bash
curl -X POST http://localhost:8080/api/v1/development/preview/initialize \
  -H "X-Preview-Bootstrap-Key: <the same local key>"
```
The response shows the counts of what this call created (0 for data that was already there). The seeded owner signs in with
the usual email code; the demo customers use `demo1@example.com` … `demo10@example.com` with phones `01000000001` … `01000000010`.

Reading the sign-in code locally (Development with Preview enabled): ask for the code on the login page, then
```bash
curl "http://localhost:8080/api/v1/development/preview/email-code?email=owner@example.com" \
  -H "X-Preview-Bootstrap-Key: <the same local key>"
```
returns the newest code sent to that address (`code`, `purpose` = sign-in or step-up, `expiresAtUtc`; 404 when none). Same guards
as the seed; it does not exist outside Development. Playwright signs in this way.

To test real email delivery instead: Development keeps emails in memory (never sent), so run the API under any other environment name with `Identity:Email:Smtp` pointed at a local SMTP sink (no TLS), plus the non-Development
requirements: `DataProtection:KeyRingPath`, `DataProtection:CertificatePath`/`CertificatePassword` (a throwaway self-signed PFX)
and HTTPS (the `__Host-` cookies and CSRF need it; serve on `https://localhost` with the dev certificate). Point the web app at
it through a local HTTP→HTTPS bridge, because the Next proxy rejects the untrusted dev certificate. Stop any other local API
on the same database first, or its outbox worker takes the email.

## End-to-end tests (Playwright)
Critical flows run in Chromium against a local stack (never production): PostgreSQL, the API in Development with Preview enabled (see "Demo data"), and the web app (`npm run dev -w @rigx/web`, which proxies `/api` to the API). The specs live in `apps/web/e2e/`: sign-in with a wrong code, a new customer ordering and tracking, the owner taking an order to completed, a POS sale, a purchase received into stock, the warranty lookup, sold-out parts listed last in the catalog (with the old `/sold` page gone), and SEO (old RX link → 301 to the slug, canonical, Product JSON-LD, sitemap, robots). They are not part of `npm test`.

| Variable | Meaning | Default |
|---|---|---|
| `E2E_BASE_URL` | web origin the browser and the API calls use | `http://localhost:3000` |
| `E2E_PREVIEW_KEY` | the API's `Preview__BootstrapKey` (required) | none |
| `E2E_OWNER_EMAIL` | the seeded owner | `owner@example.com` |

```bash
npx playwright install chromium   # once, from apps/web
E2E_PREVIEW_KEY=<the local key> npm run e2e -w @rigx/web
```
A run seeds the demo data (idempotent; seeding signs every session out, so it comes first), signs the owner in through the login page once (`login.spec.ts`, saved to the git-ignored `apps/web/e2e/.auth`) and reads sign-in codes from the preview endpoint. After one full run, a single spec can be repeated with `npx playwright test <spec> --no-deps` (it reuses the saved owner state). Notes: every run uses a few demo parts and creates test orders, so reset the demo data now and then; the API allows 10 sign-in code requests per hour per IP, so avoid more than a few full runs an hour; guest checkout is off by default, so the storefront spec signs in as a new customer. While another `next dev` runs in `apps/web` (Next allows one per folder), run the specs against a production build instead: `next build` then `next start -p <port>` with `RIGX_API_ORIGIN` set for both, and an API with its own database (two APIs on one database steal each other's sign-in emails from the outbox). The HTML report is written to `apps/web/playwright-report/` (git-ignored).

## Configuration
- Secrets only in `.env` locally / the host's secret store in production. Never committed.
- `NEXT_PUBLIC_SITE_URL` (web): the public origin used for canonical URLs, hreflang, the sitemap, robots and Open Graph. It is baked in at `next build`, so set it for the production build (`https://rigxhardware.com`); unset means `http://localhost:3000`.
- SMTP variables are listed (commented) in `.env.example`; Development uses an in-memory email provider.

## Backups and restore
**What a backup is.** One zip per day, `rigx-backup-{yyyyMMdd-HHmmss}Z.zip`: `database.dump` (`pg_dump --format=custom --no-owner` of the whole database: data, settings, audit log), `media/` (every photo, incl. `private/` and `settings/`) and `manifest.json` (time, app version, database size, file count, SHA-256 of the dump, applied migrations per module). It is built in a temp folder, then copied to **every** destination (`.tmp` first, then moved). A run counts as successful only if all destinations got the file; a failed attempt is recorded and retried at the next hourly tick.

**Configuration** (server config only, never the web API):
```
System__Backup__Destinations__0=/backups/local            # a local volume
System__Backup__Destinations__1=/mnt/gdrive/rigx-backups  # a synced off-host folder (mounted Google Drive / rclone mount)
System__Backup__PgDumpPath=pg_dump                        # defaults
System__Backup__PgRestorePath=pg_restore
ConnectionStrings__Backup=Host=...;Username=...;Password=...   # optional; falls back to ConnectionStrings:Migrations, then Default
```
With no destination, backups are off and the backup and reset endpoints answer 409. The role in the connection string must read every table (including the audit log) and have `CREATEDB` for the restore drill. The API image installs `postgresql-client-17` (PGDG) so `pg_dump` matches the server; the password goes to the tools only through `PGPASSWORD`. The destination folders must be writable by the container user.

**Schedule.** Store settings, section `backup` (`PUT /api/v1/admin/storefront/settings/backup`): `enabled` (default on), `hourCairo` 0-23 (default 3), `retentionDays` 3-90 (default 14). The hourly job runs a backup once the Cairo hour is reached if no scheduled backup succeeded today (Cairo date), then deletes archives older than the retention from each destination (only files named like a backup; the age comes from the name).

**Pages / API** (`/api/v1/admin/system/backups`): list and run now (`System.ManageBackups`), restore drill `POST /{id}/verify` (`System.ManageBackups`), download `GET /{id}/download` (owner-only `System.BackupDownload` + email code; the zip holds all customer data). Run a drill after the first deployment and then now and then: it checks the SHA-256 against the manifest, restores into a temporary database `rigx_restore_check_*` (always dropped), counts tables and rows, and records the result on the run.

**Real restore (manual, never from the web).** The app cannot overwrite the live database on purpose.
1. Take the newest good archive (a run with a passed drill) from a destination; unzip it somewhere safe.
2. Stop the API (`docker compose stop api`) so nothing writes.
3. Restore the database: `pg_restore --clean --if-exists --no-owner --dbname=<db> --host=... --username=... database.dump` (run as a role allowed to drop and create the tables; then re-run `dotnet HardwareStore.Api.dll --migrate` if the archive is older than the deployed app).
4. Copy the unzipped `media/` folder back over the media root (`Catalog:MediaRoot`, default `App_Data/catalog-media`; in compose the `api_data_catalog_media` volume).
5. Start the API and check `/health/ready` and a sign-in.

## Data reset
`POST /api/v1/admin/system/reset` with `{ "sections": ["business", "content", "customers"], "confirm": "RESET" }`; owner-only (`System.ResetData`) with a fresh email code. A successful backup (trigger `BeforeReset`) is taken first; if backups are off or the backup fails, the answer is 409 and nothing is deleted.
- `business`: stock items and units, builds, purchases and sellers, orders (lines, payments, returns, reservations), invoices and their counters, warranties and extensions, money movements (accounts kept; balances fall back to the opening balance), expenses and recurring templates, partner earnings/payouts/capital/month closes, customer requests, saved builds, wishlists, and the private photos (request photos, receipts).
- `content`: articles with revisions, FAQ entries, the media library (assets, usages, image files).
- `customers` (needs `business` in the same call): accounts holding only the Customer role, with sessions, addresses, notifications, restrictions, Google links and pending codes.
- Never reset: staff and owner accounts, roles and permissions, store settings and logos, categories/brands/spec schemas, catalog products, audit logs (their link to a deleted customer becomes empty), backup runs, person-code counters and the database sequences behind public codes. Each module clears its own explicit table list in one transaction.
