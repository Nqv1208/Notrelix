"use client"

import { MondayDocEditor } from "@/components/docs/editor"

interface EditorShellProps {
  pageId: string
  workspaceId: string
}

export function EditorShell({ pageId, workspaceId }: EditorShellProps) {
  return <MondayDocEditor pageId={pageId} workspaceId={workspaceId} />
}
