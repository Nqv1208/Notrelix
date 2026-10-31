import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { CircleAlert } from "lucide-react";
import { endpoints } from "@notrelix/contracts";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { Board } from "@notrelix/work-management-core";
import {
  fillBoardShareCopy as fill,
  useBoardShareCopy,
} from "@notrelix/work-management-web";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import { useBoardSharing } from "./use-board-sharing";
import { toastFailure } from "./failure-toast";

type Refusal = "last-manager" | "last-owner";

/**
 * Leave the board: ends the person's own explicit board role. Grants given to them stay (Governance owns them);
 * on a Workspace-visible board they still see it. The last Manager and the last owner stay (the server refuses
 * with a rule code and nothing changes); the dialog says why and offers the way out.
 */
export function BoardLeaveDialog({
  workspaceId,
  boardId,
  board,
  open,
  onClose,
  onOpenShare,
}: {
  workspaceId: string;
  boardId: string;
  board: Board | undefined;
  open: boolean;
  onClose: () => void;
  onOpenShare: () => void;
}) {
  const copy = useBoardShareCopy().leave;
  const { api } = useAppRuntime();
  const navigate = useNavigate();
  const sharing = useBoardSharing(workspaceId, boardId, open);
  const [busy, setBusy] = useState(false);
  const [refusal, setRefusal] = useState<Refusal | null>(null);
  const name = board?.title ?? "";
  const isPrivate = board?.visibility === "private";
  const mine = sharing.data?.entries.find((entry) => entry.you);
  const noRole =
    Boolean(sharing.data?.entriesDisclosed) && !(mine && mine.memberRole);

  async function leave() {
    setBusy(true);
    setRefusal(null);
    try {
      await api.api.post(
        endpoints.boards.leave(boardId),
        {},
        {
          headers: { "Idempotency-Key": crypto.randomUUID() },
          skipGlobalErrorToast: true,
        },
      );
      toast.success(fill(copy.left, { board: name }));
      onClose();
      if (isPrivate)
        await navigate({
          to: "/workspaces/$workspaceId",
          params: { workspaceId },
        });
    } catch (error) {
      const rule = (error as { ruleCode?: string } | null)?.ruleCode;
      if (rule === "WorkManagement_Board_LastManager")
        setRefusal("last-manager");
      else if (rule === "WorkManagement_Board_LastOwner")
        setRefusal("last-owner");
      else toastFailure(copy.failed, error, "");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent className="gap-3 sm:max-w-[460px]">
        <DialogHeader>
          <DialogTitle>{copy.title}</DialogTitle>
          <DialogDescription>
            {fill(isPrivate ? copy.private : copy.open, { board: name })}
          </DialogDescription>
        </DialogHeader>
        {refusal ? (
          <div
            role="alert"
            className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-compact leading-[1.4] text-danger-text"
          >
            <CircleAlert
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0"
              strokeWidth={1.8}
            />
            <span className="flex min-w-0 flex-1 flex-col gap-px">
              <b className="font-semibold text-foreground">
                {copy.lastManagerTitle}
              </b>
              <span>
                {fill(
                  refusal === "last-owner"
                    ? copy.lastOwnerBody
                    : copy.lastManagerBody,
                  { board: name },
                )}
              </span>
            </span>
          </div>
        ) : null}
        {noRole ? (
          <p className="m-0 text-compact text-meta-foreground">{copy.noRole}</p>
        ) : null}
        <p className="m-0 text-compact leading-[1.45] text-meta-foreground">
          {copy.grants}
        </p>
        <DialogFooter>
          {sharing.data?.canManage ? (
            <button
              type="button"
              onClick={onOpenShare}
              className="inline-flex h-8 items-center rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium text-foreground hover:bg-state-hover-strong"
            >
              {refusal ? copy.chooseManager : copy.openShare}
            </button>
          ) : null}
          <button
            type="button"
            onClick={onClose}
            className="inline-flex h-8 items-center rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium text-foreground hover:bg-state-hover-strong"
          >
            {copy.cancel}
          </button>
          <button
            type="button"
            disabled={busy || noRole}
            onClick={() => void leave()}
            className="inline-flex h-8 items-center rounded-[7px] border border-danger bg-danger px-3 text-compact font-semibold text-danger-foreground hover:opacity-90 disabled:opacity-60"
          >
            {copy.confirm}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
