import type { NotrelixClient } from "@notrelix/contracts";
import type {
  AutomationRuleDetailDto,
  AutomationRuleDocumentDto,
  AutomationRunsDto,
} from "@notrelix/work-management-core";

/** Gap `CTR-GAP-WM-BOARD-AUTOMATIONS`: the rules of a board and their run log. */
export function createBoardAutomationsApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const key = () => crypto.randomUUID();
  const base = (boardId: string) => `/boards/${boardId}/automations`;
  return {
    get: (boardId: string, ruleId: string) =>
      api.get<AutomationRuleDetailDto>(`${base(boardId)}/${ruleId}`, quiet),
    create: (boardId: string) =>
      api.post<{ id: string }>(
        base(boardId),
        {},
        { idempotencyKey: key(), ...quiet },
      ),
    save: (
      boardId: string,
      ruleId: string,
      revision: number,
      definition: AutomationRuleDocumentDto,
      mode: "draft" | "enable" | "version",
    ) =>
      api.put<void>(
        `${base(boardId)}/${ruleId}`,
        { revision, definition, mode },
        { idempotencyKey: key(), ...quiet },
      ),
    changeRunner: (boardId: string, ruleId: string, personId: string) =>
      api.patch<void>(
        `${base(boardId)}/${ruleId}/runner`,
        { personId },
        { idempotencyKey: key(), ...quiet },
      ),
    runs: (boardId: string) =>
      api.get<AutomationRunsDto>(`/boards/${boardId}/automation-runs`, quiet),
    rerunStep: (boardId: string, runId: string, stepIndex: number) =>
      api.post<void>(
        `/boards/${boardId}/automation-runs/${runId}/rerun`,
        { stepIndex },
        { idempotencyKey: key(), ...quiet },
      ),
  };
}

export type BoardAutomationsApi = ReturnType<typeof createBoardAutomationsApi>;
