import type { WorkspaceScopeState } from "@notrelix/features-workspace/web";

export interface WorkspaceScopeInput {
  readonly workspaceId: string;
  readonly list: {
    readonly isLoading: boolean;
    readonly isError: boolean;
    readonly ids: readonly string[] | undefined;
  };
  readonly detail: {
    readonly isLoading: boolean;
    readonly isError: boolean;
    readonly hasData: boolean;
    readonly errorKind: string | undefined;
    /** The record says the workspace is archived (read-only for every member). */
    readonly isArchived?: boolean;
  };
}

/**
 * The workspace scope state (WX-SHELL-004). `null` means the scope is ready.
 * Lost access — the workspace is not in the member's list, or the producer
 * answers forbidden/not found — is a distinct state from a failed load.
 */
export function resolveWorkspaceScope(
  input: WorkspaceScopeInput,
): WorkspaceScopeState | null {
  const { list, detail } = input;
  const listed = list.ids ? list.ids.includes(input.workspaceId) : undefined;

  // The member lists leave an archived workspace out (it is read-only, listed to its owners to restore).
  // The record is authorized by the producer, so one that answered and says archived is proof of access;
  // anything else missing from the list is lost access, decided once the record has answered.
  const archivedOutsideList =
    listed === false && detail.hasData && detail.isArchived === true;
  if (listed === false && !archivedOutsideList && !detail.isLoading) {
    return "forbidden";
  }
  if (detail.isError) {
    return detail.errorKind === "forbidden" || detail.errorKind === "not_found"
      ? "forbidden"
      : "error";
  }
  if (list.isError) return "error";
  if (
    list.isLoading ||
    detail.isLoading ||
    !detail.hasData ||
    listed === undefined
  ) {
    return "loading";
  }
  return null;
}
