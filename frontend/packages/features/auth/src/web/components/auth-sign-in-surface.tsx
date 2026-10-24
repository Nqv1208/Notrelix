import {
  useEffect,
  useRef,
  useState,
  type ClipboardEvent,
  type KeyboardEvent,
} from "react";
import { Clock, Lock, Mail, Timer } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillAuthCopy } from "../copy/fill-auth-copy";
import type { AuthCopy } from "../copy/auth.copy";
import { GoogleMark } from "./google-mark";
import { MicrosoftMark } from "./microsoft-mark";
import {
  AuthHeading,
  BUTTON_CLASS,
  FieldError,
  INPUT_CLASS,
  LINK_BUTTON_CLASS,
  PasswordField,
} from "./auth-parts";

/**
 * The sign-in page (the Auth mockup): the email first, then, by what the
 * email says, a password, the company's single sign-on, or the second step.
 * Pure: the step, the copy and the callbacks are injected; the inputs it owns
 * are the typed email, password and code.
 */
export type AuthSignInStep =
  | {
      readonly kind: "email";
      readonly error?:
        | "invalid"
        | "unreachable"
        /** A provider sign-in was stopped: the account has a second factor, so it signs in with its password. */
        | "mfa-required"
        | "provider-failed"
        | null;
    }
  | {
      readonly kind: "password";
      readonly email: string;
      readonly error?: "wrong" | "unreachable" | null;
      /** Seconds the account stays locked; 0 or absent = open. */
      readonly lockedSeconds?: number;
    }
  | {
      readonly kind: "sso";
      readonly email: string;
      readonly org: {
        readonly name: string;
        readonly initials: string;
        readonly providerLabel: string;
        readonly domain: string;
      };
    }
  | {
      readonly kind: "otp";
      readonly email: string;
      readonly error?:
        "wrong" | "expired" | "inactive" | "locked" | "unreachable" | null;
      /** Seconds the second step stays locked after too many tries; 0 or absent = open. */
      readonly lockedSeconds?: number;
      /** Asking for a backup code instead of the app's six digits. */
      readonly backup?: boolean;
    };

