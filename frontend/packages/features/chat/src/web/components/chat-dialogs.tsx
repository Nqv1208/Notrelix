import { useState, type FormEvent } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  Switch,
  Textarea,
} from "@notrelix/ui-web";
import { CHAT_LIMITS } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";

export interface CreateChannelValues {
  readonly name: string;
  readonly topic: string;
  readonly isPrivate: boolean;
}

export interface CreateChannelDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  /** An error from the last attempt, already worded for the user. */
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: CreateChannelValues) => void;
}

/** Create a channel (UC-CHT-A01). The caller becomes its first manager. */
export function CreateChannelDialog({
  open,
  busy,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: CreateChannelDialogProps) {
  const [name, setName] = useState("");
  const [topic, setTopic] = useState("");
  const [isPrivate, setPrivate] = useState(false);
  const trimmed = name.trim();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (trimmed.length === 0 || busy) return;
    onSubmit({ name: trimmed, topic: topic.trim(), isPrivate });
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{copy.create.title}</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-create-name">{copy.create.nameLabel}</Label>
            <Input
              id="chat-create-name"
              value={name}
              maxLength={CHAT_LIMITS.channelNameMaxLength}
              onChange={(event) => setName(event.target.value)}
              autoComplete="off"
              autoFocus
              aria-describedby="chat-create-name-hint"
              aria-invalid={error !== null || undefined}
            />
            <p
              id="chat-create-name-hint"
              className="text-xs text-muted-foreground"
            >
              {copy.create.nameHint}
            </p>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-create-topic">{copy.create.topicLabel}</Label>
            <Textarea
              id="chat-create-topic"
              value={topic}
              maxLength={CHAT_LIMITS.channelTopicMaxLength}
              onChange={(event) => setTopic(event.target.value)}
              rows={2}
            />
          </div>
          <div className="flex items-start justify-between gap-4">
            <div className="flex flex-col gap-0.5">
              <Label htmlFor="chat-create-private">
                {copy.create.privateLabel}
              </Label>
              <span className="text-xs text-muted-foreground">
                {copy.create.privateHint}
              </span>
            </div>
            <Switch
              id="chat-create-private"
              checked={isPrivate}
              onCheckedChange={setPrivate}
            />
          </div>
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
              {copy.create.cancel}
            </Button>
            <Button type="submit" disabled={trimmed.length === 0 || busy}>
              {copy.create.submit}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export interface RenameChannelDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  readonly initialName: string;
  readonly initialTopic: string | null;
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: { name: string; topic: string }) => void;
}

/** Rename a channel or set its topic (UC-CHT-A03). Only a manager, an Owner or an Admin. */
export function RenameChannelDialog({
  open,
  busy,
  initialName,
  initialTopic,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: RenameChannelDialogProps) {
  const [name, setName] = useState(initialName);
  const [topic, setTopic] = useState(initialTopic ?? "");
  const trimmed = name.trim();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (trimmed.length === 0 || busy) return;
    onSubmit({ name: trimmed, topic: topic.trim() });
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{copy.edit.title}</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-edit-name">{copy.edit.nameLabel}</Label>
            <Input
              id="chat-edit-name"
              value={name}
              maxLength={CHAT_LIMITS.channelNameMaxLength}
              onChange={(event) => setName(event.target.value)}
              autoFocus
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-edit-topic">{copy.edit.topicLabel}</Label>
            <Textarea
              id="chat-edit-topic"
              value={topic}
              maxLength={CHAT_LIMITS.channelTopicMaxLength}
              onChange={(event) => setTopic(event.target.value)}
              rows={2}
            />
          </div>
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
              {copy.edit.cancel}
            </Button>
            <Button type="submit" disabled={trimmed.length === 0 || busy}>
              {copy.edit.save}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export interface ConfirmDialogProps {
  readonly open: boolean;
  readonly title: string;
  readonly body: string;
  readonly confirmLabel: string;
  readonly cancelLabel: string;
  readonly destructive?: boolean;
  readonly busy?: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly onConfirm: () => void;
}

/** A confirmation before a change that cannot be taken back quickly (archive, delete). */
export function ConfirmDialog({
  open,
  title,
  body,
  confirmLabel,
  cancelLabel,
  destructive = false,
  busy = false,
  onOpenChange,
  onConfirm,
}: ConfirmDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{body}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{cancelLabel}</AlertDialogCancel>
          <AlertDialogAction
            variant={destructive ? "destructive" : "default"}
            disabled={busy}
            onClick={(event) => {
              event.preventDefault();
              onConfirm();
            }}
          >
            {confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

export interface ModerateMessageDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  /** An error from the last attempt, already worded for the user. */
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (reason: string) => void;
}

/**
 * Delete another person's message as a moderator (UC-CHT-H03). The reason is required (1 to 500 characters); it goes to the Audit
 * record, never onto the message.
 */
export function ModerateMessageDialog({
  open,
  busy,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: ModerateMessageDialogProps) {
  const [reason, setReason] = useState("");
  const trimmed = reason.trim();
  const text = copy.moderation;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (trimmed.length === 0 || busy) return;
    onSubmit(trimmed);
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) setReason("");
        onOpenChange(next);
      }}
    >
      <DialogContent>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{text.title}</DialogTitle>
            <DialogDescription>{text.body}</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-moderate-reason">{text.reasonLabel}</Label>
            <Textarea
              id="chat-moderate-reason"
              value={reason}
              maxLength={CHAT_LIMITS.moderationReasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              rows={3}
              autoFocus
              required
              aria-describedby="chat-moderate-reason-hint"
            />
            <p
              id="chat-moderate-reason-hint"
              className="text-xs text-muted-foreground"
            >
              {text.reasonHint.replace(
                "{{max}}",
                String(CHAT_LIMITS.moderationReasonMaxLength),
              )}{" "}
              {text.counter
                .replace("{{count}}", String(reason.length))
                .replace(
                  "{{max}}",
                  String(CHAT_LIMITS.moderationReasonMaxLength),
                )}
            </p>
          </div>
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
              {text.cancel}
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={trimmed.length === 0 || busy}
            >
              {text.confirm}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
