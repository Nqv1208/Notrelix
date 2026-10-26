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
  cn,
} from "@notrelix/ui-web";
import type { ItemCreateCopy } from "./item-create.copy";

export interface ItemCreateOption {
  readonly id: string;
  readonly label: string;
}

export interface ItemCreateMember {
  readonly userId: string;
  readonly name: string;
  readonly initials: string;
}

export interface ItemCreateLabel {
  readonly id: string;
  readonly name: string;
  readonly color: string;
}

/** What the dialog hands back: the item's name and every fact set on it. */
export interface ItemCreateDraft {
  readonly title: string;
  readonly descriptionMd: string;
  readonly listId: string;
  readonly statusOptionId: string | null;
  readonly priorityOptionId: string | null;
  readonly assigneeIds: readonly string[];
  readonly labelIds: readonly string[];
  /** `YYYY-MM-DD` or empty. */
  readonly startDay: string;
  readonly dueDay: string;
  readonly another: boolean;
}

export interface ItemCreateDialogProps {
  readonly copy: ItemCreateCopy;
  readonly open: boolean;
  readonly groups: readonly ItemCreateOption[];
  readonly defaultGroupId: string;
  readonly statuses: readonly ItemCreateOption[];
  readonly defaultStatusId: string | null;
  /** Empty when the board has no priority field. */
  readonly priorities: readonly ItemCreateOption[];
  readonly members: readonly ItemCreateMember[];
  readonly labels: readonly ItemCreateLabel[];
  readonly pending: boolean;
  readonly error: string | null;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (draft: ItemCreateDraft) => void;
}

const NONE = "__none__";
const CHIP =
  "inline-flex h-7 items-center gap-1.5 rounded-full border px-2.5 text-crumb font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
const DATE_INPUT =
  "box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover px-3 text-item-ui text-foreground outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-focus-ring";

function toggle(list: readonly string[], id: string): string[] {
  return list.includes(id)
    ? list.filter((entry) => entry !== id)
    : [...list, id];
}

/**
 * "Add item": the full create form of a board. The name is required; the
 * description, group, status, priority, people, dates and labels are optional
 * and can all be changed later. Enter in the name creates the item.
 */
