/**
 * Where a board's settings live: `/workspaces/{id}/boards/{id}/settings` with
 * the section ("fields", "labels") in the query. The item sheet and the item
 * page link there as plain addresses, so they need no router of their own.
 */
export function boardSettingsHref(
  workspaceId: string,
  boardId: string,
  section: "fields" | "labels",
): string {
  return `/workspaces/${workspaceId}/boards/${boardId}/settings?section=${section}`;
}
