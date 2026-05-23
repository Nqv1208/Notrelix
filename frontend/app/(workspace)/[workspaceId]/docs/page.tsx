import { DocsClientPage } from "./_components/docs-client-page"

interface DocsPageProps {
  params: Promise<{ workspaceId: string }>
}

export default async function DocsPage({ params }: DocsPageProps) {
  const { workspaceId } = await params

  return <DocsClientPage workspaceId={workspaceId} />
}
