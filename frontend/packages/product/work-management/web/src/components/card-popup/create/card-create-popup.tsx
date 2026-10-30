import { useMemo, useRef, useState, type ReactNode } from "react";
import { AlertCircle, Loader2 } from "lucide-react";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogTitle,
} from "@notrelix/ui-web";
import {
  useCreateItemWithDetails,
  type CreateItemDetail,
} from "@notrelix/work-management-state";
import type { Board, BoardGroup } from "@notrelix/work-management-core";
import type { TaskDetailCapabilities } from "../../card-detail/task-detail-types";
import { CardPopupSurface } from "../card-popup-surface";
import { fillPopupCopy } from "../card-popup.copy";
import { PanelBtn } from "../popup-ui";
import { useCardPopupModel } from "../use-card-popup-model";
import { useCardPopupCopy } from "../use-card-popup-copy";
import { useCardDraft, type CardDraftDefaults } from "./use-card-draft";
import { findItemFields } from "../../card-detail/item-field-editors";

const FULL: TaskDetailCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: false,
  canDelete: false,
  canEditFields: true,
  canComment: false,
};

/** `YYYY-MM-DD` as the instant that day starts in UTC (the backend keeps UTC days). */
const dayInstant = (day?: string) => (day ? `${day}T00:00:00.000Z` : null);

export interface CardCreatePopupProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly workspaceId: string;
  /** Undefined while a board chosen elsewhere loads: nothing opens yet. */
  readonly board: Board | undefined;
  readonly groups: readonly BoardGroup[];
  readonly defaultGroupId: string;
  readonly defaultStatusId: string | null;
  /** `YYYY-MM-DD` prefilled in the dates (Calendar and Timeline). */
  readonly defaultStartDay?: string;
  readonly defaultDueDay?: string;
  readonly defaultTitle?: string;
  /** A form response prefills these. */
  readonly defaultDescription?: string;
  readonly defaultPriorityId?: string | null;
  readonly defaultAssigneeIds?: readonly string[];
  /** The signed-in member: the people picker lists them first. */
  readonly currentUserId?: string | null;
  /** A notice above the title (where a form response came from). */
  readonly intro?: ReactNode;
  readonly submitLabel?: string;
  /** The success toast's words, when the card is not just "created". */
  readonly successMessage?: string;
  readonly hideAnother?: boolean;
  /** Lane of a board not grouped by Status: written as a follow-up value. */
  readonly laneValue?: {
    readonly fieldDefinitionId: string;
    readonly value: string;
  } | null;
  /** After the card exists (also when a detail could not be saved). */
  readonly onCreated?: (result: {
    readonly boardId: string;
    readonly itemId: string | undefined;
  }) => void | Promise<void>;
  /** The popup stays on the new card: the host opens it. */
  readonly onOpenItem?: (itemId: string) => void;
}

/**
 * The card popup in create mode: the same layout as an open card over a draft
 * that lives in local state. One locked submit; the request carries one key
 * per attempt, so a retry never makes a second card.
 */
export function CardCreatePopup(props: CardCreatePopupProps) {
  if (!props.open || !props.board) return null;
  return <CreateBody {...props} board={props.board} />;
}

