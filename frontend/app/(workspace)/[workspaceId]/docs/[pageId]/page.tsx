import { EditorShell } from "./_components/editor-shell"

interface PageEditorProps {
  params: Promise<{ workspaceId: string; pageId: string }>
}

export default async function PageEditorPage({ params }: PageEditorProps) {
  const { workspaceId, pageId } = await params

  // TODO(api):
  // Server-prefetch page detail and blocks through HydrationBoundary when backend APIs are live.
  return <EditorShell pageId={pageId} workspaceId={workspaceId} />
}
