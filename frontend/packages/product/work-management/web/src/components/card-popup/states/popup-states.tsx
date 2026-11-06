import { useEffect, useRef, useState } from "react";
import {
  AlertCircle,
  Check,
  Info,
  RefreshCw,
  TriangleAlert,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  MOVE_SAVE_KEY,
  useCardPopup,
  type PopupStaleSchema,
} from "../card-popup-context";
import { fillPopupCopy } from "../card-popup.copy";
import { PanelBtn } from "../popup-ui";
import { useCreatedNotice } from "../create/created-notice";
import { useCardPopupCopy } from "../use-card-popup-copy";

/** `.sk`: a grey block in the shape of the part it stands for; it does not move. */
function Sk({
  w,
  h,
  r = 6,
  className,
}: {
  w: number | string;
  h: number;
  r?: number;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn("block shrink-0 bg-state-hover-strong", className)}
      style={{ width: w, height: h, borderRadius: r }}
    />
  );
}

/** The card's shape while it loads: the frame is there at once, the content grey. */
export function PopupSkeleton({ label }: { label?: string }) {
  const t = useCardPopupCopy().shell;
  return (
    <div
      role="status"
      aria-busy="true"
      className="flex min-h-0 flex-1 flex-col md:flex-row md:overflow-hidden"
    >
      <span className="sr-only">{label ?? t.loading}</span>
      <div className="box-border min-w-0 md:w-[720px] md:flex-none">
        <div className="box-border flex h-[60px] items-start gap-3 pt-4 pr-6 pl-6">
          <Sk w={24} h={24} r={12} className="mt-2.5" />
          <Sk w={360} h={28} r={8} className="mt-2 max-w-full" />
        </div>
        <div className="box-border flex h-11 gap-2 pt-2 pr-6 pl-[60px]">
          {[92, 92, 92, 116, 124].map((w, i) => (
            <Sk key={i} w={w} h={36} r={8} />
          ))}
        </div>
        <div className="box-border flex flex-col gap-3 pt-5 pr-6 pl-[60px]">
          <div className="flex flex-wrap gap-3">
            <Sk w={110} h={54} r={8} />
            <Sk w={190} h={54} r={8} />
            <Sk w={220} h={54} r={8} />
          </div>
          <div className="flex flex-wrap gap-3">
            <Sk w={120} h={54} r={8} />
            <Sk w={120} h={54} r={8} />
            <Sk w={150} h={54} r={8} />
          </div>
        </div>
        <div className="box-border pt-7 pr-6 pl-[60px]">
          <Sk w={140} h={22} />
          <div className="h-3.5" />
          <Sk w="100%" h={14} />
          <div className="h-2" />
          <Sk w="92%" h={14} />
          <div className="h-2" />
          <Sk w="64%" h={14} />
        </div>
        <div className="box-border pt-7 pr-6 pl-[60px]">
          <Sk w={180} h={22} />
          <div className="h-3.5" />
          <Sk w="100%" h={70} r={8} />
        </div>
      </div>
      <div className="box-border flex min-w-0 flex-col gap-3.5 border-l border-line bg-background px-5 pt-4 pb-6 md:w-[400px] md:flex-none">
        <Sk w={230} h={24} />
        <Sk w="100%" h={44} r={10} />
        <div className="flex gap-2.5">
          <Sk w={32} h={32} r={16} />
          <div className="flex flex-1 flex-col gap-2">
            <Sk w={150} h={14} />
            <Sk w="100%" h={60} r={10} />
          </div>
        </div>
        <div className="flex gap-2.5">
          <Sk w={32} h={32} r={16} />
          <div className="flex flex-1 flex-col gap-2">
            <Sk w={190} h={14} />
            <Sk w="80%" h={14} />
          </div>
        </div>
      </div>
    </div>
  );
}

function EmptyState({
  title,
  body,
  children,
  note,
}: {
  title: string;
  body: string;
  children: React.ReactNode;
  note?: string;
}) {
  return (
    <div className="grid min-h-0 flex-1 place-items-center p-6">
      <div className="flex max-w-[460px] flex-col items-center gap-3 text-center text-[13.5px] text-muted-foreground">
        <span className="grid size-11 place-items-center rounded-full bg-danger-subtle text-danger-text">
          <AlertCircle aria-hidden="true" className="size-6" />
        </span>
        <b className="text-[16px] font-semibold text-foreground">{title}</b>
        <span>{body}</span>
        <div className="mt-1 flex gap-2">{children}</div>
        {note ? (
          <span className="font-mono text-[12px] text-meta-foreground">
            {note}
          </span>
        ) : null}
      </div>
    </div>
  );
}

