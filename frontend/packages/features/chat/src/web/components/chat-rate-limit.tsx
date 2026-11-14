import { useEffect, useState } from "react";
import { Clock, Hourglass } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import { remainingSeconds, splitWait } from "../../core/model/rate-limit-model";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";
import { ChatStatusBanner } from "./chat-status-banner";

/** A wait as the copy reads it: seconds under a minute, else whole minutes or hours. */
export function formatWait(copy: ChatCopy, seconds: number): string {
  const { unit, count } = splitWait(seconds);
  return fillCopy(copy.rateLimit[unit], { count });
}

export interface ChatRateLimitNoticeProps {
  /** Whole seconds the server asked to wait. */
  readonly seconds: number;
  /** A rate limit, or the channel's slow mode. */
  readonly limit: "rate-limited" | "slow-mode";
  readonly copy: ChatCopy;
  /** Sends the held message again; offered once the wait is over. */
  readonly onRetry: () => void;
}

/**
 * The notice of a 429 (UC-CHT-H08): what was limited and a countdown to the retry. The message and its files stay held where they
 * are; the retry is offered when the wait is over. The status region is polite and announces the wait once (the ticking number is
 * hidden from assistive tech), then announces that sending is possible again.
 */
export function ChatRateLimitNotice({
  seconds,
  limit,
  copy,
  onRetry,
}: ChatRateLimitNoticeProps) {
  const [elapsed, setElapsed] = useState(0);
  const left = remainingSeconds(seconds, elapsed);

  useEffect(() => {
    setElapsed(0);
    const timer = setInterval(() => setElapsed((value) => value + 1), 1000);
    return () => clearInterval(timer);
  }, [seconds]);

  const text =
    limit === "slow-mode" ? copy.rateLimit.slowMode : copy.rateLimit.limited;
  return (
    <ChatStatusBanner
      placement="composer"
      tone="warning"
      icon={limit === "slow-mode" ? <Clock /> : <Hourglass />}
      action={
        left === 0 ? (
          <Button type="button" size="sm" variant="outline" onClick={onRetry}>
            {copy.rateLimit.retry}
          </Button>
        ) : null
      }
    >
      {left === 0 ? (
        copy.rateLimit.ready
      ) : (
        <>
          <span className="sr-only">
            {fillCopy(text, { wait: formatWait(copy, seconds) })}
          </span>
          <span aria-hidden="true" data-testid="chat-rate-limit-countdown">
            {fillCopy(text, { wait: formatWait(copy, left) })}
          </span>
        </>
      )}
    </ChatStatusBanner>
  );
}
