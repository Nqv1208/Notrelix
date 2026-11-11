import { useId, type KeyboardEvent } from "react";
import { Send } from "lucide-react";
import { Button, Textarea, cn } from "@notrelix/ui-web";
import {
  checkMessageContent,
  chatTextLength,
  showsCounter,
} from "../../core/model/chat-model";
import { CHAT_LIMITS } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";

export interface ChatComposerProps {
  readonly channelName: string;
  readonly value: string;
  /** Why posting is refused here (archived, or not a member); the composer is disabled with it. */
  readonly disabledReason: string | null;
  readonly copy: ChatCopy;
  readonly onChange: (value: string) => void;
  readonly onSend: (content: string) => void;
}

/**
 * The message composer (UC-CHT-B01). Enter sends, Shift+Enter starts a new line,
 * the counter appears near the 4,000 character limit, and the composer is
 * disabled with the reason in an archived channel or when the caller is not in it.
 */
export function ChatComposer({
  channelName,
  value,
  disabledReason,
  copy,
  onChange,
  onSend,
}: ChatComposerProps) {
  const hintId = useId();
  const length = chatTextLength(value);
  const check = checkMessageContent(value);
  const tooLong = length > CHAT_LIMITS.messageMaxLength;
  const disabled = disabledReason !== null;
  const label = fillCopy(copy.composer.label, { channel: channelName });

  function submit() {
    if (disabled) return;
    const result = checkMessageContent(value);
    if (!result.ok) return;
    onSend(result.content);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key !== "Enter" || event.shiftKey) return;
    if (event.nativeEvent.isComposing) return;
    event.preventDefault();
    submit();
  }

  let message: string | null = null;
  if (disabledReason !== null) message = disabledReason;
  else if (tooLong)
    message = fillCopy(copy.composer.tooLong, {
      max: CHAT_LIMITS.messageMaxLength,
    });

  return (
    <div className="shrink-0 bg-background px-3 pt-2 pb-3 md:px-5 md:pb-4">
      <form
        className={cn(
          "flex flex-col rounded-xl border border-input bg-card shadow-xs transition-colors focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/30",
          disabled && "opacity-60",
        )}
        onSubmit={(event) => {
          event.preventDefault();
          submit();
        }}
      >
        <Textarea
          aria-label={label}
          aria-describedby={hintId}
          aria-invalid={tooLong || undefined}
          placeholder={label}
          value={value}
          disabled={disabled}
          rows={1}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={handleKeyDown}
          className="max-h-40 min-h-11 resize-none border-0 bg-transparent px-3 py-2.5 shadow-none focus-visible:border-0 focus-visible:ring-0 md:text-sm"
        />
        <div className="flex flex-wrap items-center gap-2 border-t border-border px-2 py-1.5">
          <div
            id={hintId}
            className={cn(
              "flex min-w-0 flex-1 items-center gap-2 text-xs",
              tooLong ? "text-destructive" : "text-muted-foreground",
            )}
          >
            <span
              role={message ? "status" : undefined}
              className="min-w-0 truncate"
            >
              {message ?? copy.composer.hint}
            </span>
            {showsCounter(value) ? (
              <span className="shrink-0 tabular-nums" aria-live="off">
                {fillCopy(copy.composer.counter, {
                  count: length,
                  max: CHAT_LIMITS.messageMaxLength,
                })}
              </span>
            ) : null}
          </div>
          <Button
            type="submit"
            size="sm"
            aria-label={copy.composer.send}
            title={copy.composer.send}
            disabled={disabled || tooLong || !check.ok}
          >
            <Send aria-hidden="true" />
            {copy.composer.send}
          </Button>
        </div>
      </form>
    </div>
  );
}
