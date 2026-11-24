# Report 6: security review of the new endpoints

Range: `bcc56e9~1..HEAD` for `src/Host`, plus the module code those endpoints call. Static reading only, no running system.

## Summary
- No critical or high problem found. Every admin endpoint has a permission, responses that carry private data send `no-store`, an unknown or hidden saved build answers 404 (no 403 that would confirm it exists), and tokens come from a cryptographic random generator.
- One thing to **verify on the live server today**: the rate limits depend on the real visitor IP reaching the API.
- The rest is low severity (counter abuse, money transfer without step-up or idempotency).

## Findings
| Severity | Location | Problem and scenario | Fix |
|---|---|---|---|
| Medium (verify) | `IdentityRateLimiting.cs:73-79` (partition key = `Connection.RemoteIpAddress`), `Program.cs:97-110`, `deploy/compose.prod.yaml:19` | The limiters count per IP. The API only trusts `X-Forwarded-For` from `KnownProxies`, which is `127.0.0.1`, `::1` and `RIGX_GATEWAY` (default `172.29.208.1`). If nginx reaches the container from a different address, the header is ignored and **every visitor appears as one IP**: 10 orders per 10 minutes and 5 requests per 10 minutes for the whole store, shared sign-in-code limits, and the audit log's `ActorIp` shows the proxy. Nginx side looks right (`rigx-cloudflare-real-ip.conf` sets `real_ip_header CF-Connecting-IP`; the site config forwards `$remote_addr`). | On the server, open the activity log (or sign in twice from phone and PC) and check that `ActorIp` is the visitor's address and differs between devices. If it shows `172.x`, set `RIGX_GATEWAY` in `deploy/.env` to the address the API sees and restart. Cheap extra check: log `RemoteIpAddress` once at start-up. |
| Low | `BuildViewRules.VisitorKey`, `BuildViewCounter.cs:13,24`, `BuilderEndpoints.cs:77-84` | The anonymous view beacon counts one view per (IP, User-Agent, build, day). Changing the User-Agent on each request counts every time: about 120 views per 10 minutes per IP. Also, the "already seen today" set stops at 200,000 entries, after which no visitor is counted until the next day. Only a vanity counter is at risk. | Key the visitor by IP only (keep User-Agent only to skip robots), and lower the cap handling to evict rather than stop. Not urgent. |
| Low | `BuildViewStore.cs:16-22` | A beacon for an unknown token reads the database every time (no negative cache). Bounded by 120 requests per 10 minutes per IP. | Optional: remember unknown tokens for a few minutes. |
| Low | `RetailEndpoints.cs:96`, `MoneyAccountService.cs:89-106` | The transfer between accounts has the right permission (`Finance.Disburse`), a row lock and a no-overdraft check, and it posts both legs in one transaction. But: (1) no step-up, while smaller actions (merge customers, restore audit entry) require it; (2) no idempotency key, so a double submit posts two transfers; (3) `Date` accepts any past or future date. | Decide whether transfers above a limit need step-up (owner question). Disable the button while sending and ignore a repeat of the same amount/accounts within a few seconds. Reject dates in the future or older than the locked period. |
| Low | `AuditLogService.cs:42-55` | `MonthsAsync` runs up to 60 count queries per request (one per month). `View` permission only, so it is not an outside risk, but a large log will make the screen slow. `module` and `entity` filters are not length-checked (they are parameters, so no injection). | One grouped query by month. Add max length to the filters. |
| Low | `TaxonomyOrderService.cs:10-45` | Reorder checks that the id list matches the real set exactly (good) but has no row version, so two people reordering at the same time silently overwrite each other. | Accept the last write (current behaviour) or add a version to the request. Document the choice. |
| Low | `PosService.cs:98-117`, `RetailEndpoints.cs:52` | `GET /pos/customers` gives name, phone, email, address and "blacklisted" for any past buyer to anyone holding `Sales.CreatePOS`. This is by design (see the contract comment), but a cashier can probe any phone number for a blacklist match, and the loop calls the blacklist once per customer (up to `MaxResults`). | Keep. Make sure cashier roles are given only to trusted staff; batch the blacklist query. |
| Low | `RequestEndpoints.cs:134-140` | The reply is saved and then the customer's bell entry is added in a second step. If the second fails, the reply exists but the customer is not told. | Add the notification inside the same transaction, or retry in the background. |

## Checked and fine
- Admin request endpoints (`reply`, `note`, `offer`, `decision`, `convert`): `Requests.Manage` or `Requests.View`, row version required, reply 2 to 2000 characters (`CustomerRequestRules.cs:15,112`), only allowed types take a reply.
- Public request form (complaint, suggestion and the other types): rate limit 5 per 10 minutes per IP, honeypot field (`Website`), duplicate suppression, photo upload only with the receipt token.
- `POST /builder/saved/{token}/copy`: signed in, `CanView` check then 404 (never 403), copy is created private, de-duplicates identical part lists, per-account limit, rate limit shared with save (20 per 10 minutes).
- Audit months, days and entries: `Audit.View` permission, `no-store`, restore still needs `RequireStepUp`. Names of linked users are resolved server-side only for ids present in the entry.
- Brand and category order: `Catalog.ManageTaxonomy`, ids must be exactly the sibling set.
- POS blacklist flag and blacklist endpoints: list needs `Sales.ViewCustomerContact`, add and remove need `Sales.ApproveOrder`, removal goes through the trash audit.
- Tracking and invoice links: tokens from `RandomNumberGenerator`, phone masked on the public tracking page (`TrackingRules.MaskPhone`), details hidden after 90 days.
- `/api/v1/orders/mine/coupons`: signed in, scoped to the caller's own account.

## Questions for the owner
1. Should a money transfer between accounts ask for the extra confirmation code (step-up) like merging customers does, or only above an amount?
2. Who should hold the cashier permission (`Sales.CreatePOS`)? It lets that person search any past buyer's contact details.

## What I did not check
- `CustomerRequestRules.CheckSubmission` field limits for complaints and suggestions, and `AuditDays.ParseMonth`.
- Photo reorder endpoints, the invoice endpoints, and `OrderEmails` changes.
- Nginx and Cloudflare settings beyond the two files named above; no live test.
