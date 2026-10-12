import { useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Sheet, SheetContent, SheetTitle } from "@notrelix/ui-web";
import {
  useCard,
  useCardActivity,
  useCardComments,
  useCardFiles,
  useCreateCardUpdate,
  useDeleteCard,
  useDeleteCardUpdate,
  useDuplicateCard,
  useUpdateCard,
  useUpdateCardUpdate,
  useUpdateFieldValue,
} from "@notrelix/work-management-state";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { useIsMobile } from "@notrelix/ui-web";
import { TaskDetailPanelSurface } from "./task-detail-panel-surface";
import { defaultTaskDetailCapabilities } from "./task-detail-types";
import { useItemSheetCopy } from "./use-item-sheet-copy";
import type { ItemSheetCopy } from "./item-sheet.copy";

export function TaskDetailPanel({
  board,
  cardId,
  open,
  onClose,
  onExitComplete,
}: {
  board: Board;
  cardId: string | null;
  open: boolean;
  onClose: () => void;
  onExitComplete?: () => void;
}) {
  const isMobile = useIsMobile();
  const copy = useItemSheetCopy();
  // Read once so the due and comment wording judge against one instant.
  const [now] = useState(() => new Date());
  const content = cardId ? (
    <TaskDetailPanelContent
      board={board}
      cardId={cardId}
      copy={copy}
      now={now}
      onClose={onClose}
    />
  ) : null;

  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  });

  useEffect(() => {
    if (!open || isMobile) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") onCloseRef.current();
    }

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isMobile, open]);

  if (isMobile) {
    return (
      <Sheet
        open={open}
        onOpenChange={(nextOpen: boolean) => {
          if (!nextOpen) onClose();
        }}
      >
        <SheetContent
          side="right"
          className="w-full gap-0 p-0 sm:max-w-none"
          showCloseButton={false}
        >
          <SheetTitle className="sr-only">{copy.dialogLabel}</SheetTitle>
          {content}
        </SheetContent>
      </Sheet>
    );
  }

  return (
    <AnimatePresence mode="wait" onExitComplete={onExitComplete}>
      {open && cardId ? (
        <motion.div
          key={cardId}
          initial={{ x: 40, opacity: 0 }}
          animate={{ x: 0, opacity: 1 }}
          exit={{ x: 40, opacity: 0 }}
          transition={{ duration: 0.18, ease: [0.2, 0, 0, 1] }}
          className="flex h-full min-h-0 w-full min-w-0 overflow-hidden border-l border-border bg-popover"
        >
          {content}
        </motion.div>
      ) : null}
    </AnimatePresence>
  );
}

function TaskDetailPanelContent({
  board,
  cardId,
  copy,
  now,
  onClose,
}: {
  board: Board;
  cardId: string;
  copy: ItemSheetCopy;
  now: Date;
  onClose: () => void;
}) {
  const { card, isLoading, error } = useCard(cardId, board.workspaceId);

  if (isLoading)
    return (
      <TaskDetailPanelSurface
        status="loading"
        board={board}
        card={null}
        capabilities={defaultTaskDetailCapabilities}
        callbacks={emptyCallbacks(onClose)}
        copy={copy}
        now={now}
      />
    );
  if (error || !card)
    return (
      <TaskDetailPanelSurface
        status="error"
        board={board}
        card={null}
        capabilities={defaultTaskDetailCapabilities}
        callbacks={emptyCallbacks(onClose)}
        copy={copy}
        now={now}
      />
    );

  return (
    <TaskDetailReadyContainer
      board={board}
      card={card as CardDetail}
      copy={copy}
      now={now}
      onClose={onClose}
    />
  );
}

function TaskDetailReadyContainer({
  board,
  card,
  copy,
  now,
  onClose,
}: {
  board: Board;
  card: CardDetail;
  copy: ItemSheetCopy;
  now: Date;
  onClose: () => void;
}) {
  const files = useCardFiles(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const deleteCard = useDeleteCard(card.boardId, card.workspaceId);
  const duplicateCard = useDuplicateCard(card.boardId, card.workspaceId);
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId);
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);

  return (
    <TaskDetailPanelSurface
      status="ready"
      board={board}
      card={card}
      capabilities={defaultTaskDetailCapabilities}
      copy={copy}
      now={now}
      detailData={{
        files: files.data ?? card.files,
        filesLoading: files.isLoading,
        activity: activity.data ?? card.activity,
        activityLoading: activity.isLoading,
        activityFetching: activity.isFetching,
        updates: updates.data ?? card.updates,
        updatesLoading: updates.isLoading,
      }}
      callbacks={{
        onClose,
        onRenameTitle: (cardId, patch) => updateCard.mutate({ cardId, patch }),
        onToggleWatch: () => undefined,
        onDuplicate: (cardId) => duplicateCard.mutate(cardId),
        onArchive: (cardId) => {
          deleteCard.mutate(cardId);
          onClose();
        },
        onUpdateFieldValue: (payload) => updateFieldValue.mutate(payload),
        onRefreshActivity: () => {
          void activity.refetch();
        },
        onCreateUpdate: (input, options) =>
          createUpdate.mutate(input, {
            onSuccess: options?.onSuccess,
          }),
        onUpdateUpdate: (updateId, body) =>
          updateUpdate.mutate({ updateId, body }),
        onDeleteUpdate: (updateId) => deleteUpdate.mutate(updateId),
        onSelectTab: () => undefined,
      }}
    />
  );
}

function emptyCallbacks(onClose: () => void) {
  return {
    onClose,
    onRenameTitle: () => undefined,
    onToggleWatch: () => undefined,
    onDuplicate: () => undefined,
    onArchive: () => undefined,
    onUpdateFieldValue: () => undefined,
    onRefreshActivity: () => undefined,
    onCreateUpdate: () => undefined,
    onUpdateUpdate: () => undefined,
    onDeleteUpdate: () => undefined,
    onSelectTab: () => undefined,
  };
}
