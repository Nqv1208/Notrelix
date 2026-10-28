import { useState } from "react";
import { Mail, MailCheck, MailWarning } from "lucide-react";
import type { AuthCopy } from "../copy/auth.copy";
import {
  AuthHeading,
  BUTTON_CLASS,
  FieldError,
  INPUT_CLASS,
  LINK_BUTTON_CLASS,
} from "./auth-parts";

/**
 * Email confirmation: the page the link in the email opens. It confirms the
 * token, says so, or (a link used up, expired, or none at all) offers a new
 * link. Pure: the step and the callbacks are injected.
 */
export type AuthVerifyEmailStep =
  | { readonly kind: "confirming" }
  | { readonly kind: "done" }
  | { readonly kind: "failed" }
  | { readonly kind: "waiting" };

export interface AuthVerifyEmailSurfaceProps {
  readonly copy: AuthCopy;
  readonly step: AuthVerifyEmailStep;
  readonly pending?: boolean;
  readonly initialEmail?: string;
  /** A new link went out. */
  readonly sent?: boolean;
  readonly error?: "invalid" | "unreachable" | null;
  readonly onResend: (email: string) => void;
  readonly onContinue: () => void;
  readonly onBack: () => void;
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/;

export function AuthVerifyEmailSurface(props: AuthVerifyEmailSurfaceProps) {
  const t = props.copy.verifyEmail;
  const { step } = props;
  if (step.kind === "confirming") {
    return (
      <div role="status" className="flex flex-col gap-4.5">
        <AuthHeading title={t.confirmingTitle} lead={t.confirmingLead} />
      </div>
    );
  }
  if (step.kind === "done") {
    return (
      <>
        <span
          aria-hidden="true"
          className="grid size-13 place-items-center rounded-[14px] bg-success-subtle text-success-text"
        >
          <MailCheck className="size-6" />
        </span>
        <AuthHeading title={t.doneTitle} lead={t.doneLead} />
        <button
          type="button"
          onClick={props.onContinue}
          className={BUTTON_CLASS.primary}
        >
          {t.continue}
        </button>
      </>
    );
  }
  return <ResendStep {...props} />;
}

function ResendStep({
  copy,
  step,
  pending,
  initialEmail = "",
  sent,
  error,
  onResend,
  onBack,
}: AuthVerifyEmailSurfaceProps) {
  const t = copy.verifyEmail;
  const [email, setEmail] = useState(initialEmail);
  const [tried, setTried] = useState(false);
  const invalid =
    error === "invalid" || (tried && !EMAIL_SHAPE.test(email.trim()));
  const failed = step.kind === "failed";
  return (
    <>
      <span
        aria-hidden="true"
        className={
          failed
            ? "grid size-13 place-items-center rounded-[14px] bg-state-hover text-muted-foreground"
            : "grid size-13 place-items-center rounded-[14px] bg-brand-subtle text-brand-subtle-foreground"
        }
      >
        {failed ? (
          <MailWarning className="size-6" />
        ) : (
          <Mail className="size-6" />
        )}
      </span>
      <AuthHeading
        title={failed ? t.failedTitle : t.waitingTitle}
        lead={failed ? t.failedLead : t.waitingLead}
      />
      <form
        noValidate
        className="flex flex-col gap-4.5"
        onSubmit={(event) => {
          event.preventDefault();
          setTried(true);
          if (EMAIL_SHAPE.test(email.trim())) onResend(email.trim());
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label htmlFor="auth-verify" className="text-item-ui font-semibold">
            {t.label}
          </label>
          <input
            id="auth-verify"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            placeholder={t.placeholder}
            aria-invalid={invalid || undefined}
            aria-describedby={invalid ? "auth-verify-error" : undefined}
            onChange={(event) => setEmail(event.target.value)}
            className={INPUT_CLASS}
          />
          {invalid ? (
            <FieldError id="auth-verify-error">{t.invalid}</FieldError>
          ) : null}
          {error === "unreachable" ? (
            <FieldError>{t.unreachable}</FieldError>
          ) : null}
          {sent ? (
            <p role="status" className="m-0 text-item-small text-success-text">
              {t.sentNotice}
            </p>
          ) : null}
        </div>
        <button
          type="submit"
          disabled={pending}
          className={BUTTON_CLASS.primary}
        >
          {pending ? t.sending : t.send}
        </button>
      </form>
      <div>
        <button type="button" onClick={onBack} className={LINK_BUTTON_CLASS}>
          {t.back}
        </button>
      </div>
    </>
  );
}
