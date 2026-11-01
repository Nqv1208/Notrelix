import { describeWorkspaceFailure } from "@notrelix/features-workspace/core";
import type { SectionState } from "@notrelix/features-workspace/web";

/** Maps a query's status to a page section state (DESIGN.md §31). */
export function sectionState<T>(
  query: {
    readonly isError: boolean;
    readonly isSuccess: boolean;
    readonly error?: unknown;
  },
  data: T | undefined,
): SectionState<T> {
  if (query.isError) {
    const { kind } = describeWorkspaceFailure(query.error);
    return kind === "forbidden"
      ? { status: "error", reason: "forbidden" }
      : kind === "network"
        ? { status: "error", reason: "offline" }
        : { status: "error" };
  }
  if (!query.isSuccess || data === undefined) return { status: "loading" };
  return { status: "ready", data };
}
