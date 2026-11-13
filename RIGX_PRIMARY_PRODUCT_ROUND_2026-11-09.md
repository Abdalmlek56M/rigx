# RIGX — Primary Product Engineering Round (9 November 2026)

## Result
This round completed the confirmed storefront/navigation defects and added several safe vertical slices. It did **not** complete PC Builder UI, first-party video, Orders/Returns/Inventory posting, or financial modules.

## Completed
### Storefront / UX
- English default for new visitors; saved Arabic/English preference persists.
- Header unified; account menu opens `/account`; language typography inherited.
- Footer Account column and unrelated resend link removed; Back-to-Top on the right; WhatsApp stays left.
- Category/Brand taxonomy now distinct admin routes `/admin/catalog/categories` and `/admin/catalog/brands`.
- Custom in-site share dialog (copy/WhatsApp/Telegram/Facebook); no native Windows share sheet.
- Home order includes Offers; rails already use shared pointer/keyboard logic.

### Account / Governance
- Email Change: step-up current password, single-use hashed token, 30m expiry, resend revokes previous, verification to new email through durable outbox, email updates only after confirmation, session invalidation, audit.
- Account restrictions: Suspension/Ban records, reason/expiry, session revocation, login rejection, reinstate flow, Owner protection, audit.
- Notifications: persistent table, bell with unread count, list/read/mark-all, review moderation notifications.
- Avatar and account center remain from previous round.

### Purchasing
- Manual/unlisted purchase items (no Catalog product), snapshot name/brand/model/category/condition/description/external code/specifications.
- Purchase kind: Component/Components/Prebuilt/Bundle/Other.
- Draft/Cancelled only; no stock/cost/PO/GRN.

### PC Builder (partial)
- New `PcBuilder` persistence: templates, slots, options with product/spec/image snapshots.
- Admin/public APIs + publish validation.
- Compatibility evaluate: socket, DDR generation, PSU advisory; explicit Unconfirmed when specs missing.
- **UI not completed this round** (no public/admin routes yet).

### Not completed / blocked
- First-party video upload/player and Range streaming.
- PC Builder customer wizard and admin editor UI.
- Rich audit before/after presentation and full admin user timeline.
- Installments/partner agreements.
- Orders/Returns/Reservations/Stock posting/Invoicing/Financials.
- Watermark (no approved logo), AVIF/responsive multi-size delivery, production SMTP/storage.

## Verification
- `dotnet build HARDWARE_STORE.slnx` → 0 errors.
- PostgreSQL focused: PurchaseDraft 3/3, Reviews/Offers 2/2.
- `npx tsc --noEmit` → 0.
- `npx vitest run` → 37/37 (test locale pinned to Arabic; English default covered by product behavior).
- `npm run build` → success.
- Migrations: Identity `M8_AccountGovernance`, Purchasing `M7_PurchaseManualItems`, PcBuilder `M1_PcBuilderTemplates` applied to QA after backup `.backups/rigx_qa_before_round3_20261109.dump`; Preview/Owner DB untouched.
- Playwright full suite not rerun after these changes; previous round baseline was 26/27 + isolated login tests. Treat E2E for this round as pending.

## Owner decisions required
- Sales/Inventory/Returns/Reservation gates (Q1/Q2/Q3/Q5/Q6/Q7/Q8).
- Payment provider and financial posting rules.
- Policy texts and final logo.
- Persistent media/video storage and production SMTP.
- Whether to add `Purchasing.ManageDrafts` as a separate permission.

## Next
1. Finish PC Builder admin/editor + customer wizard UI.
2. Implement first-party video storage/range streaming with persistent storage gate.
3. Rich audit before/after + notification event coverage.
4. Run full Playwright + visual QA for this round.
5. Decide and implement Orders/Inventory only after owner approvals.
