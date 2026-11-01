import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Lock, TriangleAlert } from "lucide-react";
import { endpoints } from "@notrelix/contracts";
import type { OperationResponse } from "@notrelix/contracts";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  buildSpaceIndex,
  moveTargets,
} from "@notrelix/features-workspace/core";
import type { Board } from "@notrelix/work-management-core";
import {
  fillLifecycleCopy as fill,
  pluralLifecycle,
  useBoardLifecycleCopy,
} from "@notrelix/work-management-web";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import { useWorkspaceViewer } from "./use-workspace-viewer";
import { useBoardPlacements } from "./board-placement-bridge";
import { useSpaceHooks } from "./use-workspace-spaces";
import { toastFailure } from "./failure-toast";

type Preview = OperationResponse<
  "WorkManagement.Boards.GetDuplicatePreview",
  200
>;
const ROOT = "__root__";
const BUTTON =
  "inline-flex h-8 items-center rounded-[7px] border px-3 text-compact font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-60";

/**
 * Duplicate a board (WM-DuplicateBoard): a name, where it goes, whether the items come along (off by default;
 * locked with the reason over 500), and what is never copied. The server copies in one transaction, so a failure
 * leaves no half board; a retry of the same attempt makes one copy (the key stays until the dialog closes).
 */
export function BoardDuplicateDialog({
  workspaceId,
  boardId,
  board,
  open,
  onClose,
}: {
  workspaceId: string;
  boardId: string;
  board: Board | undefined;
  open: boolean;
  onClose: () => void;
}) {
  const copy = useBoardLifecycleCopy().duplicate;
  const { api } = useAppRuntime();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const hooks = useSpaceHooks();
  const tree = hooks.useSpaceTree(workspaceId);
  const viewer = useWorkspaceViewer(workspaceId);
  const placements = useBoardPlacements(workspaceId);
  // The Space the original sits in is the default place of the copy.
  const spaceId =
    placements.data?.find((placement) => placement.id === boardId)?.spaceId ??
    null;
  const original = board?.title ?? "";
  const [name, setName] = useState("");
  const [place, setPlace] = useState<string>(spaceId ?? ROOT);
  const [withItems, setWithItems] = useState(false);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(() => crypto.randomUUID());

  useEffect(() => {
    if (!open) return;
    setName(fill(copy.defaultName, { name: original }));
    setPlace(spaceId ?? ROOT);
    setWithItems(false);
    setFailed(null);
    setAttempt(crypto.randomUUID());
    // The dialog opens for one board; the default name is taken once per opening.
  }, [open]);

  const index = useMemo(() => buildSpaceIndex(tree.data ?? []), [tree.data]);
  const targets = useMemo(
    () => moveTargets(index, { kind: "board" }, viewer.role),
    [index, viewer.role],
  );
  const choices = useMemo(
    () =>
      [...index.byId.values()].filter(
        (node) => targets.spaceIds.has(node.id) && !node.isArchived,
      ),
    [index, targets],
  );

  const target = place === ROOT ? null : place;
  const preview = useQuery({
    queryKey: ["work-management", "board-duplicate-preview", boardId, target],
    enabled: open,
    gcTime: 0,
    staleTime: 0,
    retry: false,
    queryFn: () => {
      const path = endpoints.boards.duplicatePreview(boardId);
      return api.api.get<Preview>(target ? `${path}?spaceId=${target}` : path, {
        skipGlobalErrorToast: true,
      });
    },
  });
  const itemCount = preview.data?.itemCount ?? 0;
  const over = preview.data?.overLimit === true;
  const max = preview.data?.maxItems ?? 500;
  const dropped = withItems ? (preview.data?.droppedAssignees ?? 0) : 0;
  const nameOk = name.trim() !== "";

  async function submit() {
    if (!nameOk || busy) return;
    setBusy(true);
    setFailed(null);
    try {
      const result = await api.api.post<{
        id?: string;
        droppedAssignees?: number;
      }>(
        endpoints.boards.duplicate(boardId),
        {
          name: name.trim(),
          spaceId: target,
          includeItems: withItems && !over,
        },
        { idempotencyKey: attempt, skipGlobalErrorToast: true },
      );
      const id = result?.id;
      await queryClient.invalidateQueries({ queryKey: ["work-management"] });
      const left = result?.droppedAssignees ?? 0;
      toast.success(
        left > 0
          ? pluralLifecycle(copy, "doneDropped", left, { name: name.trim() })
          : fill(copy.done, { name: name.trim() }),
      );
      onClose();
      if (id)
        await navigate({
          to: "/workspaces/$workspaceId/boards/$boardId",
          params: { workspaceId, boardId: id },
        });
    } catch (error) {
      const status = (error as { status?: number } | null)?.status;
      setFailed(status === 403 || status === 404 ? copy.denied : copy.failed);
      if (status !== 403 && status !== 404)
        toastFailure(copy.failed, error, "");
      setAttempt(crypto.randomUUID());
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => (next || busy ? undefined : onClose())}
    >
      <DialogContent className="gap-3 sm:max-w-[520px]">
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            void submit();
          }}
          className="flex flex-col gap-3"
        >
          <DialogHeader>
            <DialogTitle>{copy.title}</DialogTitle>
            <DialogDescription>
              {fill(copy.intro, { name: original })}
            </DialogDescription>
          </DialogHeader>
          <label className="flex flex-col gap-1.5 text-compact font-semibold">
            {copy.nameLabel}
            <input
              value={name}
              maxLength={120}
              disabled={busy}
              aria-invalid={nameOk ? undefined : true}
              onChange={(event) => setName(event.target.value)}
              className="box-border h-[34px] w-full rounded-[7px] border border-line-strong bg-popover px-2.5 text-item-ui font-normal text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger"
            />
            {nameOk ? null : (
              <span
                role="alert"
                className="text-crumb font-normal text-danger-text"
              >
                {copy.nameRequired}
              </span>
            )}
          </label>
          <div className="flex flex-col gap-1.5">
            <span className="text-compact font-semibold">
              {copy.placeLabel}
            </span>
            <Select value={place} onValueChange={setPlace} disabled={busy}>
              <SelectTrigger aria-label={copy.placeLabel} className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {targets.root || spaceId === null ? (
                  <SelectItem value={ROOT}>{copy.placeRoot}</SelectItem>
                ) : null}
                {choices.map((node) => (
                  <SelectItem key={node.id} value={node.id}>
                    {node.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <span className="text-metadata text-meta-foreground">
              {copy.placeHint}
            </span>
          </div>
          <Row title={copy.alwaysTitle} body={copy.alwaysBody} />
          <div className="flex flex-col gap-1.5 rounded-[10px] border border-line px-3 py-2.5">
            <label className="flex items-start gap-2.5">
              <input
                type="checkbox"
                checked={withItems && !over}
                disabled={busy || over || !preview.isSuccess}
                onChange={(event) => setWithItems(event.target.checked)}
                className="mt-0.5 size-4 accent-primary"
              />
              <span className="flex-1">
                <b className="block text-compact font-semibold">
                  {copy.itemsTitle}
                  {preview.isSuccess ? (
                    <span className="ml-1.5 font-normal text-meta-foreground">
                      {pluralLifecycle(copy, "itemsCount", itemCount)}
                    </span>
                  ) : null}
                </b>
                <span className="block text-metadata text-meta-foreground">
                  {over
                    ? fill(copy.tooMany, { count: itemCount, max })
                    : preview.isError
                      ? copy.previewFailed
                      : copy.itemsHint}
                </span>
              </span>
            </label>
            {withItems && !over ? (
              <div className="flex flex-col gap-1 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
                <span className="text-metadata text-meta-foreground">
                  {fill(copy.itemsOn, { max })}
                </span>
                {dropped > 0 ? (
                  <span
                    role="status"
                    className="flex items-start gap-1.5 rounded-lg bg-warning-subtle px-2.5 py-1.5 text-metadata text-warning-text"
                  >
                    <TriangleAlert
                      aria-hidden="true"
                      className="mt-0.5 size-3.5 shrink-0"
                    />
                    {pluralLifecycle(copy, "dropped", dropped)}
                  </span>
                ) : null}
              </div>
            ) : null}
          </div>
          <Row
            title={copy.membersTitle}
            body={copy.membersBody}
            locked={copy.fixed}
          />
          <Row
            title={copy.automationsTitle}
            body={copy.automationsBody}
            locked={copy.fixed}
          />
          {busy && withItems ? (
            <div role="status" className="flex flex-col gap-1.5">
              <div className="h-1.5 overflow-hidden rounded-full bg-state-hover-strong">
                <div className="h-full w-1/3 rounded-full bg-primary motion-safe:animate-pulse" />
              </div>
              <span className="text-metadata text-meta-foreground">
                {fill(copy.workingItems, { count: itemCount })}
              </span>
            </div>
          ) : null}
          {failed ? (
            <p role="alert" className="m-0 text-compact text-danger-text">
              {failed}
            </p>
          ) : null}
          <p className="m-0 text-metadata text-meta-foreground">
            {copy.footer}
          </p>
          <DialogFooter>
            <button
              type="button"
              disabled={busy}
              onClick={onClose}
              className={cn(
                BUTTON,
                "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong",
              )}
            >
              {copy.cancel}
            </button>
            <button
              type="submit"
              disabled={busy || !nameOk}
              className={cn(
                BUTTON,
                "border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90",
              )}
            >
              {busy ? copy.working : copy.submit}
            </button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Row({
  title,
  body,
  locked,
}: {
  title: string;
  body: string;
  locked?: string;
}) {
  return (
    <div className="flex items-start gap-2.5 rounded-[10px] border border-line bg-background px-3 py-2.5">
      <span className="flex-1">
        <b className="block text-compact font-semibold">{title}</b>
        <span className="block text-metadata text-meta-foreground">{body}</span>
      </span>
      {locked ? (
        <span className="inline-flex shrink-0 items-center gap-1 text-metadata text-meta-foreground">
          <Lock aria-hidden="true" className="size-3.5" />
          {locked}
        </span>
      ) : null}
    </div>
  );
}
