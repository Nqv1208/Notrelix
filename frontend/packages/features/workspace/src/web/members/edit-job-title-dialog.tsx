import * as React from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Field,
  FieldError,
  FieldLabel,
  Input,
} from "@notrelix/ui-web";
import type { WorkspaceMember } from "../../core/types/workspace";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceMembersCopy } from "./workspace-members.copy";

/** The producer's job title limit (`WorkspaceMember.MaxJobTitleLength`). */
export const JOB_TITLE_MAX = 100;

/** What the write answered: the dialog shows each failure in its own words. */
export type JobTitleOutcome =
  | { readonly kind: "saved" }
  | { readonly kind: "invalid" }
  | { readonly kind: "conflict" }
  | { readonly kind: "denied" }
  | { readonly kind: "failed" };

const hasControl = (value: string) =>
  [...value].some((char) => char.charCodeAt(0) < 32 || char === "\u007f");

/**
 * Edits another member's job title (`Wsp-Var-JobTitle`, `Wsp-Var-JobTitle-Err`): the field, Save and
 * Cancel with the pending, too-long, stale-version and refused states. Changes no role or permission.
 */
export function EditJobTitleDialog({
  copy,
  member,
  onSave,
  onReload,
  onClose,
}: {
  copy: WorkspaceMembersCopy;
  member: WorkspaceMember | null;
  onSave: (
    member: WorkspaceMember,
    title: string | null,
  ) => Promise<JobTitleOutcome>;
  onReload: () => void;
  onClose: () => void;
}) {
  const [value, setValue] = React.useState("");
  const [pending, setPending] = React.useState(false);
  const [outcome, setOutcome] = React.useState<JobTitleOutcome | null>(null);
  const fieldId = React.useId();
  const errorId = React.useId();
  const userId = member?.userId;

  React.useEffect(() => {
    setValue(member?.jobTitle ?? "");
    setPending(false);
    setOutcome(null);
    // The form restarts only for another person, never when the same row is read again.
  }, [userId]);

  if (!member) return null;
  const trimmed = value.trim();
  const tooLong = trimmed.length > JOB_TITLE_MAX;
  const malformed = hasControl(value);
  const fieldError = tooLong
    ? formatCopy(copy.jobTitle.tooLong, { max: JOB_TITLE_MAX })
    : malformed || outcome?.kind === "invalid"
      ? copy.jobTitle.invalid
      : null;
  const unchanged = trimmed === (member.jobTitle ?? "");
  const values = { name: member.name, max: JOB_TITLE_MAX };

  const banner =
    outcome?.kind === "conflict"
      ? copy.jobTitle.conflict
      : outcome?.kind === "denied"
        ? copy.jobTitle.denied
        : outcome?.kind === "failed"
          ? copy.jobTitle.failed
          : null;

  const submit = async () => {
    if (pending || fieldError || unchanged) return;
    setPending(true);
    setOutcome(null);
    const result = await onSave(member, trimmed === "" ? null : trimmed);
    setPending(false);
    if (result.kind === "saved") onClose();
    else setOutcome(result);
  };

  return (
    <Dialog open onOpenChange={(open) => (open ? undefined : onClose())}>
      <DialogContent className="sm:max-w-md">
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            void submit();
          }}
          className="flex flex-col gap-4"
        >
          <div className="flex flex-col gap-1">
            <DialogTitle>{formatCopy(copy.jobTitle.title, values)}</DialogTitle>
            <DialogDescription>
              {formatCopy(copy.jobTitle.description, values)}
            </DialogDescription>
          </div>
          <Field data-invalid={fieldError ? true : undefined}>
            <FieldLabel htmlFor={fieldId}>{copy.jobTitle.label}</FieldLabel>
            <Input
              id={fieldId}
              value={value}
              autoComplete="off"
              placeholder={copy.jobTitle.placeholder}
              aria-invalid={fieldError ? true : undefined}
              aria-describedby={fieldError ? errorId : undefined}
              disabled={pending}
              onChange={(event) => {
                setValue(event.target.value);
                setOutcome(null);
              }}
              className="max-md:h-touch"
            />
            {fieldError ? (
              <FieldError id={errorId}>{fieldError}</FieldError>
            ) : null}
          </Field>
          {banner ? (
            <div
              role="alert"
              className="flex flex-wrap items-center gap-3 rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
            >
              <span className="min-w-0 flex-1">{banner}</span>
              {outcome?.kind === "conflict" ? (
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={() => {
                    setOutcome(null);
                    onReload();
                  }}
                >
                  {copy.jobTitle.reload}
                </Button>
              ) : null}
            </div>
          ) : null}
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              className="max-md:h-touch"
              onClick={onClose}
            >
              {copy.jobTitle.cancel}
            </Button>
            <Button
              type="submit"
              className="max-md:h-touch"
              disabled={pending || Boolean(fieldError) || unchanged}
              aria-busy={pending || undefined}
            >
              {pending ? copy.jobTitle.saving : copy.jobTitle.save}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
