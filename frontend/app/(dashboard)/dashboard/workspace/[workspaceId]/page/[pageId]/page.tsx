import { DocumentPageView } from "@/app/(dashboard)/_components/document-page-view"

type PageProps = {
  params: Promise<{
    workspaceId: string
    pageId: string
  }>
}

export default async function WorkspacePageRoute({ params }: PageProps) {
  const { workspaceId, pageId } = await params
  return <DocumentPageView workspaceId={workspaceId} pageId={pageId} />
}
