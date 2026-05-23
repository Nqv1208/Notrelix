import { BoardWorkbenchClient } from "./_components/board-workbench-client"

export default async function BoardPage({ params }: { params: Promise<{ workspaceId: string; boardId: string }> }) {
  const { workspaceId, boardId } = await params
  return <BoardWorkbenchClient workspaceId={workspaceId} boardId={boardId} />
}
