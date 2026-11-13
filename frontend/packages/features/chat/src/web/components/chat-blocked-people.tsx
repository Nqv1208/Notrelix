import {
  Avatar,
  AvatarFallback,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy } from "./use-chat-copy";

export interface ChatBlockedPeopleDialogProps {
  readonly open: boolean;
  /** The people the caller blocked, from the server list (newest first). */
  readonly userIds: readonly string[];
  readonly status: "loading" | "ready" | "error";
  readonly nameOf: (userId: string) => string;
  /** The person whose unblock is in flight; its button is disabled. */
  readonly busyId: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onUnblock: (userId: string) => void;
}

/** The caller's blocked people with an Unblock button each (UC-CHT-E06). It lists only what the server list holds. */
export function ChatBlockedPeopleDialog({
  open,
  userIds,
  status,
  nameOf,
  busyId,
  copy,
  onOpenChange,
  onUnblock,
}: ChatBlockedPeopleDialogProps) {
  const text = copy.dm.blocked;
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent data-testid="chat-blocked-people">
        <DialogHeader>
          <DialogTitle>{text.title}</DialogTitle>
          <DialogDescription>{text.description}</DialogDescription>
        </DialogHeader>
        {status === "error" ? (
          <p role="alert" className="text-sm text-destructive">
            {text.error}
          </p>
        ) : userIds.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            {status === "loading" ? text.loading : text.empty}
          </p>
        ) : (
          <ul className="flex flex-col gap-1">
            {userIds.map((id) => {
              const name = nameOf(id);
              return (
                <li key={id} className="flex items-center gap-3 py-1">
                  <Avatar className="size-8">
                    <AvatarFallback className={avatarToneOf(id)}>
                      {initialsOf(name)}
                    </AvatarFallback>
                  </Avatar>
                  <span className="min-w-0 flex-1 truncate text-sm">
                    {name}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={busyId === id}
                    aria-label={fillCopy(text.unblockLabel, { name })}
                    onClick={() => onUnblock(id)}
                  >
                    {text.unblock}
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </DialogContent>
    </Dialog>
  );
}
