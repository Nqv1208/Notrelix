import { useId, useState, type ReactNode } from "react";
import { Info, X } from "lucide-react";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import {
  BUTTON_CLASS,
  FIELD_LABEL_CLASS,
  INPUT_CLASS,
  TEXTAREA_CLASS,
} from "../board-settings/board-settings-parts";
import { fillFormsCopy, type FormsCopy } from "./forms.copy";

export interface ResponseItemValues {
  readonly title: string;
  readonly description: string;
  readonly groupId: string;
  readonly priorityOptionId: string | null;
  readonly statusOptionId: string | null;
  /** `YYYY-MM-DD` or "". */
  readonly startDay: string;
  /** `YYYY-MM-DD` or "". */
  readonly dueDay: string;
  readonly assigneeIds: readonly string[];
  readonly labelIds: readonly string[];
}

export interface ResponseItemDialogProps {
  readonly copy: FormsCopy;
  readonly boardTitle: string;
  /** "Prefilled from the response of ..." */
  readonly notice: string;
  readonly initial: ResponseItemValues;
  /** The question each prefilled field came from. */
  readonly from: {
    readonly title?: string;
    readonly description?: string;
    readonly priority?: string;
    readonly due?: string;
  };
  readonly groups: readonly { readonly id: string; readonly title: string }[];
  readonly priorities: readonly {
    readonly id: string;
    readonly label: string;
  }[];
  readonly statuses: readonly {
    readonly id: string;
    readonly label: string;
  }[];
  readonly members: readonly {
    readonly userId: string;
    readonly name: string;
  }[];
  readonly currentUserId?: string | null;
  readonly labels: readonly { readonly id: string; readonly name: string }[];
  /** Today as `YYYY-MM-DD` in the workspace time zone: the quick-due chips count from it. */
  readonly today: string;
  /** Answers no field takes. */
  readonly left: readonly string[];
  readonly pending: boolean;
  readonly failed: boolean;
  readonly onSubmit: (values: ResponseItemValues) => void;
  readonly onClose: () => void;
}

const FIELD_BOX = "rounded-[9px] px-3 text-sm";
const SELECT = cn(INPUT_CLASS, FIELD_BOX, "h-10 max-md:h-11");
const ROW = "grid grid-cols-2 gap-4 max-sm:grid-cols-1";
const LABEL = cn(
  FIELD_LABEL_CLASS,
  "flex flex-wrap items-center gap-1.5 text-[12.5px] leading-[1.45]",
);
const PILL =
  "inline-flex h-[30px] cursor-pointer items-center rounded-full border border-line-strong bg-popover px-2.5 text-[12.5px] font-medium text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring max-md:h-11";
const PILL_ON =
  "border-brand bg-brand-subtle text-brand-subtle-foreground aria-pressed:border-brand";
const CHIP_BOX =
  "inline-flex h-[30px] cursor-pointer items-center rounded-[6px] bg-state-hover px-2.5 text-[12.5px] font-semibold text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-pressed:bg-brand-subtle aria-pressed:text-brand-subtle-foreground max-md:h-11";

/** Today, tomorrow, the coming Friday and next week as `YYYY-MM-DD`, from a `YYYY-MM-DD` today. */
function quickDays(
  today: string,
  t: { today: string; tomorrow: string; friday: string; nextWeek: string },
) {
  const base = new Date(`${today}T00:00:00Z`);
  const plus = (days: number) => {
    const d = new Date(base);
    d.setUTCDate(d.getUTCDate() + days);
    return d.toISOString().slice(0, 10);
  };
  const friday = plus((5 - base.getUTCDay() + 7) % 7 || 7);
  const short = (day: string) => `${day.slice(8, 10)}/${day.slice(5, 7)}`;
  return [
    { label: t.today, day: plus(0) },
    { label: t.tomorrow, day: plus(1) },
    { label: fillFormsCopy(t.friday, { date: short(friday) }), day: friday },
    { label: t.nextWeek, day: plus(7) },
  ];
}

function Note({ children, role }: { children: ReactNode; role?: "note" }) {
  return (
    <div
      role={role}
      className="flex items-start gap-2 rounded-[9px] bg-state-hover px-3 py-2.5 text-[12.5px] leading-[1.45] text-muted-foreground"
    >
      <Info aria-hidden="true" className="mt-px size-4 shrink-0" />
      <span>{children}</span>
    </div>
  );
}

function Source({ copy, question }: { copy: FormsCopy; question?: string }) {
  if (!question) return null;
  return (
    <span className="inline-flex h-5 items-center rounded-[6px] bg-brand-subtle px-[7px] text-micro font-semibold whitespace-nowrap text-brand-subtle-foreground">
      {fillFormsCopy(copy.createFromResponse.from, { question })}
    </span>
  );
}