/** A read that failed: the card is untouched; retry, close and a code to quote. */
export function PopupLoadError({
  onRetry,
  onClose,
  requestId,
}: {
  onRetry: () => void;
  onClose: () => void;
  /** The request code of the failed read, when the server gave one. */
  requestId?: string;
}) {
  const t = useCardPopupCopy().shell.loadError;
  return (
    <EmptyState
      title={t.title}
      body={t.body}
      note={requestId ? fillPopupCopy(t.ref, { ref: requestId }) : undefined}
    >
      <PanelBtn variant="primary" onClick={onRetry}>
        <RefreshCw aria-hidden="true" className="size-3" />
        {t.retry}
      </PanelBtn>
      <PanelBtn onClick={onClose}>{t.close}</PanelBtn>
    </EmptyState>
  );
}

/** Deleted, moved or no access read the same: one message, nothing about the card. */
export function PopupGone({
  onClose,
  draft,
}: {
  onClose: () => void;
  /** The comment the person had typed and not sent: their own text, so it stays to copy. */
  draft?: string;
}) {
  const t = useCardPopupCopy().shell.gone;
  return (
    <EmptyState title={t.title} body={t.body}>
      {draft ? <KeptDraft text={draft} /> : null}
      <PanelBtn variant="primary" onClick={onClose}>
        {t.close}
      </PanelBtn>
    </EmptyState>
  );
}

function KeptDraft({ text }: { text: string }) {
  const t = useCardPopupCopy().shell.keptDraft;
  return (
    <div className="flex w-full flex-col gap-1.5 text-left">
      <span className="text-[12.5px] font-semibold text-foreground">
        {t.title}
      </span>
      <textarea
        readOnly
        value={text}
        aria-label={t.title}
        className="box-border max-h-32 min-h-16 w-full resize-none rounded-[10px] border border-line-strong bg-popover p-2.5 text-[13.5px] text-foreground"
      />
      <PanelBtn onClick={() => void navigator.clipboard?.writeText(text)}>
        {t.copy}
      </PanelBtn>
    </div>
  );
}

/** The card was deleted by someone while it was open: names who, to a person who could read its board; a person who may restore it gets Restore. */
export function PopupDeleted({
  by,
  onClose,
  draft,
  onRestore,
}: {
  by: string | null;
  onClose: () => void;
  draft?: string;
  /** Rejects with the refusal reason; absent: the person may not restore (only Back to the board shows). */
  onRestore?: () => Promise<void>;
}) {
  const t = useCardPopupCopy().shell.deleted;
  const [phase, setPhase] = useState<"idle" | "pending" | "failed">("idle");
  const [refusal, setRefusal] = useState<string | null>(null);
  const restore = () => {
    if (!onRestore) return;
    setPhase("pending");
    setRefusal(null);
    onRestore().then(
      () => setPhase("idle"),
      (reason: unknown) => {
        setPhase("failed");
        setRefusal(
          (typeof reason === "string"
            ? (t as Record<string, string>)[reason]
            : undefined) ?? t.restoreFailed,
        );
      },
    );
  };
  return (
    <EmptyState
      title={fillPopupCopy(t.title, { who: by ?? t.someone })}
      body={t.body}
    >
      {draft ? <KeptDraft text={draft} /> : null}
      {refusal ? (
        <p role="alert" className="m-0 text-[13px] text-danger-text">
          {refusal}
        </p>
      ) : null}
      {onRestore ? (
        <PanelBtn
          variant="primary"
          disabled={phase === "pending"}
          onClick={restore}
        >
          {phase === "pending" ? t.restoring : t.restore}
        </PanelBtn>
      ) : null}
      <PanelBtn variant={onRestore ? undefined : "primary"} onClick={onClose}>
        {t.close}
      </PanelBtn>
    </EmptyState>
  );
}

/** The card was moved to another board the person can read: a link to the new place. */
export function PopupMoved({
  boardTitle,
  onOpen,
  onClose,
  draft,
}: {
  boardTitle: string;
  onOpen?: () => void;
  onClose: () => void;
  draft?: string;
}) {
  const t = useCardPopupCopy().shell.moved;
  return (
    <EmptyState
      title={fillPopupCopy(t.title, { board: boardTitle })}
      body={t.body}
    >
      {draft ? <KeptDraft text={draft} /> : null}
      {onOpen ? (
        <PanelBtn variant="primary" onClick={onOpen}>
          {t.open}
        </PanelBtn>
      ) : null}
      <PanelBtn variant={onOpen ? undefined : "primary"} onClick={onClose}>
        {t.close}
      </PanelBtn>
    </EmptyState>
  );
}

