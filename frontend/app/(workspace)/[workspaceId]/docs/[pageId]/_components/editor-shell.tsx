"use client"

import { MondayDocEditor } from "./editor/monday-doc-editor"
import { useWorkspaceTabbedRoute } from "../../../_components/shell/workspace-tabbed-shell"

export function EditorShell() {
  const route = useWorkspaceTabbedRoute()

  if (route.kind !== "docs" || !route.pageId) return null

  return <MondayDocEditor pageId={route.pageId} workspaceId={route.workspaceId} showToolbar={false} />
}
