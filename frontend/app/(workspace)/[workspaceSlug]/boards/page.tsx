import { BoardWorkbench } from "./_components/board-workbench"

export default async function BoardsPage({ params }: { params: Promise<{ workspaceSlug: string }> }) {
  const { workspaceSlug } = await params
  return <BoardWorkbench workspaceSlug={workspaceSlug} />
}
