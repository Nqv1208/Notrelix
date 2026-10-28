import * as React from "react";
import { cn } from "@notrelix/ui-web";
import {
  PASSWORD_MIN_LENGTH,
  checkNewPassword,
  type NewPasswordProblem,
} from "../../core/password";
import { BTN_PRIMARY, BTN_SM, FOCUS, INPUT } from "../shared/account-parts";
import { fillCopy } from "../shared/account-surface-model";
import type { AccountSecurityCopy } from "./account-security.copy";

/** What the producer refused in the last attempt; `null` = nothing yet. */
export type AccountPasswordError =
  "wrongCurrent" | "wrongCode" | "rejected" | "wait" | "failed";

export interface AccountPasswordSubmission {
  readonly current: string;
  readonly next: string;
  /** The authenticator or backup code; `""` when two-factor is off. */
  readonly code: string;
}

function Field({
  id,
  label,
  hint,
  error,
  errorId,
  children,
}: {
  id: string;
  label: string;
  hint?: string;
  error: string | null;
  errorId: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-w-[220px] flex-1 flex-col gap-1">
      <label htmlFor={id} className="text-xs font-semibold">
        {label}
      </label>
      {children}
      {hint ? (
        <span className="text-xs text-meta-foreground">{hint}</span>
      ) : null}
      {error ? (
        <span id={errorId} role="alert" className="text-xs text-danger-text">
          {error}
        </span>
      ) : null}
    </div>
  );
}

/**
 * Changing the password of the account: the current one, the new one twice
 * and, while two-factor is on, a code from the authenticator. Mistakes the
 * page can see (length, repeat, same as before) are told before anything is
 * sent; the producer checks all of it again.
 */
export function AccountPasswordPanel({
  copy,
  needsCode,
  error,
  wait,
  busy,
  onSubmit,
  onClose,
}: {
  copy: AccountSecurityCopy;
  needsCode: boolean;
  error: AccountPasswordError | null;
  wait?: number;
  busy: boolean;
  onSubmit?: (submission: AccountPasswordSubmission) => void;
  onClose?: () => void;
}) {
  const t = copy.passwordFlow;
  const ids = {
    current: React.useId(),
    next: React.useId(),
    confirm: React.useId(),
    code: React.useId(),
    currentError: React.useId(),
    nextError: React.useId(),
    confirmError: React.useId(),
    codeError: React.useId(),
  };
  const [current, setCurrent] = React.useState("");
  const [next, setNext] = React.useState("");
  const [confirm, setConfirm] = React.useState("");
  const [code, setCode] = React.useState("");
  const [tried, setTried] = React.useState(false);

  const problem: NewPasswordProblem | null = checkNewPassword({
    current,
    next,
    confirm,
  });
  const shown = tried ? problem : null;
  const nextText =
    shown === "too-short"
      ? fillCopy(t.tooShort, { count: PASSWORD_MIN_LENGTH })
      : shown === "same-as-current"
        ? t.sameAsCurrent
        : error === "rejected"
          ? fillCopy(t.rejected, { count: PASSWORD_MIN_LENGTH })
          : null;
  const currentText =
    tried && current === ""
      ? t.currentRequired
      : error === "wrongCurrent"
        ? t.wrongCurrent
        : null;
  const confirmText = shown === "mismatch" ? t.mismatch : null;
  const codeText =
    tried && needsCode && code.trim() === ""
      ? t.codeRequired
      : error === "wrongCode"
        ? t.wrongCode
        : null;
  const failure =
    error === "wait"
      ? fillCopy(t.wait, { seconds: wait ?? 0 })
      : error === "failed"
        ? copy.actionError
        : null;

  return (
    <section
      aria-label={t.title}
      className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
    >
      <b className="block text-body font-semibold text-foreground">{t.title}</b>
      <p className="m-0 mt-1 text-crumb text-muted-foreground">
        {needsCode ? t.textWithCode : t.text}
      </p>
      <form
        noValidate
        className="mt-2.5 flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          setTried(true);
          if (
            busy ||
            current === "" ||
            problem !== null ||
            (needsCode && code.trim() === "")
          )
            return;
          onSubmit?.({ current, next, code: code.trim() });
        }}
      >
        <div className="flex flex-wrap gap-2.5">
          <Field
            id={ids.current}
            label={t.currentLabel}
            error={currentText}
            errorId={ids.currentError}
          >
            <input
              id={ids.current}
              type="password"
              autoComplete="current-password"
              value={current}
              aria-invalid={currentText ? true : undefined}
              aria-describedby={currentText ? ids.currentError : undefined}
              onChange={(event) => setCurrent(event.target.value)}
              className={INPUT}
            />
          </Field>
          {needsCode ? (
            <Field
              id={ids.code}
              label={t.codeLabel}
              error={codeText}
              errorId={ids.codeError}
            >
              <input
                id={ids.code}
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                value={code}
                aria-invalid={codeText ? true : undefined}
                aria-describedby={codeText ? ids.codeError : undefined}
                onChange={(event) => setCode(event.target.value)}
                className={INPUT}
              />
            </Field>
          ) : null}
        </div>
        <div className="flex flex-wrap gap-2.5">
          <Field
            id={ids.next}
            label={t.newLabel}
            hint={fillCopy(t.newHint, { count: PASSWORD_MIN_LENGTH })}
            error={nextText}
            errorId={ids.nextError}
          >
            <input
              id={ids.next}
              type="password"
              autoComplete="new-password"
              value={next}
              aria-invalid={nextText ? true : undefined}
              aria-describedby={nextText ? ids.nextError : undefined}
              onChange={(event) => setNext(event.target.value)}
              className={INPUT}
            />
          </Field>
          <Field
            id={ids.confirm}
            label={t.confirmLabel}
            error={confirmText}
            errorId={ids.confirmError}
          >
            <input
              id={ids.confirm}
              type="password"
              autoComplete="new-password"
              value={confirm}
              aria-invalid={confirmText ? true : undefined}
              aria-describedby={confirmText ? ids.confirmError : undefined}
              onChange={(event) => setConfirm(event.target.value)}
              className={INPUT}
            />
          </Field>
        </div>
        <p className="m-0 text-xs text-meta-foreground">{t.signOutNote}</p>
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

/** The password is changed; the producer ended every session, this one too. */
export function AccountPasswordChangedPanel({
  copy,
  onContinue,
}: {
  copy: AccountSecurityCopy;
  onContinue?: () => void;
}) {
  const t = copy.passwordFlow;
  return (
    <section
      aria-label={t.doneTitle}
      className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
    >
      <b className="block text-body font-semibold text-foreground">
        {t.doneTitle}
      </b>
      <p role="status" className="m-0 mt-1 text-crumb text-muted-foreground">
        {t.doneText}
      </p>
      <button
        type="button"
        onClick={onContinue}
        className={cn(BTN_PRIMARY, FOCUS, "mt-3")}
      >
        {t.signInAgain}
      </button>
    </section>
  );
}
