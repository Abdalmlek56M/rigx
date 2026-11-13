# HARDWARE STORE / RIGX — Autonomous Engineering Standard

**Status:** Project-level technical operating standard.  
**Applies to:** OpenCode agents and any authorized implementation agent working in this project.  
**This is not the owner's private ChatGPT continuity file.** It contains only project engineering rules and may be read by project agents.

## 1. Goal-first engineering

The owner defines the desired outcome, product boundaries, and sensitive decisions. The implementation agent owns the technical path. Treat a task as an outcome plus acceptance criteria and guardrails, not as a closed list of files or literal micro-steps.

The agent is expected to:
- Understand the existing implementation and the root cause before changing it.
- Follow dependencies across components, pages, API contracts, tests, configuration, and documentation when they are directly related to the approved goal.
- Fix linked defects together when they share a cause or are necessary for a complete result.
- Make routine technical and design decisions without asking for approval at every step.
- Add tests and cleanup that are directly needed to make the result reliable.
- Preserve working behavior and avoid unrelated rewrites, decorative complexity, and unnecessary dependencies.

## 2. Scope boundary: autonomy without uncontrolled expansion

**Allowed within the approved goal:**
- Cross-file and cross-layer changes required to deliver the outcome.
- Root-cause fixes, directly related accessibility/responsive/performance improvements, regression fixes, and test coverage.
- Reuse or improvement of existing patterns, components, and tools.
- Free/local tools and dependencies when they are genuinely needed, non-duplicative, compatible, and documented.

**Requires owner approval:**
- Starting a new commercial Feature or Milestone outside the authorized goal.
- Changing business, legal, tax, accounting, pricing, stock, payment, order, warranty, or fulfillment policy.
- Persistent database/schema/data changes, migrations against durable environments, destructive operations, production actions, or public deployment.
- Paid services, subscriptions, purchases, or consuming paid API credits beyond the approved budget.
- Changing authentication/authorization/security boundaries in a way that changes product policy.

Do not confuse a technical dependency of the current task with a new product milestone. Do not use "scope control" to leave a directly related defect unfixed.

## 3. Workspace and safety

Authorized workspace:

`C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`

Protected original workspace:

`C:\Users\Abdalmlek\Downloads\DEV\HARDWARE STORE`

Never modify the protected original. Confirm the active path before writing. Do not run destructive Docker commands, remove volumes, drop databases, or expose secrets. Back up appropriate files before broad changes. Keep API keys in OpenCode credential storage or secure environment variables; never commit or print them.

Only one agent may write to this workspace at a time. Research/review subagents may work in parallel if they do not modify the same files.

## 4. Standard work loop

1. **Understand:** read `AGENTS.md`, relevant Skills, current `PROGRESS.md`/`NEXT_TASK.md`, and the files/callers involved.
2. **Baseline:** inspect the current behavior and capture only the tests/screenshots needed to compare the result.
3. **Investigate:** identify root causes, dependencies, affected routes, states, and risks. Do not assume a prior report proves the current source state.
4. **Plan:** make a concise internal plan and group related changes into a coherent implementation batch. Do not stop for routine technical choices.
5. **Execute:** implement the whole vertical slice needed for the approved outcome. Reuse existing architecture and semantic tokens.
6. **Verify:** run risk-based TypeScript/build/unit/integration/E2E/browser/accessibility/performance checks that apply. Fix regressions discovered in the same batch.
7. **Review:** inspect all changed files, dead code, duplicate layers/listeners, security boundaries, generated artifacts, and accidental scope expansion.
8. **Document and deliver:** update only the source-of-truth docs that changed, provide a full report, and stop at the approved goal boundary.

Avoid both extremes: tiny task fragmentation and unbounded rewrites. The batch should be large enough to solve related problems together and small enough to verify and roll back.

## 5. Design principles for RIGX

- The owner-selected HTML references are approved creative references, not source code or palettes to copy verbatim.
- Unify them into one RIGX identity: Graphite/Obsidian foundation, restrained ambient light, Warm Orange accent/glow, editorial information hierarchy, technical character, and direct retail language.
- Product identity, specifications, availability only when sourced, and price clarity stay ahead of effects.
- Use existing semantic tokens. Avoid hard-coded colors, palette mixing, and unrelated page-by-page themes.
- One shared ambient background layer should continue behind the page through the full footer. Footer may be transparent or use a subtle semantic surface; it must not become an unrelated opaque slab.
- Do not duplicate fixed backgrounds, canvases, animation loops, observers, or pointer listeners across sections/routes.
- Keep Storefront expressive but clear. Keep Admin operational and information-dense while sharing the same tokens, typography, borders, focus, and restrained motion.
- Support RTL/LTR and Light/Dark. Respect `prefers-reduced-motion`, touch devices, keyboard, focus visibility, contrast, and performance.
- Preview/M4/Local Demo markers must not appear in customer-facing Production mode.

## 6. Definition of Done

A task is done only when:
- The requested outcome and acceptance criteria are met.
- Directly related root causes and regressions discovered during work are addressed or explicitly documented with evidence.
- Applicable tests and visual checks are run and reported accurately.
- No known failure is hidden behind a successful build or a polished screenshot.
- Documentation reflects the actual current state without claiming unverified completion.
- The final report lets the owner understand what changed and what remains without reading every source file.

## 7. Mandatory final report

Include:
1. Executive summary and completion status.
2. Root causes and evidence.
3. Technical/design decisions and rationale.
4. All changed/created/deleted files grouped by area.
5. Tests run, exact outcomes, and tests not run with reasons.
6. Browser/visual evidence: route, viewport, language, theme, states, screenshot paths.
7. Security, data, performance, accessibility, and regression impact.
8. Tools/dependencies/configuration changed and why.
9. Remaining issues, limitations, and unresolved owner decisions.
10. The next recommended step without autonomously starting a new Milestone.
