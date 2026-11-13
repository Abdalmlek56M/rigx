# RIGX — API Contract Inventory

Authoritative DTO/permission/rate-limit detail: `docs/api/BACKEND_UI_CONTRACTS.md`.
Endpoint traceability: `docs/api/BACKEND_UI_TRACEABILITY.md`.

## Endpoint groups (source count)
| File | Mapped endpoints |
|---|---|
| RetailEndpoints | 40 |
| CatalogEndpoints | 27 |
| ContentEndpoints | 25 |
| PurchasingEndpoints | 11 |
| IdentityAdministrationEndpoints | 10 |
| IdentityAccountLifecycleEndpoints | 10 |
| ReviewEndpoints | 9 |
| AccountGovernanceEndpoints | 8 |
| ContentVideoEndpoints | 8 |
| PcBuilderEndpoints | 8 |
| StorefrontEndpoints | 8 |
| OfferEndpoints | 7 |
| ContentMediaEndpoints | 6 |
| PurchaseDraftEndpoints | 5 |
| CustomerAddressEndpoints | 4 |
| EmailOtpEndpoints | 4 |
| InventoryEndpoints | 4 |
| ArticleVideoEndpoints | 3 |
| ContactEndpoints | 3 |
| IdentityCurrentUserEndpoints | 2 |
| ProfileAvatarEndpoints | 2 |
| AdminAnalyticsEndpoints | 1 |
| AuditEndpoints | 1 |
| IdentityLoginEndpoints | 1 |
| IdentityRegistrationEndpoints | 1 |

## Conventions
- Base path `/api/v1`; Admin under `/api/v1/admin`; public storefront under `/api/v1` public groups.
- Errors: `ProblemDetails`/`{ error }`; business exceptions map to 400/401/403/404/409.
- Concurrency: `rowVersion` in write DTOs; stale writes return 409.
- CSRF: unsafe methods require `X-CSRF-TOKEN` from `/api/v1/identity/antiforgery`.
- Rate limits: login/OTP/registration/contact and search suggestions have explicit policies.
- Public DTOs exclude cost, supplier, serial, partner, and private user data.
