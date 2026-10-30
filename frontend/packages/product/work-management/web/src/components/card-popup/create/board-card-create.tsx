import { useId, useMemo, useState } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import { useFullBoard } from "@notrelix/work-management-state";
import { useCardPopupCopy } from "../use-card-popup-copy";
import { CardCreatePopup } from "./card-create-popup";

export interface BoardCardCreateBoard {
  readonly id: string;
  readonly label: string;
}

/**
 * "Add card" from outside a board (Home, My work): the member picks the board
 * (a single board, or one named by the caller, skips the question), then the
 * popup opens in create mode on it. The member is ticked as an assignee so the
 * card lands in their own work.
 */
export function BoardCardCreate({
  open,
  onOpenChange,
  workspaceId,
  boards,
  initialBoardId,
  initialTitle,
  assigneeId,
  onCreated,
}: {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly workspaceId: string;
  /** Boards the member can add a card to, in the order they are offered. */
  readonly boards: readonly BoardCardCreateBoard[];
  readonly initialBoardId?: string | null;
  readonly initialTitle?: string;
  readonly assigneeId?: string | null;
  readonly onCreated?: (result: {
    readonly boardId: string;
    readonly itemId: string | undefined;
  }) => void;
}) {
  const t = useCardPopupCopy().create;
  const fieldId = useId();
  const [picked, setPicked] = useState<string | null>(null);
  const [chosen, setChosen] = useState<string | null>(null);
  const known = (id?: string | null) =>
    boards.find((board) => board.id === id)?.id;
  const boardId =
    known(chosen) ??
    known(initialBoardId) ??
    (boards.length === 1 ? boards[0]?.id : undefined);
  const { board, groups } = useFullBoard(
    open ? boardId : undefined,
    workspaceId,
  );
  const loaded = board && board.id === boardId ? board : undefined;
  const defaultAssigneeIds = useMemo(
    () =>
      assigneeId && loaded?.members.some((m) => m.userId === assigneeId)
        ? [assigneeId]
        : [],
    [assigneeId, loaded],
  );
  const close = (next: boolean) => {
    if (!next) {
      setChosen(null);
      setPicked(null);
    }
    onOpenChange(next);
  };

  if (open && !boardId)
    return (
      <Dialog open onOpenChange={close}>
        <DialogContent showCloseButton={false} className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{t.chooseBoardTitle}</DialogTitle>
          </DialogHeader>
          {boards.length === 0 ? (
            <p className="text-muted-foreground">{t.noBoards}</p>
          ) : (
            <Select value={picked ?? ""} onValueChange={setPicked}>
              <SelectTrigger
                id={fieldId}
                aria-label={t.board}
                className="w-full"
              >
                <SelectValue placeholder={t.chooseBoard} />
              </SelectTrigger>
              <SelectContent>
                {boards.map((candidate) => (
                  <SelectItem key={candidate.id} value={candidate.id}>
                    {candidate.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => close(false)}>
              {t.cancel}
            </Button>
            <Button disabled={!picked} onClick={() => setChosen(picked)}>
              {t.chooseBoardGo}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );

  return (
    <CardCreatePopup
      open={open}
      onOpenChange={close}
      workspaceId={workspaceId}
      board={loaded}
      groups={loaded ? groups : []}
      defaultGroupId={loaded ? (groups[0]?.id ?? "") : ""}
      defaultStatusId={null}
      defaultTitle={initialTitle}
      defaultAssigneeIds={defaultAssigneeIds}
      onCreated={onCreated}
    />
  );
}
