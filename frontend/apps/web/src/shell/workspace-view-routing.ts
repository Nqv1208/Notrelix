import type { WorkspaceView } from "@notrelix/features-workspace/core";

export function resolveActiveWorkspaceView(
  views: readonly WorkspaceView[],
  pathname: string,
): WorkspaceView | null {
  const pathParts = pathname.split("/").filter(Boolean);

  if (pathParts.includes("dashboard")) {
    return views.find((view) => view.type === "dashboard") ?? null;
  }

  const boardsIndex = pathParts.indexOf("boards");
  if (boardsIndex >= 0) {
    const boardId = pathParts[boardsIndex + 1];
    return views.find((view) => view.target.boardId === boardId) ?? null;
  }

  const docsIndex = pathParts.indexOf("docs");
  if (docsIndex >= 0) {
    const pageId = pathParts[docsIndex + 1];
    return views.find((view) => view.target.pageId === pageId) ?? null;
  }

  return null;
}
