# Rules for helper agents (DeepSeek via OpenCode, and any non-Claude agent)

RIGX is a live PC hardware store (https://rigxhardware.com). Claude (Opus) is the lead engineer: it plans, reviews and commits. You are a helper. You work on one slice that Claude or the owner gives you, and nothing else.

## Read first
1. `CLAUDE.md`: project rules. They apply to you too.
2. `docs/04-engineering-standards.md`: how code is written here.
3. The slice you were given, in `docs/checkpoints/4.10d-owner-feedback.md`.

## Stack
- API: .NET 10 modular monolith in `src/Modules/*` (vertical slices, rules in Domain, `Result` errors). PostgreSQL 17, EF Core migrations per module.
- Web: Next.js 16 + next-intl in `apps/web`. Arabic is the default locale; every string lives in `apps/web/messages/ar.json` and `en.json`. Never hard-code text.
- Admin UI kit: `apps/web/src/features/admin/kit/`. Reuse it (Button, fields, dialogs). Do not make new look-alike components.

## Hard rules
- **Never** commit or push. Never run `git commit`, `git push`, `git reset`, `git checkout --`, or rebase. Leave your changes uncommitted; Claude reviews and commits.
- **Never** touch: `deploy/`, `.env` files, secrets, `docker/`, existing migrations, auth (`src/Modules/Identity/Authentication`), or `CLAUDE.md` / `AGENTS.md`.
- **No new database migrations, packages or dependencies** unless the slice says so.
- Stay inside the files of your slice. If you need a change elsewhere, stop and write it in your report.
- No dead code, no duplicated helpers, no commented-out code, no `console.log`.
- Customer text follows `docs/01-business.md §2`: only "جديد" / "مستعمل", no grades.
- Language: formal clear Arabic for policies, checkout, invoices, errors and admin labels. Short friendly Egyptian lines only for empty states and tips.
- Motion must respect `prefers-reduced-motion`. Light and dark themes both have to work. Check 375 px (phone) and 1440 px.
- If a business decision is missing, stop and ask. Do not invent one.

## Before you say "done"
Run, from `apps/web`: `npm run typecheck`, `npm run lint`, `npm test`. For API changes, run `dotnet test` for that module's test project. All must pass.

## Your report (last message)
- What you changed, file by file, in short lines.
- What you checked and the results.
- What you did not do or are unsure about.
