# RIGX — Architecture Decision Index

Full records: `docs/adr/ARCHITECTURE_DECISION_RECORDS.md`.

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Modular monolith with per-module DbContexts | Accepted |
| ADR-002 | ASP.NET Core minimal APIs + PostgreSQL/EF Core | Accepted |
| ADR-003 | Cookie authentication with database ticket store + CSRF | Accepted |
| ADR-004 | Orders start `New/Unpaid`; stock is claimed only on Admin confirmation | Accepted |
| ADR-005 | Partner allocation precedence Unit → Product → Category → DefaultAgreement | Accepted |
| ADR-006 | Delivery/Pickup matrix; Pickup has no public payment or address | Accepted |
| ADR-007 | Terms acceptance stored server-side with policy version | Accepted |
| ADR-008 | YouTube-first video provider with strict URL validation | Accepted |
| ADR-009 | Assets served through catalog media pipeline; historical references guarded | Accepted |
| ADR-010 | Marker-only Financials/Invoicing/Sales assemblies | Accepted (not implemented) |
| ADR-011 | V1.9.1 design tokens implemented as native CSS layers, no iframe/pasted prototype | Accepted |

Superseded decisions are recorded in the full ADR document; this index must not contradict it.
