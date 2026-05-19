import { CardDetail } from "../../_components/card-detail"

export default async function CardPage({ params }: { params: Promise<{ workspaceSlug: string; boardId: string; cardId: string }> }) {
  const { workspaceSlug, boardId, cardId } = await params
  return <CardDetail workspaceSlug={workspaceSlug} boardId={boardId} cardId={cardId} mode="page" />
}
