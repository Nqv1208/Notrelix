import {
  AlignLeft,
  CalendarDays,
  ChartLine,
  ChartPie,
  ChartNoAxesGantt,
  Clock,
  SquareKanban,
  Table2,
  CircleCheck,
  CircleChevronDown,
  Download,
  Flag,
  Hash,
  Link2,
  Sigma,
  SquareCheck,
  Tag,
  Type,
  User,
  type LucideIcon,
} from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import type {
  BoardSettingsFieldType,
  BoardSettingsViewDto,
} from "@notrelix/work-management-core";

/** Atoms the board settings sections share. Classes are the Final-Settings rules. */

/** 32 px button (mockup `.btn`). */
const BTN =
  "inline-flex h-8 shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border px-3 max-md:h-11 text-item-small font-medium whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:opacity-50 disabled:cursor-not-allowed disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0";

export const BUTTON_CLASS = {
  outline: cn(
    BTN,
    "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong",
  ),
  primary: cn(
    BTN,
    "border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90",
  ),
  danger: cn(
    BTN,
    "border-danger bg-danger font-semibold text-danger-foreground hover:opacity-90",
  ),
  dangerOutline: cn(
    BTN,
    "border-danger bg-popover text-danger-text hover:bg-state-hover-strong",
  ),
  ghost: cn(
    BTN,
    "border-transparent bg-transparent text-muted-foreground hover:bg-state-hover-strong hover:text-foreground",
  ),
  ghostDanger: cn(
    BTN,
    "border-transparent bg-transparent text-danger-text hover:bg-state-hover-strong",
  ),
  ghostBrand: cn(
    BTN,
    "h-7 border-transparent bg-transparent px-2 text-brand-subtle-foreground hover:bg-state-hover-strong",
  ),
} as const;

/** Text input and select (mockup `.inp`). */
export const INPUT_CLASS =
  "box-border flex h-[34px] max-md:h-11 w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-item-ui text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger disabled:cursor-not-allowed disabled:bg-background disabled:text-meta-foreground";

export const TEXTAREA_CLASS = cn(
  INPUT_CLASS,
  "block h-auto min-h-14 resize-y py-[7px] leading-[1.45]",
);

export const ICON_BUTTON_CLASS =
  "grid size-7.5 max-md:size-11 shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent p-0 text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:opacity-40 [&_svg]:size-3.5 [&_svg]:shrink-0";

export const FIELD_LABEL_CLASS =
  "text-crumb font-semibold text-muted-foreground";
export const HINT_CLASS = "text-metadata leading-[1.45] text-meta-foreground";

const TYPE_ICON: Readonly<Record<BoardSettingsFieldType, LucideIcon>> = {
  text: Type,
  longtext: AlignLeft,
  number: Hash,
  date: CalendarDays,
  checkbox: SquareCheck,
  file: Download,
  select: CircleChevronDown,
  multiselect: Tag,
  status: CircleCheck,
  person: User,
  relation: Link2,
  formula: Sigma,
  rollup: ChartLine,
  progress: ChartPie,
  autonumber: Hash,
  link: Link2,
  "datetime-system": Clock,
};

/** The icon of a field: the flag for the priority, else the one of its type. */
export function fieldIcon(
  type: BoardSettingsFieldType,
  key?: string,
): LucideIcon {
  return key === "priority" ? Flag : TYPE_ICON[type];
}

/** The 24 px (or 30 px) square that holds a type icon (mockup `.ty`). */
export function TypeMark({
  icon: Icon,
  size = "sm",
  className,
}: {
  icon: LucideIcon;
  size?: "sm" | "md";
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center border border-line bg-background text-muted-foreground",
        size === "sm" ? "size-6.5 rounded-[6px]" : "size-8 rounded-lg",
        className,
      )}
    >
      <Icon className={size === "sm" ? "size-3.5" : "size-4"} />
    </span>
  );
}

/** The small coloured square of an option or a group (mockup `.sw-c`). */
export function ColorSquare({
  color,
  className,
}: {
  color: string;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn("size-3.5 shrink-0 rounded-[4px]", className)}
      style={{ backgroundColor: color }}
    />
  );
}

