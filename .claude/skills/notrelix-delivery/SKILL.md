---
name: notrelix-delivery
description: Notrelix feature delivery process: roles, gates, hand-offs, freeze. Use when planning the roadmap, opening a feature, dispatching any agent, reviewing, merging, or reporting product work in this repo.
---

# Notrelix delivery process

Notrelix is an enterprise work-management system. Each **feature** is built to full completeness, front end to back end, by separate **roles**. Once complete it is **frozen**, and only then does the next feature open. This skill is the process. Each role's own instructions live in `.claude/agents/nrx-*.md`.

## Hard rules

1. **One feature open at a time.** The roadmap (`.superpowers/roadmap.md`) lists features in order. A feature opens only when the previous one is frozen. Agents run in parallel only on tasks inside the open feature.
2. **One agent, one role, for its whole life.** Every dispatch names exactly one `nrx-*` role as `subagent_type`. A running or resumed agent stays in that role. Work for another role goes to a fresh agent of that role.
3. **Author, reviewer and tester are three different agents.** Whoever produced a deliverable never approves or tests it.
4. **Each role delivers only its own deliverable.** Reviewers and QA report findings, and the authoring role fixes them. The coordinator plans, dispatches, gates and merges. The coordinator never writes product code, designs or tests.
5. **A gate opens on evidence, never on a claim.** Evidence means a measured design review, a reviewer verdict on the head SHA, a QA report on the head SHA, and green CI.
6. **Scope is the brief.** Nothing outside the brief's acceptance criteria is built. Nothing inside it is cut to finish sooner. Any change to scope goes back to the BA and the owner.
7. **Escalate to the owner** on product meaning, security, tenant scope, export of tenant data, and anything RULE.md says needs a decision. Record every answer in the feature's `decisions.md`.

## Roles

| Role (agent file) | Owns | Produces |
|---|---|---|
| Coordinator (`nrx-coordinator`) | roadmap, gates, dispatch, worktrees, merge | `status.md`, gate records |
| Business analyst (`nrx-business-analyst`) | product meaning, scope | `brief.md` with numbered acceptance criteria (AC-n) |
| UX/UI designer (`nrx-ux-designer`) | every UI state on the canvas | artboards + `design.md` |
| Design reviewer (`nrx-design-reviewer`) | measured design QA | design verdict |
| Solution architect (`nrx-solution-architect`) | contract, data, security, task split | `tech-design.md` |
| Backend engineer (`nrx-backend-engineer`) | Domain, Application, Infrastructure, API, migrations, backend tests | backend PR |
| Frontend engineer (`nrx-frontend-engineer`) | mock backend, query/mutation, realtime client, UI, component tests | frontend PR |
| Code reviewer (`nrx-code-reviewer`) | diff review against brief, tech design, RULE.md | code verdict per head SHA |
| QA engineer (`nrx-qa-engineer`) | acceptance tests per AC, role matrix, a11y, devices, dev-stack run | QA report per head SHA |
| Reporter (`nrx-reporter`) | facts for the owner | progress / closing report |

## Feature folder

Every feature has a folder `.superpowers/features/<NN>-<slug>/`. It is gitignored and stays local. It holds:

- `brief.md` (written by the BA)
- `decisions.md` (owner answers, plus coordinator decisions inside the brief)
- `design.md` (the designer's artboard index and measurements)
- `design-review.md` (the coordinator records each verdict)
- `tech-design.md` (written by the architect)
- `reviews/` (one code verdict per PR head)
- `qa/` (one QA report per head)
- `status.md` (the coordinator's gate log)
- `reports/` (the reporter's reports to the owner)

Templates are in [handoffs.md](handoffs.md).

## Pipeline

The gates run in order. Each gate's completion criterion must hold before the next role is dispatched.

1. **G0 Open.** The coordinator opens the next roadmap feature, creates its folder, and writes `status.md`. Done when the previous feature shows `frozen` in the roadmap.
2. **G1 Brief.** Dispatch `nrx-business-analyst`. Done when:
   - `brief.md` covers every section of the brief template;
   - every acceptance criterion is testable;
   - every open question is answered in `decisions.md`.
3. **G2 Design.** Dispatch `nrx-ux-designer`, then a fresh `nrx-design-reviewer`, and loop until the verdict is PASS. Done when:
   - every UI state the brief lists has an artboard;
   - the reviewer's verdict on the latest canvas version is PASS.
4. **G3 Technical design.** Dispatch `nrx-solution-architect`. Done when:
   - `tech-design.md` names the contract, authorization, RLS, data, migration, realtime and query keys;
   - it splits the work into tasks that each own their own files;
   - every security or tenant item has the owner's answer.
5. **G4 Build.** The coordinator creates one worktree per task, then dispatches engineers per the task split. Backend lands its contract first; frontend builds against the mock backend generated from it. Each task ends as a PR whose pre-push passed.
6. **G5 Code review.** Dispatch a fresh `nrx-code-reviewer` per PR head. A CHANGES verdict goes back to the same engineer, and a new review runs on the new head. Done when the verdict on the current head is APPROVE.
7. **G6 QA.** Dispatch `nrx-qa-engineer`. Done when every AC-n has a passing automated test and a dev-stack check on the current head. Bugs go back to the owning engineer, then the flow returns to G5.
8. **G7 Merge.** The coordinator merges only when all of these hold on the same head SHA: APPROVE, QA PASS, and every CI check green.
9. **G8 Close.** QA re-runs the acceptance checks on develop on the dev stack. The coordinator then marks the feature `frozen` in the roadmap, and `nrx-reporter` writes the closing report. A frozen feature reopens only for a bug, as a new bug item.

What "complete" means at each gate is defined in [gate-checklists.md](gate-checklists.md). Every role that writes to the repo follows [repo-rules.md](repo-rules.md).

## Dispatching

Each dispatch brief names five things:
- the role;
- the feature folder path;
- the gate;
- the worktree path (only for roles that write);
- the exact deliverable and its completion criterion.

The coordinator creates every git worktree from the main session. Agent `isolation: worktree` cannot run git under the RTK hook. The number of agents running at once follows the owner's current limit. Reviewers count toward it. A resumed agent stays in its role.
