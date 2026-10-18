import {
  cloneElement,
  useId,
  type MouseEvent,
  type ReactElement,
  type ReactNode,
} from "react";
import { Tooltip, TooltipContent, TooltipTrigger, cn } from "@notrelix/ui-web";
import type { CardMember } from "@notrelix/work-management-core";
import { identityMarkClass } from "../shared/item-display";
import type { ItemSheetMarkTone } from "../card-detail/task-detail-types";

/** Atoms the item page sections share. Classes are the Final-Detail rules. */

/** 32 px icon button of the top bar and the file list (mockup `.icon-btn`). */
export const ICON_BUTTON_CLASS =
  "grid size-8 shrink-0 place-items-center rounded-[7px] border-0 bg-transparent p-0 text-meta-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:opacity-40 aria-disabled:hover:bg-transparent aria-disabled:hover:text-meta-foreground [&_svg]:size-4 [&_svg]:shrink-0";

const BUTTON_SM_CLASS =
  "inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[7px] border px-2.5 text-crumb font-medium whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:opacity-50 [&_svg]:shrink-0";

/** 28 px buttons (mockup `.btn.sm`). */
export const BUTTON_SM_VARIANT_CLASS = {
  outline: cn(
    BUTTON_SM_CLASS,
    "border-line-strong bg-popover text-foreground hover:bg-state-hover-strong aria-disabled:hover:bg-popover",
  ),
  primary: cn(
    BUTTON_SM_CLASS,
    "border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90",
  ),
  ghost: cn(
    BUTTON_SM_CLASS,
    "border-transparent bg-transparent text-muted-foreground hover:bg-state-hover-strong hover:text-foreground aria-disabled:hover:bg-transparent aria-disabled:hover:text-muted-foreground",
  ),
} as const;

/** Section heading with its count (mockup `.sec h2`). */
export function SectionHeading({
  id,
  children,
  count,
  action,
}: {
  id: string;
  children: ReactNode;
  count?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <h2
      id={id}
      className="m-0 mb-2.5 flex items-center gap-2 text-item-heading text-foreground"
    >
      {children}
      {count !== undefined ? (
        <span className="text-item-small font-normal text-meta-foreground">
          {count}
        </span>
      ) : null}
      {action ? <span className="ml-auto">{action}</span> : null}
    </h2>
  );
}

/**
 * A control that cannot work yet: it stays in the tab order with
 * `aria-disabled`, says why in a tooltip and to assistive technology, and
 * never acts. The child is the control element.
 */
export function DisabledControl({
  reason,
  children,
}: {
  reason: string;
  children: ReactElement<{
    "aria-disabled"?: boolean | "true" | "false";
    "aria-describedby"?: string;
    onClick?: (event: MouseEvent) => void;
  }>;
}) {
  const reasonId = useId();
  return (
    <>
      <Tooltip>
        <TooltipTrigger asChild>
          {cloneElement(children, {
            "aria-disabled": "true",
            "aria-describedby": reasonId,
            onClick: (event: MouseEvent) => event.preventDefault(),
          })}
        </TooltipTrigger>
        <TooltipContent>{reason}</TooltipContent>
      </Tooltip>
      <span id={reasonId} className="sr-only">
        {reason}
      </span>
    </>
  );
}

/**
 * A link that takes plain clicks to `onNavigate` (the host's router) and
 * leaves modified clicks and middle clicks to the browser. Without an `href`
 * it is plain text.
 */
export function PageLink({
  href,
  onNavigate,
  className,
  children,
  ...rest
}: {
  href?: string;
  onNavigate?: () => void;
  className?: string;
  children: ReactNode;
  "aria-label"?: string;
  "aria-current"?: "page";
}) {
  if (!href && !onNavigate)
    return <span className={className}>{children}</span>;
  return (
    <a
      {...rest}
      href={href}
      className={className}
      onClick={(event) => {
        if (
          !onNavigate ||
          event.defaultPrevented ||
          event.button !== 0 ||
          event.metaKey ||
          event.ctrlKey ||
          event.shiftKey ||
          event.altKey
        )
          return;
        event.preventDefault();
        onNavigate();
      }}
    >
      {children}
    </a>
  );
}

/** A person's mark: initials on a stable identity tone. */
export function PersonMark({
  person,
  className,
  title,
}: {
  person: Pick<CardMember, "id" | "userId" | "initials">;
  /** Size and initials role, e.g. `size-5.5 text-mark-xs`. */
  className: string;
  title?: string;
}) {
  return (
    <span
      title={title}
      className={cn(
        "grid shrink-0 place-items-center rounded-full",
        className,
        identityMarkClass(person.userId || person.id),
      )}
    >
      {person.initials}
    </span>
  );
}

/** Department mark tones (literal classes so Tailwind generates them). */
export const MARK_TONE_CLASS: Readonly<Record<ItemSheetMarkTone, string>> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};
