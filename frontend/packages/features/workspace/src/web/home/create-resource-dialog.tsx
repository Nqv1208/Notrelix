import { useEffect, useId, useState, type FormEvent } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldError,
  FieldLabel,
  Input,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

export type CreatableResource = "board" | "doc";

export interface CreateResourceDialogProps {
  readonly copy: WorkspaceHomeCopy;
  /** The resource being created; `null` closes the dialog. */
  readonly kind: CreatableResource | null;
  readonly pending: boolean;
  /** A failure message to show; the typed name is kept. */
  readonly error: string | null;
  /** The Space the resource is created in (OT-5); `null` = workspace level. */
  readonly spaceName?: string | null;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (kind: CreatableResource, name: string) => void;
}

/** Names a new Board or Doc (WX-HOME-002, WX-DIR-001). */
export function CreateResourceDialog({
  copy,
  kind,
  pending,
  error,
  spaceName = null,
  onOpenChange,
  onSubmit,
}: CreateResourceDialogProps) {
  const [name, setName] = useState("");
  const [touched, setTouched] = useState(false);
  const inputId = useId();
  const errorId = useId();

  useEffect(() => {
    if (kind === null) {
      setName("");
      setTouched(false);
    }
  }, [kind]);

  const isBoard = kind === "board";
  const trimmed = name.trim();
  const missing = touched && trimmed === "";
  const message = missing ? copy.create.required : error;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (!kind || trimmed === "" || pending) return;
    onSubmit(kind, trimmed);
  };

  return (
    <Dialog open={kind !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <form onSubmit={submit} noValidate className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>
              {isBoard ? copy.create.boardTitle : copy.create.docTitle}
            </DialogTitle>
            <DialogDescription>
              {isBoard
                ? copy.create.boardDescription
                : copy.create.docDescription}
              {spaceName
                ? ` ${formatCopy(copy.create.inSpace, { space: spaceName })}`
                : null}
            </DialogDescription>
          </DialogHeader>
          <Field data-invalid={message ? true : undefined}>
            <FieldLabel htmlFor={inputId}>
              {isBoard ? copy.create.boardLabel : copy.create.docLabel}
            </FieldLabel>
            <Input
              id={inputId}
              value={name}
              autoFocus
              maxLength={200}
              placeholder={
                isBoard
                  ? copy.create.boardPlaceholder
                  : copy.create.docPlaceholder
              }
              aria-invalid={message ? true : undefined}
              aria-describedby={message ? errorId : undefined}
              onChange={(event) => setName(event.target.value)}
            />
            {message ? <FieldError id={errorId}>{message}</FieldError> : null}
          </Field>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              {copy.create.cancel}
            </Button>
            <Button type="submit" disabled={pending}>
              {pending
                ? copy.create.submitting
                : isBoard
                  ? copy.create.submitBoard
                  : copy.create.submitDoc}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
