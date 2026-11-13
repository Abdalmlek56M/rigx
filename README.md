# RIGX Hardware

Online store + back-office for RIGX — new/used PC hardware, PC builds and services in Egypt.

| | |
|---|---|
| Backend | .NET 10 modular monolith · PostgreSQL 17 · EF Core |
| Frontend | Next.js (rebuilt from the approved design — roadmap phase 4) |
| Design | Canvas page "المعتمد": https://claude.ai/artifact/Vvv6KmuoNaYk3m6t8e6jwS |

## Docs (read in this order)
1. [Business & rules](docs/01-business.md) — what the store does and the rules code must follow
2. [Architecture](docs/02-architecture.md) — modules, layers, cross-cutting, decisions
3. [Security](docs/03-security.md) — controls in place, rules, open gaps
4. [Engineering standards](docs/04-engineering-standards.md) — code, tests, git, docs
5. [Roadmap](docs/05-roadmap.md) — phases and current status
6. [Operations](docs/06-operations.md) — run, migrate, test

## Repository layout
```
src/Host/HardwareStore.Api   API host (composition root)
src/Modules/*                feature modules
src/Common/*                 shared building blocks
tests/HardwareStore.Api.Tests integration tests (real PostgreSQL)
docker/postgres              database init scripts
docs/                        the only documentation
```
