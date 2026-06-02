"use client"

import { MondayDocEditor } from "./editor/monday-doc-editor"

interface EditorShellProps {
  pageId: string
  workspaceId: string
}

export function EditorShell({ pageId, workspaceId }: EditorShellProps) {
  return <MondayDocEditor pageId={pageId} workspaceId={workspaceId} />
}
