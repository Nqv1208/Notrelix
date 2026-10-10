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

  if (listed === false) return "forbidden";
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
