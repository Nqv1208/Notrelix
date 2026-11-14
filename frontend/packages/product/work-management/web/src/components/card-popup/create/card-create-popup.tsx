import { useMemo, useRef, useState, type ReactNode } from "react";
import { CloudOff, Loader2 } from "lucide-react";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogTitle,
  cn,
} from "@notrelix/ui-web";
import {
  useCreateItemWithDetails,
  useRetryItemDetails,
  type CreateItemDetail,
  type ItemDetailRemainder,
} from "@notrelix/work-management-state";
import type { Board, BoardGroup } from "@notrelix/work-management-core";
import type { TaskDetailCapabilities } from "../../card-detail/task-detail-types";
import { CardPopupSurface } from "../card-popup-surface";
import { fillPopupCopy } from "../card-popup.copy";
import { PanelBtn } from "../popup-ui";
import { useCardPopupModel } from "../use-card-popup-model";
import { useCardPopupCopy } from "../use-card-popup-copy";
import { announceCreated } from "./created-notice";
import {
  keepDraft,
  useCardDraft,
  type CardDraftDefaults,
} from "./use-card-draft";
import { findItemFields } from "../../card-detail/item-field-editors";
import { useLimitMessage } from "../../shared/use-limit-message";

const FULL: TaskDetailCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: false,
  canDelete: false,
  canEditFields: true,
  canComment: true,
};

/** `YYYY-MM-DD` as the instant that day starts in UTC (the backend keeps UTC days). */
const dayInstant = (day?: string) => (day ? `${day}T00:00:00.000Z` : null);

/** The item exists but some of its details did not save: what is still missing, and the version to chain from. */
interface PartialCreate {
  readonly itemId: string;
  readonly version: number;
  readonly failed: readonly CreateItemDetail[];
  readonly remaining: ItemDetailRemainder;
}

