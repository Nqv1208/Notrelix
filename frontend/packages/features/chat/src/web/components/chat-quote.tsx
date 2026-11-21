import { X } from "lucide-react";
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
      className="flex min-w-0 items-center gap-2.5 rounded-t-[26px] border-b border-border bg-background px-3.5 py-2 text-(length:--type-scale-12p5) leading-[1.45]"
      data-testid="chat-reply-chip"
    >
      <span
        className="w-[3px] self-stretch rounded-sm bg-primary"
        aria-hidden="true"
      />
      <span className="min-w-0 flex-1 text-muted-foreground">
        <b className="block truncate font-bold text-foreground">
          {fillCopy(copy.quote.replyTo, { name })}
        </b>
        <span className="block truncate">{text}</span>
      </span>
      <Button
        type="button"
        size="icon-xs"
        variant="ghost"
        className="size-7"
        aria-label={copy.quote.cancel}
        title={copy.quote.cancel}
        onClick={onCancel}
      >
        <X aria-hidden="true" />
      </Button>
    </div>
  );
}
