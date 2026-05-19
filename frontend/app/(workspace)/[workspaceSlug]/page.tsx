import { WorkspaceBoardShell } from "./_components/workspace-board-shell"

export default async function WorkspaceHomePage({
  params,
  searchParams,
}: {
  params: Promise<{ workspaceSlug: string }>
  searchParams: Promise<{ view?: string }>
}) {
  const { workspaceSlug } = await params
  const { view } = await searchParams

  return <WorkspaceBoardShell workspaceSlug={workspaceSlug} requestedView={view} />
}
