import { useState, type FormEvent } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from "@notrelix/ui-web";
import { CHAT_LIMITS } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";

export interface CategoryNameDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  /** "create" starts empty; "rename" starts with `initialName`. */
  readonly mode: "create" | "rename";
  readonly initialName?: string;
  /** An error from the last attempt, already worded for the user (a duplicate name reads inline under the field). */
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (name: string) => void;
}

/**
 * Create or rename a sidebar category (UC-CHT-A02), laid out as the channel dialogs of the Chat-Var-Channel-Create
 * board: a name field with its counter and hint, the error under it, Cancel and the primary action.
 */
export function CategoryNameDialog({
  open,
  busy,
  mode,
  initialName = "",
  error,
  copy,
  onOpenChange,
  onSubmit,
}: CategoryNameDialogProps) {
  const text = copy.groups;
  const [name, setName] = useState(initialName);
  const trimmed = name.trim();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (trimmed.length === 0 || busy) return;
    onSubmit(trimmed);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>
              {mode === "create" ? text.createTitle : text.renameTitle}
            </DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-category-name">{text.nameLabel}</Label>
            <div className="relative">
              <Input
                id="chat-category-name"
                value={name}
                maxLength={CHAT_LIMITS.channelGroupNameMaxLength}
                onChange={(event) => setName(event.target.value)}
                autoComplete="off"
                autoFocus
                className="pe-16"
                aria-describedby="chat-category-name-hint chat-category-name-error"
                aria-invalid={error !== null || undefined}
              />
              <span
                aria-hidden="true"
                className="pointer-events-none absolute inset-y-0 end-3 flex items-center text-xs text-muted-foreground tabular-nums"
              >
                {`${name.length}/${CHAT_LIMITS.channelGroupNameMaxLength}`}
              </span>
            </div>
            <p
              id="chat-category-name-hint"
              className="text-xs text-muted-foreground"
            >
              {text.nameHint}
            </p>
          </div>
          {error ? (
            <p
              id="chat-category-name-error"
              role="alert"
              className="text-sm text-destructive"
            >
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
            <Button type="submit" disabled={trimmed.length === 0 || busy}>
              {mode === "create" ? text.create : text.save}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
