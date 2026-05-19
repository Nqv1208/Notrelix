import { DocsClientPage } from "./_components/docs-client-page"

interface DocsPageProps {
  params: Promise<{ workspaceSlug: string }>
}

export default async function DocsPage({ params }: DocsPageProps) {
  const { workspaceSlug } = await params

  // TODO(api):
  // Resolve workspace ID by slug on the server before prefetching:
  // Endpoint: GET /api/workspaces/:slug
  const workspaceId = workspaceSlug

  return <DocsClientPage workspaceId={workspaceId} workspaceSlug={workspaceSlug} />
}