function Banner({
  tone,
  title,
  body,
  action,
  schema,
  children,
}: {
  tone: "danger" | "warn" | "info" | "success";
  /** A schema-change notice (D12): the artboard's 10 x 14 px padding, 13 px text on a 1.4 line, a 44 px button. */
  schema?: boolean;
  title: string;
  body: string;
  /** The one button of the notice, at its right edge. */
  action?: React.ReactNode;
  children?: React.ReactNode;
}) {
  const Icon =
    tone === "info" ? Info : tone === "success" ? Check : TriangleAlert;
  return (
    <div
      role={tone === "info" || tone === "success" ? "status" : "alert"}
      className={cn(
        "mx-4 mt-3 box-border flex md:mx-6 items-start rounded-[10px] text-foreground",
        schema
          ? "gap-2.5 px-3.5 py-2.5 text-[13px] leading-[1.4]"
          : "gap-3 px-4 py-3 text-[13.5px] leading-[1.45]",
        tone === "danger" && "bg-danger-subtle",
        tone === "warn" &&
          (schema
            ? "bg-warning-subtle"
            : "bg-warning-subtle shadow-[inset_0_0_0_1px_var(--warning-text)]"),
        tone === "info" && "bg-brand-subtle",
        tone === "success" && "bg-success-subtle",
      )}
    >
      <Icon
        aria-hidden="true"
        className={cn(
          schema ? "size-4 shrink-0" : "mt-px size-5 shrink-0",
          tone === "danger" && "text-danger-text",
          tone === "success" && "text-success-text",
          schema && tone === "warn" && "text-warning-text",
        )}
      />
      <div className="min-w-0 flex-1">
        <b className="font-[650]">{title}</b>
        <br />
        <span
          className={cn(
            tone === "danger" && "text-danger-text",
            schema && tone === "warn" && "text-warning-text",
          )}
        >
          {body}
        </span>
        {children}
      </div>
      {action ? <div className="shrink-0">{action}</div> : null}
    </div>
  );
}

const SCHEMA_BTN =
  "h-11 rounded-[7px] px-3.5 text-[13px] font-medium md:h-11 md:text-[13px]";

type Phase = "idle" | "pending" | "done" | "failed";

