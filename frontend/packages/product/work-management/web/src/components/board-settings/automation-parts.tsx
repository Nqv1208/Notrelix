import {
  forwardRef,
  useEffect,
  useRef,
  useState,
  type ButtonHTMLAttributes,
  type ReactNode,
} from "react";
import { Check, ChevronDown, type LucideIcon } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import type { AutomationPersonDto } from "@notrelix/work-management-core";

/** Atoms of the automation editor and the run log. Classes are the Final-Automation rules. */

/** A chip of a rule sentence (mockup `.pc`): a choice the sentence is made of. */
export const Chip = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & {
    tone?: "plain" | "key" | "bad";
    icon?: ReactNode;
    caret?: boolean;
  }
>(function Chip(
  { tone = "plain", icon, caret = true, className, children, onClick, ...rest },
  ref,
) {
  return (
    <button
      ref={ref}
      type="button"
      onClick={onClick}
      {...rest}
      className={cn(
        "box-border inline-flex h-7 cursor-pointer items-center gap-[5px] rounded-[7px] border py-0 pr-[7px] pl-[9px] text-item-small font-semibold whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-expanded:shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--primary)] [&_svg]:size-[13px] [&_svg]:shrink-0",
        tone === "plain" &&
          "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong [&_svg]:text-meta-foreground",
        tone === "key" &&
          "border-transparent bg-brand-subtle text-brand-subtle-foreground [&_svg]:text-brand-subtle-foreground",
        tone === "bad" &&
          "border-danger bg-danger-subtle text-danger-text [&_svg]:text-danger-text",
        className,
      )}
    >
      {icon}
      {children}
      {caret ? <ChevronDown aria-hidden="true" /> : null}
    </button>
  );
});

/** A coloured mark with a person's initials (mockup `.av`). */
export function PersonDot({
  person,
  size = 20,
  className,
}: {
  person: Pick<AutomationPersonDto, "initials" | "color">;
  size?: 18 | 20 | 30;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      style={{ backgroundColor: person.color }}
      className={cn(
        "inline-grid shrink-0 place-items-center rounded-full font-bold text-white",
        size === 18 && "size-[18px] text-mark-xs",
        size === 20 && "size-5 text-mark-xs",
        size === 30 && "size-[30px] text-mark-md",
        className,
      )}
    >
      {person.initials}
    </span>
  );
}

const TAG_TONE = {
  brand: "bg-brand-subtle text-brand-subtle-foreground",
  ok: "bg-success-subtle text-success-text",
  warn: "bg-warning-subtle text-warning-text",
  err: "bg-danger-subtle text-danger-text",
  neutral:
    "bg-background text-muted-foreground shadow-[inset_0_0_0_1px_var(--line-strong)]",
} as const;

/** A status tag (mockup `.tag`). */
export function StatusTag({
  tone,
  icon: Icon,
  children,
}: {
  tone: keyof typeof TAG_TONE;
  icon?: LucideIcon;
  children: ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex h-[22px] shrink-0 items-center gap-[5px] rounded-[6px] px-2 text-item-note font-semibold whitespace-nowrap",
        TAG_TONE[tone],
      )}
    >
      {Icon ? <Icon aria-hidden="true" className="size-3" /> : null}
      {children}
    </span>
  );
}

export interface ChoiceItem {
  readonly id: string;
  readonly label: string;
  readonly hint?: string;
  readonly leading?: ReactNode;
}

export interface ChoiceGroup {
  readonly label?: string;
  readonly items: readonly ChoiceItem[];
}

/**
 * A chip that opens a list of choices under it (mockup `aria-haspopup=
 * "listbox"`): the arrow keys move, Enter chooses, Escape closes.
 */
export function ChoiceChip({
  label,
  leading,
  groups,
  currentId,
  empty,
  tone = "plain",
  invalid,
  describedBy,
  onSelect,
  ariaLabel,
  open,
  onOpenChange,
}: {
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  label: string;
  leading?: ReactNode;
  groups: readonly ChoiceGroup[];
  currentId: string | null;
  /** Said under the chip when there is nothing to choose from. */
  empty: string;
  tone?: "plain" | "key" | "bad";
  invalid?: boolean;
  describedBy?: string;
  onSelect: (id: string) => void;
  ariaLabel?: string;
}) {
  return (
    <ChoicePopover
      groups={groups}
      currentId={currentId}
      empty={empty}
      onSelect={onSelect}
      open={open}
      onOpenChange={onOpenChange}
      trigger={(isOpen) => (
        <Chip
          tone={tone}
          icon={leading}
          aria-haspopup="listbox"
          aria-expanded={isOpen}
          aria-invalid={invalid ? true : undefined}
          aria-describedby={describedBy}
          aria-label={ariaLabel}
        >
          {label}
        </Chip>
      )}
    />
  );
}

