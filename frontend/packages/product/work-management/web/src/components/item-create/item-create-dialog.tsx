import {
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type FormEvent,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { CircleCheck } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
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
import {
  DayField,
  LabelPicker,
  PeoplePicker,
  QuickDue,
  dueHint,
  toDayString,
} from "./item-create-fields";

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

/** Where the form was opened from: shown as a chip beside the title. */
export interface ItemCreateContext {
  readonly kind: "lane" | "quick" | "outside";
  readonly name: string;
  readonly dotClassName?: string;
  readonly dotStyle?: CSSProperties;
}

/** The item just created while the form stays open for the next one. */
export interface ItemCreateCreated {
  /** Changes with every creation, so the form clears for the next item. */
  readonly nonce: number;
  readonly title: string;
  readonly onOpen?: () => void;
  readonly onUndo?: () => void;
}

export interface ItemCreateDialogProps {
  readonly copy: ItemCreateCopy;
  readonly open: boolean;
  readonly groups: readonly ItemCreateOption[];
  readonly defaultGroupId: string;
  /** The groups mirror the statuses (a Kanban lane is a status): no Group field. */
  readonly hideGroup?: boolean;
  readonly statuses: readonly ItemCreateOption[];
  readonly defaultStatusId: string | null;
  /** `YYYY-MM-DD` the dates start on (the Calendar and Timeline pass the day they show). */
  readonly defaultStartDay?: string;
  readonly defaultDueDay?: string;
  /** A name already typed elsewhere (a lane's quick add hands it over). */
  readonly defaultTitle?: string;
  /** The title came from a quick add: the cursor goes to the description. */
  readonly focusDescription?: boolean;
  /** Members ticked when the form opens (My work and Home assign the member). */
  readonly defaultAssigneeIds?: readonly string[];
  /** The signed-in member, when known: offers "Giao cho tôi". */
  readonly currentUserId?: string | null;
  readonly context?: ItemCreateContext;
  /** Shown above the name: the board choice when the form is opened off a board. */
  readonly boardSlot?: ReactNode;
  /** The board is still loading: the form shows but cannot be submitted. */
  readonly unavailable?: boolean;
  /** Adds a group when the board has none (an item cannot exist without one). */
  readonly onAddGroup?: () => void;
  readonly addGroupPending?: boolean;
  /** Empty when the board has no priority field. */
  readonly priorities: readonly ItemCreateOption[];
  readonly members: readonly ItemCreateMember[];
  readonly labels: readonly ItemCreateLabel[];
  readonly pending: boolean;
  readonly error: string | null;
  readonly created?: ItemCreateCreated | null;
  /** `YYYY-MM-DD` of today (the quick dates count from it); the clock when omitted. */
  readonly today?: string;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (draft: ItemCreateDraft) => void;
}

const NONE = "__none__";
const FIELD =
  "box-border w-full rounded-[9px] border border-line-strong bg-popover text-foreground outline-none placeholder:text-meta-foreground focus-visible:border-primary focus-visible:ring-3 focus-visible:ring-brand-subtle";

function fill(template: string, values: Record<string, string>) {
  return template.replace(
    /\{\{(\w+)\}\}/g,
    (_, key: string) => values[key] ?? "",
  );
}

/**
 * "Add item": the full create form of a board. The name is required; the
 * description, status, priority, people, dates and labels are optional and can
 * all be changed later. Enter in the name creates the item, Ctrl/Cmd+Enter
 * creates from any field, and with "create another" the form stays open with
 * the name and description cleared.
 */
export function ItemCreateDialog({
  copy,
  open,
  groups,
  defaultGroupId,
  hideGroup = false,
  statuses,
  defaultStatusId,
  defaultStartDay = "",
  defaultDueDay = "",
  defaultTitle = "",
  focusDescription = false,
  defaultAssigneeIds,
  currentUserId,
  context,
  boardSlot,
  unavailable = false,
  onAddGroup,
  addGroupPending = false,
  priorities,
  members,
  labels,
  pending,
  error,
  created,
  today: todayProp,
  onOpenChange,
  onSubmit,
}: ItemCreateDialogProps) {
  const ids = {
    name: useId(),
    nameError: useId(),
    description: useId(),
    group: useId(),
    status: useId(),
    priority: useId(),
    start: useId(),
    due: useId(),
    dueHint: useId(),
    dateError: useId(),
  };
  const nameRef = useRef<HTMLInputElement>(null);
  const descriptionRef = useRef<HTMLTextAreaElement>(null);
  const dueWrapRef = useRef<HTMLDivElement>(null);
  const startWrapRef = useRef<HTMLDivElement>(null);
  const [title, setTitle] = useState("");
  const [descriptionMd, setDescriptionMd] = useState("");
  const [listId, setListId] = useState(defaultGroupId);
  const [statusId, setStatusId] = useState<string | null>(defaultStatusId);
  const [priorityId, setPriorityId] = useState<string | null>(null);
  const [assigneeIds, setAssigneeIds] = useState<string[]>([]);
  const [labelIds, setLabelIds] = useState<string[]>([]);
  const [startDay, setStartDay] = useState("");
  const [dueDay, setDueDay] = useState("");
  const [startBad, setStartBad] = useState(false);
  const [dueBad, setDueBad] = useState(false);
  const [another, setAnother] = useState(false);
  const [touched, setTouched] = useState(false);
  const [stripGone, setStripGone] = useState(false);
  const today = todayProp ?? toDayString(new Date());

  const defaultAssigneeKey = (defaultAssigneeIds ?? []).join(",");

  // A fresh form each time it opens (the defaults follow the view that opened it).
  useEffect(() => {
    if (!open) return;
    setTitle(defaultTitle);
    setDescriptionMd("");
    setStartDay(defaultStartDay);
    setDueDay(defaultDueDay);
    setTouched(false);
    setStripGone(false);
    // `another` is kept: a person adding several items wants it to stay on.
  }, [open, defaultTitle, defaultStartDay, defaultDueDay]);

  // What belongs to one board follows the board, without losing what was typed.
  useEffect(() => {
    if (!open) return;
    setListId(defaultGroupId);
    setStatusId(defaultStatusId);
    setPriorityId(null);
    setAssigneeIds(defaultAssigneeKey ? defaultAssigneeKey.split(",") : []);
    setLabelIds([]);
  }, [open, defaultGroupId, defaultStatusId, defaultAssigneeKey]);

  // The cursor: the name, or the description when the name came from a quick add.
  useEffect(() => {
    if (!open) return;
    const frame = window.requestAnimationFrame(() => {
      (focusDescription && defaultTitle
        ? descriptionRef.current
        : nameRef.current
      )?.focus();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [open, focusDescription, defaultTitle]);

  // "Create another": the name and description clear, everything else stays.
  const lastNonce = useRef<number | null>(created?.nonce ?? null);
  useEffect(() => {
    if (!created || created.nonce === lastNonce.current) return;
    lastNonce.current = created.nonce;
    setStripGone(false);
    setTitle("");
    setDescriptionMd("");
    setTouched(false);
    nameRef.current?.focus();
  }, [created]);

  const trimmed = title.trim();
  const datesBackwards = startDay !== "" && dueDay !== "" && dueDay < startDay;
  const nameError = touched && trimmed === "" ? copy.nameRequired : null;
  const dateError = datesBackwards
    ? copy.dueBeforeStart
    : startBad || dueBad
      ? copy.dateInvalid
      : null;
  const submit = (event?: FormEvent) => {
    event?.preventDefault();
    setTouched(true);
    if (pending || unavailable) return;
    if (trimmed === "") {
      nameRef.current?.focus();
      return;
    }
    if (startBad || dueBad || datesBackwards) {
      const wrap = startBad ? startWrapRef : dueWrapRef;
      wrap.current?.querySelector("input")?.focus();
      return;
    }
    if (listId === "") {
      // The notice above the footer names the reason and offers the fix.
      return;
    }
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
  const onFormKeyDown = (event: KeyboardEvent<HTMLFormElement>) => {
    if (event.key === "Enter" && (event.metaKey || event.ctrlKey)) {
      event.preventDefault();
      submit();
    }
  };
  const blockedByGroup = !unavailable && listId === "";
  const blocker = unavailable
    ? copy.chooseBoard
    : blockedByGroup
      ? copy.noGroup
      : null;
  const hint = dueDay ? dueHint(copy, today, dueDay) : "";
  const contextLabel = context
    ? context.kind === "lane"
      ? copy.fromLane
      : context.kind === "quick"
        ? copy.fromQuickAdd
        : copy.fromOutside
    : "";
  const showStrip = Boolean(created) && another && !stripGone;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] gap-0 overflow-y-auto p-0 sm:max-w-[640px]">
        <form
          onSubmit={submit}
          onKeyDown={onFormKeyDown}
          noValidate
          className="flex flex-col"
        >
          <DialogHeader className="flex-row flex-wrap items-center gap-2.5 py-3.5 pr-12 pl-5.5 text-left">
            <DialogTitle className="text-lg font-semibold tracking-tight">
              {copy.title}
            </DialogTitle>
            {context ? (
              <span
                data-item-create-context=""
                className="inline-flex h-6.5 items-center gap-1.5 rounded-[7px] bg-state-hover-strong px-2.5 text-crumb font-medium text-muted-foreground"
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "size-2.25 shrink-0 rounded-full",
                    context.dotClassName ?? "bg-muted-foreground",
                  )}
                  style={context.dotStyle}
                />
                <span>
                  {contextLabel}{" "}
                  <b className="font-semibold text-foreground">
                    {context.name}
                  </b>
                </span>
              </span>
            ) : null}
            <DialogDescription className="sr-only">
              {copy.note}
            </DialogDescription>
          </DialogHeader>

          {showStrip && created ? (
            <div
              role="status"
              data-item-create-added=""
              className="mx-5.5 mt-0.5 flex min-h-11 items-center gap-2.5 rounded-[9px] bg-work-status-done-lane py-0 pr-1.5 pl-3 text-label text-foreground"
            >
              <CircleCheck
                aria-hidden="true"
                className="size-4.5 shrink-0 text-work-status-done"
              />
              <span className="min-w-0 flex-1 truncate">
                <b className="font-semibold">
                  {fill(copy.addedStrip, { title: created.title })}
                </b>
              </span>
              {created.onOpen ? (
                <button
                  type="button"
                  onClick={created.onOpen}
                  className="h-9 rounded-[7px] px-2.5 text-label font-semibold text-brand-subtle-foreground hover:bg-popover focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
                >
                  {copy.open}
                </button>
              ) : null}
              {created.onUndo ? (
                <button
                  type="button"
                  onClick={() => {
                    created.onUndo?.();
                    setStripGone(true);
                  }}
                  className="h-9 rounded-[7px] px-2.5 text-label font-semibold text-brand-subtle-foreground hover:bg-popover focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
                >
                  {copy.undo}
                </button>
              ) : null}
            </div>
          ) : null}

          <div className="flex flex-col gap-4 px-5.5 pt-2 pb-1">
            {boardSlot}

            <Field data-invalid={nameError ? true : undefined}>
              <FieldLabel htmlFor={ids.name}>
                {copy.name}{" "}
                <span aria-hidden="true" className="text-danger-text">
                  *
                </span>
              </FieldLabel>
              <Input
                ref={nameRef}
                id={ids.name}
                value={title}
                maxLength={500}
                placeholder={copy.namePlaceholder}
                aria-invalid={nameError ? true : undefined}
                aria-describedby={nameError ? ids.nameError : undefined}
                onChange={(event) => setTitle(event.target.value)}
                className="h-12 rounded-[9px] text-base font-medium"
              />
              {nameError ? (
                <FieldError id={ids.nameError}>{nameError}</FieldError>
              ) : null}
            </Field>

            <Field>
              <FieldLabel htmlFor={ids.description}>
                {copy.description}{" "}
                <span className="font-normal text-meta-foreground">
                  {copy.optional}
                </span>
              </FieldLabel>
              <textarea
                ref={descriptionRef}
                id={ids.description}
                rows={3}
                value={descriptionMd}
                maxLength={20000}
                placeholder={copy.descriptionPlaceholder}
                onChange={(event) => setDescriptionMd(event.target.value)}
                className={cn(
                  FIELD,
                  "block min-h-22 resize-y px-3 py-2.5 text-label leading-normal",
                )}
              />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              {groups.length > 1 && !hideGroup ? (
                <Field>
                  <FieldLabel htmlFor={ids.group}>{copy.group}</FieldLabel>
                  <Select
                    value={listId}
                    // The select reports "" while its options register: not a choice.
                    onValueChange={(value) => value && setListId(value)}
                  >
                    <SelectTrigger id={ids.group} className="h-10 w-full">
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
                      value && setStatusId(value === NONE ? null : value)
                    }
                  >
                    <SelectTrigger id={ids.status} className="h-10 w-full">
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
                  <FieldLabel htmlFor={ids.priority}>
                    {copy.priority}{" "}
                    <span className="font-normal text-meta-foreground">
                      {copy.optional}
                    </span>
                  </FieldLabel>
                  <Select
                    value={priorityId ?? NONE}
                    onValueChange={(value) =>
                      setPriorityId(value === NONE ? null : value)
                    }
                  >
                    <SelectTrigger id={ids.priority} className="h-10 w-full">
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
                <div ref={startWrapRef}>
                  <DayField
                    id={ids.start}
                    label={copy.startDate}
                    copy={copy}
                    value={startDay}
                    invalid={startBad || datesBackwards}
                    onChange={setStartDay}
                    onInvalidText={setStartBad}
                  />
                </div>
              </Field>
              <Field data-invalid={dueBad || datesBackwards ? true : undefined}>
                <FieldLabel htmlFor={ids.due}>{copy.dueDate}</FieldLabel>
                <div ref={dueWrapRef}>
                  <DayField
                    id={ids.due}
                    label={copy.dueDate}
                    copy={copy}
                    value={dueDay}
                    min={startDay || undefined}
                    invalid={dueBad || datesBackwards}
                    describedBy={`${ids.dueHint} ${ids.dateError}`}
                    onChange={setDueDay}
                    onInvalidText={setDueBad}
                  />
                </div>
              </Field>
            </div>
            <div className="-mt-2 flex flex-wrap items-center gap-1.5">
              <QuickDue
                copy={copy}
                today={today}
                value={dueDay}
                onPick={setDueDay}
              />
              {hint && !dateError ? (
                <span
                  id={ids.dueHint}
                  className="ml-auto text-crumb text-meta-foreground"
                >
                  {hint}
                </span>
              ) : (
                <span id={ids.dueHint} className="hidden" />
              )}
            </div>
            {dateError ? (
              <p
                id={ids.dateError}
                role="alert"
                className="-mt-2 m-0 text-crumb font-medium text-danger-text"
              >
                {dateError}
              </p>
            ) : (
              <span id={ids.dateError} className="hidden" />
            )}

            <PeoplePicker
              copy={copy}
              members={members}
              value={assigneeIds}
              currentUserId={currentUserId}
              onChange={setAssigneeIds}
            />

            <LabelPicker
              copy={copy}
              labels={labels}
              value={labelIds}
              onChange={setLabelIds}
            />

            {error ? (
              <p role="alert" className="m-0 text-crumb text-danger-text">
                {error}
              </p>
            ) : null}

            {blocker ? (
              <div
                role="alert"
                data-testid="item-create-blocker"
                className="flex flex-wrap items-center gap-2.5 rounded-[9px] border border-line bg-background px-3 py-2.5 text-crumb text-foreground"
              >
                <span className="min-w-0 flex-1">{blocker}</span>
                {blockedByGroup && onAddGroup ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={addGroupPending}
                    onClick={onAddGroup}
                  >
                    {addGroupPending ? copy.addingGroup : copy.addGroup}
                  </Button>
                ) : null}
              </div>
            ) : null}
          </div>

          <div className="mt-3 flex flex-wrap items-center gap-2.5 border-t border-line px-5.5 py-3.5">
            <label className="inline-flex min-h-11 cursor-pointer items-center gap-2 text-label text-foreground">
              <input
                type="checkbox"
                checked={another}
                onChange={(event) => setAnother(event.target.checked)}
                className="size-4.5 accent-primary"
              />
              {copy.another}
              {another ? (
                <small className="text-crumb text-meta-foreground">
                  · {copy.anotherKept}
                </small>
              ) : null}
            </label>
            <span className="flex-1" />
            <Button
              type="button"
              variant="outline"
              className="h-11 rounded-[9px] px-4"
              onClick={() => onOpenChange(false)}
            >
              {copy.cancel}
            </Button>
            <Button
              type="submit"
              className="h-11 rounded-[9px] px-4 font-semibold"
              disabled={pending || unavailable || blockedByGroup}
              title={blocker ?? undefined}
            >
              {pending ? copy.submitting : copy.submit}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
