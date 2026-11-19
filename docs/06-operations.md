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

## Stock import from Excel
Admin → "استيراد المخزون" (`/admin/stock/import`, `Purchasing.ReceiveGoods`): download the template (`GET /api/v1/admin/retail/stock-import/template`), fill one row per model, upload for a dry run (`POST /api/v1/admin/retail/stock-import?dryRun=true`, multipart `file`, .xlsx ≤ 2 MB, ≤ 500 rows, ≤ 2,000 items), fix the rows marked red, then import (`dryRun=false`). The import is all-or-nothing in one transaction through the same factory as a purchase receipt (RX codes, slugs, units with source type `Import`); category by code or name, warranty months must be one of the store's warranty options; the same file (SHA-256, `stock_imports`) cannot be imported twice. Imported items show on the site only after 3 photos each.

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
- Production settings: `deploy/production.env.example` (copied to `deploy/.env` on the server; see "Production"). Development uses an in-memory email provider.

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

## Production (owner VPS, roadmap 4.8)
Owner facts (18 Nov 2026):
- **VPS:** Ubuntu 24.04, shared with two low-traffic sites: one served by host nginx, the other probably in Docker. The owner logs in over SSH with a user and password; the plan is an SSH key, then password login off (runbook §2). The server IP is never written in the repo (only in the owner's own notes / `deploy/.env` if ever needed).
- **Proxy:** host nginx already holds ports 80 and 443. RIGX adds one server block for `rigxhardware.com` that proxies to the RIGX compose services on loopback ports. No second reverse proxy, no Coolify. Before installing, the owner runs `sudo ss -tlnp` and `docker ps` (and the network list) and pastes the output, so nothing collides with the two live sites.
- **DNS / TLS:** `rigxhardware.com` is on Cloudflare (other subdomains already exist there). Records `@` and `www` are proxied; SSL mode Full (strict) for these two hostnames only (a Configuration Rule when the zone mode is not already Full (strict), so the other subdomains are untouched); nginx serves a Cloudflare Origin certificate (`/etc/ssl/rigx/origin.pem` + `.key`); Always Use HTTPS on.
- **Email:** Gmail SMTP (`smtp.gmail.com:587`, STARTTLS) from `rigxhardware@gmail.com` with an App Password (needs 2-step verification). The owner puts it in the server `deploy/.env` themselves; it is never pasted in chat or committed. Domain mail later.
- **Google sign-in:** a production OAuth client (Web application) in a Google Cloud project with no billing (the "Start free" trial is not needed for OAuth). Consent screen: name RIGX, support email, home page, privacy policy `/policies/privacy`, terms `/policies/terms`, authorized domain `rigxhardware.com`, published (basic scopes need no review). The button is Google Identity Services (popup + ID token checked by the API), so the client needs the JavaScript origin `https://rigxhardware.com` and **no redirect URI**; no client secret is stored anywhere.
- **Sentry:** free Developer plan, projects `rigx-api` (ASP.NET Core) and `rigx-web` (Next.js), one uptime monitor on `https://rigxhardware.com/health`.
- **DeepSeek:** the owner may let DeepSeek (OpenCode) draft deploy files; Opus reviews them. For 4.8 the files were written directly, so no brief was needed.

**Files** (`deploy/`):
| File | What |
|---|---|
| `compose.prod.yaml` | project `rigx`: `postgres` (no host port), `migrate` (one-shot `--migrate`, must exit 0), `api` (`127.0.0.1:${API_PORT:-8081}`), `web` (`127.0.0.1:${WEB_PORT:-3001}`); private network `${RIGX_SUBNET}` whose gateway is the API's trusted proxy |
| `production.env.example` | every setting; copied to `deploy/.env` (git-ignored, mode 600) |
| `init-server.sh` | one-time, idempotent: `/srv/rigx/backups` (owned by uid 1654, the image's `app` user), `/srv/rigx/secrets/dataprotection.pfx` (self-signed, 10 years, password in `.env`), random DB passwords; prints no secret |
| `nginx/rigxhardware.com.conf` | port 80 → https apex, `www` → apex, `/api/` → API, `/health` → API `/health/ready`, rest → web; HSTS (no `includeSubDomains`, other subdomains exist); 15 MB bodies |
| `nginx/rigx-cloudflare-real-ip.conf` | `set_real_ip_from` Cloudflare ranges + `CF-Connecting-IP`, included in the RIGX server block only |
| `offsite-backup.sh` | root cron 03:30 server time: `rclone copy` new archives to `rigxbackup:` (rclone **crypt** over `gdrive:rigx-backups`), deletes off-host copies older than 30 days, fails when none is there |
| `apps/web/Dockerfile` | Next.js standalone build; `NEXT_PUBLIC_*` are build args (site URL, Google client id, Sentry DSN, GA id, pixel id), so changing one needs `up -d --build web` |

**How requests flow.** Visitor → Cloudflare → host nginx (real IP from `CF-Connecting-IP`) → `X-Forwarded-For` = visitor, `X-Forwarded-Proto: https` → docker-proxy → container. The API trusts forwarded headers only from the RIGX network gateway (`ForwardedHeaders__KnownProxies__0=${RIGX_GATEWAY}`), which is the source address docker-proxy uses; without it the `__Host-` cookies and CSRF fail (the request looks like plain HTTP) and rate limits see one IP. The browser calls `/api` on the same origin through nginx; server-side rendering calls `http://api:8080` inside the network (public data only, no cookies).

**Deploy / update** (runbook §8 and "التحديثات"): `cd /srv/rigx/app && git pull && docker compose -f deploy/compose.prod.yaml up -d --build`. `migrate` runs every module migration first (as the database owner); `api` starts only after it exits 0, `web` only after the API is healthy. First owner: `docker compose -f deploy/compose.prod.yaml run --rm -e OwnerBootstrap__Enabled=true api --bootstrap-owner`.

**Production settings the API needs** (all in `compose.prod.yaml` from `deploy/.env`): `ASPNETCORE_ENVIRONMENT=Production`; `DataProtection:KeyRingPath` (volume) + `CertificatePath` + `CertificatePassword`; `Identity:Email:Smtp:*`; `Identity:Google:ClientId`; `Identity:SignInUrl` / `EmailChangeBaseUrl` and `Sales:*Url` on `SITE_URL`; `ConnectionStrings:Migrations` and `:Backup` as the database owner (backups need `CREATEDB` for the restore drill); `System:Backup:Destinations:0=/backups`; `Sentry:Dsn`.

**Security headers.** Web (production builds, `apps/web/src/lib/security/csp.ts` via `next.config.ts` `headers()`): CSP with every external origin listed (Google sign-in, and only when configured: Google tag, Meta pixel, the Sentry ingest host), `'unsafe-inline'` scripts (no nonce, decision 45), `frame-ancestors 'none'`, `object-src 'none'`, `base-uri`/`form-action 'self'`; `nosniff`, `strict-origin-when-cross-origin`, Permissions-Policy, COOP `same-origin-allow-popups` (Google popup). API: `default-src 'none'; frame-ancestors 'none'` plus the existing headers. HSTS from nginx.

**Error tracking and uptime.** API: `Sentry.AspNetCore`, on only when `Sentry:Dsn` is set; unexpected 500s are sent from `ProblemDetailsExceptionHandler` (business errors are not), no personal data (`SendDefaultPii` off). Web: `@sentry/nextjs` without the build wrapper (no source-map upload yet): browser errors (`instrumentation-client.ts`, `NEXT_PUBLIC_SENTRY_DSN`) and server render errors (`instrumentation.ts` `onRequestError`, `SENTRY_DSN`). Uptime: Sentry monitor on `/health` (API + database), 5 minutes.

**Analytics.** GA4 (`NEXT_PUBLIC_GA_ID`) and the Meta pixel (`NEXT_PUBLIC_META_PIXEL_ID`) load only when set and never on `/admin` (incl. POS). Page views: GA4 enhanced measurement (history changes) and a pixel `PageView` per route. A click on any WhatsApp link (`wa.me`, `api.whatsapp.com`) sends GA4 `generate_lead` (`method: whatsapp`) and Meta `Contact`, since orders close on WhatsApp. No cookie banner (decision recorded in the 4.8 checkpoint); the privacy policy names both tools. Search Console: Domain property verified by a Cloudflare TXT record, `sitemap.xml` submitted.

**Backups off the host.** The API keeps writing the daily zip to `/backups` (`/srv/rigx/backups`, 14 days by default). `offsite-backup.sh` copies it encrypted to Google Drive (rclone crypt; the two crypt passwords are kept by the owner outside the server, without them the copy cannot be read) and keeps 30 days there. Restore from off-host: `rclone copy rigxbackup:<file> /tmp/restore/`, then "Real restore" above. Run the restore drill from the admin after the first deploy and monthly.

## Payment gateway (designed, not built)
Online card payment is documented only; the owner decides when (Phase 6). Orders keep closing on WhatsApp with InstaPay / cash / wallet until then.
- **Provider:** Paymob (cards, mobile wallets; Egyptian merchant account, settles in EGP). Fawry as the alternative if cash-at-outlet is wanted.
- **What is paid online:** the order's upfront (shipping + deposit, decision 23) or the full total, by a setting; never a part of a build.
- **Flow:** checkout creates the order as today, then `POST /api/v1/orders/{id}/payment-intent` (Sales) creates a Paymob intention with the amount from the order (never from the browser), the order number as merchant reference and a 30-minute expiry; the browser goes to Paymob's hosted checkout (no card data touches RIGX; PCI scope stays SAQ-A). Paymob calls `POST /api/v1/payments/paymob/webhook`: HMAC checked with the secret from `.env`, amount and currency compared with the order, idempotent by Paymob transaction id (unique), then the existing "record payment" path posts the payment and the money movement into a `Paymob` money account (kind BankTransfer). The return page only shows the status; the webhook is the truth.
- **Refunds:** from the Paymob dashboard, then the existing return flow records the refund (no API refund at first).
- **Config:** `Payments:Paymob:ApiKey`, `:SecretKey`, `:PublicKey`, `:HmacSecret`, `:IntegrationIds` in `deploy/.env`; CSP adds the Paymob checkout origin to `form-action`/`frame-src`.
- **Tests:** HMAC check, wrong amount, replayed webhook, expired intent, order cancelled before payment.
