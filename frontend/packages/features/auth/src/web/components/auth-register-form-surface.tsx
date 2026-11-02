import { useState } from "react";
import { Check, Mail } from "lucide-react";
import { PASSWORD_MIN_LENGTH } from "../../core/schemas/password-policy";
import { fillAuthCopy } from "../copy/fill-auth-copy";
import type { AuthCopy } from "../copy/auth.copy";
import {
  AuthHeading,
  BUTTON_CLASS,
  FieldError,
  INPUT_CLASS,
  LABEL_CLASS,
  LINK_BUTTON_CLASS,
  PasswordField,
} from "./auth-parts";

/**
 * Registration. No artboard draws it, so it is built on the pattern of the
 * drawn sign-in pages (the Final-Auth fields, buttons and links). The answer to
 * the form is one neutral "check your email" step: it says the same thing for
 * a new address and for one that already has an account, so the page never
 * tells whether an address is registered. Pure: the step, the copy and the
 * callbacks are injected; the typed values are its own.
 */
export type AuthRegisterStep =
  | {
      readonly kind: "form";
      readonly error?: "unreachable" | "limited" | null;
      /** The server's word on a field, shown under it. */
      readonly fieldErrors?: {
        readonly name?: string;
        readonly email?: string;
        readonly password?: string;
      };
    }
  | { readonly kind: "sent"; readonly email: string };

export interface RegisterSubmitData {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly password: string;
}

