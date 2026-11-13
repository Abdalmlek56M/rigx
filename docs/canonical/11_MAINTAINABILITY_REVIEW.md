# RIGX — OOP/SOLID and Maintainability Review

## Strengths
- Domain constants (`OrderStatus`, `PaymentStatus`, `UnitStatus`, `AssemblyStatus`, partner precedence) live in the domain layer.
- Each module owns its DbContext and persistence configuration; no cross-context EF navigation.
- Application services are injected through DI; endpoints stay thin and translate exceptions to HTTP.
- Business validation is server-side (prices, variants, terms, stock, permissions, row versions).
- Permission checks are explicit and test-covered.

## Findings
| Finding | Impact | Decision |
|---|---|---|
| `RetailService` 1,319 lines handles receipts, assemblies, orders, partners, expenses, reports | High maintainability risk / God-class tendency | Defer split; behavior is release-critical and covered by 16 integration tests |
| `CatalogApplication` 1,180 lines mixes product, taxonomy, media and query concerns | Maintainability | Defer split; document boundary |
| Repositories are EF DbContexts directly; no generic repository layer | Acceptable for this scope | Retain; avoids indirection |
| Some endpoints apply permission strings per route rather than grouped policies | Duplication risk | Retain; all routes are audited and tested |
| Two catalog admin components coexist | Duplication risk | Both are routed to different screens; consolidate in design phase |
| Legacy token bridge `--ds-*` | Styling duplication | Retain until Admin rebuild |

## Rules preserved
- No new abstractions were added solely to increase class count.
- No business rule was moved to the frontend.
- Concurrency and inventory invariants are enforced in the database transaction.

## Round 5 refactoring decision
- RetailService and CatalogApplication remain large. Splitting them now would touch high-risk transactional/pricing code without failing behavior to justify the risk.
- Release-scope change hardened the stock-claim boundary in place and added concurrency proof, without new abstractions.
- Future split: receipts/assemblies, orders/stock, partners/finance, reports; Catalog products/taxonomy/media/query. Requires characterization tests first.

