import { useEffect, useId, useState, type FormEvent } from "react";
import { Lock, Users } from "lucide-react";
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
  RadioGroup,
  RadioGroupItem,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import {
  SPACE_TYPES,
  SPACE_VISIBILITIES,
  defaultVisibilityFor,
  type SpaceType,
  type SpaceVisibility,
} from "../../core/model/spaces";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/** The producer's name limit (Space.Rename/Create, 160). */
export const SPACE_NAME_MAX = 160;

export interface CreateSpaceValues {
  readonly name: string;
  readonly type: SpaceType;
  readonly visibility: SpaceVisibility;
}

export interface CreateSpaceDialogProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly open: boolean;
  /** The parent Space; `null` creates a top-level Space. */
  readonly parent: { readonly id: string; readonly name: string } | null;
  readonly workspaceName: string | null;
  readonly pending: boolean;
  /** A failure message to show; the typed values are kept. */
  readonly error: string | null;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: CreateSpaceValues) => void;
}

const VISIBILITY_ICON = { Private: Lock, Workspace: Users } as const;

/**
 * Names a new Space and chooses its type and visibility (WSP-021, WSP-023).
 * A Department starts Private; other types start open to the parent's
 * audience, until the member picks a visibility themselves.
 */
export function CreateSpaceDialog({
  copy,
  open,
  parent,
  workspaceName,
  pending,
  error,
  onOpenChange,
  onSubmit,
}: CreateSpaceDialogProps) {
  const [name, setName] = useState("");
  const [type, setType] = useState<SpaceType>("Department");
  const [visibility, setVisibility] = useState<SpaceVisibility | null>(null);
  const [touched, setTouched] = useState(false);
  const nameId = useId();
  const nameErrorId = useId();
  const typeId = useId();
  const typeHelpId = useId();
  const visibilityLabelId = useId();

  useEffect(() => {
    if (!open) {
      setName("");
      setType("Department");
      setVisibility(null);
      setTouched(false);
    }
  }, [open]);

  const trimmed = name.trim();
  const nameMessage =
    touched && trimmed === "" ? copy.create.nameRequired : null;
  const message = nameMessage ?? error;
  const effectiveVisibility = visibility ?? defaultVisibilityFor(type);

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (pending || trimmed === "") return;
    onSubmit({ name: trimmed, type, visibility: effectiveVisibility });
  };

  const location = parent
    ? parent.name
    : formatCopy(copy.create.locationRoot, { workspace: workspaceName ?? "" });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <form onSubmit={submit} noValidate className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>
              {parent
                ? formatCopy(copy.create.childTitle, { parent: parent.name })
                : copy.create.title}
            </DialogTitle>
            <DialogDescription>{copy.create.description}</DialogDescription>
          </DialogHeader>
          <p className="text-metadata text-muted-foreground">
            <span className="font-medium text-foreground">
              {copy.create.locationLabel}:
            </span>{" "}
            {location}
          </p>
          <Field data-invalid={message ? true : undefined}>
            <FieldLabel htmlFor={nameId}>{copy.create.nameLabel}</FieldLabel>
            <Input
              id={nameId}
              value={name}
              autoFocus
              maxLength={SPACE_NAME_MAX}
              placeholder={copy.create.namePlaceholder}
              aria-invalid={message ? true : undefined}
              aria-describedby={message ? nameErrorId : undefined}
              onChange={(event) => setName(event.target.value)}
            />
            {message ? (
              <FieldError id={nameErrorId}>{message}</FieldError>
            ) : null}
          </Field>
          <Field>
            <FieldLabel htmlFor={typeId}>{copy.create.typeLabel}</FieldLabel>
            <Select
              value={type}
              onValueChange={(value) => setType(value as SpaceType)}
            >
              <SelectTrigger
                id={typeId}
                className="w-full"
                aria-describedby={typeHelpId}
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {SPACE_TYPES.map((option) => (
                  <SelectItem key={option} value={option}>
                    {copy.types[option]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <FieldDescription id={typeHelpId}>
              {copy.typeHelp[type]}
            </FieldDescription>
          </Field>
          <fieldset className="flex flex-col gap-2">
            <legend
              id={visibilityLabelId}
              className="mb-2 text-label text-foreground"
            >
              {copy.create.visibilityLabel}
            </legend>
            <RadioGroup
              value={effectiveVisibility}
              onValueChange={(value) => setVisibility(value as SpaceVisibility)}
              aria-labelledby={visibilityLabelId}
              className="gap-2"
            >
              {SPACE_VISIBILITIES.map((option) => {
                const Icon = VISIBILITY_ICON[option];
                const optionId = `${visibilityLabelId}-${option}`;
                const selected = effectiveVisibility === option;
                return (
                  <label
                    key={option}
                    htmlFor={optionId}
                    className={cn(
                      "flex cursor-pointer items-start gap-3 rounded-md border border-border p-3 hover:bg-state-hover",
                      selected && "border-focus-ring bg-state-selected",
                    )}
                  >
                    <RadioGroupItem
                      id={optionId}
                      value={option}
                      aria-describedby={`${optionId}-help`}
                      className="mt-0.5"
                    />
                    <span className="flex min-w-0 flex-col gap-0.5">
                      <span className="flex items-center gap-1.5 text-label text-foreground">
                        <Icon aria-hidden="true" className="size-4 text-icon" />
                        {copy.visibility[option]}
                      </span>
                      <span
                        id={`${optionId}-help`}
                        className="text-metadata text-muted-foreground"
                      >
                        {copy.visibilityHelp[option]}
                      </span>
                    </span>
                  </label>
                );
              })}
            </RadioGroup>
          </fieldset>
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