/** Section line over a list: title, count and a right-hand note (mockup `.sec`). */
export function SectionLine({
  title,
  count,
  note,
}: {
  title: ReactNode;
  count?: ReactNode;
  note?: ReactNode;
}) {
  return (
    <div className="mt-[18px] mb-1.5 flex flex-wrap items-center gap-x-2 text-crumb font-semibold text-meta-foreground">
      {title}
      {count !== undefined ? (
        <span className="font-medium">{count}</span>
      ) : null}
      {note ? <span className="ml-auto font-normal">{note}</span> : null}
    </div>
  );
}

/** Heading of a section: title, intro and the primary action (mockup `.lh`). */
export function SectionHeader({
  id,
  title,
  intro,
  actions,
}: {
  id: string;
  title: string;
  intro: string;
  actions?: ReactNode;
}) {
  return (
    <div className="mb-3.5 flex items-start gap-3 max-md:flex-wrap">
      <div className="max-md:min-w-0 max-md:basis-full">
        <h2 id={id} className="m-0 text-[17px] leading-[1.45] font-[650]">
          {title}
        </h2>
        <p className="mt-0.5 mb-0 max-w-[62ch] text-item-small text-meta-foreground">
          {intro}
        </p>
      </div>
      <span className="flex-1" />
      <div className="flex shrink-0 items-center gap-2">{actions}</div>
    </div>
  );
}

/** A note box, neutral or warning (mockup `.callout`). */
export function Callout({
  icon: Icon,
  tone = "neutral",
  role,
  className,
  children,
}: {
  icon: LucideIcon;
  tone?: "neutral" | "warning";
  role?: "alert" | "status";
  className?: string;
  children: ReactNode;
}) {
  return (
    <div
      role={role}
      className={cn(
        "flex gap-[9px] rounded-lg px-3 py-2.5 text-crumb leading-normal",
        tone === "warning"
          ? "bg-warning-subtle text-foreground"
          : "border border-line bg-background text-muted-foreground",
        className,
      )}
    >
      <Icon
        aria-hidden="true"
        className={cn(
          "mt-0.5 size-3.5 shrink-0",
          tone === "warning" ? "text-warning-text" : undefined,
        )}
      />
      {children}
    </div>
  );
}

/** A switch (mockup `.sw`): a 34 x 20 track with a 16 px thumb. */
export function SwitchControl({
  checked,
  onChange,
  label,
  disabled,
  describedBy,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  label: string;
  disabled?: boolean;
  describedBy?: string;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      aria-describedby={describedBy}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative h-5 w-[34px] shrink-0 cursor-pointer rounded-full border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-50",
        checked ? "bg-primary" : "bg-line-strong",
        "after:absolute after:top-0.5 after:size-4 after:rounded-full after:bg-white after:shadow-[0_1px_2px_rgb(0_0_0/0.2)] after:transition-[left] after:content-['']",
        checked ? "after:left-4" : "after:left-0.5",
      )}
    />
  );
}

/** A small tag (mockup `.tag`). */
export function Tag_({
  tone = "brand",
  children,
}: {
  tone?: "brand" | "warning";
  children: ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex h-5 items-center rounded-[5px] px-[7px] text-[11.5px] font-semibold whitespace-nowrap",
        tone === "brand"
          ? "bg-brand-subtle text-brand-subtle-foreground"
          : "bg-warning-subtle text-warning-text",
      )}
    >
      {children}
    </span>
  );
}

/** Role of a labelled form row: label above a control with an optional hint. */
export function FormGroup({
  label,
  htmlFor,
  hint,
  children,
}: {
  label: ReactNode;
  htmlFor?: string;
  hint?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      {htmlFor ? (
        <label htmlFor={htmlFor} className={FIELD_LABEL_CLASS}>
          {label}
        </label>
      ) : (
        <span className={FIELD_LABEL_CLASS}>{label}</span>
      )}
      {children}
      {hint ? <span className={HINT_CLASS}>{hint}</span> : null}
    </div>
  );
}

/** The icon of a saved view by its kind. */
export const VIEW_ICON: Readonly<
  Record<BoardSettingsViewDto["kind"], LucideIcon>
> = {
  table: Table2,
  kanban: SquareKanban,
  calendar: CalendarDays,
  timeline: ChartNoAxesGantt,
};
