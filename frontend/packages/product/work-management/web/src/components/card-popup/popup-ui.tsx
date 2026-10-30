import {
  forwardRef,
  type ButtonHTMLAttributes,
  type ComponentProps,
  type ReactNode,
} from "react";
import {
  Popover,
  PopoverAnchor,
  PopoverContent,
  Sheet,
  SheetContent,
  SheetTitle,
  cn,
  useMediaQuery,
} from "@notrelix/ui-web";
import {
  AlignLeft,
  LayoutList,
  Link2,
  ListChecks,
  Paperclip,
} from "lucide-react";
import type { CardMember } from "@notrelix/work-management-core";
import { identityMarkClass } from "../shared/item-display";

/**
 * The popup's own parts, sized to the artboard (px values are the artboard's).
 * Every part of the popup uses these so the buttons, chips, sections and
 * panels look like one family.
 */

const FOCUS =
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

/** `.ib`: a 36 px (32 px small) icon button. */
export const IconBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & { small?: boolean }
>(function IconBtn({ className, small, type = "button", ...props }, ref) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        "inline-grid shrink-0 place-items-center rounded-[8px] border-0 bg-transparent p-0 text-meta-foreground hover:bg-state-hover-strong hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50",
        small ? "size-11 md:size-8" : "size-11 md:size-9",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

const FIELD_BTN =
  "inline-flex h-11 items-center gap-[7px] rounded-[8px] border border-line-strong bg-popover px-3 text-[13.5px] md:h-9 font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong disabled:cursor-not-allowed disabled:opacity-50";

/** `.qb`: a button of the quick row. */
export const QuickBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & { on?: boolean }
>(function QuickBtn({ className, on, type = "button", ...props }, ref) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        FIELD_BTN,
        "md:h-8 md:px-2.5 md:text-[13px]",
        on && "bg-state-hover-strong",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

/** `.ch`: a chip of the chip row; `tone` paints it selected, set or read-only. */
export const ChipBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & {
    tone?: "default" | "on" | "ok";
  }
>(function ChipBtn(
  { className, tone = "default", type = "button", ...props },
  ref,
) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        FIELD_BTN,
        tone === "on" &&
          "border-primary bg-brand-subtle font-semibold text-brand-subtle-foreground",
        tone === "ok" &&
          "border-transparent bg-success-subtle font-semibold text-success-subtle-foreground",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

/** A chip that cannot be edited: dashed, no arrow. */
export function ChipReadOnly({ className, ...props }: ComponentProps<"span">) {
  return (
    <span
      className={cn(
        "inline-flex h-9 items-center gap-[7px] rounded-[8px] border border-dashed border-line-strong bg-background px-3 text-[13.5px] font-medium whitespace-nowrap text-muted-foreground",
        className,
      )}
      {...props}
    />
  );
}

/** `.sb`: a small outlined button of a section header. */
export const SmallBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & { on?: boolean }
>(function SmallBtn({ className, on, type = "button", ...props }, ref) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        "inline-flex h-11 items-center justify-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-3 text-[13px] font-medium text-foreground md:h-7 md:px-2.5 md:text-[12.5px] hover:bg-state-hover-strong disabled:cursor-not-allowed disabled:opacity-50",
        on && "bg-state-hover-strong ring-2 ring-primary",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

/** `.btn`: a 36 px button of a panel or a dialog. */
export const PanelBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & {
    variant?: "default" | "primary" | "danger" | "ghost";
    small?: boolean;
  }
>(function PanelBtn(
  { className, variant = "default", small, type = "button", ...props },
  ref,
) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        "inline-flex items-center justify-center gap-[7px] rounded-[8px] border text-center font-medium disabled:cursor-not-allowed disabled:opacity-55",
        small
          ? "h-11 px-3 text-[13px] md:h-8"
          : "h-11 px-3.5 text-[13.5px] md:h-9",
        variant === "default" &&
          "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong",
        variant === "primary" &&
          "border-primary bg-primary font-semibold text-primary-foreground hover:brightness-95",
        variant === "danger" &&
          "border-danger text-danger-text hover:bg-danger-subtle",
        variant === "ghost" &&
          "border-transparent bg-transparent text-muted-foreground hover:bg-state-hover-strong hover:text-foreground",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

/** `.fg`: a labelled group of the chip row. */
export function FieldGroup({
  label,
  className,
  children,
}: {
  label: ReactNode;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div className={cn("flex min-w-0 flex-col gap-1.5", className)}>
      <span className="text-[12px] leading-[18px] font-semibold whitespace-nowrap text-muted-foreground">
        {label}
      </span>
      {children}
    </div>
  );
}

/** `.sec`: a section of the left column with its icon in the gutter. */
export function PopSection({
  label,
  icon,
  className,
  children,
}: {
  label: string;
  icon: ReactNode;
  className?: string;
  children: ReactNode;
}) {
  return (
    <section
      aria-label={label}
      className={cn("relative pt-5 pr-4 pl-4 md:pr-6 md:pl-[60px]", className)}
    >
      <span
        aria-hidden="true"
        className="absolute top-[26px] left-6 hidden text-muted-foreground md:block"
      >
        {icon}
      </span>
      {children}
    </section>
  );
}

/** `.sh`: a section header, the title and its buttons. */
export function SectionHead({
  title,
  children,
}: {
  title: ReactNode;
  children?: ReactNode;
}) {
  return (
    <div className="flex min-h-11 items-center gap-2 md:min-h-8">
      <h3 className="m-0 flex-1 text-[16px] leading-[1.45] md:text-[15px] font-[650] tracking-[-0.005em]">
        {title}
      </h3>
      {children}
    </div>
  );
}

/** `.act`: the quiet action of a collapsed section ("+ Thêm"): link-coloured text, no frame. */
export const LinkBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement>
>(function LinkBtn({ className, type = "button", ...props }, ref) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        "inline-flex h-11 items-center gap-1.5 rounded-[6px] border-0 bg-transparent px-2.5 text-[13px] font-semibold text-brand-ink md:h-7 hover:bg-state-hover-strong disabled:opacity-50",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

/** `.sec.e`: a section with nothing in it, folded to one line: its name, "Chưa có gì" and its action. */
export function EmptySection({
  label,
  icon,
  none,
  children,
}: {
  label: string;
  icon: ReactNode;
  none: string;
  children?: ReactNode;
}) {
  return (
    <section
      aria-label={label}
      className="relative box-border flex min-h-[52px] items-center gap-2 pt-5 pr-4 pl-4 md:pr-6 md:pl-[60px]"
    >
      <span
        aria-hidden="true"
        className="absolute top-[26px] left-6 hidden text-muted-foreground md:block"
      >
        {icon}
      </span>
      <b className="text-[14px] leading-[1.45] font-[650]">{label}</b>
      <span className="text-[13.5px] leading-[1.45] text-meta-foreground">
        · {none}
      </span>
      <span className="flex-1" />
      {children}
    </section>
  );
}

/** `.pv`: the panel of a picker, a menu or a form that opens under a button. */
function PanelPopoverDesktop({
  trigger,
  anchor,
  open,
  onOpenChange,
  width = 320,
  align = "start",
  side,
  className,
  children,
  restoreFocus = true,
}: {
  trigger: ReactNode;
  /** A control elsewhere that opened the panel; it opens under it instead of under `trigger`. */
  anchor?: HTMLElement | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  width?: number;
  align?: "start" | "center" | "end";
  side?: "top" | "bottom";
  className?: string;
  children: ReactNode;
  restoreFocus?: boolean;
}) {
  return (
    <Popover open={open} onOpenChange={onOpenChange}>
      {trigger}
      {anchor ? <PopoverAnchor virtualRef={{ current: anchor }} /> : null}
      <PopoverContent
        align={
          // Under a control on the right half of the screen the panel grows leftward, over the card, not the comments.
          anchor &&
          anchor.getBoundingClientRect().left > window.innerWidth * 0.4
            ? "end"
            : align
        }
        side={side}
        sideOffset={4}
        style={{ width }}
        collisionPadding={12}
        tabIndex={-1}
        onOpenAutoFocus={(event) => {
          // A panel with a search or a name field puts the cursor there.
          const field = (
            event.currentTarget as HTMLElement
          ).querySelector<HTMLElement>("[data-picker-search],[data-autofocus]");
          event.preventDefault();
          // Without one the panel itself takes focus: no ring on the first item.
          (field ?? (event.currentTarget as HTMLElement)).focus();
        }}
        onInteractOutside={(event) => {
          // A press on the control that opened the panel is not a dismissal.
          if (anchor?.contains(event.target as Node)) event.preventDefault();
        }}
        onCloseAutoFocus={(event) => {
          if (anchor) {
            event.preventDefault();
            anchor.focus();
          } else if (!restoreFocus) event.preventDefault();
        }}
        className={cn(
          "z-modal gap-0 rounded-[12px] border border-line-strong bg-popover p-3 text-[14px] text-popover-foreground shadow-popover ring-0 outline-none",
          className,
        )}
      >
        {children}
      </PopoverContent>
    </Popover>
  );
}

/**
 * The panel of a picker or a menu: a popover under its button, and from the
 * phone width down a bottom sheet (the artboard's `.bsh`: 16 px top corners, a
 * grab handle, up to 94 % of the screen).
 */
export function PanelPopover(
  props: ComponentProps<typeof PanelPopoverDesktop>,
) {
  const phone = useMediaQuery("(max-width: 767px)");
  if (!phone) return <PanelPopoverDesktop {...props} />;
  const { trigger, open, onOpenChange, children } = props;
  return (
    <>
      <Popover open={false} onOpenChange={onOpenChange}>
        {trigger}
      </Popover>
      <Sheet open={open} onOpenChange={onOpenChange}>
        <SheetContent
          side="bottom"
          showCloseButton={false}
          aria-describedby={undefined}
          onOpenAutoFocus={(event) => {
            const sheet = event.currentTarget as HTMLElement;
            event.preventDefault();
            (
              sheet.querySelector<HTMLElement>(
                "[data-picker-search],[data-autofocus]",
              ) ?? sheet
            ).focus();
          }}
          className="max-h-[94%] gap-0 overflow-y-auto rounded-t-[16px] data-[side=bottom]:border-t-0 bg-popover px-4 pt-2 pb-6 text-[14px] text-popover-foreground"
        >
          <SheetTitle className="sr-only" />
          <div
            aria-hidden="true"
            className="mx-auto mb-2 h-1 w-10 rounded-[2px] bg-line-strong"
          />
          {children}
        </SheetContent>
      </Sheet>
    </>
  );
}

/** `.pv-h`: the title row of a panel, with an optional back and close. */
export function PanelHead({
  title,
  onBack,
  onClose,
  backLabel,
  closeLabel,
  icons,
}: {
  title: ReactNode;
  onBack?: () => void;
  onClose?: () => void;
  backLabel?: string;
  closeLabel?: string;
  icons: { back: ReactNode; close: ReactNode };
}) {
  return (
    <div className="relative mb-2 flex h-11 items-center justify-center md:h-8">
      {onBack ? (
        <IconBtn
          small
          aria-label={backLabel}
          onClick={onBack}
          className="absolute top-0 -left-1 size-11 md:size-8"
        >
          {icons.back}
        </IconBtn>
      ) : null}
      <b className="text-[14px] font-[650]">{title}</b>
      {onClose ? (
        <IconBtn
          small
          aria-label={closeLabel}
          onClick={onClose}
          className="absolute top-0 -right-1 size-11 md:size-8"
        >
          {icons.close}
        </IconBtn>
      ) : null}
    </div>
  );
}

/** `.in`: the 36 px text input of a panel. */
export const PanelInput = forwardRef<
  HTMLInputElement,
  ComponentProps<"input"> & { invalid?: boolean }
>(function PanelInput({ className, invalid, ...props }, ref) {
  return (
    <input
      ref={ref}
      className={cn(
        "box-border h-11 w-full rounded-[8px] border border-line-strong bg-popover px-3 text-[14px] text-foreground md:h-9 placeholder:text-meta-foreground focus:border-primary focus:ring-[3px] focus:ring-brand-subtle focus:outline-none",
        invalid && "border-danger ring-[3px] ring-danger-subtle",
        className,
      )}
      {...props}
    />
  );
});

/** `.av`: a person's mark. */
export function PersonMark({
  member,
  size = 32,
  className,
}: {
  member: Pick<CardMember, "userId" | "name" | "initials">;
  size?: 20 | 24 | 32 | 36;
  className?: string;
}) {
  const dim = {
    20: "size-5 text-[8.5px]",
    24: "size-6 text-[9.5px]",
    32: "size-8 text-[11px]",
    36: "size-9 text-[12px]",
  }[size];
  return (
    <span
      role="img"
      aria-label={member.name}
      title={member.name}
      className={cn(
        "inline-grid shrink-0 place-items-center rounded-full font-bold",
        dim,
        identityMarkClass(member.userId || member.name),
        className,
      )}
    >
      {member.initials || Array.from(member.name)[0]?.toUpperCase() || "?"}
    </span>
  );
}

/** Most marks the stack of assignees draws before it folds the rest into "+N". */
export const STACK_MAX = 3;

/**
 * `.stk`: the assignees as one stack: 24 px marks overlapping by 6 px, each with a 2 px ring of the panel's
 * colour, at most three and then a "+N" disc. Every mark names its person on hover; the disc names the rest.
 */
export function AvatarStack({
  members,
  className,
}: {
  members: readonly Pick<CardMember, "userId" | "name" | "initials">[];
  className?: string;
}) {
  const shown = members.slice(0, STACK_MAX);
  const rest = members.slice(STACK_MAX);
  return (
    <span className={cn("flex items-center", className)}>
      {shown.map((member, index) => (
        <PersonMark
          key={member.userId || member.name}
          member={member}
          size={24}
          className={cn("ring-2 ring-popover", index > 0 && "-ml-1.5")}
        />
      ))}
      {rest.length > 0 ? (
        <span
          role="img"
          aria-label={rest.map((member) => member.name).join(", ")}
          title={rest.map((member) => member.name).join(", ")}
          className="-ml-1.5 inline-grid size-6 shrink-0 place-items-center rounded-full bg-state-hover-strong text-[9.5px] font-bold text-muted-foreground ring-2 ring-popover"
        >
          +{rest.length}
        </span>
      ) : null}
    </span>
  );
}

/** The gutter icons of the sections. */
export const SECTION_ICONS = {
  description: <AlignLeft aria-hidden="true" className="size-5" />,
  fields: <LayoutList aria-hidden="true" className="size-5" />,
  checklist: <ListChecks aria-hidden="true" className="size-5" />,
  attachments: <Paperclip aria-hidden="true" className="size-5" />,
  links: <Link2 aria-hidden="true" className="size-5" />,
} as const;