export interface CardCreatePopupProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly workspaceId: string;
  /** Undefined while a board chosen elsewhere loads: nothing opens yet. */
  readonly board: Board | undefined;
  /** The groups the dialog can name: only the id and title are read. */
  readonly groups: readonly Pick<BoardGroup, "id" | "title">[];
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
  /**
   * A sub-item: the new item goes under this parent, in the parent's group,
   * which this dialog does not change. The parent is shown above the title.
   */
  readonly parent?: {
    readonly itemId: string;
    readonly key?: string | null;
    readonly title: string;
    readonly groupId: string;
  };
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
  parent,
}: CardCreatePopupProps & { board: Board }) {
  const copy = useCardPopupCopy();
  const t = copy.create;
  const create = useCreateItemWithDetails(board.id, workspaceId);
  const retry = useRetryItemDetails(board.id, workspaceId);
  const [partial, setPartial] = useState<PartialCreate | null>(null);
  const [defaults] = useState<CardDraftDefaults>(() => ({
    title: defaultTitle,
    descriptionMd: defaultDescription,
    listId: parent?.groupId || defaultGroupId || groups[0]?.id || "",
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
    groupLocked: Boolean(parent),
  });
  const [another, setAnother] = useState(false);
  const [failed, setFailed] = useState(false);
  // The board is at its published item limit (D78.8): say so; the same create would be refused again.
  const [limited, setLimited] = useState<string | null>(null);
  const limitMessage = useLimitMessage();
  const [attempted, setAttempted] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const attempt = useRef<{ key: string; signature: string } | null>(null);
  const sending = useRef(false);
  const pending = create.isPending || retry.isPending;
  const blank = !partial && d.draft.title.trim() === "";
  const introText = parent
    ? fillPopupCopy(t.subitemOf, {
        key: parent.key ?? "",
        title: parent.title,
      })
    : intro;

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
        introText || failed || partial ? (
          <>
            {introText ? (
              <div className="mx-6 mt-3 text-[13.5px] leading-[1.45] text-muted-foreground">
                {introText}
              </div>
            ) : null}
            {partial ? (
              <PartialBanner
                failed={partial.failed}
                onRetry={retryMissing}
                onOpen={() => openCreated(partial.itemId)}
              />
            ) : null}
            {failed ? (
              <FailedBanner
                limit={limited}
                onRetry={() => submit()}
                onClose={() => {
                  keepDraft(board.id, d.draft);
                  onOpenChange(false);
                }}
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
      p.dependencies > 0 ||
      p.fields;
    if (dirty) setConfirming(true);
    else onOpenChange(false);
  };

  /** The success path: the toast, then the next card, the new card, or closing. */
  function finishCreated(itemId: string | undefined) {
    toast.success(successMessage ?? t.created);
    if (another && !hideAnother) {
      d.resetKeeping();
      setAttempted(false);
      return;
    }
    if (itemId && onOpenItem) openCreated(itemId);
    else onOpenChange(false);
  }

  /** Opens the new card: the host shows it, and this dialog closes. */
  function openCreated(itemId: string) {
    if (onOpenItem) {
      announceCreated({
        itemId,
        // The lane the header names: the Status option, or the group of a board not grouped by Status.
        groupTitle:
          findItemFields(board).status?.options.find(
            (option) => option.id === d.draft.statusOptionId,
          )?.label ??
          groups.find((group) => group.id === d.draft.listId)?.title ??
          "",
      });
      onOpenItem(itemId);
    }
    onOpenChange(false);
  }

  /** Writes only the missing part of the item the create made: nothing is created again. */
  function retryMissing() {
    const current = partial;
    if (!current || retry.isPending) return;
    retry.mutate(
      {
        itemId: current.itemId,
        version: current.version,
        remaining: current.remaining,
      },
      {
        onSuccess: (outcome) => {
          if (outcome.failed.length > 0) {
            setPartial({
              ...current,
              version: outcome.version,
              failed: outcome.failed,
              remaining: outcome.remaining,
            });
            return;
          }
          setPartial(null);
          finishCreated(current.itemId);
        },
      },
    );
  }

  function submit() {
    if (partial) {
      retryMissing();
      return;
    }
    if (pending || sending.current) return;
    if (blank) {
      setAttempted(true);
      return;
    }
    setFailed(false);
    setLimited(null);
    // A retry of the same draft reuses its key; an edited draft is a new request.
    const signature = JSON.stringify({
      ...d.toInput(""),
      files: d.draft.files.map((f) => f.id),
      parent: parent?.itemId ?? null,
    });
    if (attempt.current?.signature !== signature)
      attempt.current = { key: crypto.randomUUID(), signature };
    sending.current = true;
    create.mutate(
      { ...d.toInput(attempt.current.key), parentItemId: parent?.itemId },
      {
        onSettled: () => {
          sending.current = false;
        },
        onSuccess: (result) => {
          attempt.current = null;
          void onCreated?.({ boardId: board.id, itemId: result.itemId });
          if (result.failed.length > 0 && result.itemId) {
            // The item exists: the dialog stays, names what is missing, and a retry writes only that.
            setPartial({
              itemId: result.itemId,
              version: result.version,
              failed: result.failed,
              remaining: result.remaining,
            });
            return;
          }
          finishCreated(result.itemId);
        },
        onError: (error) => {
          setLimited(limitMessage(error));
          setFailed(true);
        },
      },
    );
  }

  const footer = (
    <div className="relative z-[5] flex flex-none items-center gap-2.5 border-t border-line bg-popover px-6 py-3 max-md:flex-col max-md:items-stretch max-md:gap-2 max-md:px-4">
      {pending ? (
        <span className="inline-flex items-center gap-1.5 text-[12.5px] leading-[1.45] text-meta-foreground">
          <Loader2 aria-hidden="true" className="size-[13px] animate-spin" />
          <span>
            {t.submittingNote}
            <span className="max-md:hidden"> {t.submittingNoteMore}</span>
          </span>
        </span>
      ) : (
        <>
          {hideAnother || partial ? null : (
            <label className="inline-flex min-h-9 items-center gap-[9px] text-[13.5px] max-md:min-h-11">
              <input
                type="checkbox"
                aria-label={t.another}
                checked={another}
                onChange={(event) => setAnother(event.target.checked)}
                className="m-0 size-[18px] shrink-0 accent-primary"
              />
              {t.another}
              <span className="text-[12.5px] text-meta-foreground max-md:hidden">
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
      <span className="flex-1 max-md:hidden" />
      <div className="flex gap-2.5 max-md:gap-2">
        <PanelBtn
          className="max-md:h-11"
          disabled={pending}
          onClick={requestClose}
        >
          {t.cancel}
        </PanelBtn>
        <PanelBtn
          variant="primary"
          aria-disabled={pending || blank || undefined}
          aria-busy={pending || undefined}
          className={cn(
            "max-md:h-11 max-md:flex-1",
            (pending || blank) && "opacity-55",
          )}
          onClick={submit}
        >
          {pending ? (
            <>
              <Loader2 aria-hidden="true" className="size-4 animate-spin" />
              {t.submitting}
            </>
          ) : partial ? (
            t.retryMissing
          ) : (
            (submitLabel ?? t.submit)
          )}
        </PanelBtn>
      </div>
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

/** The item was created but a detail did not save: names the missing parts; a retry writes only those. */
function PartialBanner({
  failed,
  onRetry,
  onOpen,
}: {
  failed: readonly CreateItemDetail[];
  onRetry: () => void;
  onOpen: () => void;
}) {
  const t = useCardPopupCopy().create;
  return (
    <div
      role="alert"
      className="mx-6 mt-3 box-border flex items-start gap-3 rounded-[10px] bg-danger-subtle px-4 py-3 text-[13.5px] leading-[1.45] ring-1 ring-danger ring-inset"
    >
      <CloudOff
        aria-hidden="true"
        className="mt-px size-5 shrink-0 text-danger-text"
      />
      <div className="flex-1">
        <b className="font-[650]">{t.partialTitle}</b>
        <br />
        {fillPopupCopy(t.partialBody, {
          details: failed.map((detail) => t.detailNames[detail]).join(", "),
        })}
        <br />
        {t.partialKept}
        <div className="mt-2 flex flex-wrap gap-2">
          <PanelBtn small variant="primary" onClick={onRetry}>
            {t.retryMissing}
          </PanelBtn>
          <PanelBtn small onClick={onOpen}>
            {t.openItem}
          </PanelBtn>
        </div>
      </div>
    </div>
  );
}

function FailedBanner({
  limit,
  onRetry,
  onClose,
}: {
  /** The limit the create would go over, told instead of the generic body, without Retry. */
  limit?: string | null;
  onRetry: () => void;
  onClose: () => void;
}) {
  const t = useCardPopupCopy().create;
  return (
    <div
      role="alert"
      className="mx-6 mt-3 box-border flex items-start gap-3 rounded-[10px] bg-danger-subtle px-4 py-3 text-[13.5px] leading-[1.45] ring-1 ring-danger ring-inset"
    >
      <CloudOff
        aria-hidden="true"
        className="mt-px size-5 shrink-0 text-danger-text"
      />
      <div className="flex-1">
        <b className="font-[650]">{t.failedTitle}</b>
        <br />
        {limit ?? t.failedBody}
        <div className="mt-2 flex flex-wrap gap-2">
          {limit ? null : (
            <PanelBtn small variant="primary" onClick={onRetry}>
              {t.retry}
            </PanelBtn>
          )}
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
          <PanelBtn
            autoFocus
            className="leading-[1.45]"
            onClick={() => onOpenChange(false)}
          >
            {t.discardKeep}
          </PanelBtn>
          <PanelBtn
            variant="primary"
            className="border-danger bg-danger leading-[1.45] text-danger-foreground"
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
