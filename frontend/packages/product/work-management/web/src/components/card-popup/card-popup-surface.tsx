import type { ReactNode } from "react";
import { X } from "lucide-react";
import { Dialog, DialogContent, DialogTitle, cn } from "@notrelix/ui-web";
import { CardPopupProvider, type CardPopupModel } from "./card-popup-context";
import { PopupHeader } from "./popup-header";
import { PopupTitleRow } from "./popup-title-row";
import { PopupQuickRow } from "./popup-quick-row";
import { IconBtn } from "./popup-ui";
import { ChipsRow } from "./chips-row";
import { DescriptionSection } from "./sections/description-section";
import { FieldsSection } from "./sections/fields-section";
import { ChecklistsSection } from "./sections/checklists-section";
import { AttachmentsSection } from "./sections/attachments-section";
import { LinksSection } from "./sections/links-section";
import { ApprovalSection } from "./approval/approval-section";
import { CommentsBar, CommentsColumn } from "./comments/comments-column";
import {
  PopupBanners,
  PopupDeleted,
  PopupGone,
  PopupLoadError,
  PopupSkeleton,
} from "./states/popup-states";
import { useCardPopupCopy } from "./use-card-popup-copy";

export type CardPopupStatus =
  "loading" | "error" | "gone" | "deleted" | "ready";

export interface CardPopupSurfaceProps {
  readonly status: CardPopupStatus;
  /** The model; required once the card is `ready`. */
  readonly model?: CardPopupModel;
  readonly onClose: () => void;
  readonly onRetry?: () => void;
  /** Gives focus back to the opener as the popup closes. */
  readonly onCloseAutoFocus?: (event: Event) => void;
  /** The bar under the columns (create mode: "Tạo tiếp thẻ khác", Huỷ, Tạo thẻ). */
  readonly footer?: ReactNode;
  /** Another person's save won: the notice shows and the fields are fresh. */
  readonly conflict?: boolean;
  /** Who saved first, when the server named them. */
  readonly conflictBy?: string | null;
  /** Who deleted the card (status `deleted`); null: the server named no one. */
  readonly deletedBy?: string | null;
  /** The conflict is a field the board no longer has (D12): names who changed it. */
  readonly staleSchema?: { fieldName: string; byName: string | null } | null;
  readonly onReloadConflict?: () => void;
  /** Dialogs of the host that must sit inside the popup (the draft confirm). */
  readonly overlays?: ReactNode;
}

/**
 * The card popup: a modal over the board (a full-height sheet on a phone).
 * Radix traps focus, Escape closes and focus returns to the card that opened
 * it. The left column holds the card, the right one its comments and activity.
 */
export function CardPopupSurface({
  status,
  model,
  onClose,
  onRetry,
  onCloseAutoFocus,
  footer,
  conflict,
  conflictBy,
  deletedBy,
  staleSchema,
  onReloadConflict,
  overlays,
}: CardPopupSurfaceProps) {
  const fallbackCopy = useCardPopupCopy();
  const t = (model?.copy ?? fallbackCopy).shell;
  const ready = status === "ready" && model;
  const canAttach = Boolean(
    ready &&
    model.mode === "view" &&
    model.readOnly === null &&
    model.capabilities.canEditFields &&
    model.callbacks.onAddFile,
  );

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent
        showCloseButton={false}
        onCloseAutoFocus={onCloseAutoFocus}
        aria-describedby={undefined}
        // An inline editor closes itself on Escape; the popup stays open.
        onEscapeKeyDown={(event) => {
          if (
            (event.target as HTMLElement | null)?.closest?.("[data-popup-esc]")
          )
            event.preventDefault();
        }}
        // Files dropped or pasted anywhere on the card are attached, like "Chọn tệp" in the attach panel.
        onDragOver={(event) => {
          if (canAttach && event.dataTransfer.types.includes("Files"))
            event.preventDefault();
        }}
        onDrop={(event) => {
          if (!canAttach || event.dataTransfer.files.length === 0) return;
          event.preventDefault();
          for (const dropped of Array.from(event.dataTransfer.files))
            model?.callbacks.onAddFile?.(dropped);
        }}
        onPaste={(event) => {
          if (!canAttach || event.clipboardData.files.length === 0) return;
          event.preventDefault();
          for (const pasted of Array.from(event.clipboardData.files))
            model?.callbacks.onAddFile?.(pasted);
        }}
        overlayClassName="bg-black/40 backdrop-blur-none"
        className={cn(
          "fixed top-3 left-0 flex h-[calc(100dvh-12px)] max-h-none w-screen max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-hidden rounded-t-[16px] rounded-b-none bg-popover p-0 text-[14px] text-foreground ring-0",
          "md:top-1/2 md:left-1/2 md:h-[calc(100dvh-128px)] md:w-[1120px] md:max-w-[calc(100vw-48px)] md:-translate-x-1/2 md:-translate-y-1/2 md:rounded-[16px] md:shadow-popover",
          "sm:max-w-none",
        )}
      >
        <DialogTitle className="sr-only">
          {ready ? model.card.title || t.dialogLabel : t.dialogLabel}
        </DialogTitle>
        {ready ? (
          <CardPopupProvider value={model}>
            <PopupHeader onClose={onClose} />
            <div className="flex min-h-0 flex-1 flex-col overflow-y-auto md:flex-row md:overflow-hidden">
              <div
                // 720 px when the popup is 1120 px wide; the column gives way first when the window is narrower.
                className="relative box-border min-w-0 pb-6 md:flex-[0_1_720px] md:overflow-y-auto"
                data-popup-left=""
              >
                {model.slots.coverBanner}
                <PopupBanners
                  conflict={conflict}
                  conflictBy={conflictBy}
                  staleSchema={staleSchema}
                  onReload={onReloadConflict}
                />
                <PopupTitleRow />
                <PopupQuickRow />
                <ChipsRow />
                <DescriptionSection />
                <FieldsSection />
                <ChecklistsSection />
                <AttachmentsSection />
                <LinksSection />
              </div>
              <aside
                aria-label={model.copy.shell.comments}
                className="relative box-border flex min-w-0 flex-col gap-3.5 border-t border-line bg-background px-4 pt-4 pb-6 md:min-w-[300px] md:flex-[0_1_400px] md:px-5 md:overflow-y-auto md:border-t-0 md:border-l"
              >
                <ApprovalSection />
                <CommentsColumn />
              </aside>
            </div>
            <CommentsBar />
            {footer}
            {overlays}
          </CardPopupProvider>
        ) : (
          <>
            <div className="flex h-14 flex-none items-center justify-end border-b border-line pr-3 pl-4">
              <IconBtn aria-label={t.close} onClick={onClose}>
                <X aria-hidden="true" className="size-4" />
              </IconBtn>
            </div>
            {status === "loading" ? <PopupSkeleton /> : null}
            {status === "error" ? (
              <PopupLoadError onRetry={onRetry ?? onClose} onClose={onClose} />
            ) : null}
            {status === "gone" ? <PopupGone onClose={onClose} /> : null}
            {status === "deleted" ? (
              <PopupDeleted by={deletedBy ?? null} onClose={onClose} />
            ) : null}
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