function Field({
  label,
  htmlFor,
  optional,
  source,
  children,
}: {
  label: string;
  htmlFor: string;
  optional?: string;
  source?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label
        htmlFor={htmlFor}
        className={cn(
          FIELD_LABEL_CLASS,
          "flex flex-wrap items-center gap-1.5 text-[12.5px] leading-[1.45]",
        )}
      >
        {label}
        {optional ? (
          <span className="font-normal text-meta-foreground">{optional}</span>
        ) : null}
        {source}
      </label>
      {children}
    </div>
  );
}

/**
 * The "Tạo việc từ phản hồi" dialog of WM-Forms-Inbox and WM-Forms-Dem: a 640 px card with the board it adds to,
 * a notice, the mapped fields prefilled (each with a "Từ: question" chip), a group picker, the answers no field
 * takes, and Huỷ / Tạo công việc. Presentational: the container creates the item and marks the response processed.
 */
export function ResponseItemDialog(props: ResponseItemDialogProps) {
  const { copy, initial, from, groups, priorities } = props;
  const t = copy.createFromResponse;
  const id = useId();
  const [values, setValues] = useState(initial);
  const [attempted, setAttempted] = useState(false);
  const blank = values.title.trim() === "";
  const toggle = (key: "assigneeIds" | "labelIds", value: string) =>
    setValues((v) => ({
      ...v,
      [key]: v[key].includes(value)
        ? v[key].filter((x) => x !== value)
        : [...v[key], value],
    }));
  const set = (patch: Partial<ResponseItemValues>) =>
    setValues((v) => ({ ...v, ...patch }));

  return (
    <Dialog
      open
      onOpenChange={(next) => !next && !props.pending && props.onClose()}
    >
      <DialogContent
        showCloseButton={false}
        className="max-h-[calc(100dvh-32px)] gap-0 overflow-y-auto rounded-[16px] bg-popover p-0 sm:max-w-[640px]"
      >
        <DialogHeader className="flex-row items-center gap-2.5 pt-3.5 pr-3 pb-2.5 pl-[22px]">
          <DialogTitle className="text-[18px] leading-[1.45] font-[650]">
            {t.title}
          </DialogTitle>
          <span className="inline-flex h-[26px] items-center rounded-[7px] bg-state-hover px-2.5 text-[12.5px] font-medium text-muted-foreground">
            {fillFormsCopy(t.addTo, { board: props.boardTitle })}
          </span>
          <DialogDescription className="sr-only">
            {props.notice}
          </DialogDescription>
          <span className="flex-1" />
          <DialogClose
            aria-label={t.close}
            disabled={props.pending}
            className="grid size-[30px] place-items-center rounded-[7px] text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring max-md:size-11 [&_svg]:size-4"
          >
            <X aria-hidden="true" />
          </DialogClose>
        </DialogHeader>
        <form
          className="flex flex-col"
          noValidate
          onSubmit={(e) => {
            e.preventDefault();
            if (props.pending) return;
            if (blank) {
              setAttempted(true);
              return;
            }
            props.onSubmit(values);
          }}
        >
          <div className="flex flex-col gap-4 px-[22px] pt-2 pb-1">
            <Note>{props.notice}</Note>
            <Field
              label={t.nameLabel}
              htmlFor={`${id}-t`}
              source={<Source copy={copy} question={from.title} />}
            >
              <input
                id={`${id}-t`}
                value={values.title}
                onChange={(e) => set({ title: e.target.value })}
                aria-invalid={attempted && blank}
                aria-describedby={attempted && blank ? `${id}-te` : undefined}
                required
                className={cn(
                  INPUT_CLASS,
                  FIELD_BOX,
                  "h-12 text-base font-medium max-md:h-12",
                )}
              />
              {attempted && blank ? (
                <span
                  id={`${id}-te`}
                  role="alert"
                  className="text-crumb text-danger-text"
                >
                  {t.nameRequired}
                </span>
              ) : null}
            </Field>
            <Field
              label={t.descriptionLabel}
              htmlFor={`${id}-d`}
              optional={t.optional}
              source={<Source copy={copy} question={from.description} />}
            >
              <textarea
                id={`${id}-d`}
                value={values.description}
                onChange={(e) => set({ description: e.target.value })}
                className={cn(
                  TEXTAREA_CLASS,
                  FIELD_BOX,
                  "min-h-16 py-[9px] max-md:min-h-16",
                )}
              />
            </Field>
            <div className={ROW}>
              <Field label={t.statusLabel} htmlFor={`${id}-s`}>
                <select
                  id={`${id}-s`}
                  value={values.statusOptionId ?? ""}
                  onChange={(e) =>
                    set({ statusOptionId: e.target.value || null })
                  }
                  className={SELECT}
                >
                  {props.statuses.map((o) => (
                    <option key={o.id} value={o.id}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </Field>
              {priorities.length > 0 ? (
                <Field
                  label={t.priorityLabel}
                  htmlFor={`${id}-p`}
                  optional={t.optional}
                  source={<Source copy={copy} question={from.priority} />}
                >
                  <select
                    id={`${id}-p`}
                    value={values.priorityOptionId ?? ""}
                    onChange={(e) =>
                      set({ priorityOptionId: e.target.value || null })
                    }
                    className={SELECT}
                  >
                    <option value="">{t.noPriority}</option>
                    {priorities.map((o) => (
                      <option key={o.id} value={o.id}>
                        {o.label}
                      </option>
                    ))}
                  </select>
                </Field>
              ) : null}
            </div>
            <div className={ROW}>
              <Field label={t.startLabel} htmlFor={`${id}-b`}>
                <input
                  id={`${id}-b`}
                  type="date"
                  value={values.startDay}
                  onChange={(e) => set({ startDay: e.target.value })}
                  className={SELECT}
                />
              </Field>
              <Field
                label={t.dueLabel}
                htmlFor={`${id}-u`}
                source={<Source copy={copy} question={from.due} />}
              >
                <input
                  id={`${id}-u`}
                  type="date"
                  value={values.dueDay}
                  onChange={(e) => set({ dueDay: e.target.value })}
                  className={SELECT}
                />
              </Field>
            </div>
            <div className="-mt-2 flex flex-wrap items-center gap-1.5">
              <span className="text-[12px] text-meta-foreground">
                {t.quickDue}
              </span>
              {quickDays(props.today, t).map((q) => (
                <button
                  key={q.label}
                  type="button"
                  aria-pressed={values.dueDay === q.day}
                  onClick={() => set({ dueDay: q.day })}
                  className={cn(PILL, values.dueDay === q.day && PILL_ON)}
                >
                  {q.label}
                </button>
              ))}
            </div>
            <div className="flex min-w-0 flex-col gap-1.5">
              <span className={LABEL}>
                {t.assigneeLabel}
                <span className="font-normal text-meta-foreground">
                  {t.optional}
                </span>
              </span>
              <div className="flex flex-wrap items-center gap-2">
                <select
                  aria-label={t.addPerson}
                  value=""
                  onChange={(e) => {
                    if (e.target.value) toggle("assigneeIds", e.target.value);
                  }}
                  className={cn(CHIP_BOX, "appearance-none")}
                >
                  <option value="">{t.addPerson}</option>
                  {props.members
                    .filter((m) => !values.assigneeIds.includes(m.userId))
                    .map((m) => (
                      <option key={m.userId} value={m.userId}>
                        {m.name}
                      </option>
                    ))}
                </select>
                {props.currentUserId ? (
                  <button
                    type="button"
                    aria-pressed={values.assigneeIds.includes(
                      props.currentUserId,
                    )}
                    onClick={() => toggle("assigneeIds", props.currentUserId!)}
                    className={CHIP_BOX}
                  >
                    {t.assignMe}
                  </button>
                ) : null}
                {values.assigneeIds
                  .filter((uid) => uid !== props.currentUserId)
                  .map((uid) => (
                    <button
                      key={uid}
                      type="button"
                      aria-pressed
                      onClick={() => toggle("assigneeIds", uid)}
                      className={cn(CHIP_BOX, PILL_ON)}
                    >
                      {props.members.find((m) => m.userId === uid)?.name ?? uid}
                    </button>
                  ))}
              </div>
            </div>
            {props.labels.length > 0 ? (
              <div className="flex min-w-0 flex-col gap-1.5">
                <span className={LABEL}>
                  {t.labelsLabel}
                  <span className="font-normal text-meta-foreground">
                    {t.optional}
                  </span>
                </span>
                <div className="flex flex-wrap gap-2">
                  {props.labels.map((l) => (
                    <button
                      key={l.id}
                      type="button"
                      aria-pressed={values.labelIds.includes(l.id)}
                      onClick={() => toggle("labelIds", l.id)}
                      className={cn(
                        PILL,
                        values.labelIds.includes(l.id) && PILL_ON,
                      )}
                    >
                      {l.name}
                    </button>
                  ))}
                </div>
              </div>
            ) : null}
            <Field label={t.groupLabel} htmlFor={`${id}-g`}>
              <select
                id={`${id}-g`}
                value={values.groupId}
                onChange={(e) => set({ groupId: e.target.value })}
                className={SELECT}
              >
                {groups.map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.title}
                  </option>
                ))}
              </select>
            </Field>
            {props.left.map((line) => (
              <Note key={line} role="note">
                {line}
              </Note>
            ))}
            {props.failed ? (
              <p role="alert" className="text-item-small text-danger-text">
                {t.failed}
              </p>
            ) : null}
          </div>
          <div className="mt-3 flex items-center justify-end gap-2.5 border-t border-line px-[22px] py-3.5 max-md:flex-col-reverse max-md:items-stretch">
            <button
              type="button"
              className={cn(BUTTON_CLASS.outline, "h-11 px-[18px] text-sm")}
              disabled={props.pending}
              onClick={props.onClose}
            >
              {t.cancel}
            </button>
            <button
              type="submit"
              aria-busy={props.pending}
              disabled={props.pending}
              className={cn(BUTTON_CLASS.primary, "h-11 px-[18px] text-sm")}
            >
              {t.submit}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
