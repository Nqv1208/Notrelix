import { BoardWorkbench } from "./_components/board-workbench"

export default async function BoardsPage({ params }: { params: Promise<{ workspaceId: string }> }) {
  const { workspaceId } = await params
  return <BoardWorkbench workspaceId={workspaceId} />
}