function CreateBody({
  onOpenChange,
  workspaceId,
  board,
  groups,
  defaultGroupId,
  defaultStatusId,
  defaultStartDay,
  defaultDueDay,
  defaultTitle,
  defaultDescription,
  defaultPriorityId,
  defaultAssigneeIds,
  currentUserId,
  intro,
  submitLabel,
  successMessage,
  hideAnother,
  laneValue,
  onCreated,
  onOpenItem,
}: CardCreatePopupProps & { board: Board }) {
  const copy = useCardPopupCopy();
  const t = copy.create;
  const create = useCreateItemWithDetails(board.id, workspaceId);
  const [defaults] = useState<CardDraftDefaults>(() => ({
    title: defaultTitle,
    descriptionMd: defaultDescription,
    listId: defaultGroupId || groups[0]?.id || "",
    statusOptionId:
      defaultStatusId ?? findItemFields(board).status?.options[0]?.id ?? null,
    priorityOptionId: defaultPriorityId ?? null,
    startDate: dayInstant(defaultStartDay),
    dueDate: dayInstant(defaultDueDay),
    assigneeIds: defaultAssigneeIds ?? [],
  }));
  const d = useCardDraft({
    board,
    workspaceId,
    defaults,
    laneValue,
    viewerUserId: currentUserId,
  });
  const [another, setAnother] = useState(false);
  const [failed, setFailed] = useState(false);
  const [attempted, setAttempted] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const attempt = useRef<{ key: string; signature: string } | null>(null);
  const pending = create.isPending;
  const blank = d.draft.title.trim() === "";

  const groupsForSheet = useMemo(
    () => groups.map((group) => ({ id: group.id, title: group.title })),
    [groups],
  );
  const model = useCardPopupModel({
    mode: "create",
    board,
    card: d.card,
    capabilities: FULL,
    data: d.data,
    callbacks: d.callbacks,
    actions: d.actions,
    groups: groupsForSheet,
    now: new Date(),
    readOnly: pending ? "creating" : undefined,
    slots: {
      coverBanner:
        intro || failed ? (
          <>
            {intro ? (
              <div className="mx-6 mt-3 text-[13.5px] leading-[1.45] text-muted-foreground">
                {intro}
              </div>
            ) : null}
            {failed ? (
              <FailedBanner
                onRetry={() => submit()}
                onClose={() => onOpenChange(false)}
              />
            ) : null}
          </>
        ) : undefined,
    },
  });

  const requestClose = () => {
    if (pending) return;
    const p = d.parts;
    const dirty =
      p.name ||
      p.description ||
      p.labels > 0 ||
      p.assignees ||
      p.dates ||
      p.checklists ||
      p.attachments > 0 ||
      p.fields;
    if (dirty) setConfirming(true);
    else onOpenChange(false);
  };

  function submit() {
    if (pending) return;
    if (blank) {
      setAttempted(true);
      return;
    }
    setFailed(false);
    // A retry of the same draft reuses its key; an edited draft is a new request.
    const signature = JSON.stringify({
      ...d.toInput(""),
      files: d.draft.files.map((f) => f.id),
    });
    if (attempt.current?.signature !== signature)
      attempt.current = { key: crypto.randomUUID(), signature };
    create.mutate(d.toInput(attempt.current.key), {
      onSuccess: ({ failed: missing, itemId }) => {
        attempt.current = null;
        void onCreated?.({ boardId: board.id, itemId });
        if (missing.length > 0)
          toast.warning(
            fillPopupCopy(t.createdWithout, {
              details: missing
                .map((detail: CreateItemDetail) => t.detailNames[detail])
                .join(", "),
            }),
          );
        else toast.success(successMessage ?? t.created);
        if (another && !hideAnother) {
          d.resetKeeping();
          setAttempted(false);
          return;
        }
        if (itemId) onOpenItem?.(itemId);
        onOpenChange(false);
      },
      onError: () => setFailed(true),
    });
  }

  const footer = (
    <div className="relative z-[5] flex flex-none items-center gap-2.5 border-t border-line bg-popover px-6 py-3">
      {pending ? (
        <span className="inline-flex items-center gap-1.5 text-[12.5px] text-meta-foreground">
          <Loader2 aria-hidden="true" className="size-3.5 animate-spin" />
          {t.submittingNote}
        </span>
      ) : (
        <>
          {hideAnother ? null : (
            <label className="inline-flex min-h-9 items-center gap-[9px] text-[13.5px]">
              <input
                type="checkbox"
                checked={another}
                onChange={(event) => setAnother(event.target.checked)}
                className="m-0 size-[18px] shrink-0 accent-primary"
              />
              {t.another}
              <span className="text-[12.5px] text-meta-foreground">
                {t.anotherKept}
              </span>
            </label>
          )}
          {blank && attempted ? (
            <span
              role="alert"
              className="text-[12.5px] font-medium text-danger-text"
            >
              {t.nameRequired}
            </span>
          ) : null}
        </>
      )}
      <span className="flex-1" />
      <PanelBtn disabled={pending} onClick={requestClose}>
        {t.cancel}
      </PanelBtn>
      <PanelBtn
        variant="primary"
        aria-disabled={pending || blank || undefined}
        aria-busy={pending || undefined}
        className={pending || blank ? "opacity-55" : undefined}
        onClick={submit}
      >
        {pending ? (
          <>
            <Loader2 aria-hidden="true" className="size-4 animate-spin" />
            {t.submitting}
          </>
        ) : (
          (submitLabel ?? t.submit)
        )}
      </PanelBtn>
    </div>
  );

  return (
    <CardPopupSurface
      status="ready"
      model={model}
      onClose={requestClose}
      footer={footer}
      overlays={
        <DiscardConfirm
          open={confirming}
          onOpenChange={setConfirming}
          parts={d.parts}
          onDiscard={() => onOpenChange(false)}
        />
      }
    />
  );
}

