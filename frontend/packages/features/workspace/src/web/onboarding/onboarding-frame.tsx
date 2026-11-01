import type { ReactNode } from "react";
import { Check } from "lucide-react";
import { NotrelixLogoMark, cn } from "@notrelix/ui-web";
import type { OnboardingCopy } from "./onboarding.copy";
import { fillOnboarding } from "./fill-copy";
import { ONBOARDING_STEPS, type OnboardingStepId } from "./onboarding-models";

/** Atoms and the frame of the onboarding pages (the Onboarding mockup). */

export const FIELD_INPUT_CLASS =
  "box-border h-10 w-full rounded-[8px] border border-input bg-popover px-3 text-body text-foreground outline-none placeholder:text-meta-foreground focus:border-focus-ring focus:ring-[3px] focus:ring-brand-subtle aria-invalid:border-danger-text aria-invalid:ring-1 aria-invalid:ring-danger-text";

export const BUTTON_PRIMARY_CLASS =
  "inline-flex h-10 cursor-pointer items-center justify-center gap-2 rounded-[8px] border border-primary bg-primary px-4.5 text-body font-semibold text-primary-foreground outline-none hover:opacity-90 focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:border-state-disabled disabled:bg-state-disabled disabled:text-state-disabled-foreground";

export const BUTTON_OUTLINE_CLASS =
  "inline-flex h-10 cursor-pointer items-center justify-center gap-2 rounded-[8px] border border-line-strong bg-popover px-4 text-body font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-50";

export const BUTTON_QUIET_CLASS =
  "inline-flex h-10 cursor-pointer items-center justify-center rounded-[8px] border-0 bg-transparent px-3 text-body font-medium text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";

export const OPTION_CLASS =
  "inline-flex h-10 cursor-pointer items-center gap-2.5 rounded-[8px] border border-line-strong bg-popover px-3.5 text-body font-medium text-foreground has-[:checked]:border-primary has-[:checked]:bg-brand-subtle has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-focus-ring";

export function FieldLabel({
  htmlFor,
  children,
}: {
  htmlFor?: string;
  children: ReactNode;
}) {
  return (
    <label htmlFor={htmlFor} className="text-item-ui font-semibold">
      {children}
    </label>
  );
}

export function Hint({ id, children }: { id?: string; children: ReactNode }) {
  return (
    <p id={id} className="m-0 text-compact leading-normal text-meta-foreground">
      {children}
    </p>
  );
}

export function StepTitle({
  title,
  lead,
  id,
}: {
  title: ReactNode;
  lead?: ReactNode;
  id?: string;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <h1 id={id} className="m-0 text-item-title leading-tight tracking-tight">
        {title}
      </h1>
      {lead ? (
        <p className="m-0 max-w-[60ch] text-item-prose leading-normal text-muted-foreground">
          {lead}
        </p>
      ) : null}
    </div>
  );
}

/** The four steps as a bar: the ones before the current are ticked. */
export function StepBar({
  copy,
  current,
}: {
  copy: OnboardingCopy;
  current: OnboardingStepId;
}) {
  const index =
    current === "done"
      ? ONBOARDING_STEPS.length
      : ONBOARDING_STEPS.indexOf(current);
  return (
    <ol
      aria-label={copy.frame.progress}
      className="m-0 grid list-none grid-cols-4 gap-2 p-0"
    >
      {ONBOARDING_STEPS.map((id, position) => {
        const state =
          position < index ? "done" : position === index ? "current" : "todo";
        return (
          <li
            key={id}
            aria-current={state === "current" ? "step" : undefined}
            className="flex flex-col gap-2.5"
          >
            <span
              aria-hidden="true"
              className={cn(
                "h-1 rounded-full",
                state === "todo" ? "bg-line-strong" : "bg-primary",
              )}
            />
            <span
              className={cn(
                "flex items-center gap-2 text-item-ui",
                state === "current"
                  ? "font-semibold text-foreground"
                  : "font-medium text-muted-foreground",
              )}
            >
              <span
                aria-hidden="true"
                className={cn(
                  "grid size-4.5 shrink-0 place-items-center rounded-full text-micro font-semibold",
                  state === "todo"
                    ? "border border-line-strong text-muted-foreground"
                    : "bg-primary text-primary-foreground",
                )}
              >
                {state === "done" ? (
                  <Check className="size-3" strokeWidth={3} />
                ) : (
                  position + 1
                )}
              </span>
              {/* On a phone only the current step names itself; the others keep their number. */}
              <span
                className={state === "current" ? undefined : "max-sm:sr-only"}
              >
                {copy.frame.steps[id]}
              </span>
              <span className="sr-only">
                ,{" "}
                {state === "done"
                  ? copy.frame.srDone
                  : state === "current"
                    ? copy.frame.srCurrent
                    : copy.frame.srTodo}
              </span>
            </span>
          </li>
        );
      })}
    </ol>
  );
}

