import { useEffect, useId, useRef, type KeyboardEvent } from "react";
import { AlertCircle } from "lucide-react";
import { Button, Skeleton } from "@notrelix/ui-web";
import type {
  Board,
  CardDetail,
  CardDetailTab,
} from "@notrelix/work-management-core";
import {
  TaskDetailHeader,
  TaskDetailProperties,
  TaskDetailTitle,
} from "./task-detail-header";
import { TaskDetailTabs } from "./task-detail-tabs";
import { UpdateComposer } from "./update-composer";
import type { ItemSheetCopy } from "./item-sheet.copy";
import {
  canWriteComment,
  type ItemSheetGroup,
  type ItemSheetMark,
  type TaskDetailCallbacks,
  type TaskDetailCapabilities,
  type TaskDetailData,
} from "./task-detail-types";

export type TaskDetailPanelStatus = "loading" | "error" | "ready";

export interface TaskDetailPanelSurfaceProps {
  readonly status: TaskDetailPanelStatus;
  readonly board: Board;
  readonly card: CardDetail | null;
  readonly capabilities: TaskDetailCapabilities;
  readonly detailData?: TaskDetailData;
  readonly callbacks: TaskDetailCallbacks;
  readonly activeTab?: CardDetailTab;
  readonly copy: ItemSheetCopy;
  /** Injected clock for due and comment wording. */
  readonly now: Date;
  /** IANA time zone for calendar days; the runtime zone when omitted. */
  readonly timeZone?: string;
  /** Department (Space) mark before the board name in the crumb. */
  readonly departmentMark?: ItemSheetMark;
  /** Board groups offered by "Chuyển nhóm". */
  readonly groups?: readonly ItemSheetGroup[];
  /**
   * Called as the sheet unmounts. Without `preventDefault()` focus goes back
   * to the element that was focused when the sheet opened.
   */
  readonly onReturnFocus?: (event: Event) => void;
  /**
   * Whether the surface is the dialog itself (default). A host that already
   * wraps it in a dialog (the mobile Radix Sheet) passes false so dialogs do
   * not nest.
   */
  readonly asDialog?: boolean;
}

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return (
    target.isContentEditable ||
    target.tagName === "INPUT" ||
    target.tagName === "TEXTAREA" ||
    target.tagName === "SELECT"
  );
}

/**
 * Item side sheet: a non-modal dialog (the board behind stays usable).
 * Focus moves in on open and back on close, Escape closes, K/J step through
 * the items while focus is inside. Pure: data, copy, clock and callbacks are
 * injected.
 */
