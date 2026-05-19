import { WorkspaceShell } from "./_components/workspace-shell"

export default async function WorkspaceLayout({
  children,
  params,
}: {
  children: React.ReactNode
  params: Promise<{ workspaceSlug: string }>
}) {
  const { workspaceSlug } = await params
  return <WorkspaceShell workspaceSlug={workspaceSlug}>{children}</WorkspaceShell>
}
