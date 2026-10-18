import type { SectionState } from "@notrelix/features-workspace/web";

/** Maps a query's status to a page section state (DESIGN.md §31). */
export function sectionState<T>(
  query: { readonly isError: boolean; readonly isSuccess: boolean },
  data: T | undefined,
): SectionState<T> {
  if (query.isError) return { status: "error" };
  if (!query.isSuccess || data === undefined) return { status: "loading" };
  return { status: "ready", data };
}
