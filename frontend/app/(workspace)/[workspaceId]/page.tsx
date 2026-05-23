import { WorkspaceBoardShell } from "./_components/workspace-board-shell"

export default async function WorkspaceHomePage({
  params,
  searchParams,
}: {
  params: Promise<{ workspaceId: string }>
  searchParams: Promise<{ view?: string }>
}) {
  const { workspaceId } = await params
  const { view } = await searchParams

  return <WorkspaceBoardShell workspaceId={workspaceId} requestedView={view} />
}
