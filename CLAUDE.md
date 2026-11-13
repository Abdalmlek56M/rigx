# Instructions for AI agents working on RIGX

Read first: `docs/01-business.md`, `docs/02-architecture.md`, `docs/04-engineering-standards.md`, `docs/05-roadmap.md`.

- Work on the next unchecked roadmap step only; tick it and update docs in the same commit.
- Follow the standards: vertical slices inside modules, rules in Domain, `Result` errors, no duplication, no dead code.
- Customer-facing text follows `docs/01-business.md §2` (only "جديد"/"مستعمل", no grades, no "final price" explanations).
- Auth is email OTP + Google only. No passwords, no SMS.
- Cost discipline: read big files once; no polling loops; don't re-run builds/tests without a change; run module tests while working and the full suite before committing shared changes.
- Never commit secrets, archives, logs, screenshots, or generated output.
- Missing business decision → ask the owner; don't invent one.
- Reply to the owner in Egyptian Arabic, short and clear.
