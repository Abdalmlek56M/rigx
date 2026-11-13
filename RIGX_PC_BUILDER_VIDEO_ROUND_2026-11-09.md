# RIGX — PC Builder UI + Video Round (9 November 2026)

## Completed
### PC Builder
- Public `/pc-builder`: published templates list.
- Public `/pc-builder/[id]`: customer wizard with real template slots, alternatives, price total, compatibility status/messages, save-on-device, share link with configuration, and explicit "no stock reservation / no order" note.
- Admin `/admin/pc-builder`: template list, create/edit workspace with live preview, slots, alternatives, default option, publish/unpublish.
- Fixed publish defect: `SetPublishedAsync` read the template with `AsNoTracking`, so status changes were never saved. Now tracked and verified end-to-end.

### Video Media (first-party)
- MP4/WebM upload with signature validation, size limits, safe keys under the existing media root (`videos/{id}`).
- Admin `/admin/media/videos`: upload/list/publish/unpublish.
- Public `/videos/[id]`: HTML5 player using first-party stream.
- `GET /api/v1/content/videos/{id}/stream` supports HTTP Range (206, Content-Range, Accept-Ranges) for seeking.
- **Storage decision required:** current storage is the Docker/local `catalog-media` volume. Production must choose:
  1. Persistent mounted volume with snapshot/off-site backup, or
  2. S3-compatible object storage (free/self-hosted) with signed delivery.
  Also required: max upload size, retention, backup policy. No paid hosting was added.

### Purchase UI verification
- Manual/unlisted item path verified E2E: name + external code, no catalog product, qty/price, draft created.
- Purchase kind and manual fields remain Draft-only; no stock/cost/PO/GRN.

## Verification (actual)
- `dotnet build HARDWARE_STORE.slnx` → 0 errors.
- `npx tsc --noEmit` → 0.
- `npx vitest run` → 37/37.
- `npm run build` → success.
- Playwright (main-engine, split into isolated groups because of the retained 5/IP/min login limiter): **8/8 passed**
  - Header/Offers, Account Center, Catalog Nav, Reviews, Purchase Draft, Manual Purchase Item, Dashboard, PC Builder publish→public wizard.
- Playwright foundation + content-commerce: **7/7 passed**.
- Migrations applied to QA only: Catalog `M8_ContentVideos`; backup `.backups/rigx_qa_before_round3_20261109.dump` covers pre-round state.
- Not rerun: full single-process Playwright suite (login limiter causes cross-test rate exhaustion); no claim of a single 28/28 run.

## Screenshots
`docs/review/pc-builder-video-2026-11-09/`
- `home-default-english-1440.png`
- `pc-builder-list-1440.png`
- `pc-builder-wizard-1440.png`
- `admin-pc-builder-1440.png`
- `admin-videos-1440.png`
- `account-notifications-1440.png`

## Owner decisions
1. Persistent video/media storage backend + backup policy.
2. Video max size/retention.
3. Approved logo (watermark), policy texts, production SMTP.
4. Sales/Inventory/Returns/Financial posting gates.
5. `Purchasing.ManageDrafts` permission split.

## Remaining
- Orders/Returns/Reservations/Stock/Invoicing/Financials.
- Articles video block integration (standalone video is complete; embedding inside article editor is not).
- Rich audit before/after and full admin user timeline.
- Partner/installments.