/** Any trigger that opens the list of choices under it. */
export function ChoicePopover({
  trigger,
  groups,
  currentId,
  empty,
  onSelect,
  open: controlledOpen,
  onOpenChange,
  align = "start",
}: {
  trigger: (open: boolean) => ReactNode;
  groups: readonly ChoiceGroup[];
  currentId: string | null;
  empty: string;
  onSelect: (id: string) => void;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  align?: "start" | "end";
}) {
  const [innerOpen, setInnerOpen] = useState(false);
  const open = controlledOpen ?? innerOpen;
  const setOpen = (next: boolean) => {
    setInnerOpen(next);
    onOpenChange?.(next);
  };
  const flat = groups.flatMap((group) => group.items);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>{trigger(open)}</PopoverTrigger>
      <PopoverContent
        align={align}
        sideOffset={6}
        className="flex max-h-[320px] w-auto min-w-[200px] max-w-[320px] flex-col gap-0 overflow-auto rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-lg ring-0"
      >
        <ChoiceList
          groups={groups}
          flat={flat}
          currentId={currentId}
          empty={empty}
          onSelect={(id) => {
            onSelect(id);
            setOpen(false);
          }}
        />
      </PopoverContent>
    </Popover>
  );
}

function ChoiceList({
  groups,
  flat,
  currentId,
  empty,
  onSelect,
}: {
  groups: readonly ChoiceGroup[];
  flat: readonly ChoiceItem[];
  currentId: string | null;
  empty: string;
  onSelect: (id: string) => void;
}) {
  const [active, setActive] = useState(
    Math.max(
      flat.findIndex((item) => item.id === currentId),
      0,
    ),
  );
  const listRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    listRef.current?.focus();
  }, []);
  if (flat.length === 0)
    return (
      <p className="m-0 px-2.5 py-2 text-item-small text-meta-foreground">
        {empty}
      </p>
    );
  let index = -1;
  return (
    <div
      ref={listRef}
      role="listbox"
      tabIndex={0}
      aria-activedescendant={`choice-${flat[active]?.id ?? ""}`}
      className="flex flex-col gap-0.5 outline-none"
      onKeyDown={(event) => {
        if (event.key === "ArrowDown") {
          event.preventDefault();
          setActive((value) => (value + 1) % flat.length);
        } else if (event.key === "ArrowUp") {
          event.preventDefault();
          setActive((value) => (value - 1 + flat.length) % flat.length);
        } else if (event.key === "Enter") {
          event.preventDefault();
          const item = flat[active];
          if (item) onSelect(item.id);
        }
      }}
    >
      {groups.map((group, groupIndex) => (
        <div key={group.label ?? groupIndex} className="flex flex-col gap-0.5">
          {group.label ? (
            <div className="px-2.5 pt-1.5 pb-1 text-item-note font-semibold text-meta-foreground">
              {group.label}
            </div>
          ) : null}
          {group.items.map((item) => {
            index += 1;
            const position = index;
            return (
              <div
                key={item.id}
                id={`choice-${item.id}`}
                role="option"
                aria-selected={item.id === currentId}
                onMouseEnter={() => setActive(position)}
                onClick={() => onSelect(item.id)}
                className={cn(
                  "flex min-h-8 cursor-pointer items-center gap-2 rounded-[7px] px-2.5 text-item-small text-foreground",
                  position === active && "bg-state-hover-strong",
                )}
              >
                {item.leading}
                <span className="min-w-0 flex-1">
                  <span className="block truncate">{item.label}</span>
                  {item.hint ? (
                    <span className="block text-item-note text-meta-foreground">
                      {item.hint}
                    </span>
                  ) : null}
                </span>
                {item.id === currentId ? (
                  <Check
                    aria-hidden="true"
                    className="size-3.5 text-brand-subtle-foreground"
                  />
                ) : null}
              </div>
            );
          })}
        </div>
      ))}
    </div>
  );
}

/** A plain button that can be a popover trigger (the popover gives it its click). */
export const TriggerButton = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement>
>(function TriggerButton({ onClick, children, ...rest }, ref) {
  return (
    <button ref={ref} type="button" onClick={onClick} {...rest}>
      {children}
    </button>
  );
});