function FailedBanner({
  onRetry,
  onClose,
}: {
  onRetry: () => void;
  onClose: () => void;
}) {
  const t = useCardPopupCopy().create;
  return (
    <div
      role="alert"
      className="mx-6 mt-3 box-border flex items-start gap-3 rounded-[10px] bg-danger-subtle px-4 py-3 text-[13.5px] leading-[1.45] ring-1 ring-danger ring-inset"
    >
      <AlertCircle
        aria-hidden="true"
        className="mt-px size-4 shrink-0 text-danger-text"
      />
      <div className="flex-1">
        <b className="font-[650]">{t.failedTitle}</b>
        <br />
        {t.failedBody}
        <div className="mt-2 flex flex-wrap gap-2">
          <PanelBtn small variant="primary" onClick={onRetry}>
            {t.retry}
          </PanelBtn>
          <PanelBtn small onClick={onClose}>
            {t.keepDraft}
          </PanelBtn>
        </div>
      </div>
    </div>
  );
}

/** "Bỏ thẻ đang soạn?": says what is lost; the default button keeps writing. */
function DiscardConfirm({
  open,
  onOpenChange,
  parts,
  onDiscard,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  parts: ReturnType<typeof useCardDraft>["parts"];
  onDiscard: () => void;
}) {
  const t = useCardPopupCopy().create;
  const list: string[] = [];
  if (parts.name) list.push(t.parts.name);
  if (parts.description) list.push(t.parts.description);
  if (parts.labels > 0)
    list.push(
      fillPopupCopy(
        parts.labels === 1 ? t.parts.labels_one : t.parts.labels_other,
        { count: parts.labels },
      ),
    );
  if (parts.assignees) list.push(t.parts.assignees);
  if (parts.dates) list.push(t.parts.dates);
  if (parts.fields) list.push(t.parts.fields);
  if (parts.checklists) list.push(t.parts.checklists);
  if (parts.attachments > 0)
    list.push(
      fillPopupCopy(
        parts.attachments === 1
          ? t.parts.attachments_one
          : t.parts.attachments_other,
        { count: parts.attachments },
      ),
    );
  const last = list.pop();
  const joined = list.length
    ? `${list.join(", ")} ${t.parts.and} ${last}`
    : (last ?? "");
  const body = fillPopupCopy(t.discardBody, {
    parts: joined.charAt(0).toUpperCase() + joined.slice(1),
  });
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="box-border w-[440px] max-w-[calc(100%-2rem)] gap-0 rounded-[14px] bg-popover p-5 pb-4 shadow-popover ring-0 sm:max-w-[440px]">
        <AlertDialogTitle className="m-0 mb-2 text-[16px] font-[650]">
          {t.discardTitle}
        </AlertDialogTitle>
        <AlertDialogDescription className="m-0 text-[13.5px] leading-[1.5] text-muted-foreground">
          {body}
        </AlertDialogDescription>
        <div className="mt-4 flex justify-end gap-2">
          <PanelBtn autoFocus onClick={() => onOpenChange(false)}>
            {t.discardKeep}
          </PanelBtn>
          <PanelBtn
            variant="primary"
            className="border-danger bg-danger text-danger-foreground"
            onClick={() => {
              onOpenChange(false);
              onDiscard();
            }}
          >
            {t.discardConfirm}
          </PanelBtn>
        </div>
      </AlertDialogContent>
    </AlertDialog>
  );
}