export function ItemCreateDialog({
  copy,
  open,
  groups,
  defaultGroupId,
  statuses,
  defaultStatusId,
  priorities,
  members,
  labels,
  pending,
  error,
  onOpenChange,
  onSubmit,
}: ItemCreateDialogProps) {
  const ids = {
    name: useId(),
    description: useId(),
    group: useId(),
    status: useId(),
    priority: useId(),
    start: useId(),
    due: useId(),
  };
  const [title, setTitle] = useState("");
  const [descriptionMd, setDescriptionMd] = useState("");
  const [listId, setListId] = useState(defaultGroupId);
  const [statusId, setStatusId] = useState<string | null>(defaultStatusId);
  const [priorityId, setPriorityId] = useState<string | null>(null);
  const [assigneeIds, setAssigneeIds] = useState<string[]>([]);
  const [labelIds, setLabelIds] = useState<string[]>([]);
  const [startDay, setStartDay] = useState("");
  const [dueDay, setDueDay] = useState("");
  const [another, setAnother] = useState(false);
  const [touched, setTouched] = useState(false);

  // A fresh form each time it opens (the defaults follow the view that opened it).
  useEffect(() => {
    if (!open) return;
    setTitle("");
    setDescriptionMd("");
    setListId(defaultGroupId);
    setStatusId(defaultStatusId);
    setPriorityId(null);
    setAssigneeIds([]);
    setLabelIds([]);
    setStartDay("");
    setDueDay("");
    setTouched(false);
    // `another` is kept: a person adding several items wants it to stay on.
  }, [open, defaultGroupId, defaultStatusId]);

  const trimmed = title.trim();
  const datesBackwards = startDay !== "" && dueDay !== "" && dueDay < startDay;
  const nameError = touched && trimmed === "" ? copy.nameRequired : null;
  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (pending || trimmed === "" || datesBackwards || listId === "") return;
    onSubmit({
      title: trimmed,
      descriptionMd,
      listId,
      statusOptionId: statusId,
      priorityOptionId: priorityId,
      assigneeIds,
      labelIds,
      startDay,
      dueDay,
      another,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-xl">
        <form onSubmit={submit} noValidate className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{copy.title}</DialogTitle>
            <DialogDescription>{copy.note}</DialogDescription>
          </DialogHeader>

          <Field data-invalid={nameError ? true : undefined}>
            <FieldLabel htmlFor={ids.name}>{copy.name}</FieldLabel>
            <Input
              id={ids.name}
              value={title}
              autoFocus
              maxLength={500}
              placeholder={copy.namePlaceholder}
              aria-invalid={nameError ? true : undefined}
              onChange={(event) => setTitle(event.target.value)}
            />
            {nameError ? <FieldError>{nameError}</FieldError> : null}
          </Field>

          <Field>
            <FieldLabel htmlFor={ids.description}>
              {copy.description}
            </FieldLabel>
            <textarea
              id={ids.description}
              rows={3}
              value={descriptionMd}
              maxLength={20000}
              placeholder={copy.descriptionPlaceholder}
              onChange={(event) => setDescriptionMd(event.target.value)}
              className="box-border min-h-[84px] w-full resize-y rounded-[8px] border border-line-strong bg-popover px-3 py-2 text-item-ui text-foreground outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-focus-ring"
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            {groups.length > 1 ? (
              <Field>
                <FieldLabel htmlFor={ids.group}>{copy.group}</FieldLabel>
                <Select value={listId} onValueChange={setListId}>
                  <SelectTrigger id={ids.group} className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {groups.map((group) => (
                      <SelectItem key={group.id} value={group.id}>
                        {group.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            ) : null}
            {statuses.length > 0 ? (
              <Field>
                <FieldLabel htmlFor={ids.status}>{copy.status}</FieldLabel>
                <Select
                  value={statusId ?? NONE}
                  onValueChange={(value) =>
                    setStatusId(value === NONE ? null : value)
                  }
                >
                  <SelectTrigger id={ids.status} className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {statuses.map((status) => (
                      <SelectItem key={status.id} value={status.id}>
                        {status.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            ) : null}
            {priorities.length > 0 ? (
              <Field>
                <FieldLabel htmlFor={ids.priority}>{copy.priority}</FieldLabel>
                <Select
                  value={priorityId ?? NONE}
                  onValueChange={(value) =>
                    setPriorityId(value === NONE ? null : value)
                  }
                >
                  <SelectTrigger id={ids.priority} className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={NONE}>{copy.noPriority}</SelectItem>
                    {priorities.map((priority) => (
                      <SelectItem key={priority.id} value={priority.id}>
                        {priority.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            ) : null}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field>
              <FieldLabel htmlFor={ids.start}>{copy.startDate}</FieldLabel>
              <input
                id={ids.start}
                type="date"
                value={startDay}
                onChange={(event) => setStartDay(event.target.value)}
                className={DATE_INPUT}
              />
            </Field>
            <Field data-invalid={datesBackwards ? true : undefined}>
              <FieldLabel htmlFor={ids.due}>{copy.dueDate}</FieldLabel>
              <input
                id={ids.due}
                type="date"
                value={dueDay}
                min={startDay || undefined}
                aria-invalid={datesBackwards ? true : undefined}
                onChange={(event) => setDueDay(event.target.value)}
                className={DATE_INPUT}
              />
              {datesBackwards ? (
                <FieldError>{copy.dueBeforeStart}</FieldError>
              ) : null}
            </Field>
          </div>

          <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
            <legend className="mb-1 p-0 text-item-ui font-medium text-foreground">
              {copy.assignees}
            </legend>
            {members.length === 0 ? (
              <p className="m-0 text-crumb text-muted-foreground">
                {copy.noMembers}
              </p>
            ) : (
              <div className="flex max-h-28 flex-wrap gap-1.5 overflow-y-auto">
                {members.map((member) => {
                  const on = assigneeIds.includes(member.userId);
                  return (
                    <button
                      key={member.userId}
                      type="button"
                      aria-pressed={on}
                      onClick={() =>
                        setAssigneeIds((current) =>
                          toggle(current, member.userId),
                        )
                      }
                      className={cn(
                        CHIP,
                        on
                          ? "border-primary bg-brand-subtle text-brand-subtle-foreground"
                          : "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong",
                      )}
                    >
                      <span
                        aria-hidden="true"
                        className="grid size-4 place-items-center rounded-full bg-muted text-[9px] font-semibold text-muted-foreground"
                      >
                        {member.initials}
                      </span>
                      {member.name}
                    </button>
                  );
                })}
              </div>
            )}
          </fieldset>

          {labels.length > 0 ? (
            <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
              <legend className="mb-1 p-0 text-item-ui font-medium text-foreground">
                {copy.labels}
              </legend>
              <div className="flex flex-wrap gap-1.5">
                {labels.map((label) => {
                  const on = labelIds.includes(label.id);
                  return (
                    <button
                      key={label.id}
                      type="button"
                      aria-pressed={on}
                      onClick={() =>
                        setLabelIds((current) => toggle(current, label.id))
                      }
                      className={cn(
                        CHIP,
                        on
                          ? "border-primary bg-brand-subtle text-brand-subtle-foreground"
                          : "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong",
                      )}
                    >
                      {label.name}
                    </button>
                  );
                })}
              </div>
            </fieldset>
          ) : null}

          {error ? (
            <p role="alert" className="m-0 text-crumb text-destructive">
              {error}
            </p>
          ) : null}

          <DialogFooter className="items-center sm:justify-between">
            <label className="flex items-center gap-2 text-crumb text-muted-foreground">
              <input
                type="checkbox"
                checked={another}
                onChange={(event) => setAnother(event.target.checked)}
              />
              {copy.another}
            </label>
            <div className="flex gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
              >
                {copy.cancel}
              </Button>
              <Button type="submit" disabled={pending}>
                {pending ? copy.submitting : copy.submit}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
