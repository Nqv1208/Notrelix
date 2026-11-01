import { useState } from "react";
import { PASSWORD_MIN_LENGTH } from "../../core";
import type { AuthCopy } from "../copy/auth.copy";
import {
  AuthHeading,
  BUTTON_CLASS,
  FieldError,
  INPUT_CLASS,
  LINK_BUTTON_CLASS,
  PasswordField,
} from "./auth-parts";

/**
 * Password reset, second half: the person holds the 6-digit code from the
 * email and chooses the new password. The answer for a wrong, used or expired
 * code is the same. Pure: the refusal and the callbacks are injected.
 */
export type AuthResetError =
  "rejected" | "too-many" | "limited" | "unreachable" | null;

export interface AuthResetSurfaceProps {
  readonly copy: AuthCopy;
  readonly initialEmail?: string;
  readonly pending?: boolean;
  readonly error?: AuthResetError;
  readonly onSubmit: (value: {
    email: string;
    code: string;
    newPassword: string;
  }) => void;
  /** "Send a new code": back to the request step with this email. */
  readonly onSendAgain: (email: string) => void;
  readonly onBack: () => void;
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/;
const CODE_SHAPE = /^\d{6}$/;

export function AuthResetSurface({
  copy,
  initialEmail = "",
  pending,
  error,
  onSubmit,
  onSendAgain,
  onBack,
}: AuthResetSurfaceProps) {
  const t = copy.reset;
  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");
  const [tried, setTried] = useState(false);
  const badEmail = tried && !EMAIL_SHAPE.test(email.trim());
  const badCode = tried && !CODE_SHAPE.test(code.trim());
  const badPassword = tried && password.length < PASSWORD_MIN_LENGTH;
  const refusal =
    error === "rejected"
      ? t.rejected
      : error === "too-many"
        ? t.tooMany
        : error === "limited"
          ? t.limited
          : error === "unreachable"
            ? t.unreachable
            : null;
  return (
    <>
      <AuthHeading title={t.title} lead={t.lead} />
      <form
        noValidate
        className="flex flex-col gap-4.5"
        onSubmit={(event) => {
          event.preventDefault();
          setTried(true);
          if (
            EMAIL_SHAPE.test(email.trim()) &&
            CODE_SHAPE.test(code.trim()) &&
            password.length >= PASSWORD_MIN_LENGTH
          )
            onSubmit({
              email: email.trim(),
              code: code.trim(),
              newPassword: password,
            });
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="auth-reset-email"
            className="text-item-ui font-semibold"
          >
            {t.emailLabel}
          </label>
          <input
            id="auth-reset-email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            aria-invalid={badEmail || undefined}
            aria-describedby={badEmail ? "auth-reset-email-error" : undefined}
            onChange={(event) => setEmail(event.target.value)}
            className={INPUT_CLASS}
          />
          {badEmail ? (
            <FieldError id="auth-reset-email-error">
              {t.invalidEmail}
            </FieldError>
          ) : null}
        </div>
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="auth-reset-code"
            className="text-item-ui font-semibold"
          >
            {t.codeLabel}
          </label>
          <input
            id="auth-reset-code"
            name="code"
            type="text"
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={6}
            value={code}
            aria-invalid={badCode || undefined}
            aria-describedby={
              badCode ? "auth-reset-code-error" : "auth-reset-code-hint"
            }
            onChange={(event) =>
              setCode(event.target.value.replace(/\D/g, "").slice(0, 6))
            }
            className={`${INPUT_CLASS} font-mono tracking-widest`}
          />
          {badCode ? (
            <FieldError id="auth-reset-code-error">{t.invalidCode}</FieldError>
          ) : (
            <p
              id="auth-reset-code-hint"
              className="m-0 text-item-small text-meta-foreground"
            >
              {t.codeHint}
            </p>
          )}
        </div>
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="auth-reset-password"
            className="text-item-ui font-semibold"
          >
            {t.passwordLabel}
          </label>
          <PasswordField
            id="auth-reset-password"
            value={password}
            onChange={setPassword}
            invalid={badPassword}
            describedBy={
              badPassword
                ? "auth-reset-password-error"
                : "auth-reset-password-hint"
            }
            autoComplete="new-password"
            showLabel={t.show}
            hideLabel={t.hide}
          />
          {badPassword ? (
            <FieldError id="auth-reset-password-error">
              {t.shortPassword}
            </FieldError>
          ) : (
            <p
              id="auth-reset-password-hint"
              className="m-0 text-item-small text-meta-foreground"
            >
              {t.passwordHint}
            </p>
          )}
        </div>
        {refusal ? <FieldError>{refusal}</FieldError> : null}
        <button
          type="submit"
          disabled={pending}
          className={BUTTON_CLASS.primary}
        >
          {pending ? t.submitting : t.submit}
        </button>
      </form>
      <div className="flex items-center justify-between gap-3">
        <button type="button" onClick={onBack} className={LINK_BUTTON_CLASS}>
          {t.back}
        </button>
        <button
          type="button"
          onClick={() => onSendAgain(email.trim())}
          className={LINK_BUTTON_CLASS}
        >
          {t.sendAgain}
        </button>
      </div>
    </>
  );
}
