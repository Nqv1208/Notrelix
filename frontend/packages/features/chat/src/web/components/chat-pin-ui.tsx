import { useState } from "react";
import {
  AlertTriangle,
  ArrowUp,
  Link2,
  MoreHorizontal,
  Pin,
  PinOff,
  X,
} from "lucide-react";
import {
  Button,
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  useIsMobile,
} from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";
import { ChatActionSheet, ChatBottomSheet } from "./chat-action-sheet";
import { fillCopy } from "./use-chat-copy";

export interface UnpinDialogProps {
  readonly open: boolean;
  readonly copy: ChatCopy;
  /** The caller, as the announcement names them. */
  readonly actorName: string;
  readonly busy: boolean;
  /** The last attempt failed: the dialog stays open and says so. */
  readonly failed: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly onConfirm: () => void;
}

/**
 * Unpin confirmation (Chat-Pins boards): a 440 dialog with the pin badge from 768 px, a bottom sheet with two stacked 46 px
 * buttons on a phone. Unpinning is announced in the channel, so the text says what everyone will see.
 */
export function UnpinDialog({
  open,
  copy,
  actorName,
  busy,
  failed,
  onOpenChange,
  onConfirm,
}: UnpinDialogProps) {
  const isMobile = useIsMobile();
  const text = copy.pins;
  const announce = fillCopy(text.unpinAnnounce, { name: actorName });
  const error = failed ? (
    <p role="alert" className="mt-2 text-sm text-destructive">
      {text.unpinRetry}
    </p>
  ) : null;
  if (isMobile) {
    return (
      <ChatBottomSheet
        open={open}
        title={text.unpinTitle}
        closeLabel={text.sheetClose}
        onOpenChange={onOpenChange}
        testId="chat-unpin-sheet"
      >
        <div className="px-3.5 pt-1 pb-2 leading-[1.55] text-muted-foreground">
          <p>{text.unpinBody}</p>
          {error}
        </div>
        <div className="flex flex-col gap-2 px-3.5 py-2">
          <Button
            type="button"
            className="h-[46px]"
            disabled={busy}
            onClick={onConfirm}
          >
            {text.unpinAction}
          </Button>
          <Button
            type="button"
            variant="outline"
            className="h-[46px]"
            onClick={() => onOpenChange(false)}
          >
            {copy.list.cancel}
          </Button>
        </div>
      </ChatBottomSheet>
    );
  }
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="w-[400px] gap-0 overflow-hidden rounded-2xl p-0 sm:max-w-[400px] xl:w-[440px] xl:max-w-[440px]"
        data-testid="chat-unpin-dialog"
      >
        <div className="flex items-center justify-between border-b border-border px-5 py-4">
          <DialogTitle className="text-calendar-title leading-[1.45] font-[650]">
            {text.unpinTitle}
          </DialogTitle>
          <DialogClose asChild>
            <Button
              type="button"
              size="icon-sm"
              variant="ghost"
              className="text-muted-foreground"
              aria-label={text.sheetClose}
            >
              <X aria-hidden="true" />
            </Button>
          </DialogClose>
        </div>
        <div className="px-5 pt-1.5 pb-4">
          <div className="flex gap-3.5 pt-3">
            <span
              className="grid size-11 shrink-0 place-items-center rounded-xl bg-primary/10 text-link"
              aria-hidden="true"
            >
              <Pin className="size-5" />
            </span>
            <div className="flex-1">
              <DialogDescription className="leading-[1.55] text-muted-foreground">
                {text.unpinBody}
                <span className="max-xl:hidden"> {announce}</span>
              </DialogDescription>
              {error}
            </div>
          </div>
        </div>
        <div className="flex justify-end gap-2 border-t border-border bg-muted/40 px-5 py-3">
          <DialogClose asChild>
            <Button type="button" variant="outline" className="h-[34px]">
              {copy.list.cancel}
            </Button>
          </DialogClose>
          <Button
            type="button"
            className="h-[34px]"
            disabled={busy}
            onClick={onConfirm}
          >
            {text.unpinAction}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

/** A row of the pin menu: 40 high, 14 inside, 14 between the icon and the label. */
const PIN_MENU_ITEM = "min-h-10 gap-3.5 rounded-lg px-3.5 text-sm";

export interface PinRowMenuProps {
  readonly copy: ChatCopy;
  /** The pin's text, so the trigger names which pin it acts on. */
  readonly snippet: string;
  readonly onOpenMessage: () => void;
  readonly onCopyLink: () => void;
  /** Absent when the caller may not unpin. */
  readonly onUnpin?: () => void;
  readonly disabled?: boolean;
}

/**
 * The menu of one pinned message (Chat-Pins boards): Open message, Copy link, then Unpin in red after a rule. A 220 popup
 * from 768 px, a bottom sheet titled "Pinned messages" on a phone.
 */
export function PinRowMenu({
  copy,
  snippet,
  onOpenMessage,
  onCopyLink,
  onUnpin,
  disabled = false,
}: PinRowMenuProps) {
  const isMobile = useIsMobile();
  const [sheetOpen, setSheetOpen] = useState(false);
  const text = copy.pins;
  const trigger = (
    <Button
      type="button"
      size="icon-sm"
      variant="ghost"
      className="size-7 text-muted-foreground"
      disabled={disabled}
      aria-label={`${text.rowMore}: ${snippet}`}
      title={text.rowMore}
      data-testid="chat-pin-more"
      onClick={isMobile ? () => setSheetOpen(true) : undefined}
    >
      <MoreHorizontal aria-hidden="true" />
    </Button>
  );
  if (isMobile) {
    return (
      <>
        {trigger}
        <ChatActionSheet
          open={sheetOpen}
          title={text.title}
          closeLabel={text.sheetClose}
          onOpenChange={setSheetOpen}
          testId="chat-pin-sheet"
          actions={[
            {
              key: "open",
              label: text.goToMessage,
              icon: ArrowUp,
              onSelect: onOpenMessage,
            },
            {
              key: "link",
              label: text.copyLink,
              icon: Link2,
              onSelect: onCopyLink,
            },
            ...(onUnpin
              ? [
                  {
                    key: "unpin",
                    label: text.unpinAction,
                    icon: PinOff,
                    onSelect: onUnpin,
                    destructive: true,
                  },
                ]
              : []),
          ]}
        />
      </>
    );
  }
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        aria-label={text.menuLabel}
        className="w-[220px] rounded-xl p-1"
      >
        <DropdownMenuItem className={PIN_MENU_ITEM} onSelect={onOpenMessage}>
          <ArrowUp className="size-3.5" aria-hidden="true" />
          {text.openMessage}
        </DropdownMenuItem>
        <DropdownMenuItem className={PIN_MENU_ITEM} onSelect={onCopyLink}>
          <Link2 className="size-3.5" aria-hidden="true" />
          {text.copyLink}
        </DropdownMenuItem>
        {onUnpin ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              variant="destructive"
              className={PIN_MENU_ITEM}
              onSelect={onUnpin}
            >
              <PinOff className="size-3.5" aria-hidden="true" />
              {text.unpinAction}
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** The banner under the channel header when a pin was refused for the limit (Chat-Pins board, limit state). */
export function PinLimitBanner({
  copy,
  onView,
}: {
  readonly copy: ChatCopy;
  readonly onView: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex shrink-0 items-center gap-2.5 bg-danger-subtle px-4 py-2 text-item-small font-semibold text-danger-subtle-foreground"
      data-testid="chat-pin-limit-banner"
    >
      <AlertTriangle className="size-3.5 shrink-0" aria-hidden="true" />
      <span className="flex-1">{copy.pins.limit}</span>
      <Button
        type="button"
        variant="outline"
        className="h-7 px-2.5 text-xs"
        onClick={onView}
      >
        {copy.pins.limitAction}
      </Button>
    </div>
  );
}