export interface OnboardingFrameProps {
  readonly copy: OnboardingCopy;
  /** "Bước 1/4" or "Lời mời đã nhận". */
  readonly stepLabel: string;
  /** "Tạo workspace". */
  readonly stepName: string;
  readonly email: string;
  /** The button at the right of the header; absent on the last page. */
  readonly exit?: {
    readonly label: string;
    readonly onClick: () => void;
    readonly disabled?: boolean;
  };
  readonly children: ReactNode;
  readonly aside: ReactNode;
  readonly footer: ReactNode;
}

export function OnboardingFrame({
  copy,
  stepLabel,
  stepName,
  email,
  exit,
  children,
  aside,
  footer,
}: OnboardingFrameProps) {
  return (
    <div className="flex min-h-screen flex-col bg-popover text-foreground">
      <header className="flex h-14 shrink-0 items-center gap-3 border-b border-line bg-popover px-4 sm:gap-4 sm:px-6">
        <span className="flex items-center gap-2.5">
          <NotrelixLogoMark className="h-auto w-6.5" aria-hidden="true" />
          <b className="text-dialog-title font-bold tracking-tight">
            {copy.frame.brand}
          </b>
        </span>
        <span
          aria-hidden="true"
          className="hidden h-5.5 w-px bg-line sm:block"
        />
        <span className="text-item-prose whitespace-nowrap text-muted-foreground">
          <b className="font-semibold text-foreground">{stepLabel}</b>
          <span className="max-sm:sr-only">
            {" · "}
            {stepName}
          </span>
        </span>
        <span className="flex-1" />
        <span className="hidden text-item-ui text-muted-foreground sm:inline">
          {email}
        </span>
        {exit ? (
          <button
            type="button"
            disabled={exit.disabled}
            onClick={exit.onClick}
            className="inline-flex h-9 shrink-0 cursor-pointer items-center rounded-[8px] border border-line-strong bg-popover px-3.5 text-item-ui font-medium whitespace-nowrap text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-50 max-md:h-touch"
          >
            {exit.label}
          </button>
        ) : null}
      </header>
      <div className="grid min-h-0 flex-1 lg:grid-cols-[minmax(0,1fr)_29.25rem]">
        <main className="flex min-w-0 flex-col px-6 lg:px-0">
          <div className="mx-auto flex w-full max-w-160 flex-1 flex-col gap-5 pt-7 pb-8">
            {children}
          </div>
          <div className="mx-auto flex w-full max-w-160 items-center justify-between gap-3 border-t border-line py-4">
            {footer}
          </div>
        </main>
        <aside
          aria-label={copy.preview.label}
          className="relative hidden flex-col overflow-hidden border-l border-line bg-muted lg:flex"
        >
          <span
            aria-hidden="true"
            className="h-1 w-full bg-linear-to-r from-identity-ember via-identity-gold via-50% to-identity-blue"
          />
          <div className="flex flex-1 flex-col justify-center gap-3 px-10 pb-10">
            {aside}
          </div>
        </aside>
      </div>
    </div>
  );
}

export function fillCopy(
  message: string,
  values: Record<string, string | number>,
): string {
  return fillOnboarding(message, values);
}
