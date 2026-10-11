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
  FieldDescription,
  FieldError,
  FieldLabel,
  Input,
  Textarea,
} from "@notrelix/ui-web";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

/** The producer's Team name limit (`Team.cs`, 160). */
export const TEAM_NAME_MAX = 160;
/** The producer's Team description limit (`Team.cs`, 1024). */
export const TEAM_DESCRIPTION_MAX = 1024;

export interface CreateTeamValues {
  readonly name: string;
  /** `null` when the description is left empty. */
  readonly description: string | null;
}

export interface CreateTeamDialogProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly open: boolean;
  readonly pending: boolean;
  /** A failure message to show; the typed values are kept. */
  readonly error: string | null;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: CreateTeamValues) => void;
}

/**
 * Names a new Team (WX-TEAM-001). The name is required and at most 160
 * characters; the optional description at most 1024. The producer makes
 * the creator the first Lead, so no role is chosen here.
 */
export function CreateTeamDialog({
  copy,
  open,
  pending,
  error,
  onOpenChange,
  onSubmit,
}: CreateTeamDialogProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [touched, setTouched] = useState(false);
  const nameId = useId();
  const nameErrorId = useId();
  const descriptionId = useId();
  const descriptionHelpId = useId();

  useEffect(() => {
    if (!open) {
      setName("");
      setDescription("");
      setTouched(false);
    }
  }, [open]);

  const trimmedName = name.trim();
  const trimmedDescription = description.trim();
  const nameMessage = !touched
    ? null
    : trimmedName === ""
      ? copy.create.nameRequired
      : trimmedName.length > TEAM_NAME_MAX
        ? copy.create.nameTooLong
        : null;
  const descriptionMessage =
    trimmedDescription.length > TEAM_DESCRIPTION_MAX
      ? copy.create.descriptionTooLong
      : null;
  const message = nameMessage ?? error;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (
      pending ||
      trimmedName === "" ||
      trimmedName.length > TEAM_NAME_MAX ||
      descriptionMessage
    ) {
      return;
    }
    onSubmit({
      name: trimmedName,
      description: trimmedDescription === "" ? null : trimmedDescription,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <form onSubmit={submit} noValidate className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{copy.create.title}</DialogTitle>
            <DialogDescription>{copy.create.description}</DialogDescription>
          </DialogHeader>
          <Field data-invalid={message ? true : undefined}>
            <FieldLabel htmlFor={nameId}>{copy.create.nameLabel}</FieldLabel>
            <Input
              id={nameId}
              value={name}
              autoFocus
              maxLength={TEAM_NAME_MAX}
              placeholder={copy.create.namePlaceholder}
              aria-invalid={message ? true : undefined}
              aria-describedby={message ? nameErrorId : undefined}
              onChange={(event) => setName(event.target.value)}
            />
            {message ? (
              <FieldError id={nameErrorId} className="break-words">
                {message}
              </FieldError>
            ) : null}
          </Field>
          <Field data-invalid={descriptionMessage ? true : undefined}>
            <FieldLabel htmlFor={descriptionId}>
              {copy.create.descriptionLabel}
            </FieldLabel>
            <Textarea
              id={descriptionId}
              value={description}
              rows={3}
              aria-describedby={descriptionHelpId}
              aria-invalid={descriptionMessage ? true : undefined}
              onChange={(event) => setDescription(event.target.value)}
            />
            {descriptionMessage ? (
              <FieldError id={descriptionHelpId}>
                {descriptionMessage}
              </FieldError>
            ) : (
              <FieldDescription id={descriptionHelpId}>
                {copy.create.descriptionHelp}
              </FieldDescription>
            )}
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
              {pending ? copy.create.submitting : copy.create.submit}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
