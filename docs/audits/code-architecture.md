# Code architecture audit (22 Nov 2026)

Method: measured the repository against `docs/04-engineering-standards.md` (file and method size, rules in the right layer, no duplication, no magic strings, tests). Production C# only (migrations, `obj`, `bin` excluded) and `apps/web/src`. Numbers come from scripts run on `main` at commit df6dbd2; method length is approximate (members at class level, found by braces). Nothing was changed by this audit.

## Verdict
The code is in good shape for its size (35 k lines of C#, 37 k of TypeScript, 1141 API + 562 web tests). The serious standards are met almost everywhere. What is left is **five concrete weaknesses**, none urgent, all cheap to fix one at a time. No rewrite is needed.

## What is already good (keep as is)
- **Rules sit in Domain / `*-rules.ts` and are unit-tested.** Web: 63 logic files; all business-bearing ones have a sibling test. API: 93 test files, 23 of them pure rule tests with no database.
- **No `IResult`, no anonymous responses, no TODO/FIXME, no commented-out code, no `any` worth the name** (3 in the web). `DateTime.UtcNow` appears once (email layout), everything else uses `TimeProvider`.
- **Statuses are constants/enums** (one string compare of a status in the whole API). Business errors go through `AppException` and the Arabic catalogue, guarded by a test.
- **Vertical slices where it matters:** Retail (`Features/` with 15 slices), Purchasing, PcBuilder and Storefront follow the slice layout. Services are small: about 15 methods exceed 60 lines, nearly all of them EF model configuration, endpoint maps or demo seeding; the longest real business methods are `DatabaseTicketStore.StoreAsync` (72) and `IdentityEmailChangeService.ConfirmAsync` (67).
- **Cross-module orchestration is in Host on purpose** (architecture decision 11): `PurchaseReceiptService`, `PurchasePaymentService`, `BuildPartPurchaseService`, `CatalogPriceOptionSource`. Modules do not reference each other.
- **Web is layered:** `*-api.ts` (calls) → `*-rules.ts` (pure) → components; only 6 TSX files exceed 180 lines (`icons.tsx` is a list of icons).

## Findings, most valuable first

### 1. Permission names are 143 string literals with no guard — DONE 22 Nov
`RequirePermission("Sales.CreatePOS")` and the web's `has("Sales.CreatePOS")` use plain strings; `PermissionCatalog` is a list, not constants, and nothing checks that a literal exists. A typo compiles, passes review and silently gives 403 (API) or a missing button (web). Today every literal does match the catalogue (checked by script), so this is prevention, not a bug.
**Fix:** (a) a test that scans the API sources and the web sources for permission literals and fails when one is not in `PermissionCatalog.All`; (b) optionally generate constants (`Permissions.Sales.CreatePos`) from the catalogue. (a) alone is one hour and catches the whole class.
**Done:** `PermissionLiteralsTests` (API sources) and `permission-literals.test.ts` (web sources) fail when a permission is not in `PermissionCatalog`. Constants (b) were not added.


### 2. Host endpoints read other modules' databases directly — three of six DONE 22 Nov
Six Host files open `IdentityDbContext` / `CatalogDbContext` themselves: `ProfileAvatarEndpoints` (writes the avatar key on the account), `ReviewEndpoints` (loads author names), `RetailEndpoints.StockItems` (model-name suggestions from the catalogue), `DashboardComposer`, `RetailMediaReferenceGuard`, `DatabaseHealthCheck`. The first three put persistence logic in an endpoint, against "thin endpoints, rules in the module".
**Fix:** move each into the owning module as a small service (`IdentityProfileService.SetAvatarAsync`, `IAuthorDirectory` in Identity contracts, `CatalogService.SuggestProductNamesAsync`) and have the endpoint call it. The last three are read-only composition in Host and may stay.
**Done:** `IdentityProfileService.ReplaceAvatarAsync` and `GetAuthorsAsync` (avatar and review authors) and `ProductManagementService.FindNamesAsync` (model suggestions); the three endpoints no longer touch a module's database. Tests: `StaffProfileTests` (avatar, authors), `CatalogPostgreSqlTests` (name lookup).


### 3. Transaction boilerplate is copied 26 times
`try { … commit … } catch { await transaction.RollbackQuietlyAsync(); throw; }` appears in 15 files (62 `BeginTransactionAsync` in total). It is correct each time, but a new service has to remember it, and one forgotten rollback leaves a connection busy until disposal.
**Fix:** one helper `db.InTransactionAsync(ct, async () => …)` (in `Common.Infrastructure`) that begins, commits and rolls back; migrate service by service when touched, no big-bang.

### 4. A few mixed "everything in one file" modules
Files above 300 lines (14 of 340): the useful ones to split are the ones that hold an **entity + DTOs + EF configuration + service in one file** — `Catalog/Reviews.cs` (491 lines, 15 types), `Storefront/Offers.cs` (453), `Catalog/CatalogMediaStorage.cs` (546: options, two interfaces, the Skia processor and the local storage), `Catalog/ContentMediaService.cs` (390). `Identity` keeps 25 files at module root instead of slices (`Features/Accounts`, `Features/Addresses`, `Features/Authentication`…). The remaining big files are data (`ApiErrorMessages` 702 lines of text) or one-off setup (`OnModelCreating` of `RetailDbContext` 486 lines, `IdentityDbContext` 277).
**Fix:** split by use case when next touched: `Reviews/{ReviewModels,ReviewService,ReviewConfiguration}.cs`, same for offers; split `CatalogMediaStorage` into processor and storage; move Retail entity mappings into `IEntityTypeConfiguration<T>` classes as `StockItemConfiguration` already is. Mechanical, no behaviour change, covered by the existing tests.

### 5. Demo seeding ships in the production Host
`PreviewInitializationService` + `PreviewDemoSeeder.*` + `PreviewDemoContent/Definitions` are 1 400 lines (16 % of Host) of preview/demo data in the same assembly as the live API. They are switched off in production by configuration, but they enlarge the attack and review surface, and Host is already the biggest project (8.8 k lines, 104 files; Retail 1.3 k, ApiErrors 0.8 k, Backups 0.7 k).
**Fix:** move them to a `HardwareStore.Api.Preview` project referenced only by the preview build, or at least a `Preview/` folder with one registration call.

## Smaller points
- **`CatalogEndpoints.cs` (362 lines; **split 22 Nov 2026 in 4.11 F2** into `.Public`, `.Taxonomy`, `.Products` and the shared file) and `ContentEndpoints.cs` (289)** are one file for many endpoints. Retail already shows the better layout (`RetailEndpoints.Sales.cs`, `.Assemblies.cs`, `.StockItems.cs`); split the two the same way when they are next edited.
- **Web duplicates:** `categoryPath` exists twice with different shapes (`lib/categories.ts` for the storefront tree, `admin/inventory/inventory-rules.ts` for the admin tree) and `parseAmount` twice. Both are small; merge only if the two trees get one type.
- **Web logic without tests:** correction: the cart rules (`cart-rules.ts`: totals, merge, stale-price reconcile) were already tested; `cart-store.ts` is now tested too (22 Nov), `refresh-lines.ts` is a thin API call and stays untested. Still untested: `share-link.ts` (builder links), `newest-items.ts`, `catalog-title.ts`, `coupon-problem-text.ts`, `published-policies.ts`, `account-page.ts`. Brief 10 adds its own `cart-rules` tests.
- **API services not named in any test (27 of 90):** most are covered through the HTTP tests (e.g. `InvoiceService` by `InvoiceTests`), so this is a list to check, not a list of gaps. Worth checking first: `QuoteService`, `SavedBuildService`, `CustomerMergeService` (merging customers is destructive), `TaxonomyImageService`, `RequestPhotoService`, `NotificationService`.
- **Admin has 319 files in one feature tree**; each slice is small and tidy, so leave it. If it grows, group sales / stock / money / system under four folders.

## Order to fix (smallest risk first)
~~1.~~ done. **Permission guard test** (finding 1a) — 1 hour, prevents a whole class of bug.
2. **Cart tests** (`cart-store`, `refresh-lines`) — before brief 10 is merged, since it changes the cart refresh.
3. **Move the three endpoint-level reads into their modules** (finding 2) — about half a day, with tests.
4. **`InTransactionAsync` helper** (finding 3) — add it, use it in new code, migrate old code when touched.
5. **File splits** (finding 4 and the endpoint files) — only while editing the file for another reason.
6. **Preview project split** (finding 5) — after launch, when the preview environment is settled.

None of these blocks launch. Items 1 to 3 can run in one session on `main` without touching the storefront files DeepSeek is editing.

## Found while fixing (22 Nov)
- **Search with `%`, `_` or `\` found nothing** in seven places (customers, customer requests, purchases, sellers, the security log, POS customer lookup, stock-form model suggestions): the text was escaped for ILIKE but the query did not name the escape character, and through Npgsql PostgreSQL then does not treat the backslash as an escape (proved with a failing test: searching "100%" returned nothing). The escaping code was copied in seven places. Now one `LikePattern` (Common.Domain: `Contains(text)` + `Escape`) is used by all of them. The product-search suggestions and a few other `ILike` calls still pass the typed text unescaped, so a `%` there acts as a wildcard (harmless; left as is).