/** The notices at the top of the left column: someone else's save, or why nothing is editable. */
export function PopupBanners({
  conflict,
  conflictBy,
  staleSchema,
  onReload,
}: {
  conflict?: boolean;
  conflictBy?: string | null;
  staleSchema?: PopupStaleSchema | null;
  onReload?: () => void | Promise<unknown>;
}) {
  const { card, copy, readOnly, fieldConflicts, saves } = useCardPopup();
  // The reload of the fields: the notice becomes "reloading", then says what changed, or that it failed.
  const [phase, setPhase] = useState<Phase>("idle");
  const kept = useRef<PopupStaleSchema | null>(null);
  if (staleSchema) kept.current = staleSchema;
  const reload = async () => {
    setPhase("pending");
    try {
      await onReload?.();
      setPhase(kept.current ? "done" : "idle");
    } catch {
      setPhase("failed");
    }
  };
  useEffect(() => {
    if (phase !== "done") return;
    const id = window.setTimeout(() => setPhase("idle"), 8000);
    return () => window.clearTimeout(id);
  }, [phase]);
  const move = saves[MOVE_SAVE_KEY];
  const created = useCreatedNotice(card.id);
  const t = copy.shell;
  const typeName = (type: string | undefined) =>
    type ? (copy.sections.fields.typeNames[type] ?? type) : "";
  return (
    <>
      {created ? (
        <Banner
          tone="success"
          title={fillPopupCopy(copy.create.createdIn, {
            key: card.itemKey ?? "",
            group: created.groupTitle,
          })}
          body={copy.create.createdStay}
        />
      ) : null}
      {/* A custom field's clash is drawn at its own pill; the banner keeps the title's. */}
      {fieldConflicts
        .filter((c) => !c.key.startsWith("field:"))
        .map((c) => (
          <Banner
            key={c.key}
            tone="warn"
            title={fillPopupCopy(t.fieldConflict.title, {
              field: c.field,
              value: c.theirs,
            })}
            body={fillPopupCopy(t.fieldConflict.body, { mine: c.mine })}
          >
            <div className="mt-2 flex flex-wrap gap-2">
              <PanelBtn small onClick={c.takeTheirs}>
                {t.fieldConflict.takeTheirs}
              </PanelBtn>
              <PanelBtn small variant="primary" onClick={c.keepMine}>
                {t.fieldConflict.keepMine}
              </PanelBtn>
            </div>
          </Banner>
        ))}
      {move?.status === "error" ? (
        <Banner
          tone="warn"
          title={t.moveFailed.title}
          body={
            move.reason === "forbidden"
              ? t.moveFailed.forbidden
              : move.reason === "network"
                ? t.moveFailed.offline
                : t.moveFailed.body
          }
        >
          {move.reason === "forbidden" ? null : (
            <div className="mt-2 flex flex-wrap gap-2">
              <PanelBtn small onClick={move.retry}>
                {t.moveFailed.retry}
              </PanelBtn>
            </div>
          )}
        </Banner>
      ) : null}
      {phase === "pending" ? (
        <Banner
          schema
          tone="info"
          title={t.staleSchema.pending}
          body={t.staleSchema.pendingBody}
        />
      ) : phase === "failed" ? (
        <Banner
          schema
          tone="danger"
          title={t.staleSchema.failed}
          body={t.staleSchema.failedBody}
          action={
            <PanelBtn className={SCHEMA_BTN} onClick={() => void reload()}>
              <RefreshCw aria-hidden="true" className="size-3" />
              {t.staleSchema.retry}
            </PanelBtn>
          }
        />
      ) : phase === "done" && kept.current && !staleSchema ? (
        <Banner
          schema
          tone="info"
          title={fillPopupCopy(
            kept.current.kind === "option-removed"
              ? t.staleSchema.doneOption
              : kept.current.kind === "type-changed"
                ? t.staleSchema.doneType
                : t.staleSchema.doneDeleted,
            { field: kept.current.fieldName },
          )}
          body={
            kept.current.kind === "option-removed"
              ? t.staleSchema.doneOptionBody
              : kept.current.kind === "type-changed"
                ? t.staleSchema.doneTypeBody
                : t.staleSchema.doneDeletedBody
          }
        />
      ) : staleSchema ? (
        <Banner
          schema
          tone={
            staleSchema.kind === "field-deleted" || !staleSchema.kind
              ? "danger"
              : "warn"
          }
          title={fillPopupCopy(
            staleSchema.kind === "option-removed"
              ? t.staleSchema.optionRemoved
              : staleSchema.kind === "type-changed"
                ? t.staleSchema.typeChanged
                : t.staleSchema.deleted,
            {
              who: staleSchema.byName ?? t.staleSchema.someone,
              field: staleSchema.fieldName,
              from: typeName(staleSchema.fromType),
              to: typeName(staleSchema.toType),
            },
          )}
          body={
            staleSchema.kind === "option-removed"
              ? fillPopupCopy(t.staleSchema.optionRemovedBody, {
                  option: staleSchema.optionLabel ?? "",
                })
              : staleSchema.kind === "type-changed"
                ? t.staleSchema.typeChangedBody
                : t.staleSchema.body
          }
          action={
            onReload ? (
              <PanelBtn className={SCHEMA_BTN} onClick={() => void reload()}>
                <RefreshCw aria-hidden="true" className="size-3" />
                {staleSchema.kind === "option-removed"
                  ? t.staleSchema.optionReload
                  : t.staleSchema.reload}
              </PanelBtn>
            ) : null
          }
        />
      ) : conflict ? (
        <Banner
          tone="warn"
          title={fillPopupCopy(t.conflict.title, {
            who: conflictBy ?? t.conflict.someone,
          })}
          body={t.conflict.body}
        >
          {onReload ? (
            <div className="mt-2 flex flex-wrap gap-2">
              <PanelBtn small onClick={onReload}>
                {t.conflict.reload}
              </PanelBtn>
            </div>
          ) : null}
        </Banner>
      ) : null}
      {readOnly === "permission" ? (
        <Banner
          tone="info"
          title={t.readOnlyPermissionTitle}
          body={t.readOnlyPermissionBody}
        />
      ) : null}
      {readOnly === "item-archived" ? (
        <Banner
          tone="info"
          title={
            card.archivedByName
              ? fillPopupCopy(t.readOnlyItemArchivedBy, {
                  who: card.archivedByName,
                })
              : t.readOnlyItemArchivedTitle
          }
          body={t.readOnlyItemArchivedBody}
        />
      ) : null}
      {readOnly === "archived" ? (
        <Banner
          tone="info"
          title={t.readOnlyArchivedTitle}
          body={t.readOnlyArchivedBody}
        />
      ) : null}
    </>
  );
}
