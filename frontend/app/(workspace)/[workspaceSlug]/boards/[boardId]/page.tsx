import { BoardWorkbenchClient } from "./_components/board-workbench-client"

export default async function BoardPage({ params }: { params: Promise<{ workspaceSlug: string; boardId: string }> }) {
  const { workspaceSlug, boardId } = await params
  return <BoardWorkbenchClient workspaceSlug={workspaceSlug} boardId={boardId} />
}
