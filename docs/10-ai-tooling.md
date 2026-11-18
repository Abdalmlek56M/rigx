# RIGX — AI tooling (skills + MCP)

Picked and installed 18 Nov 2026 (claudemarketplaces.com, with the owner) at user level (`~/.claude/skills`), never committed to the repo. `high-end-visual-design` is now part of `design-taste-frontend`; `schema-markup` is called `schema`; `ai-seo` added (being cited in AI answers).
Install form: `npx -y skills add <repo> --skill <name> --agent claude-code`.

## Already installed
| Skill | Use |
|---|---|
| `impeccable` | design audit, polish, animate, `DESIGN.md` |
| `mobile-native` | phone feel (tap states, safe areas, 100vh, inputs) |
| `break-ui` | worst-case data on a screen |
| `review-animations` | motion review (owner-invoked) |

## Installed 18 Nov 2026
| Repo → skill | Use | Step |
|---|---|---|
| `leonxlnx/taste-skill` → `design-taste-frontend` (keep the store calm: no shader scenes or heavy effects) | visual taste, premium look | 4.6, brand |
| `emilkowalski/skill` → `emil-design-eng` | motion and interaction detail | 4.6 |
| `anthropics/skills` → `frontend-design` | intentional visual direction | brand |
| `vercel-labs/agent-skills` → `web-design-guidelines`, `vercel-react-best-practices` | UI rules + React/Next performance | all frontend |
| `raphaelsalaja/userinterface-wiki` → `userinterface-wiki` | 152 UI rules for reviews | reviews |
| `laguagu/claude-code-nextjs-skills` → `nextjs-seo` | Next.js 16 metadata, sitemap, robots | SEO |
| `coreyhaines31/marketingskills` → `seo-audit`, `schema`, `ai-seo` | audit + structured data | SEO |
| `addyosmani/web-quality-skills` → `seo`, `performance` | Lighthouse-based quality | SEO, launch |
| `mattpocock/skills` → `grill-me` | stress-test plans before big decisions | brand, Phase 5 |
| `vercel-labs/skills` → `find-skills` | find a skill for a new need | any |

## MCP servers (added to the user config 18 Nov 2026, active from the next session)
- **Context7**: current Next.js / Tailwind / next-intl docs.
- **Chrome DevTools MCP**: Lighthouse, performance traces, SEO checks on the running site.

## Left out (and why)
- `caveman` (compressed replies): clashes with "reply short and clear in Egyptian Arabic".
- `mattpocock` code-review: the built-in `/code-review` covers it.
- Many overlapping "design system" skills: conflicting instructions; the set above is enough.
- `canvas-design` and `ui-ux-pro-max` stay as planned for the brand book (`08-brand-direction.md` §4).
