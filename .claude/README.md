# .claude

Claude Code project setup for Notrelix. Repository truth lives in `AGENTS.md`, `RULE.md`, `PRODUCT.md`, `DESIGN.md` and the canonical docs they point to; nothing here overrides them.

## Delivery process

- `skills/notrelix-delivery/` — how a feature is built: one feature open at a time, gates G0–G8, gate checklists (the canonical Definition of Done is `docs/delivery/definition-of-done.md`), hand-off templates, repo rules for every role that writes.
- `agents/nrx-*.md` — one file per role. Each agent holds exactly one role for its whole life.

| Role | Agent |
|---|---|
| Coordinator | `nrx-coordinator` |
| Business analyst | `nrx-business-analyst` |
| UX/UI designer | `nrx-ux-designer` |
| Design reviewer | `nrx-design-reviewer` |
| Solution architect | `nrx-solution-architect` |
| Backend engineer | `nrx-backend-engineer` |
| Frontend engineer | `nrx-frontend-engineer` |
| Code reviewer | `nrx-code-reviewer` |
| QA engineer | `nrx-qa-engineer` |
| Reporter | `nrx-reporter` |

Run a session as the coordinator with `claude --agent nrx-coordinator`; the coordinator dispatches every other role.

## Other skills

- `skills/impeccable/` — design craft and critique for UI work (used by the designer).
