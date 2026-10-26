import { useId, useMemo, useState } from "react";
import {
  Field,
  FieldLabel,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import { useFullBoard } from "@notrelix/work-management-state";
import { ItemCreateHost } from "./item-create-host";
import { useItemCreateCopy } from "./use-item-create-copy";

export interface BoardItemCreateBoard {
  readonly id: string;
  readonly label: string;
}

/**
 * "Add item" from outside a board (Home, My work): the member picks the board,
 * then fills the same form a board's own "Add item" opens. The board is read
 * when chosen; the member is ticked as an assignee so the item lands in
 * their own work.
 */
export function BoardItemCreateDialog({
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
  /** Boards the member can add an item to, in the order they are offered. */
  readonly boards: readonly BoardItemCreateBoard[];
  readonly initialBoardId?: string | null;
  readonly initialTitle?: string;
  /** The member the item is assigned to by default. */
  readonly assigneeId?: string | null;
  readonly onCreated?: (result: {
    readonly boardId: string;
    readonly itemId: string | undefined;
  }) => void;
}) {
  const copy = useItemCreateCopy();
  const boardFieldId = useId();
  const [chosenId, setChosenId] = useState<string | null>(null);
  const boardId =
    boards.find((board) => board.id === (chosenId ?? initialBoardId))?.id ??
    boards[0]?.id;
  const { board, groups } = useFullBoard(
    open ? boardId : undefined,
    workspaceId,
  );
  const loaded = board && board.id === boardId ? board : undefined;
  const defaultAssigneeIds = useMemo(
    () =>
      assigneeId &&
      loaded?.members.some((member) => member.userId === assigneeId)
        ? [assigneeId]
        : [],
    [assigneeId, loaded],
  );

  return (
    <ItemCreateHost
      open={open}
      onOpenChange={(next) => {
        if (!next) setChosenId(null);
        onOpenChange(next);
      }}
      workspaceId={workspaceId}
      board={loaded}
      groups={loaded ? groups : []}
      defaultGroupId={loaded ? (groups[0]?.id ?? "") : ""}
      defaultStatusId={null}
      defaultTitle={initialTitle}
      defaultAssigneeIds={defaultAssigneeIds}
      onCreated={onCreated}
      boardSlot={
        <Field>
          <FieldLabel htmlFor={boardFieldId}>{copy.board}</FieldLabel>
          <Select
            value={boardId ?? ""}
            onValueChange={(value) => setChosenId(value)}
          >
            <SelectTrigger id={boardFieldId} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {boards.map((candidate) => (
                <SelectItem key={candidate.id} value={candidate.id}>
                  {candidate.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      }
    />
  );
}
