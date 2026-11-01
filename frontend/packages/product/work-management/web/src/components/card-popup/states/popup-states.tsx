import { AlertCircle, Info, RefreshCw, TriangleAlert } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { useCardPopup } from "../card-popup-context";
import { fillPopupCopy } from "../card-popup.copy";
import { PanelBtn } from "../popup-ui";
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
        <RefreshCw aria-hidden="true" className="size-4" />
        {t.retry}
      </PanelBtn>
      <PanelBtn onClick={onClose}>{t.close}</PanelBtn>
    </EmptyState>
  );
}

/** Deleted, moved or no access read the same: one message, nothing about the card. */
export function PopupGone({ onClose }: { onClose: () => void }) {
  const t = useCardPopupCopy().shell.gone;
  return (
    <EmptyState title={t.title} body={t.body}>
      <PanelBtn variant="primary" onClick={onClose}>
        {t.close}
      </PanelBtn>
    </EmptyState>
  );
}

/** The card was deleted by someone while it was open: names who, to a person who could read its board. */
export function PopupDeleted({
  by,
  onClose,
}: {
  by: string | null;
  onClose: () => void;
}) {
  const t = useCardPopupCopy().shell.deleted;
  return (
    <EmptyState
      title={fillPopupCopy(t.title, { who: by ?? t.someone })}
      body={t.body}
    >
      <PanelBtn variant="primary" onClick={onClose}>
        {t.close}
      </PanelBtn>
    </EmptyState>
  );
}

function Banner({
  tone,
  title,
  body,
  children,
}: {
  tone: "warn" | "info";
  title: string;
  body: string;
  children?: React.ReactNode;
}) {
  const Icon = tone === "warn" ? TriangleAlert : Info;
  return (
    <div
      role={tone === "warn" ? "alert" : "status"}
      className={cn(
        "mx-4 mt-3 box-border flex md:mx-6 items-start gap-3 rounded-[10px] px-4 py-3 text-[13.5px] leading-[1.45] text-foreground",
        tone === "warn"
          ? "bg-warning-subtle shadow-[inset_0_0_0_1px_var(--warning-text)]"
          : "bg-brand-subtle",
      )}
    >
      <Icon aria-hidden="true" className="mt-px size-5 shrink-0" />
      <div className="flex-1">
        <b className="font-[650]">{title}</b>
        <br />
        {body}
        {children}
      </div>
    </div>
  );
}

/** The notices at the top of the left column: someone else's save, or why nothing is editable. */
export function PopupBanners({
  conflict,
  conflictBy,
  staleSchema,
  onReload,
}: {
  conflict?: boolean;
  conflictBy?: string | null;
  staleSchema?: { fieldName: string; byName: string | null } | null;
  onReload?: () => void;
}) {
  const { card, copy, readOnly, fieldConflicts } = useCardPopup();
  const t = copy.shell;
  return (
    <>
      {fieldConflicts.map((c) => (
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
      {staleSchema ? (
        <Banner
          tone="warn"
          title={fillPopupCopy(t.staleSchema.deleted, {
            who: staleSchema.byName ?? t.staleSchema.someone,
            field: staleSchema.fieldName,
          })}
          body={t.staleSchema.body}
        >
          {onReload ? (
            <div className="mt-2 flex flex-wrap gap-2">
              <PanelBtn small onClick={onReload}>
                {t.staleSchema.reload}
              </PanelBtn>
            </div>
          ) : null}
        </Banner>
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