export function TaskDetailPanelSurface({
  status,
  board,
  card,
  capabilities,
  detailData,
  callbacks,
  activeTab,
  copy,
  now,
  timeZone,
  departmentMark,
  groups,
  onReturnFocus,
  asDialog = true,
}: TaskDetailPanelSurfaceProps) {
  const rootRef = useRef<HTMLElement>(null);
  const titleId = useId();
  const returnFocusRef = useRef(onReturnFocus);
  useEffect(() => {
    returnFocusRef.current = onReturnFocus;
  });

  useEffect(() => {
    const previous =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    rootRef.current?.focus({ preventScroll: true });
    return () => {
      const event = new Event("returnfocus", { cancelable: true });
      returnFocusRef.current?.(event);
      if (!event.defaultPrevented && previous?.isConnected) previous.focus();
    };
  }, []);

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    // Keys from portalled menus bubble through React but are not ours.
    if (!rootRef.current?.contains(event.target as Node)) return;
    if (event.key === "Escape") {
      event.stopPropagation();
      callbacks.onClose();
      return;
    }
    if (event.metaKey || event.ctrlKey || event.altKey) return;
    if (isTypingTarget(event.target)) return;
    const key = event.key.toLowerCase();
    const step =
      key === "k"
        ? callbacks.onPrevious
        : key === "j"
          ? callbacks.onNext
          : null;
    if (step) {
      event.preventDefault();
      step();
    }
  }

  const ready = status === "ready" && card && detailData;

  return (
    <section
      ref={rootRef}
      role={asDialog ? "dialog" : undefined}
      aria-modal={asDialog ? "false" : undefined}
      aria-labelledby={asDialog && ready ? titleId : undefined}
      aria-label={asDialog && !ready ? copy.dialogLabel : undefined}
      tabIndex={-1}
      onKeyDown={handleKeyDown}
      className="flex h-full min-h-0 w-full min-w-0 flex-col overflow-hidden bg-popover text-popover-foreground outline-none"
    >
      {status === "loading" ? <TaskDetailPanelSkeleton copy={copy} /> : null}
      {status === "error" ? (
        <TaskDetailPanelError copy={copy} onClose={callbacks.onClose} />
      ) : null}
      {ready ? (
        <>
          <TaskDetailHeader
            board={board}
            card={card}
            capabilities={capabilities}
            copy={copy}
            departmentMark={departmentMark}
            groups={groups}
            onClose={callbacks.onClose}
            onPrevious={callbacks.onPrevious}
            onNext={callbacks.onNext}
            onCopyLink={callbacks.onCopyLink}
            onOpenFullPage={callbacks.onOpenFullPage}
            onDuplicate={callbacks.onDuplicate}
            onArchive={callbacks.onArchive}
            onDelete={callbacks.onDelete}
            onMoveToBoard={callbacks.onMoveToBoard}
            onMoveToGroup={callbacks.onMoveToGroup}
            following={detailData.following}
            onToggleFollow={callbacks.onToggleWatch}
          />
          <div className="flex min-h-0 flex-1 flex-col gap-5.5 overflow-y-auto px-5.5 pt-4.5 pb-5.5">
            <div className="flex flex-col gap-5.5">
              <TaskDetailTitle
                key={card.id}
                card={card}
                titleId={titleId}
                editable={capabilities.canEditFields}
                copy={copy}
                onRenameTitle={callbacks.onRenameTitle}
              />
              <TaskDetailProperties
                board={board}
                card={card}
                capabilities={capabilities}
                copy={copy}
                now={now}
                timeZone={timeZone}
                onUpdateFieldValue={callbacks.onUpdateFieldValue}
                onUpdateItem={callbacks.onUpdateItem}
                boardLabels={detailData.boardLabels}
                onToggleLabel={callbacks.onToggleLabel}
                onCreateLabel={callbacks.onCreateLabel}
                onCreateField={callbacks.onCreateField}
              />
            </div>
            <TaskDetailTabs
              key={card.id}
              card={card}
              data={detailData}
              capabilities={capabilities}
              callbacks={callbacks}
              copy={copy}
              now={now}
              timeZone={timeZone}
              activeTab={activeTab}
            />
          </div>
          <UpdateComposer
            key={card.id}
            cardId={card.id}
            members={card.members.length > 0 ? card.members : card.watchers}
            people={detailData.people}
            viewerUserId={detailData.viewerUserId}
            disabled={!canWriteComment(capabilities)}
            copy={copy}
            onCreateUpdate={callbacks.onCreateUpdate}
          />
        </>
      ) : null}
    </section>
  );
}

export function TaskDetailPanelSkeleton({ copy }: { copy: ItemSheetCopy }) {
  return (
    <div
      className="flex h-full w-full flex-col gap-4 bg-popover p-4"
      aria-label={copy.loading}
      aria-busy="true"
      role="status"
    >
      <Skeleton className="h-8 rounded-lg" />
      <Skeleton className="h-16 rounded-lg" />
      <Skeleton className="h-8 rounded-lg" />
      <Skeleton className="h-40 rounded-lg" />
    </div>
  );
}

export function TaskDetailPanelError({
  copy,
  onClose,
}: {
  copy: ItemSheetCopy;
  onClose: () => void;
}) {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center bg-popover p-6 text-center">
      <AlertCircle
        aria-hidden="true"
        className="mb-3 size-8 text-danger-text"
      />
      <h2 className="text-sm font-semibold text-foreground">
        {copy.unavailable.title}
      </h2>
      <p className="mt-2 max-w-xs text-sm text-muted-foreground">
        {copy.unavailable.body}
      </p>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="mt-4"
        onClick={onClose}
      >
        {copy.unavailable.close}
      </Button>
    </div>
  );
}
