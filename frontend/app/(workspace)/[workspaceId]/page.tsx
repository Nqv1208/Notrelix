import { WorkspaceBoardShell } from "./_components/board-layout/workspace-board-shell"

export default async function WorkspaceHomePage({
  params,
  searchParams,
}: {
  params: Promise<{ workspaceId: string }>
  searchParams: Promise<{ view?: string; panel?: string }>
}) {
  const [{ workspaceId }, { view, panel }] = await Promise.all([params, searchParams])

  return <WorkspaceBoardShell workspaceId={workspaceId} requestedView={view} panel={panel} />
}