export interface AuthRegisterFormSurfaceProps {
  readonly copy: AuthCopy;
  readonly step: AuthRegisterStep;
  readonly pending?: boolean;
  readonly onSubmit: (data: RegisterSubmitData) => void;
  readonly onSignIn: () => void;
  /** "Use another email": back to the form with empty fields. */
  readonly onOtherEmail: () => void;
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/;

export function AuthRegisterFormSurface(props: AuthRegisterFormSurfaceProps) {
  return props.step.kind === "form" ? (
    <RegisterForm {...props} step={props.step} />
  ) : (
    <SentStep {...props} step={props.step} />
  );
}

function RegisterForm({
  copy,
  step,
  pending,
  onSubmit,
  onSignIn,
}: AuthRegisterFormSurfaceProps & {
  step: Extract<AuthRegisterStep, { kind: "form" }>;
}) {
  const t = copy.register;
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [terms, setTerms] = useState(false);
  const [tried, setTried] = useState(false);

  const firstError = tried && !firstName.trim() ? t.firstRequired : null;
  const lastError = tried && !lastName.trim() ? t.lastRequired : null;
  const emailError =
    step.fieldErrors?.email ??
    (tried && !EMAIL_SHAPE.test(email.trim()) ? t.emailInvalid : null);
  const passwordError =
    step.fieldErrors?.password ??
    (tried && password.length < PASSWORD_MIN_LENGTH
      ? fillAuthCopy(t.passwordShort, { count: PASSWORD_MIN_LENGTH })
      : null);
  const termsError = tried && !terms ? t.termsRequired : null;
  const nameError = step.fieldErrors?.name ?? null;

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
            !firstName.trim() ||
            !lastName.trim() ||
            !EMAIL_SHAPE.test(email.trim()) ||
            password.length < PASSWORD_MIN_LENGTH ||
            !terms
          )
            return;
          onSubmit({
            firstName: firstName.trim(),
            lastName: lastName.trim(),
            email: email.trim(),
            password,
          });
        }}
      >
        <div className="grid grid-cols-2 gap-3 max-md:grid-cols-1">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="auth-register-first" className={LABEL_CLASS}>
              {t.firstName}
            </label>
            <input
              id="auth-register-first"
              name="firstName"
              type="text"
              autoComplete="given-name"
              value={firstName}
              aria-invalid={firstError || nameError ? true : undefined}
              aria-describedby={
                firstError ? "auth-register-first-error" : undefined
              }
              onChange={(event) => setFirstName(event.target.value)}
              className={INPUT_CLASS}
            />
            {firstError ? (
              <FieldError id="auth-register-first-error">
                {firstError}
              </FieldError>
            ) : null}
          </div>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="auth-register-last" className={LABEL_CLASS}>
              {t.lastName}
            </label>
            <input
              id="auth-register-last"
              name="lastName"
              type="text"
              autoComplete="family-name"
              value={lastName}
              aria-invalid={lastError || nameError ? true : undefined}
              aria-describedby={
                lastError ? "auth-register-last-error" : undefined
              }
              onChange={(event) => setLastName(event.target.value)}
              className={INPUT_CLASS}
            />
            {lastError ? (
              <FieldError id="auth-register-last-error">{lastError}</FieldError>
            ) : null}
          </div>
        </div>
        {nameError ? <FieldError>{nameError}</FieldError> : null}
        <div className="flex flex-col gap-1.5">
          <label htmlFor="auth-register-email" className={LABEL_CLASS}>
            {t.email}
          </label>
          <input
            id="auth-register-email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            placeholder={t.emailPlaceholder}
            aria-invalid={emailError ? true : undefined}
            aria-describedby={
              emailError ? "auth-register-email-error" : undefined
            }
            onChange={(event) => setEmail(event.target.value)}
            className={INPUT_CLASS}
          />
          {emailError ? (
            <FieldError id="auth-register-email-error">{emailError}</FieldError>
          ) : null}
        </div>
        <div className="flex flex-col gap-1.5">
          <label htmlFor="auth-register-password" className={LABEL_CLASS}>
            {t.password}
          </label>
          <PasswordField
            id="auth-register-password"
            value={password}
            onChange={setPassword}
            invalid={Boolean(passwordError)}
            describedBy={
              passwordError
                ? "auth-register-password-error"
                : "auth-register-password-hint"
            }
            autoComplete="new-password"
            showLabel={t.show}
            hideLabel={t.hide}
          />
          {passwordError ? (
            <FieldError id="auth-register-password-error">
              {passwordError}
            </FieldError>
          ) : (
            <p
              id="auth-register-password-hint"
              className="m-0 text-crumb leading-normal text-meta-foreground"
            >
              {fillAuthCopy(t.passwordHint, { count: PASSWORD_MIN_LENGTH })}
            </p>
          )}
        </div>
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="auth-register-terms"
            className="flex min-h-touch cursor-pointer items-start gap-2.5 text-item-ui leading-normal text-muted-foreground md:min-h-0"
          >
            <span className="relative mt-0.5 grid size-4.5 shrink-0 place-items-center">
              <input
                id="auth-register-terms"
                name="terms"
                type="checkbox"
                checked={terms}
                aria-invalid={termsError ? true : undefined}
                aria-describedby={
                  termsError ? "auth-register-terms-error" : undefined
                }
                onChange={(event) => setTerms(event.target.checked)}
                className="peer absolute -inset-3.25 size-11 cursor-pointer opacity-0 md:inset-0 md:size-full"
              />
              <span
                aria-hidden="true"
                className="pointer-events-none grid size-4.5 place-items-center rounded-[5px] border border-input bg-popover text-primary-foreground peer-checked:border-primary peer-checked:bg-primary peer-focus-visible:ring-2 peer-focus-visible:ring-focus-ring peer-aria-invalid:border-danger-text [&>svg]:opacity-0 peer-checked:[&>svg]:opacity-100"
              >
                <Check className="size-3.5" />
              </span>
            </span>
            <span>
              {t.termsBefore}
              <a
                href="/legal/terms"
                className="font-medium text-brand-subtle-foreground hover:underline"
              >
                {t.terms}
              </a>
              {t.termsBetween}
              <a
                href="/legal/privacy"
                className="font-medium text-brand-subtle-foreground hover:underline"
              >
                {t.privacy}
              </a>
            </span>
          </label>
          {termsError ? (
            <FieldError id="auth-register-terms-error">{termsError}</FieldError>
          ) : null}
        </div>
        {step.error ? <FieldError>{t[step.error]}</FieldError> : null}
        <button
          type="submit"
          disabled={pending}
          className={BUTTON_CLASS.primary}
        >
          {pending ? t.submitting : t.submit}
        </button>
      </form>
      <p className="m-0 text-item-ui leading-normal text-muted-foreground">
        {t.haveAccount}{" "}
        <button type="button" onClick={onSignIn} className={LINK_BUTTON_CLASS}>
          {t.signIn}
        </button>
      </p>
    </>
  );
}

function SentStep({
  copy,
  step,
  onSignIn,
  onOtherEmail,
}: AuthRegisterFormSurfaceProps & {
  step: Extract<AuthRegisterStep, { kind: "sent" }>;
}) {
  const t = copy.register;
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
          title={t.sentTitle}
          lead={
            <>
              {t.sentBody}
              <b className="font-semibold text-foreground">{step.email}</b>
              {t.sentBodyEnd}
            </>
          }
        />
      </div>
      <p className="m-0 text-item-prose leading-normal text-muted-foreground">
        {t.sentSpam}
      </p>
      <button type="button" onClick={onSignIn} className={BUTTON_CLASS.primary}>
        {t.sentSignIn}
      </button>
      <div>
        <button
          type="button"
          onClick={onOtherEmail}
          className={LINK_BUTTON_CLASS}
        >
          {t.sentOther}
        </button>
      </div>
    </>
  );
}
