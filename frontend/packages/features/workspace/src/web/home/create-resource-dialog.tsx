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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
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
  /** The workspace's page templates a new Doc can start from (none = a blank page only). */
  readonly templates?: readonly CreateResourceTemplate[];
  readonly onSubmit: (
    kind: CreatableResource,
    name: string,
    templateId?: string,
  ) => void;
}

export interface CreateResourceTemplate {
  readonly id: string;
  readonly name: string;
}

const BLANK = "__blank__";

/** Names a new Board or Doc (WX-HOME-002, WX-DIR-001). */
export function CreateResourceDialog({
  copy,
  kind,
  pending,
  error,
  spaceName = null,
  templates = [],
  onOpenChange,
  onSubmit,
}: CreateResourceDialogProps) {
  const [name, setName] = useState("");
  const [templateId, setTemplateId] = useState(BLANK);
  const [touched, setTouched] = useState(false);
  const inputId = useId();
  const templateFieldId = useId();
  const errorId = useId();

  useEffect(() => {
    if (kind === null) {
      setName("");
      setTemplateId(BLANK);
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
    const chosen = templates.some((entry) => entry.id === templateId)
      ? templateId
      : undefined;
    if (kind === "doc" && chosen) onSubmit(kind, trimmed, chosen);
    else onSubmit(kind, trimmed);
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
          {!isBoard && templates.length > 0 ? (
            <Field>
              <FieldLabel htmlFor={templateFieldId}>
                {copy.create.templateLabel}
              </FieldLabel>
              <Select value={templateId} onValueChange={setTemplateId}>
                <SelectTrigger id={templateFieldId} className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={BLANK}>
                    {copy.create.templateBlank}
                  </SelectItem>
                  {templates.map((entry) => (
                    <SelectItem key={entry.id} value={entry.id}>
                      {entry.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
          ) : null}
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
