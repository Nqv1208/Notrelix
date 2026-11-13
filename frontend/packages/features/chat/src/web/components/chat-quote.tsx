import { Reply, X } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";

export interface ReplyChipProps {
  readonly copy: ChatCopy;
  /** The author of the message being answered. */
  readonly name: string;
  /** The first line of the message being answered; a deleted message reads as deleted. */
  readonly text: string;
  readonly onCancel: () => void;
}

/**
 * The reply-to chip above the composer: who the reply goes to, a snippet, and a
 * button that cancels the reply. The reply is sent with the message's id.
 */
export function ReplyChip({ copy, name, text, onCancel }: ReplyChipProps) {
  return (
    <div
      className="mx-2 mt-2 flex min-w-0 items-center gap-2 rounded-2xl border-s-2 border-primary bg-muted py-1.5 ps-3 pe-1 text-xs"
      data-testid="chat-reply-chip"
    >
      <Reply
        className="size-3.5 shrink-0 text-muted-foreground"
        aria-hidden="true"
      />
      <span className="min-w-0 flex-1 truncate">
        <span className="font-semibold">
          {fillCopy(copy.quote.replyTo, { name })}
        </span>
        <span className="text-muted-foreground"> · {text}</span>
      </span>
      <Button
        type="button"
        size="icon-xs"
        variant="ghost"
        aria-label={copy.quote.cancel}
        title={copy.quote.cancel}
        onClick={onCancel}
      >
        <X aria-hidden="true" />
      </Button>
    </div>
  );
}
