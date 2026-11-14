import { cn } from "@notrelix/ui-web";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import type { ChatCopy } from "./chat.copy";

/** The dot colours by state: tokens only, and the shape differs too so colour is not the only cue. */
const DOT: Record<ChatPresenceState, string> = {
  online: "bg-success",
  idle: "bg-warning",
  offline: "border border-muted-foreground bg-background",
};

/** The text of a state, for the accessible name of a row and the dot's title. */
export function presenceText(state: ChatPresenceState, copy: ChatCopy): string {
  return copy.presence[state];
}

/**
 * A presence dot at the corner of an avatar. Decorative: the state is part of
 * the row's accessible name (`presenceText`), so a screen reader hears it once
 * and presence changes are not announced one by one.
 */
export function PresenceDot({
  state,
  className,
}: {
  readonly state: ChatPresenceState;
  readonly className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      data-presence={state}
      className={cn(
        "absolute -end-0.5 -bottom-0.5 size-2.5 rounded-full ring-2 ring-background",
        DOT[state],
        className,
      )}
    />
  );
}
