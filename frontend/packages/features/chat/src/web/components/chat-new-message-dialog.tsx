import { useMemo, useState, type FormEvent } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from "@notrelix/ui-web";
import { startKindOf } from "../../core/model/conversation-model";
import {
  CHAT_LIMITS,
  type ChatStartConversationInput,
} from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { ChatPersonPicker, type PickablePerson } from "./chat-person-picker";
import { fillCopy, pluralCopy } from "./use-chat-copy";

export interface ChatNewMessageDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  /** The Workspace members the caller can pick (the caller is not in the list). */
  readonly people: readonly PickablePerson[];
  /** An error from the last attempt, already worded for the user. */
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (input: ChatStartConversationInput) => void;
}

/**
 * New message: one person picked starts a direct conversation, several start a group (UC-CHT-E01, E02).
 * A group takes at most `CHAT_LIMITS.groupCap` people, the caller included.
 */
export function ChatNewMessageDialog(props: ChatNewMessageDialogProps) {
  const { open, onOpenChange } = props;
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        {/* The form state resets each time the dialog opens: the content unmounts when it closes. */}
        <NewMessageForm {...props} />
      </DialogContent>
    </Dialog>
  );
}

function NewMessageForm({
  busy,
  people,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: ChatNewMessageDialogProps) {
  const [picked, setPicked] = useState<ReadonlySet<string>>(new Set());
  const [name, setName] = useState("");
  const kind = startKindOf(picked.size);
  const maxOthers = CHAT_LIMITS.groupCap - 1;
  const full = picked.size >= maxOthers;
  const ordered = useMemo(
    () => [...people].sort((a, b) => a.name.localeCompare(b.name)),
    [people],
  );

  function toggle(id: string, value: boolean) {
    setPicked((current) => {
      const next = new Set(current);
      if (value) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (kind === "none" || busy) return;
    const ids = [...picked];
    if (kind === "direct") {
      onSubmit({ kind: "direct", userId: ids[0]! });
      return;
    }
    const trimmed = name.trim();
    onSubmit({
      kind: "group",
      userIds: ids,
      name: trimmed.length > 0 ? trimmed : null,
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>{copy.dm.dialog.title}</DialogTitle>
        <DialogDescription>{copy.dm.dialog.description}</DialogDescription>
      </DialogHeader>
      <ChatPersonPicker
        people={ordered}
        selected={picked}
        full={full}
        copy={copy}
        onToggle={toggle}
      />
      <p role="status" className="min-h-5 text-xs text-muted-foreground">
        {picked.size > 0
          ? pluralCopy(
              {
                one: copy.dm.dialog.picked_one,
                other: copy.dm.dialog.picked_other,
              },
              picked.size,
            )
          : null}
        {full
          ? ` ${fillCopy(copy.dm.dialog.limit, { max: CHAT_LIMITS.groupCap })}`
          : null}
      </p>
      {kind === "group" ? (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="chat-group-name">
            {copy.dm.dialog.groupNameLabel}
          </Label>
          <Input
            id="chat-group-name"
            value={name}
            maxLength={CHAT_LIMITS.channelNameMaxLength}
            onChange={(event) => setName(event.target.value)}
            autoComplete="off"
            aria-describedby="chat-group-name-hint"
          />
          <p
            id="chat-group-name-hint"
            className="text-xs text-muted-foreground"
          >
            {copy.dm.dialog.groupNameHint}
          </p>
        </div>
      ) : null}
      {error ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
      <DialogFooter>
        <Button
          type="button"
          variant="ghost"
          onClick={() => onOpenChange(false)}
        >
          {copy.dm.dialog.cancel}
        </Button>
        <Button type="submit" disabled={kind === "none" || busy}>
          {kind === "group"
            ? copy.dm.dialog.startGroup
            : copy.dm.dialog.startDirect}
        </Button>
      </DialogFooter>
    </form>
  );
}
