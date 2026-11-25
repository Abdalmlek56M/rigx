# Brief deepseek-12: text cleanup, dead code and audits (7 tasks, one at a time)

You are a helper engineer on RIGX. Claude is the lead: it wrote this brief, and it reviews your diff before anything is committed. Work exactly as written here.

## Project
RIGX (https://rigxhardware.com) is an Egyptian store for new and used PC parts and builds. .NET 10 modular monolith (`src/Modules/*`, host in `src/Host/HardwareStore.Api`) and Next.js 16 in `apps/web` (Arabic is the default; `messages/ar.json` and `en.json`).

Read first: `AGENTS.md` (if present), `CLAUDE.md`, `docs/04-engineering-standards.md`, `docs/design/storefront-kit.md`, `docs/checkpoints/4.12-owner-visual-round.md`. For `apps/web`, also read `apps/web/AGENTS.md` if present.

## Hard rules
- Do **only the task number the owner gives you**, then stop. Never start the next one.
- Never commit, push, switch branch, reset, stash or delete untracked files. Leave your work uncommitted.
- Never touch `deploy/`, secrets, `.env`, migrations, `CLAUDE.md`, any `AGENTS.md`, or the server. No network calls other than package install.
- If `git status` shows changes you did not make (other than untracked `apps/web/AGENTS.md`), stop and tell the owner.
- No new dependencies. No refactoring, renaming or formatting outside the task.
- A missing business decision is a question for the owner. Never decide it.
- If something in this brief does not match the code, say so and skip that item. Never guess, never claim a check you did not run.

## Quality bar
- **Wording:** formal, plain Arabic that reads like an experienced, trusted Egyptian store wrote it. No dialect, no AI-sounding phrases, no showy words, no extra explanation. Keep the meaning, the placeholders (`{name}`, `{shown}`, ICU plurals) and the length close to the original. Condition words are only "جديد" and "مستعمل".
- **Code:** rules as pure functions with tests, no duplication, no dead code, logical CSS properties (RTL), touch targets of 44 px on phones, motion respects `prefers-reduced-motion`.

## Checks (run what your change touches)
In `apps/web`: `npm run typecheck`, `npm run lint`, `npm test`.
Backend: `dotnet build`. `dotnet test` crashes on the owner's machine ("Access is denied"); if it does, say so.

## Report (every task)
Write `docs/reports/deepseek-12-<N>-<name>.md`, at most 200 lines: **Summary** (3 lines), **Changes** or **Findings** (table with `path:line`), **Checks run** with their real result, **Questions for the owner**, **Not done**. Your final chat message: the file path, the check results and up to three points, in short Arabic.

---

## Task 1: "المحل" becomes "المقر" (name: `maqar`)
The owner asked: every user-facing Arabic text that says "المحل" must say "المقر".
- Search `apps/web/messages/ar.json`, `apps/web/src` and `src/**/*.cs` for "محل".
- Adjust grammar around it ("للمقر", "بالمقر", "من المقر"). Do not change words that only contain the letters ("محلي", "محلول", "محلها").
- Change only the text. Never change keys, enum values or code identifiers. English stays as it is.
- Demo and seed files (`PreviewDemo*.cs`) may be changed too. If a test locks a text, update the test and list it.
- `docs/12-policies.md`: do not edit; list any occurrence under Questions.

## Task 2: colloquial strings (name: `formal`)
- Apply the proposals in `docs/reports/deepseek-11-1-arabic.md`, the two `ar.json` tables (customer and admin). Find each string by key or text (line numbers have moved). Skip strings that are already fixed.
- `apps/web/src/app/llms.txt/route.ts`: rewrite the colloquial lines in formal Arabic.
- Then search `ar.json` for other dialect words (هي-/هت- future, "مش"، "عشان"، "كدا/كده"، "ده/دي"، "دلوقتي"، "إنت"، "لسه") and fix real ones the same way.
- Do not touch `SimpleSettings.cs`, `StockImportRules.cs` (task 4) or the `{الموبايل}` template token.

## Task 3: dead code and one duplicate (name: `deadcode`)
- From `docs/reports/deepseek-11-2-standards.md`: `components/ui/EmptyIcon.tsx`, `fetchUser`, `paymentTone`, `sellModeKey`, `parseSort`, `formatSort`, `emptyPolicyForm`, and the duplicated "optional" pill (`features/checkout/form-parts.tsx` and `ExtrasSection.tsx`).
- Before deleting each one, `git grep` the whole repository. If production code uses it now, keep it and say so. Delete tests that only test deleted code.
- Add to the report (do not delete) other unused exports and components you notice from the last 3 days of commits.

## Task 4: old import header wording (name: `import-header`)
- `src/Modules/**/StockImportRules.cs`: the template header "السيريال" becomes "الرقم التسلسلي". The importer **must still accept** files that use "السيريال". Add Domain tests for both headers next to the existing import tests.
- Check every place that reads or writes that header (template download, error messages, web).
- `SimpleSettings.cs` lead sources: **report only**. Say where the saved values are stored and used, and propose a safe change.

## Task 5: wording audit, report only (name: `wording`)
No code edits. Read the customer-facing parts of `ar.json` (home, catalog, product, cart, checkout, account, builder, services, errors, emails in `src/**/*.cs`). Flag text that sounds machine-written, exaggerated, odd, repetitive, too long, or unclear to a buyer, and inconsistent terms for one thing. Table: key, current, proposed, reason. At most 60 rows, worst first. Check that `en.json` has the same keys as `ar.json`.

## Task 6: code review of the visual round, report only (name: `review`)
No code edits. Review `git diff 5bcd3e4..HEAD` (batches 1 to 3). Look for real bugs first (wrong condition, missing state, broken RTL or phone layout, hydration problems, missing permission checks, N+1 queries, row-version gaps), then standards (duplicated helpers, dead code, files over the size rule, logic in components that belongs in `*-rules.ts`, missing `alt` or `aria-label`). Each finding: severity, `path:line`, a concrete failing scenario, the smallest fix. Mark anything unsure as "unsure". At most 30 findings.

## Task 7: tests for new pure rules (name: `tests`)
Only new or extended `*.test.ts` files in `apps/web/src`. Cover pure functions added since commit `5bcd3e4` that have no or thin tests (`git diff --stat 5bcd3e4..HEAD`). Use relative `.ts` imports. If a test shows a bug, leave that test out and describe the bug in the report. Do not edit any non-test file.
