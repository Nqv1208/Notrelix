import {
  forwardRef,
  type ButtonHTMLAttributes,
  type ComponentProps,
  type ReactNode,
} from "react";
import {
  Popover,
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
        small ? "size-8" : "size-11 md:size-9",
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
      className={cn(FIELD_BTN, on && "bg-state-hover-strong", FOCUS, className)}
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
        "inline-flex h-8 items-center justify-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-3 text-[13px] font-medium text-foreground hover:bg-state-hover-strong disabled:cursor-not-allowed disabled:opacity-50",
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
        small ? "h-8 px-3 text-[13px]" : "h-9 px-3.5 text-[13.5px]",
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
      className={cn("relative pt-7 pr-4 pl-4 md:pr-6 md:pl-[60px]", className)}
    >
      <span
        aria-hidden="true"
        className="absolute top-[31px] left-6 hidden text-muted-foreground md:block"
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
    <div className="flex min-h-8 items-center gap-2">
      <h3 className="m-0 flex-1 text-[16px] font-[650] tracking-[-0.005em]">
        {title}
      </h3>
      {children}
    </div>
  );
}

/** `.pv`: the panel of a picker, a menu or a form that opens under a button. */
function PanelPopoverDesktop({
  trigger,
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
      <PopoverContent
        align={align}
        side={side}
        sideOffset={4}
        style={{ width }}
        collisionPadding={12}
        onOpenAutoFocus={(event) => {
          // A panel with a search or a name field puts the cursor there.
          const field = (
            event.currentTarget as HTMLElement
          ).querySelector<HTMLElement>("[data-picker-search],[data-autofocus]");
          if (!field) return;
          event.preventDefault();
          field.focus();
        }}
        onCloseAutoFocus={restoreFocus ? undefined : (e) => e.preventDefault()}
        className={cn(
          "z-modal gap-0 rounded-xl border border-line-strong bg-popover p-3 text-[14px] text-popover-foreground shadow-popover ring-0",
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
          className="max-h-[94%] gap-0 overflow-y-auto rounded-t-[16px] bg-popover px-4 pt-2 pb-6 text-[14px] text-popover-foreground"
        >
          <SheetTitle className="sr-only" />
          <div
            aria-hidden="true"
            className="mx-auto mb-2 h-1 w-10 rounded-sm bg-line-strong"
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
    <div className="relative mb-2 flex h-8 items-center justify-center">
      {onBack ? (
        <IconBtn
          small
          aria-label={backLabel}
          onClick={onBack}
          className="absolute top-0 -left-1"
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
          className="absolute top-0 -right-1"
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
        "box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover px-3 text-[14px] text-foreground placeholder:text-meta-foreground focus:border-primary focus:ring-[3px] focus:ring-brand-subtle focus:outline-none",
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

/** The gutter icons of the sections. */
export const SECTION_ICONS = {
  description: <AlignLeft aria-hidden="true" className="size-5" />,
  fields: <LayoutList aria-hidden="true" className="size-5" />,
  checklist: <ListChecks aria-hidden="true" className="size-5" />,
  attachments: <Paperclip aria-hidden="true" className="size-5" />,
  links: <Link2 aria-hidden="true" className="size-5" />,
} as const;
