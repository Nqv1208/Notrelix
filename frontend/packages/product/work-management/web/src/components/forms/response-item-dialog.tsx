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
  /** `YYYY-MM-DD` or "". */
  readonly dueDay: string;
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
  /** Answers no field takes. */
  readonly left: readonly string[];
  readonly pending: boolean;
  readonly failed: boolean;
  readonly onSubmit: (values: ResponseItemValues) => void;
  readonly onClose: () => void;
}

const FIELD_BOX = "rounded-[9px] px-3 text-sm";

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
  const set = (patch: Partial<ResponseItemValues>) =>
    setValues((v) => ({ ...v, ...patch }));

  return (
    <Dialog
      open
      onOpenChange={(next) => !next && !props.pending && props.onClose()}
    >
      <DialogContent
        showCloseButton={false}
        className="max-h-[calc(100dvh-32px)] gap-0 overflow-y-auto rounded-2xl bg-popover p-0 sm:max-w-[640px]"
      >
        <DialogHeader className="flex-row items-center gap-2.5 pt-3.5 pr-3 pb-2.5 pl-[22px]">
          <DialogTitle className="text-[18px] leading-[1.3] font-[650]">
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
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label={t.groupLabel} htmlFor={`${id}-g`}>
                <select
                  id={`${id}-g`}
                  value={values.groupId}
                  onChange={(e) => set({ groupId: e.target.value })}
                  className={cn(INPUT_CLASS, FIELD_BOX, "h-10 max-md:h-11")}
                >
                  {groups.map((g) => (
                    <option key={g.id} value={g.id}>
                      {g.title}
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
                    className={cn(INPUT_CLASS, FIELD_BOX, "h-10 max-md:h-11")}
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
              <Field
                label={t.dueLabel}
                htmlFor={`${id}-u`}
                optional={t.optional}
                source={<Source copy={copy} question={from.due} />}
              >
                <input
                  id={`${id}-u`}
                  type="date"
                  value={values.dueDay}
                  onChange={(e) => set({ dueDay: e.target.value })}
                  className={cn(INPUT_CLASS, FIELD_BOX, "h-10 max-md:h-11")}
                />
              </Field>
            </div>
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
