import { CardModal } from "../../../_components/card-modal"

export default async function CardModalPage({ params }: { params: Promise<{ workspaceSlug: string; boardId: string; cardId: string }> }) {
  const { workspaceSlug, boardId, cardId } = await params
  return <CardModal workspaceSlug={workspaceSlug} boardId={boardId} cardId={cardId} />
}
