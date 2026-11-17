import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { BTN_PRIMARY, BTN_SM, FOCUS, INPUT } from "../shared/account-parts";
import { fillCopy } from "../shared/account-surface-model";
import type { AccountSecurityCopy } from "./account-security.copy";

/** What the last attempt ended in; `null` = nothing yet. */
export type AccountEmailError = "wait" | "failed";

const LOOKS_LIKE_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Asking for a new sign-in email, the proof of presence already in hand. What
 * the page can see (a malformed address, the address already in use for
 * sign-in) is told before anything is sent; the producer answers the same for
 * every other address.
 */
export function AccountEmailPanel({
  copy,
  currentEmail,
  error,
  wait,
  busy,
  onSubmit,
  onClose,
}: {
  copy: AccountSecurityCopy;
  currentEmail: string;
  error: AccountEmailError | null;
  wait?: number;
  busy: boolean;
  onSubmit?: (email: string) => void;
  onClose?: () => void;
}) {
  const t = copy.email;
  const inputId = React.useId();
  const errorId = React.useId();
  const inputRef = React.useRef<HTMLInputElement>(null);
  const [value, setValue] = React.useState("");
  const [tried, setTried] = React.useState(false);
  // The form opens in place: focus lands on the field.
  React.useEffect(() => inputRef.current?.focus(), []);
  const email = value.trim();
  const problem = !LOOKS_LIKE_EMAIL.test(email)
    ? t.invalid
    : email.toLowerCase() === currentEmail.toLowerCase()
      ? t.sameAsCurrent
      : null;
  const fieldError = tried ? problem : null;
  const failure =
    error === "wait"
      ? fillCopy(t.wait, { seconds: wait ?? 0 })
      : error === "failed"
        ? copy.actionError
        : null;
  return (
    <section
      aria-label={t.formTitle}
      className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
    >
      <b className="block text-body font-semibold text-foreground">
        {t.formTitle}
      </b>
      <p className="m-0 mt-1 text-crumb text-muted-foreground">{t.formText}</p>
      <form
        noValidate
        className="mt-2.5 flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          setTried(true);
          if (busy || problem !== null) return;
          onSubmit?.(email);
        }}
      >
        <div className="flex min-w-[220px] max-w-[420px] flex-col gap-1">
          <label htmlFor={inputId} className="text-xs font-semibold">
            {t.label}
          </label>
          <input
            ref={inputRef}
            id={inputId}
            type="email"
            autoComplete="email"
            value={value}
            aria-invalid={fieldError ? true : undefined}
            aria-describedby={fieldError ? errorId : undefined}
            onChange={(event) => setValue(event.target.value)}
            className={INPUT}
          />
          {fieldError ? (
            <span
              id={errorId}
              role="alert"
              className="text-xs text-danger-text"
            >
              {fieldError}
            </span>
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <button
            type="submit"
            disabled={busy || error === "wait"}
            className={cn(BTN_PRIMARY, FOCUS)}
          >
            {t.submit}
          </button>
          <button type="button" onClick={onClose} className={cn(BTN_SM, FOCUS)}>
            {t.cancel}
          </button>
        </div>
      </form>
      {failure ? (
        <p role="alert" className="m-0 mt-2 text-crumb text-danger-text">
          {failure}
        </p>
      ) : null}
    </section>
  );
}

/** The link is on its way; the same words whether or not the address can be used. */
export function AccountEmailSentPanel({
  copy,
  email,
  onClose,
}: {
  copy: AccountSecurityCopy;
  email: string;
  onClose?: () => void;
}) {
  const t = copy.email;
  const doneRef = React.useRef<HTMLButtonElement>(null);
  React.useEffect(() => doneRef.current?.focus(), []);
  return (
    <section
      aria-label={t.sentTitle}
      className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
    >
      <b className="block text-body font-semibold text-foreground">
        {t.sentTitle}
      </b>
      <p role="status" className="m-0 mt-1 text-crumb text-muted-foreground">
        {fillCopy(t.sentText, { email })}
      </p>
      <p className="m-0 mt-1 text-xs text-meta-foreground">{t.sentNote}</p>
      <button
        ref={doneRef}
        type="button"
        onClick={onClose}
        className={cn(BTN_SM, FOCUS, "mt-3")}
      >
        {t.sentDone}
      </button>
    </section>
  );
}