export interface AuthSignInSurfaceProps {
  readonly copy: AuthCopy;
  readonly step: AuthSignInStep;
  readonly pending?: boolean;
  /** The email typed earlier (the email step starts with it). */
  readonly initialEmail?: string;
  readonly onContinue: (email: string) => void;
  readonly onProvider: (provider: "google" | "microsoft") => void;
  readonly onCreateWorkspace: () => void;
  readonly onChangeEmail: () => void;
  readonly onSubmitPassword: (password: string) => void;
  readonly onForgot: () => void;
  readonly onSso: () => void;
  readonly onVerify: (input: { code: string }) => void;
  readonly onToggleBackup: () => void;
  readonly onBack: () => void;
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** "0:48". */
export function formatCountdown(seconds: number): string {
  const whole = Math.max(0, Math.floor(seconds));
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, "0")}`;
}

export function AuthSignInSurface(props: AuthSignInSurfaceProps) {
  const { step } = props;
  // The step changes the page: a key resets what was typed in the one before.
  switch (step.kind) {
    case "email":
      return <EmailStep key="email" {...props} step={step} />;
    case "password":
      return <PasswordStep key="password" {...props} step={step} />;
    case "sso":
      return <SsoStep key="sso" {...props} step={step} />;
    case "otp":
      return (
        <OtpStep key={step.backup ? "backup" : "otp"} {...props} step={step} />
      );
  }
}

function EmailStep({
  copy,
  step,
  pending,
  initialEmail = "",
  onContinue,
  onProvider,
  onCreateWorkspace,
}: AuthSignInSurfaceProps & {
  step: Extract<AuthSignInStep, { kind: "email" }>;
}) {
  const t = copy.email;
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
          if (EMAIL_SHAPE.test(email.trim())) onContinue(email.trim());
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label htmlFor="auth-email" className="text-item-ui font-semibold">
            {t.label}
          </label>
          <input
            id="auth-email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            placeholder={t.placeholder}
            aria-invalid={invalid || undefined}
            aria-describedby={invalid ? "auth-email-error" : undefined}
            onChange={(event) => setEmail(event.target.value)}
            className={INPUT_CLASS}
          />
          {invalid ? (
            <FieldError id="auth-email-error">{t.invalid}</FieldError>
          ) : null}
          {step.error === "unreachable" ? (
            <FieldError>{t.unreachable}</FieldError>
          ) : null}
          {step.error === "mfa-required" ? (
            <FieldError>{t.mfaRequired}</FieldError>
          ) : null}
          {step.error === "provider-failed" ? (
            <FieldError>{t.providerFailed}</FieldError>
          ) : null}
        </div>
        <button
          type="submit"
          disabled={pending}
          className={BUTTON_CLASS.primary}
        >
          {pending ? t.checking : t.continue}
        </button>
      </form>
      <div
        role="separator"
        className="flex items-center gap-3 text-crumb text-meta-foreground before:h-px before:flex-1 before:bg-line after:h-px after:flex-1 after:bg-line"
      >
        {t.or}
      </div>
      <div className="flex flex-col gap-2.5">
        <button
          type="button"
          disabled={pending}
          onClick={() => onProvider("google")}
          className={BUTTON_CLASS.outline}
        >
          <GoogleMark className="size-4.5 shrink-0" />
          {t.google}
        </button>
        <button
          type="button"
          disabled={pending}
          onClick={() => onProvider("microsoft")}
          className={BUTTON_CLASS.outline}
        >
          <MicrosoftMark className="size-4.5 shrink-0" />
          {t.microsoft}
        </button>
      </div>
      <p className="m-0 text-center text-item-ui text-muted-foreground">
        {t.noAccount}{" "}
        <button
          type="button"
          onClick={onCreateWorkspace}
          className={LINK_BUTTON_CLASS}
        >
          {t.createWorkspace}
        </button>
      </p>
    </>
  );
}

/** The email the person chose, with the way to change it. */
function WhoRow({
  email,
  change,
  changeLabel,
  onChange,
}: {
  email: string;
  change?: string;
  changeLabel?: string;
  onChange?: () => void;
}) {
  return (
    <div className="flex items-center gap-2.5 rounded-[10px] border border-line bg-popover py-2 pr-2 pl-3">
      <span
        aria-hidden="true"
        className="grid size-7 shrink-0 place-items-center rounded-full bg-state-hover-strong text-muted-foreground"
      >
        <Mail className="size-3.5" />
      </span>
      <span className="min-w-0 flex-1 truncate text-item-ui font-medium">
        {email}
      </span>
      {onChange ? (
        <button
          type="button"
          aria-label={changeLabel}
          onClick={onChange}
          className={cn(LINK_BUTTON_CLASS, "px-2 py-1")}
        >
          {change}
        </button>
      ) : null}
    </div>
  );
}

function PasswordStep({
  copy,
  step,
  pending,
  onSubmitPassword,
  onChangeEmail,
  onForgot,
  onBack,
}: AuthSignInSurfaceProps & {
  step: Extract<AuthSignInStep, { kind: "password" }>;
}) {
  const t = copy.password;
  const [password, setPassword] = useState("");
  const locked = (step.lockedSeconds ?? 0) > 0;
  const wrong = step.error === "wrong";
  // A wrong password is not kept: the field starts empty for the next try.
  useEffect(() => {
    if (wrong && !pending) setPassword("");
  }, [wrong, pending]);
  return (
    <>
      <AuthHeading title={t.title} lead={t.lead} />
      <WhoRow
        email={step.email}
        change={t.change}
        changeLabel={t.changeLabel}
        onChange={onChangeEmail}
      />
      <form
        noValidate
        className="flex flex-col gap-4.5"
        onSubmit={(event) => {
          event.preventDefault();
          if (!locked && password) onSubmitPassword(password);
        }}
      >
        <div className="flex flex-col gap-1.5">
          <div className="flex items-baseline justify-between gap-3">
            <label
              htmlFor="auth-password"
              className="text-item-ui font-semibold"
            >
              {t.label}
            </label>
            <button
              type="button"
              onClick={onForgot}
              className={LINK_BUTTON_CLASS}
            >
              {t.forgot}
            </button>
          </div>
          <PasswordField
            id="auth-password"
            value={password}
            onChange={setPassword}
            invalid={wrong}
            describedBy={wrong ? "auth-password-error" : undefined}
            autoComplete="current-password"
            showLabel={t.show}
            hideLabel={t.hide}
            autoFocus
          />
          {wrong ? (
            <FieldError id="auth-password-error">{t.wrong}</FieldError>
          ) : null}
          {step.error === "unreachable" ? (
            <FieldError>{t.unreachable}</FieldError>
          ) : null}
        </div>
        {locked ? (
          <div
            role="status"
            className="flex items-start gap-2.5 rounded-[10px] bg-warning-subtle px-3.5 py-3 text-item-ui text-foreground"
          >
            <Timer
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0 text-warning-text"
            />
            <div>
              <b className="block font-semibold text-warning-text">
                {fillAuthCopy(t.lockTitle, {
                  seconds: step.lockedSeconds ?? 0,
                })}
              </b>
              <p className="mt-0.5 mb-0">{t.lockBody}</p>
            </div>
          </div>
        ) : null}
        <button
          type="submit"
          disabled={locked || pending}
          aria-label={
            locked
              ? `${t.lockedButton}${fillAuthCopy(t.lockedLabel, { seconds: step.lockedSeconds ?? 0 })}`
              : undefined
          }
          className={BUTTON_CLASS.primary}
        >
          {locked ? (
            <>
              <Clock aria-hidden="true" className="size-4" />
              {t.lockedButton}
              <span aria-hidden="true">·</span>
              <span aria-hidden="true" className="font-mono">
                {formatCountdown(step.lockedSeconds ?? 0)}
              </span>
            </>
          ) : pending ? (
            t.signingIn
          ) : (
            t.signIn
          )}
        </button>
      </form>
      <div>
        <button type="button" onClick={onBack} className={LINK_BUTTON_CLASS}>
          {t.other}
        </button>
      </div>
    </>
  );
}

function SsoStep({
  copy,
  step,
  pending,
  onSso,
  onChangeEmail,
}: AuthSignInSurfaceProps & {
  step: Extract<AuthSignInStep, { kind: "sso" }>;
}) {
  const t = copy.sso;
  const { org } = step;
  const values = {
    org: org.name,
    provider: org.providerLabel,
    domain: org.domain,
  };
  return (
    <>
      <div className="flex items-center gap-3">
        <span
          aria-hidden="true"
          className="grid size-10 shrink-0 place-items-center rounded-[11px] bg-primary text-compact font-bold text-primary-foreground"
        >
          {org.initials}
        </span>
        <div>
          <b className="block text-item-prose font-semibold">{org.name}</b>
          <small className="block text-crumb text-meta-foreground">
            {fillAuthCopy(t.orgLine, values)}
          </small>
        </div>
      </div>
      <AuthHeading medium title={fillAuthCopy(t.title, values)} lead={t.lead} />
      <WhoRow email={step.email} />
      <button
        type="button"
        disabled={pending}
        onClick={onSso}
        className={BUTTON_CLASS.primary}
      >
        <span className="grid size-5 place-items-center rounded-[3px] bg-background">
          <MicrosoftMark className="size-3.5" />
        </span>
        {pending
          ? fillAuthCopy(t.redirecting, values)
          : fillAuthCopy(t.continue, values)}
      </button>
      <div className="flex items-start gap-2.5 rounded-[10px] bg-state-hover px-3.5 py-3 text-item-small leading-normal text-muted-foreground">
        <Lock
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0 text-meta-foreground"
        />
        <p className="m-0">
          <b className="font-semibold text-foreground">{t.noteTitle}</b>
          {t.noteBody}
        </p>
      </div>
      <div>
        <button
          type="button"
          onClick={onChangeEmail}
          className={LINK_BUTTON_CLASS}
        >
          {t.other}
        </button>
      </div>
    </>
  );
}

function OtpStep({
  copy,
  step,
  pending,
  onVerify,
  onToggleBackup,
  onBack,
}: AuthSignInSurfaceProps & {
  step: Extract<AuthSignInStep, { kind: "otp" }>;
}) {
  const t = copy.otp;
  const [digits, setDigits] = useState<string[]>(Array(6).fill(""));
  const [backup, setBackup] = useState("");
  const boxes = useRef<(HTMLInputElement | null)[]>([]);
  const code = step.backup ? backup.trim() : digits.join("");
  const ready = step.backup ? code.length >= 8 : code.length === 6;
  const focusBox = (index: number) => boxes.current[index]?.focus();

  const setFrom = (start: number, text: string) => {
    const chars = text
      .replace(/\D/g, "")
      .slice(0, 6 - start)
      .split("");
    if (chars.length === 0) return;
    setDigits((current) => {
      const next = [...current];
      chars.forEach((char, offset) => {
        next[start + offset] = char;
      });
      return next;
    });
    focusBox(Math.min(5, start + chars.length));
  };
  const onKeyDown = (index: number, event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Backspace" && !digits[index] && index > 0) {
      event.preventDefault();
      setDigits((current) => {
        const next = [...current];
        next[index - 1] = "";
        return next;
      });
      focusBox(index - 1);
    } else if (event.key === "ArrowLeft" && index > 0) focusBox(index - 1);
    else if (event.key === "ArrowRight" && index < 5) focusBox(index + 1);
  };
  const onPaste = (index: number, event: ClipboardEvent<HTMLInputElement>) => {
    event.preventDefault();
    setFrom(index === 0 ? 0 : index, event.clipboardData.getData("text"));
  };

  const failure =
    step.error === "expired"
      ? t.expired
      : step.error === "inactive"
        ? t.inactive
        : step.error === "locked"
          ? fillAuthCopy(t.locked, { seconds: step.lockedSeconds ?? 0 })
          : step.error === "wrong"
            ? t.wrong
            : step.error === "unreachable"
              ? copy.password.unreachable
              : null;

  return (
    <>
      <AuthHeading
        title={t.title}
        lead={fillAuthCopy(t.lead, { email: step.email })}
      />
      <form
        noValidate
        className="flex flex-col gap-4.5"
        onSubmit={(event) => {
          event.preventDefault();
          if (ready) onVerify({ code });
        }}
      >
        {step.backup ? (
          <div className="flex flex-col gap-1.5">
            <label htmlFor="auth-backup" className="text-item-ui font-semibold">
              {t.backupLegend}
            </label>
            <input
              id="auth-backup"
              name="backup"
              type="text"
              autoComplete="one-time-code"
              autoFocus
              value={backup}
              placeholder={t.backupPlaceholder}
              aria-invalid={failure ? true : undefined}
              onChange={(event) => setBackup(event.target.value)}
              className={cn(INPUT_CLASS, "font-mono tracking-wider")}
            />
            <p className="m-0 text-item-note text-meta-foreground">
              {t.backupHint}
            </p>
          </div>
        ) : (
          <fieldset className="m-0 min-w-0 border-0 p-0">
            <legend className="mb-2 p-0 text-item-ui font-semibold">
              {t.legend}
            </legend>
            <div className="flex gap-2.5">
              {digits.map((digit, index) => (
                <span key={index} className="contents">
                  {index === 3 ? (
                    <span aria-hidden="true" className="w-2 shrink-0" />
                  ) : null}
                  <input
                    ref={(node) => {
                      boxes.current[index] = node;
                    }}
                    type="text"
                    inputMode="numeric"
                    pattern="[0-9]"
                    maxLength={1}
                    autoComplete={index === 0 ? "one-time-code" : "off"}
                    autoFocus={index === 0}
                    value={digit}
                    aria-label={fillAuthCopy(t.digit, { index: index + 1 })}
                    aria-invalid={failure ? true : undefined}
                    onChange={(event) => setFrom(index, event.target.value)}
                    onKeyDown={(event) => onKeyDown(index, event)}
                    onPaste={(event) => onPaste(index, event)}
                    onFocus={(event) => event.target.select()}
                    className="box-border h-14.5 w-13 rounded-[10px] border border-input bg-popover p-0 text-center font-mono text-greeting text-foreground outline-none focus:border-focus-ring focus:ring-[3px] focus:ring-brand-subtle aria-invalid:border-danger-text"
                  />
                </span>
              ))}
            </div>
            <p className="mt-1.5 mb-0 text-item-note text-meta-foreground">
              {t.hint}
            </p>
          </fieldset>
        )}
        {failure ? <FieldError>{failure}</FieldError> : null}
        <button
          type="submit"
          disabled={!ready || pending}
          className={BUTTON_CLASS.primary}
        >
          {pending ? t.verifying : t.verify}
        </button>
      </form>
      <div className="flex items-center justify-between gap-3">
        <button
          type="button"
          onClick={onToggleBackup}
          className={LINK_BUTTON_CLASS}
        >
          {step.backup ? t.useApp : t.backup}
        </button>
        <button type="button" onClick={onBack} className={LINK_BUTTON_CLASS}>
          {t.back}
        </button>
      </div>
    </>
  );
}
