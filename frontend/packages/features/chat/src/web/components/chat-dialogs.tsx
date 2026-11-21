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
  RadioGroup,
  RadioGroupItem,
  Textarea,
} from "@notrelix/ui-web";
import { CHAT_LIMITS, type ChatChannelGroup } from "../../core/types/chat";
import { slowModeChoices } from "../../core/model/rate-limit-model";
import type { ChatCopy } from "./chat.copy";
import { formatWait } from "./chat-rate-limit";

export interface CreateChannelValues {
  readonly name: string;
  readonly topic: string;
  readonly isPrivate: boolean;
  readonly groupId: string | null;
}

export interface CreateChannelDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  /** An error from the last attempt, already worded for the user. */
  readonly error: string | null;
  /** The sidebar categories a channel can be placed in (the Channel group field is left out when there are none). */
  readonly groups?: readonly ChatChannelGroup[];
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: CreateChannelValues) => void;
}

/** Create a channel (UC-CHT-A01), laid out as the Chat-Var-Channel-Create board. The caller becomes its first manager. */
export function CreateChannelDialog({
  open,
  busy,
  error,
  groups = [],
  copy,
  onOpenChange,
  onSubmit,
}: CreateChannelDialogProps) {
  const [name, setName] = useState("");
  const [topic, setTopic] = useState("");
  const [isPrivate, setPrivate] = useState(false);
  const [groupId, setGroupId] = useState("");
  const trimmed = name.trim();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (trimmed.length === 0 || busy) return;
    onSubmit({
      name: trimmed,
      topic: topic.trim(),
      isPrivate,
      groupId: groupId === "" ? null : groupId,
    });
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
            <div className="relative">
              <Input
                id="chat-create-name"
                value={name}
                maxLength={CHAT_LIMITS.channelNameMaxLength}
                onChange={(event) => setName(event.target.value)}
                autoComplete="off"
                autoFocus
                className="pe-16"
                aria-describedby="chat-create-name-hint"
                aria-invalid={error !== null || undefined}
              />
              <span
                aria-hidden="true"
                className="pointer-events-none absolute inset-y-0 end-3 flex items-center text-xs text-muted-foreground tabular-nums"
              >
                {`${name.length}/${CHAT_LIMITS.channelNameMaxLength}`}
              </span>
            </div>
            <p
              id="chat-create-name-hint"
              className="text-xs text-muted-foreground"
            >
              {copy.create.nameHint}
            </p>
          </div>
          <div className="flex flex-col gap-1.5">
            <span id="chat-create-visibility" className="text-sm font-medium">
              {copy.create.visibilityLabel}
            </span>
            <RadioGroup
              aria-labelledby="chat-create-visibility"
              value={isPrivate ? "private" : "public"}
              onValueChange={(value) => setPrivate(value === "private")}
              className="gap-2"
            >
              {(
                [
                  ["public", copy.create.publicLabel, copy.create.publicHint],
                  [
                    "private",
                    copy.create.privateLabel,
                    copy.create.privateHint,
                  ],
                ] as const
              ).map(([value, label, hint]) => (
                <label
                  key={value}
                  htmlFor={`chat-create-${value}`}
                  className="flex cursor-pointer items-start gap-3 rounded-lg border border-border p-3 has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary/5"
                >
                  <RadioGroupItem
                    id={`chat-create-${value}`}
                    value={value}
                    className="mt-0.5"
                  />
                  <span className="flex flex-col gap-0.5">
                    <span className="text-sm font-medium">{label}</span>
                    <span className="text-xs text-muted-foreground">
                      {hint}
                    </span>
                  </span>
                </label>
              ))}
            </RadioGroup>
          </div>
          {groups.length > 0 ? (
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="chat-create-group">
                {copy.create.groupLabel}
              </Label>
              <select
                id="chat-create-group"
                value={groupId}
                onChange={(event) => setGroupId(event.target.value)}
                className="h-9 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring"
              >
                <option value="">{copy.create.groupNone}</option>
                {groups.map((group) => (
                  <option key={group.id} value={group.id}>
                    {group.name}
                  </option>
                ))}
              </select>
            </div>
          ) : null}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-create-topic">{copy.create.topicLabel}</Label>
            <Input
              id="chat-create-topic"
              value={topic}
              maxLength={CHAT_LIMITS.channelTopicMaxLength}
              onChange={(event) => setTopic(event.target.value)}
              autoComplete="off"
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
  /** The channel's slow mode in seconds (UC-CHT-H08). */
  readonly initialSlowMode?: number;
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: {
    name: string;
    topic: string;
    slowModeSeconds: number;
  }) => void;
}

/** Rename a channel or set its topic (UC-CHT-A03). Only a manager, an Owner or an Admin. */
export function RenameChannelDialog({
  open,
  busy,
  initialName,
  initialTopic,
  initialSlowMode = 0,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: RenameChannelDialogProps) {
  const [name, setName] = useState(initialName);
  const [topic, setTopic] = useState(initialTopic ?? "");
  const [slowMode, setSlowMode] = useState(initialSlowMode);
  const trimmed = name.trim();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (trimmed.length === 0 || busy) return;
    onSubmit({ name: trimmed, topic: topic.trim(), slowModeSeconds: slowMode });
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
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-edit-slow-mode">
              {copy.edit.slowModeLabel}
            </Label>
            <select
              id="chat-edit-slow-mode"
              aria-describedby="chat-edit-slow-mode-help"
              value={slowMode}
              disabled={busy}
              onChange={(event) => setSlowMode(Number(event.target.value))}
              className="h-9 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring disabled:opacity-60"
            >
              {slowModeChoices(initialSlowMode).map((seconds) => (
                <option key={seconds} value={seconds}>
                  {seconds === 0
                    ? copy.edit.slowModeOff
                    : formatWait(copy, seconds)}
                </option>
              ))}
            </select>
            <p
              id="chat-edit-slow-mode-help"
              className="text-xs text-muted-foreground"
            >
              {copy.edit.slowModeHelp}
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

export interface DeleteChannelDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  readonly channelName: string;
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onConfirm: () => void;
}

/** Delete a channel (UC-CHT-A08): the manager types the channel name before the button works. */
export function DeleteChannelDialog({
  open,
  busy,
  channelName,
  error,
  copy,
  onOpenChange,
  onConfirm,
}: DeleteChannelDialogProps) {
  const [typed, setTyped] = useState("");
  const matches = typed.trim() === channelName;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!matches || busy) return;
    onConfirm();
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) setTyped("");
        onOpenChange(next);
      }}
    >
      <DialogContent>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{copy.membership.deleteTitle}</DialogTitle>
            <DialogDescription>{copy.membership.deleteBody}</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-delete-name">
              {copy.membership.deleteLabel}
            </Label>
            <Input
              id="chat-delete-name"
              value={typed}
              placeholder={channelName}
              autoComplete="off"
              onChange={(event) => setTyped(event.target.value)}
              autoFocus
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
              {copy.membership.cancel}
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={!matches || busy}
            >
              {copy.membership.deleteConfirm}
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
            <DialogTitle className="leading-snug">{text.title}</DialogTitle>
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
