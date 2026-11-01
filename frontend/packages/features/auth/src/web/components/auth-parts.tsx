import { useState, type ReactNode } from "react";
import { CircleAlert, Eye, EyeOff } from "lucide-react";
import { cn } from "@notrelix/ui-web";

/** Atoms the sign-in surfaces share. Classes follow the Auth mockup rules. */

export const INPUT_CLASS =
  "box-border h-11 w-full rounded-[9px] border border-input bg-popover px-3.25 text-doc-check text-foreground outline-none placeholder:text-meta-foreground focus:border-focus-ring focus:ring-[3px] focus:ring-brand-subtle aria-invalid:border-danger-text aria-invalid:ring-1 aria-invalid:ring-danger-text read-only:border-dashed read-only:bg-state-hover";

const BUTTON_BASE =
  "box-border inline-flex h-11 w-full cursor-pointer items-center justify-center gap-2.5 rounded-[9px] border px-3 text-item-prose font-semibold no-underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:border-state-disabled disabled:bg-state-disabled disabled:text-state-disabled-foreground";

export const BUTTON_CLASS = {
  primary: cn(
    BUTTON_BASE,
    "border-primary bg-primary text-primary-foreground hover:opacity-90",
  ),
  outline: cn(
    BUTTON_BASE,
    "border-line-strong bg-popover text-foreground hover:bg-state-hover",
  ),
} as const;

export const LINK_BUTTON_CLASS =
  "cursor-pointer rounded-[4px] border-0 bg-transparent px-0 py-0.5 text-item-ui font-medium text-brand-subtle-foreground outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring";

export function AuthHeading({
  title,
  lead,
  medium = false,
  titleId,
}: {
  title: ReactNode;
  lead?: ReactNode;
  /** The two-line title of the invitation and single sign-on pages. */
  medium?: boolean;
  titleId?: string;
}) {
  return (
    <div>
      <h1
        id={titleId}
        className={cn(
          "m-0 tracking-tight",
          medium
            ? "text-page-title leading-snug font-semibold"
            : "text-item-title leading-tight font-semibold",
        )}
      >
        {title}
      </h1>
      {lead ? (
        <p className="mt-1.5 mb-0 text-item-prose leading-normal text-muted-foreground">
          {lead}
        </p>
      ) : null}
    </div>
  );
}

export function FieldError({
  id,
  children,
}: {
  id?: string;
  children: ReactNode;
}) {
  return (
    <p
      id={id}
      role="alert"
      className="m-0 flex items-start gap-1.5 text-item-small font-medium text-danger-text"
    >
      <CircleAlert aria-hidden="true" className="mt-0.5 size-3.75 shrink-0" />
      {children}
    </p>
  );
}

/** A password field with the eye that shows it. */
export function PasswordField({
  id,
  value,
  onChange,
  invalid,
  describedBy,
  autoComplete,
  showLabel,
  hideLabel,
  autoFocus,
  disabled,
}: {
  id: string;
  value: string;
  onChange: (value: string) => void;
  invalid?: boolean;
  describedBy?: string;
  autoComplete: "current-password" | "new-password";
  showLabel: string;
  hideLabel: string;
  autoFocus?: boolean;
  disabled?: boolean;
}) {
  const [shown, setShown] = useState(false);
  return (
    <div className="relative">
      <input
        id={id}
        name="password"
        type={shown ? "text" : "password"}
        value={value}
        autoComplete={autoComplete}
        autoFocus={autoFocus}
        disabled={disabled}
        aria-invalid={invalid || undefined}
        aria-describedby={describedBy}
        onChange={(event) => onChange(event.target.value)}
        className={cn(INPUT_CLASS, "pr-13")}
      />
      <button
        type="button"
        aria-label={shown ? hideLabel : showLabel}
        aria-pressed={shown}
        onClick={() => setShown((open) => !open)}
        className="absolute top-1 right-1 grid size-9 cursor-pointer max-md:top-0 max-md:right-0 max-md:size-touch place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        {shown ? (
          <EyeOff aria-hidden="true" className="size-4.5" />
        ) : (
          <Eye aria-hidden="true" className="size-4.5" />
        )}
      </button>
    </div>
  );
}
