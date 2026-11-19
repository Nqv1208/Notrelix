/**
 * The screen share of a call, drawn on the share boards (Chat-Call-Share-Own, Chat-Call-Share-M and the variants
 * Chat-Var-Call-Share-Replace, -Denied, -Ended and -Replaced): the shared screen as the main tile, the card the sharer sees
 * instead of their own screen, the notice line for a share that did not start or ended, and the dialog that asks before one
 * share replaces another. The share control itself is a round button of the call stage.
 */
import {
  AlertTriangleIcon,
  InfoIcon,
  MonitorUpIcon,
  SquareIcon,
} from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import type { ChatCopy } from "../components/chat.copy";
import { fillCopy } from "../components/use-chat-copy";
import type { ChatCallsController, ChatCurrentCall } from "./use-chat-calls";

/** The main tile while another person shares: their screen, with who is sharing. */
export function ChatCallShareTile({
  controller,
  name,
  copy,
}: {
  readonly controller: ChatCallsController;
  readonly name: string;
  readonly copy: ChatCopy;
}) {
  const c = copy.calls;
  return (
    <div
      data-testid="chat-call-share"
      className="relative min-h-0 flex-1 overflow-hidden rounded-xl bg-black"
    >
      <video
        ref={controller.screenVideoRef}
        autoPlay
        playsInline
        aria-label={fillCopy(c.shareVideo, { name })}
        className="size-full object-contain"
      />
      <span className="absolute start-3 bottom-3 inline-flex h-7 items-center gap-1.5 rounded-lg bg-black/70 px-2.5 text-xs text-white">
        <MonitorUpIcon aria-hidden="true" className="size-3.5" />
        {fillCopy(c.shareChip, { name })}
      </span>
    </div>
  );
}

/** The main tile while this person shares: their own screen is not drawn back at them, only who sees it and how to stop. */
export function ChatCallShareOwn({
  controller,
  copy,
}: {
  readonly controller: ChatCallsController;
  readonly copy: ChatCopy;
}) {
  const c = copy.calls;
  return (
    <div
      data-testid="chat-call-share-own"
      className="flex min-h-0 flex-1 flex-col items-center justify-center gap-3 rounded-xl bg-inverse-foreground/10 p-6 text-center"
    >
      <span className="grid size-[72px] place-items-center rounded-full bg-primary/20 text-primary">
        <MonitorUpIcon aria-hidden="true" className="size-6" />
      </span>
      <h3 className="text-xl font-semibold">{c.shareYouTitle}</h3>
      <p className="max-w-md text-sm text-inverse-muted">{c.shareYouBody}</p>
      <button
        type="button"
        onClick={controller.stopScreenShare}
        className="mt-1.5 inline-flex h-12 items-center justify-center gap-2 rounded-full bg-destructive px-6 text-sm font-semibold text-destructive-foreground outline-none transition-colors hover:bg-destructive/90 focus-visible:ring-3 focus-visible:ring-destructive/50 motion-reduce:transition-none [&_svg]:size-4"
      >
        <SquareIcon aria-hidden="true" />
        {c.shareStop}
      </button>
    </div>
  );
}

/** The line above the stage for a share that did not start or ended; it leaves by itself and by its button. */
export function ChatCallShareNotice({
  held,
  controller,
  nameOf,
  sharerId,
  copy,
}: {
  readonly held: ChatCurrentCall;
  readonly controller: ChatCallsController;
  readonly nameOf: (userId: string) => string;
  readonly sharerId: string | null;
  readonly copy: ChatCopy;
}) {
  const c = copy.calls;
  const notice = held.screenNotice;
  if (!notice) return null;
  const warn =
    notice === "denied" || notice === "unavailable" || notice === "taken";
  const text =
    notice === "denied"
      ? c.shareDenied
      : notice === "unavailable"
        ? c.shareUnavailable
        : notice === "taken"
          ? c.shareTaken
          : notice === "ended"
            ? c.shareEnded
            : fillCopy(c.shareReplaced, {
                name: sharerId ? nameOf(sharerId) : "",
              });
  const retry =
    notice === "denied" || notice === "unavailable"
      ? c.shareRetry
      : notice === "ended"
        ? c.shareAgain
        : null;
  const Icon = warn ? AlertTriangleIcon : InfoIcon;
  return (
    <div
      role={warn ? "alert" : "status"}
      data-testid="chat-call-share-notice"
      data-notice={notice}
      className={cn(
        "flex shrink-0 items-center gap-2.5 px-4 py-2.5 text-sm font-semibold",
        warn
          ? "bg-warning-subtle text-warning-subtle-foreground"
          : "bg-inverse-foreground/15 text-inverse-foreground",
      )}
    >
      <Icon aria-hidden="true" className="size-4 shrink-0" />
      <span className="min-w-0">{text}</span>
      {retry ? (
        <Button
          variant="outline"
          size="xs"
          className="ms-auto border-current bg-transparent text-current hover:bg-inverse-foreground/15"
          onClick={() => controller.startScreenShare(false)}
        >
          {retry}
        </Button>
      ) : null}
    </div>
  );
}

/** Asks before a share takes the place of the one in progress (the server refuses it without this confirmation). */
export function ChatCallShareReplaceDialog({
  open,
  onOpenChange,
  name,
  onConfirm,
  copy,
}: {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly name: string;
  readonly onConfirm: () => void;
  readonly copy: ChatCopy;
}) {
  const c = copy.calls;
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{c.shareReplaceTitle}</DialogTitle>
          <DialogDescription>
            {fillCopy(c.shareReplaceBody, { name })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {c.shareReplaceCancel}
          </Button>
          <Button
            onClick={() => {
              onOpenChange(false);
              onConfirm();
            }}
          >
            <MonitorUpIcon aria-hidden="true" />
            {c.shareReplaceConfirm}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
