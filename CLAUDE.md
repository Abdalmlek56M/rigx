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

## Model routing & session flow
- **Opus (main session):** planning, domain/business code, final review before every commit. Small edits stay inline — a subagent starts cold, so delegate only when the brief is shorter than doing the work.
- **Sonnet subagent** (`model: sonnet`): writing unit/integration tests and other well-specified code tasks; brief = files, contracts, expected behaviour.
- **Haiku subagent** (`model: haiku`): wide file searches, log summaries, docs/roadmap/checkpoint updates from a ready summary.
- **Sequential writers:** never two agents editing files at once. Order: code (Opus) → tests (Sonnet) → docs (Haiku) → Opus review + tests → commit. Read-only agents may run in parallel.
- **Checkpoint:** keep `docs/checkpoints/<step>.md` (done / uncommitted / remaining) current at each commit.
- **Context:** past ~150k tokens mid-step, update the checkpoint and ask the owner to run `/compact`. When a step is done, pushed and its checkpoint updated → stop and tell the owner to open a new session.
