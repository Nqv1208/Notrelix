import type { NotrelixClient } from "@notrelix/contracts";
import type {
  BoardSettingsDto,
  BoardSettingsFieldPatch,
  BoardSettingsFieldType,
  BoardSettingsGeneralPatch,
  BoardSettingsViewDto,
  BoardSettingsViewPatch,
} from "@notrelix/work-management-core";

export function createBoardSettingsApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const key = () => crypto.randomUUID();
  const base = (boardId: string) => `/boards/${boardId}/settings`;
  const post = (path: string, body: object) =>
    api.post<void>(path, body, { idempotencyKey: key(), ...quiet });
  const patch = (path: string, body: object) =>
    api.patch<void>(path, body, { idempotencyKey: key(), ...quiet });
  return {
    get: (boardId: string) => api.get<BoardSettingsDto>(base(boardId), quiet),
    updateGeneral: (
      boardId: string,
      version: number,
      changes: BoardSettingsGeneralPatch,
    ) => patch(`${base(boardId)}/general`, { ...changes, version }),
    archiveBoard: (boardId: string, version: number) =>
      post(`${base(boardId)}/archive`, { version }),
    deleteBoard: (boardId: string, version: number, confirmTitle: string) =>
      post(`${base(boardId)}/delete`, { version, confirmTitle }),

    createField: (
      boardId: string,
      version: number,
      type: BoardSettingsFieldType,
    ) => post(`${base(boardId)}/fields`, { version, type }),
    reorderFields: (boardId: string, version: number, ids: readonly string[]) =>
      post(`${base(boardId)}/fields/reorder`, { version, ids }),
    updateField: (
      boardId: string,
      version: number,
      fieldId: string,
      changes: BoardSettingsFieldPatch,
    ) => patch(`${base(boardId)}/fields/${fieldId}`, { ...changes, version }),
    archiveField: (boardId: string, version: number, fieldId: string) =>
      post(`${base(boardId)}/fields/${fieldId}/archive`, { version }),
    restoreField: (boardId: string, version: number, fieldId: string) =>
      post(`${base(boardId)}/fields/${fieldId}/restore`, { version }),
    addOption: (
      boardId: string,
      version: number,
      fieldId: string,
      option: { name: string; color: string },
    ) =>
      post(`${base(boardId)}/fields/${fieldId}/options`, {
        ...option,
        version,
      }),
    updateOption: (
      boardId: string,
      version: number,
      fieldId: string,
      optionId: string,
      changes: { name?: string; color?: string },
    ) =>
      patch(`${base(boardId)}/fields/${fieldId}/options/${optionId}`, {
        ...changes,
        version,
      }),
    deleteOption: (
      boardId: string,
      version: number,
      fieldId: string,
      optionId: string,
    ) =>
      post(`${base(boardId)}/fields/${fieldId}/options/${optionId}/delete`, {
        version,
      }),
    reorderOptions: (
      boardId: string,
      version: number,
      fieldId: string,
      ids: readonly string[],
    ) =>
      post(`${base(boardId)}/fields/${fieldId}/options/reorder`, {
        version,
        ids,
      }),

    createView: (
      boardId: string,
      version: number,
      kind: BoardSettingsViewDto["kind"],
    ) => post(`${base(boardId)}/views`, { version, kind }),
    reorderViews: (boardId: string, version: number, ids: readonly string[]) =>
      post(`${base(boardId)}/views/reorder`, { version, ids }),
    updateView: (
      boardId: string,
      version: number,
      viewId: string,
      changes: BoardSettingsViewPatch,
    ) => patch(`${base(boardId)}/views/${viewId}`, { ...changes, version }),
    duplicateView: (boardId: string, version: number, viewId: string) =>
      post(`${base(boardId)}/views/${viewId}/duplicate`, { version }),
    deleteView: (boardId: string, version: number, viewId: string) =>
      post(`${base(boardId)}/views/${viewId}/delete`, { version }),

    createGroup: (boardId: string, version: number, name: string) =>
      post(`${base(boardId)}/groups`, { version, name }),
    reorderGroups: (boardId: string, version: number, ids: readonly string[]) =>
      post(`${base(boardId)}/groups/reorder`, { version, ids }),
    updateGroup: (
      boardId: string,
      version: number,
      groupId: string,
      changes: { name?: string; color?: string },
    ) => patch(`${base(boardId)}/groups/${groupId}`, { ...changes, version }),
    deleteGroup: (
      boardId: string,
      version: number,
      groupId: string,
      moveToGroupId: string | null,
    ) =>
      post(`${base(boardId)}/groups/${groupId}/delete`, {
        version,
        moveToGroupId,
      }),

    setAutomationEnabled: (
      boardId: string,
      version: number,
      automationId: string,
      enabled: boolean,
    ) =>
      patch(`${base(boardId)}/automations/${automationId}`, {
        version,
        enabled,
      }),
  };
}

export type BoardSettingsApi = ReturnType<typeof createBoardSettingsApi>;
