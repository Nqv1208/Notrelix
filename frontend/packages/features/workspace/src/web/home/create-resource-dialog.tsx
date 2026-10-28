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
  /**
   * The starters and workspace templates a new Board can start from; none (still
   * loading, or the read failed) = the plain name form.
   */
  readonly boardTemplates?: readonly CreateBoardTemplateChoice[];
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

/** One starting point offered for a new Board: a built-in starter or a published workspace template. */
export interface CreateBoardTemplateChoice {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  /** True for the starters every workspace has. */
  readonly builtIn: boolean;
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
  boardTemplates = [],
  onOpenChange,
  onSubmit,
}: CreateResourceDialogProps) {
  const [name, setName] = useState("");
  const [templateId, setTemplateId] = useState(BLANK);
  const [boardTemplateId, setBoardTemplateId] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);
  const inputId = useId();
  const templateFieldId = useId();
  const errorId = useId();

  useEffect(() => {
    if (kind === null) {
      setName("");
      setTemplateId(BLANK);
      setBoardTemplateId(null);
      setTouched(false);
    }
  }, [kind]);

  const isBoard = kind === "board";
  // The first starter is chosen until the person picks another: naming the board is all it takes.
  const boardChoice = isBoard
    ? (boardTemplates.find((entry) => entry.id === boardTemplateId) ??
      boardTemplates[0])
    : undefined;
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
    else if (kind === "board" && boardChoice)
      onSubmit(kind, trimmed, boardChoice.id);
    else onSubmit(kind, trimmed);
  };

  return (
    <Dialog open={kind !== null} onOpenChange={onOpenChange}>
      <DialogContent
        className={
          boardTemplates.length > 0 && isBoard ? "sm:max-w-2xl" : "sm:max-w-md"
        }
      >
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
          {isBoard && boardTemplates.length > 0 ? (
            <BoardTemplateGallery
              copy={copy}
              choices={boardTemplates}
              selectedId={boardChoice?.id ?? null}
              onSelect={setBoardTemplateId}
            />
          ) : null}
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

/** The built-in starters and the workspace's templates as two groups of radio cards. */
function BoardTemplateGallery({
  copy,
  choices,
  selectedId,
  onSelect,
}: {
  readonly copy: WorkspaceHomeCopy;
  readonly choices: readonly CreateBoardTemplateChoice[];
  readonly selectedId: string | null;
  readonly onSelect: (id: string) => void;
}) {
  const starters = choices.filter((entry) => entry.builtIn);
  const own = choices.filter((entry) => !entry.builtIn);
  const selected = choices.find((entry) => entry.id === selectedId);
  const baseId = useId();
  const group = (
    key: string,
    label: string,
    entries: readonly CreateBoardTemplateChoice[],
  ) => (
    <div className="flex flex-col gap-2">
      <p
        id={`${baseId}-${key}`}
        className="m-0 text-xs font-semibold text-muted-foreground"
      >
        {label}
      </p>
      <div
        role="radiogroup"
        aria-labelledby={`${baseId}-${key}`}
        className="grid grid-cols-1 gap-2.5 sm:grid-cols-3"
      >
        {entries.map((entry) => (
          <button
            key={entry.id}
            type="button"
            role="radio"
            aria-checked={entry.id === selectedId}
            onClick={() => onSelect(entry.id)}
            className="flex min-w-0 cursor-pointer flex-col gap-1 rounded-[10px] border border-input bg-background p-3 text-left outline-none hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring aria-checked:border-primary aria-checked:bg-accent aria-checked:ring-1 aria-checked:ring-primary"
          >
            <b className="text-sm font-semibold">{entry.name}</b>
            {entry.description ? (
              <span className="text-xs text-muted-foreground">
                {entry.description}
              </span>
            ) : null}
          </button>
        ))}
      </div>
    </div>
  );
  return (
    <div className="flex flex-col gap-3">
      {group("starters", copy.create.startersLabel, starters)}
      {own.length > 0 ? (
        group("own", copy.create.workspaceTemplatesLabel, own)
      ) : (
        <p className="m-0 rounded-[10px] border border-dashed border-input p-3 text-xs text-muted-foreground">
          {copy.create.noWorkspaceTemplates}
        </p>
      )}
      {selected && !selected.builtIn ? (
        <p className="m-0 rounded-[10px] bg-muted p-3 text-xs text-muted-foreground">
          {copy.create.templateCopies}
        </p>
      ) : null}
    </div>
  );
}
