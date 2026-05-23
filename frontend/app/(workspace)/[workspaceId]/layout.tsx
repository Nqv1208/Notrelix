import { WorkspaceShell } from "./_components/workspace-shell"

export default async function WorkspaceLayout({
  children,
  params,
}: {
  children: React.ReactNode
  params: Promise<{ workspaceId: string }>
}) {
  const { workspaceId } = await params
  return <WorkspaceShell workspaceId={workspaceId}>{children}</WorkspaceShell>
}
