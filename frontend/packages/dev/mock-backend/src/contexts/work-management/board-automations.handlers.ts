/**
 * Work Management: the automation rules of a board (Final-Automation.dc.html).
 *
 * Gap `CTR-GAP-WM-BOARD-AUTOMATIONS` (docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Automation"): the editor of a rule with its
 * checks, the run log and one run. Only a manager of the board reads or
 * writes them; anyone else gets 403. A save may carry the `revision` of the
 * read it was made from: a stale one is 409.
 *
 * Operations:
 *   boards.automations.get          GET   /boards/:boardId/automations/:ruleId
 *   boards.automations.create       POST  /boards/:boardId/automations
 *   boards.automations.save         PUT   /boards/:boardId/automations/:ruleId
 *   boards.automations.runner       PATCH /boards/:boardId/automations/:ruleId/runner
 *   boards.automations.runs         GET   /boards/:boardId/automation-runs
 *   boards.automations.rerun        POST  /boards/:boardId/automation-runs/:runId/rerun
 */
import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
  type MockHttpResult,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import type {
  AutomationWrite,
  RuleDocument,
} from "../../state/mock-board-automations";
import { canManageBoard } from "./boards.handlers";

const GAP = { kind: "gap", gapId: "CTR-GAP-WM-BOARD-AUTOMATIONS" } as const;

type Params = { boardId: string } & Record<string, string>;

function guard(
  store: MockStore,
  boardId: string,
): MockHttpResult<unknown> | null {
  if (!store.getBoard(boardId)) return notFound("Board not found");
  if (!canManageBoard(boardId, store)) return forbidden();
  return null;
}

function reply<T>(result: AutomationWrite<T>): MockHttpResult<unknown> {
  if (result.result === "ok")
    return result.value === undefined ? noContent() : ok(result.value);
  if (result.result === "not-found") return notFound("Not found");
  if (result.result === "conflict")
    return conflict(result.message ?? "Conflict");
  return validationError({ [result.message ?? "request"]: ["invalid"] });
}

const base = "/boards/:boardId/automations";

export const boardAutomationsOperations = [
  defineMockOperation<Params, never, unknown>({
    id: "boards.automations.get",
    contract: GAP,
    method: "GET",
    route: `${base}/:ruleId`,
    async handle({ params, store }) {
      const denied = guard(store, params.boardId);
      if (denied) return denied;
      const detail = store.automations.detail(params.boardId, params.ruleId!);
      return detail ? ok(detail) : notFound("Rule not found");
    },
  }),
  defineMockOperation<Params, never, unknown>({
    id: "boards.automations.create",
    contract: GAP,
    method: "POST",
    route: base,
    async handle({ params, store }) {
      const denied = guard(store, params.boardId);
      if (denied) return denied;
      const result = store.automations.createDraft(params.boardId);
      if (result.result !== "ok") return reply(result);
      store.settings.registry(params.boardId).version += 1;
      return created(result.value);
    },
  }),
  defineMockOperation<
    Params,
    {
      revision?: number;
      definition: RuleDocument;
      mode: "draft" | "enable" | "version";
    },
    unknown
  >({
    id: "boards.automations.save",
    contract: GAP,
    method: "PUT",
    route: `${base}/:ruleId`,
    async handle({ params, body, store }) {
      const denied = guard(store, params.boardId);
      if (denied) return denied;
      const result = store.automations.save(
        params.boardId,
        params.ruleId!,
        body!,
      );
      if (result.result === "ok")
        store.settings.registry(params.boardId).version += 1;
      return reply(result);
    },
  }),
  defineMockOperation<Params, { personId: string }, unknown>({
    id: "boards.automations.runner",
    contract: GAP,
    method: "PATCH",
    route: `${base}/:ruleId/runner`,
    async handle({ params, body, store }) {
      const denied = guard(store, params.boardId);
      if (denied) return denied;
      const result = store.automations.changeRunner(
        params.boardId,
        params.ruleId!,
        body!.personId,
      );
      if (result.result === "ok")
        store.settings.registry(params.boardId).version += 1;
      return reply(result);
    },
  }),
  defineMockOperation<Params, never, unknown>({
    id: "boards.automations.runs",
    contract: GAP,
    method: "GET",
    route: "/boards/:boardId/automation-runs",
    async handle({ params, store }) {
      const denied = guard(store, params.boardId);
      if (denied) return denied;
      return ok(store.automations.runs(params.boardId));
    },
  }),
  defineMockOperation<Params, { stepIndex: number }, unknown>({
    id: "boards.automations.rerun",
    contract: GAP,
    method: "POST",
    route: "/boards/:boardId/automation-runs/:runId/rerun",
    async handle({ params, store }) {
      const denied = guard(store, params.boardId);
      if (denied) return denied;
      return reply(store.automations.rerunStep(params.boardId, params.runId!));
    },
  }),
];
