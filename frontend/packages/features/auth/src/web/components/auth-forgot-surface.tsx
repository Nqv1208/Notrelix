import { useState } from "react";
import { Info, Mail } from "lucide-react";
import { fillAuthCopy } from "../copy/fill-auth-copy";
import type { AuthCopy } from "../copy/auth.copy";
import {
  AuthHeading,
  BUTTON_CLASS,
  FieldError,
  INPUT_CLASS,
  LABEL_CLASS,
  LINK_BUTTON_CLASS,
} from "./auth-parts";
import { formatCountdown } from "./auth-sign-in-surface";

/**
 * Password reset (the Auth mockup): ask for the email, then say a link was
 * sent. The answer never tells whether the email has an account. Pure: the
 * step, the seconds before the link can be sent again and the callbacks are
 * injected.
 */
export type AuthForgotStep =
  | {
      readonly kind: "ask";
      readonly error?: "invalid" | "unreachable" | "limited" | null;
    }
  | {
      readonly kind: "sent";
      readonly email: string;
      /** Seconds before "Gửi lại" works; 0 = it works now. */
      readonly resendSeconds: number;
      /** A send again that was refused; the person stays on this step. */
      readonly error?: "limited" | "unreachable" | null;
    };

export interface AuthForgotSurfaceProps {
  readonly copy: AuthCopy;
  readonly step: AuthForgotStep;
  readonly pending?: boolean;
  readonly initialEmail?: string;
  readonly onSend: (email: string) => void;
  readonly onResend: () => void;
  /** "Enter the code": on to the reset screen with this email. */
  readonly onEnterCode: (email: string) => void;
  readonly onBack: () => void;
  /** "Dùng email khác": back to the form with an empty field. */
  readonly onOtherEmail: () => void;
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/;

export function AuthForgotSurface(props: AuthForgotSurfaceProps) {
  return props.step.kind === "ask" ? (
    <AskStep {...props} step={props.step} />
  ) : (
    <SentStep {...props} step={props.step} />
  );
}

function AskStep({
  copy,
  step,
  pending,
  initialEmail = "",
  onSend,
  onBack,
}: AuthForgotSurfaceProps & {
  step: Extract<AuthForgotStep, { kind: "ask" }>;
}) {
  const t = copy.forgot;
  const [email, setEmail] = useState(initialEmail);
  const [tried, setTried] = useState(false);
  const invalid =
    step.error === "invalid" || (tried && !EMAIL_SHAPE.test(email.trim()));
  return (
    <>
      <AuthHeading title={t.title} lead={t.lead} />
      <form
        noValidate
        className="flex flex-col gap-4.5"
        onSubmit={(event) => {
          event.preventDefault();
          setTried(true);
          if (EMAIL_SHAPE.test(email.trim())) onSend(email.trim());
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label htmlFor="auth-reset" className={LABEL_CLASS}>
            {t.label}
          </label>
          <input
            id="auth-reset"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            placeholder={t.placeholder}
            aria-invalid={invalid || undefined}
            aria-describedby={invalid ? "auth-reset-error" : undefined}
            onChange={(event) => setEmail(event.target.value)}
            className={INPUT_CLASS}
          />
          {invalid ? (
            <FieldError id="auth-reset-error">{t.invalid}</FieldError>
          ) : null}
          {step.error === "unreachable" ? (
            <FieldError>{t.unreachable}</FieldError>
          ) : null}
          {step.error === "limited" ? (
            <FieldError>{t.limited}</FieldError>
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
      <div className="flex items-start gap-2.5 rounded-[10px] bg-state-hover px-3.5 py-3 text-[13px] leading-normal text-muted-foreground">
        <Info
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0 text-meta-foreground"
        />
        <span>{t.sso}</span>
      </div>
      <div>
        <button type="button" onClick={onBack} className={LINK_BUTTON_CLASS}>
          {t.back}
        </button>
      </div>
    </>
  );
}

function SentStep({
  copy,
  step,
  onResend,
  onEnterCode,
  onBack,
  onOtherEmail,
}: AuthForgotSurfaceProps & {
  step: Extract<AuthForgotStep, { kind: "sent" }>;
}) {
  const t = copy.sent;
  const waiting = step.resendSeconds > 0;
  return (
    <>
      <span
        aria-hidden="true"
        className="grid size-13 place-items-center rounded-[14px] bg-success-subtle text-success-text"
      >
        <Mail className="size-6" />
      </span>
      <div role="status">
        <AuthHeading
          title={t.title}
          lead={
            <>
              {t.body}
              <b className="font-semibold text-foreground">{step.email}</b>
              {t.bodyEnd}
            </>
          }
        />
      </div>
      <p className="m-0 text-item-prose leading-normal text-muted-foreground">
        {t.spam}
      </p>
      <button
        type="button"
        onClick={() => onEnterCode(step.email)}
        className={BUTTON_CLASS.primary}
      >
        {t.enterCode}
      </button>
      <button
        type="button"
        disabled={waiting}
        aria-label={
          waiting
            ? `${t.resend}${fillAuthCopy(t.resendWaitLabel, { seconds: step.resendSeconds })}`
            : undefined
        }
        onClick={onResend}
        className={BUTTON_CLASS.outline}
      >
        {waiting ? (
          <>
            {t.resend}
            <span aria-hidden="true">·</span>
            <span aria-hidden="true" className="font-mono">
              {formatCountdown(step.resendSeconds)}
            </span>
          </>
        ) : (
          t.resend
        )}
      </button>
      {step.error ? <FieldError>{t[step.error]}</FieldError> : null}
      <div className="flex items-center justify-between gap-3">
        <button type="button" onClick={onBack} className={LINK_BUTTON_CLASS}>
          {t.back}
        </button>
        <button
          type="button"
          onClick={onOtherEmail}
          className={LINK_BUTTON_CLASS}
        >
          {t.other}
        </button>
      </div>
    </>
  );
}
