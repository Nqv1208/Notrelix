import { useCallback, useState } from "react";

export function useSelectedCardPanel(initialCardId: string | null = null) {
  const [selectedCardId, setSelectedCardId] = useState<string | null>(
    initialCardId,
  );

  const openCard = useCallback(
    (cardId: string) => setSelectedCardId(cardId),
    [setSelectedCardId],
  );
  const closePanel = useCallback(
    () => setSelectedCardId(null),
    [setSelectedCardId],
  );

  return {
    selectedCardId,
    isOpen: Boolean(selectedCardId),
    openCard,
    closePanel,
  };
}
