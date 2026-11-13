# RIGX — Remaining Backend Execution (10 November 2026)

Backend-only continuation from `HARDWARE_STORE_RIGX_NEXT_2026-11-10.zip`. No frontend file, style, translation or React component was modified in this round.

## Completed

### 1. Multi-partner allocation administration (P0)
- Added append-only `partner_allocation_events` audit table (actor, reason, snapshot, timestamp).
- Create/update requires concurrency control:
  - duplicate create without `rowVersion` → `409`;
  - stale `rowVersion` → `409`;
  - update/deactivate requires a reason of at least 3 characters.
- Aggregate active partner percentages per effective scope cannot exceed 100%.
- Added deactivate endpoint, history endpoint and effective preview endpoint with precedence Unit → Product → Category → DefaultAgreement.
- Read endpoints remain `Finance.ViewReports`; mutations require `Finance.ManagePartnerAgreements`.
- Tests: 14 Retail tests include concurrency, >100% rejection, unit precedence, deactivation, inactive rows and audit history; authorization matrix proves `Finance.ViewReports` gets `403` for allocation writes.

### 2. Expenses and business reporting (P1)
- `retail_expenses` with `Direct` vs `GeneralOverhead`, date, category, amount, note, optional order/unit/attachment reference, actor and row version.
- `GET /admin/retail/expenses`, `POST /admin/retail/expenses` (`Finance.ManageExpenses`), `GET /admin/retail/reports/business` (`Finance.ViewReports`).
- Report separates gross sales, discounts, refunds, COGS, item direct expenses, additional direct expenses, net item profit, general overhead, store net result, partner accrued/paid/recoverable, completed orders and inspected returns.
- Overhead is never allocated to item profit or partner share.
- Test verifies exact reconciliation: gross 290, discount 10, COGS 100, item direct 20, net item 170, additional direct 50, overhead 100, store net 20.

### 3. Contact inbox (P1)
- `contact_messages` with status New/Read/Closed, bounded inputs, honeypot, 10-minute duplicate suppression, 5/10min/IP rate limit and generic 202 response.
- Admin list/status APIs protected by `System.StoreSettings`, `no-store` and row-version conflict handling.
- Test covers valid submit, duplicate suppression, honeypot silently discarded, invalid email/short message/oversize rejection and stale status update.

### 4. Article video references (P1)
- `article_video_references` links articles to existing YouTube/uploaded video records with explicit ordering.
- Public article-video endpoint returns only videos referenced by a published article and themselves Published.
- Unpublishing a video referenced by a published article is blocked with `409`; existing uploaded records remain intact.
- Test covers ordering, public filtering, nocookie embed and the unpublish guard.

## Verification
| Check | Result |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 errors |
| Full backend xUnit/Testcontainers | **187/187 passed, 0 skipped** |
| Retail PostgreSQL (incl. allocation audit, expenses/report) | **14/14** |
| Contact PostgreSQL | **1/1** |
| Article-video | **1/1** |
| Finance/inventory authorization matrix | **2/2** |
| OTP / YouTube / prior slices | still green |

No frontend build/test was rerun because no frontend file changed; the contract remains additive and backward-compatible.

## Migrations (QA only, backups first)
- Retail `M5_PartnerAllocationAudit`
- Retail `M6_ExpensesAndReports`
- Storefront `M9_ContactMessages`
- Storefront `M11_ArticleVideoReferences`

Backups:
`rigx_qa_before_alloc_audit_20261110.dump`, `rigx_qa_before_expenses_20261110.dump`, `rigx_qa_before_contact_20261110.dump`, `rigx_qa_before_articlevideo_20261110.dump`.

## API handoff
`docs/api/BACKEND_UI_CONTRACTS.md` documents auth, catalog/builds/orders, allocations, partner ledger, expenses/reports, contact, video/article references, permissions, concurrency, errors and role-gated fields.

## Not completed / blocked
- **Google OIDC**: requires real client ID/secret and approved redirect domain. No fake endpoint/button was added.
- **Admin MFA/TOTP and step-up**: enrollment, recovery-code and mandatory-enforcement policy remain owner-gated.
- **Sales Invoice / Quotation PDF**: legal seller identity and ETA applicability remain owner/accountant-gated.
- **Offers quantity caps and checkout-price engine**: current offers are display-only and lack quantity caps; a generic rule model and race tests are not implemented.
- **Search suggestions**: existing catalog search remains; no new autocomplete endpoint was added.
- **Audit before/after viewer**: existing audit storage remains; no new reversal endpoints were added.
- **Contact notification Outbox email**: message persistence/inbox exists; sending to an admin address is deferred until production SMTP/recipient configuration.
- **Admin MFA status honest**: admin protection is still password + session; production launch remains blocked on MFA.

## Security findings
Closed in this round:
- Allocation mutations cannot bypass concurrency or silently overwrite/deactivate.
- Finance report-read permission cannot write allocations.
- Overhead/direct expenses are separated; reports expose only finance-authorized users.
- Contact submissions are bounded, deduplicated and honeypot-protected.
- Referenced video cannot be unpublished while a published article uses it.

Residual:
- Google OAuth and admin MFA absent.
- Offers have no quantity/price enforcement at checkout.
- Invoice/PDF and legal/tax fields absent.
- No independent pentest/dependency CVE audit this round.
- Contact admin notification email requires later SMTP wiring.
