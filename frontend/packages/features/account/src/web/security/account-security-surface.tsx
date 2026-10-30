import * as React from "react";
import {
  KeyRound,
  Laptop,
  Lock,
  Smartphone,
  Tablet,
  type LucideIcon,
} from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import {
  BTN_DANGER,
  BTN_PRIMARY,
  BTN_SM,
  FOCUS,
  INPUT,
  LINK_BUTTON_DANGER,
  LoadError,
  PageHead,
  Tag,
  Toast,
} from "../shared/account-parts";
import {
  fillCopy,
  pluralCopy,
  type AccountSection,
} from "../shared/account-surface-model";
import {
  AccountPasswordChangedPanel,
  AccountPasswordPanel,
  type AccountPasswordError,
  type AccountPasswordSubmission,
} from "./account-password-panel";
import { AccountQrCode } from "./account-qr-code";
import type { AccountSecurityCopy } from "./account-security.copy";

export type {
  AccountPasswordError,
  AccountPasswordSubmission,
} from "./account-password-panel";

export interface AccountSessionView {
  readonly id: string;
  readonly device: string;
  readonly kind: "desktop" | "phone" | "tablet";
  /** The device in your hands, in use right now. */
  readonly isCurrent: boolean;
  /** ISO timestamp of the sign-in; the producer has no place and no last-active time. */
  readonly signedInAt: string;
}

/** One line of the person's own security history. */
export interface AccountActivityView {
  readonly id: string;
  readonly type: string;
  readonly occurredAt: string;
  readonly device: string;
  readonly ipAddress: string | null;
}

/**
 * A step of turning two-factor on or of a sensitive change. `wait` is the
 * seconds a throttled step must wait (429); never a count of tries left.
 */
export type AccountSecurityFlow =
  | {
      readonly kind: "enroll";
      readonly secret: string;
      readonly otpAuthUri: string;
      readonly error: "wrong" | "wait" | "failed" | null;
      readonly wait?: number;
    }
  | {
      readonly kind: "stepUp";
      /** What the proof is for: two-factor off, fresh backup codes, or ending every other device. */
      readonly purpose: "disable" | "renew" | "others";
      readonly factor: "code" | "password";
      readonly error: "wrong" | "wait" | "failed" | null;
      readonly wait?: number;
    }
  | { readonly kind: "codes"; readonly codes: readonly string[] }
  | {
      /** Changing the password; `needsCode` while two-factor is on. */
      readonly kind: "password";
      readonly needsCode: boolean;
      readonly error: AccountPasswordError | null;
      readonly wait?: number;
    }
  /** The password changed and every session, this one too, was ended. */
  | { readonly kind: "passwordChanged" }
  /** Asking before this device is signed out. */
  | { readonly kind: "signOutThis" };

export interface AccountSecurityData {
  /** The single sign-on provider that owns the password; `null` = none. */
  readonly ssoProvider: string | null;
  readonly email: string;
  readonly workspaceName: string;
  readonly twoFactorEnabled: boolean;
  readonly backupCodesLeft: number;
  readonly sessions: readonly AccountSessionView[];
  /** "Now" for the relative times; the producer's clock. */
  readonly now: string;
  /** The person's own recent security history, newest first. */
  readonly activity?: readonly AccountActivityView[];
  /** More activity lines can be loaded. */
  readonly activityHasMore?: boolean;
}

export interface AccountSecuritySurfaceProps {
  readonly copy: AccountSecurityCopy;
  readonly security: AccountSection<AccountSecurityData>;
  /** An action is running: its buttons wait. */
  readonly busy: boolean;
  readonly actionError: boolean;
  /** The text of the toast at the foot of the page; `null` = none. */
  readonly notice: string | null;
  /** The step of two-factor or a sensitive change in progress; `null`/absent = none. */
  readonly flow?: AccountSecurityFlow | null;
  readonly onSetTwoFactor: (enabled: boolean) => void;
  /** Opens the change of password; absent = the account cannot change it here. */
  readonly onChangePassword?: () => void;
  readonly onSubmitNewPassword?: (
    submission: AccountPasswordSubmission,
  ) => void;
  /** After a change of password: leaves for the sign-in page. */
  readonly onPasswordChangedContinue?: () => void;
  /** Answers the open step: the authenticator's digits or a backup code. */
  readonly onSubmitCode?: (code: string) => void;
  readonly onSubmitPassword?: (password: string) => void;
  readonly onCloseFlow?: () => void;
  /** Signs this device out after the confirmation. */
  readonly onConfirmSignOutThis?: () => void;
  readonly onLoadMoreActivity?: () => void;
  readonly onRenewBackupCodes: () => void;
  readonly onSignOut: (sessionId: string) => void;
  readonly onSignOutOthers: () => void;
  readonly onRetry: () => void;
}

const SESSION_ICON: Record<AccountSessionView["kind"], LucideIcon> = {
  desktop: Laptop,
  phone: Smartphone,
  tablet: Tablet,
};

/** "2 giờ trước": the age of an instant against the producer's now. */
export function relativeAge(
  copy: AccountSecurityCopy,
  at: string,
  now: string,
): string {
  const minutes = Math.max(
    0,
    Math.round((Date.parse(now) - Date.parse(at)) / 60_000),
  );
  if (minutes < 1) return copy.time.now;
  if (minutes < 60)
    return pluralCopy(
      { one: copy.time.minutes_one, other: copy.time.minutes_other },
      minutes,
    );
  const hours = Math.round(minutes / 60);
  if (hours < 24)
    return pluralCopy(
      { one: copy.time.hours_one, other: copy.time.hours_other },
      hours,
    );
  return pluralCopy(
    { one: copy.time.days_one, other: copy.time.days_other },
    Math.round(hours / 24),
  );
}

function Icon({ icon: Glyph }: { icon: LucideIcon }) {
  return (
    <span
      aria-hidden="true"
      className="grid size-[34px] shrink-0 place-items-center rounded-[9px] border border-line bg-background text-muted-foreground"
    >
      <Glyph className="size-4" strokeWidth={1.8} />
    </span>
  );
}

function Block({
  titleId,
  title,
  aside,
  children,
}: {
  titleId: string;
  title: string;
  aside: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section
      aria-labelledby={titleId}
      className="rounded-[12px] border border-line bg-popover dark:bg-card"
    >
      <div className="flex items-center gap-2.5 border-b border-line px-4 py-[13px]">
        <h3
          id={titleId}
          className="m-0 text-body leading-[1.45] font-semibold text-foreground"
        >
          {title}
        </h3>
        {aside}
      </div>
      <div>{children}</div>
    </section>
  );
}

function Row({
  icon,
  title,
  text,
  control,
}: {
  icon: LucideIcon;
  title: React.ReactNode;
  text: string;
  control: React.ReactNode;
}) {
  return (
    <div
      data-f="row"
      className="flex items-center gap-3 border-t border-line px-4 py-[11px] first:border-t-0"
    >
      <Icon icon={icon} />
      <span className="min-w-0 flex-1">
        <b className="block text-nav leading-[1.45] font-semibold text-foreground">
          {title}
        </b>
        <small className="block text-xs leading-[1.45] text-meta-foreground">
          {text}
        </small>
      </span>
      {control}
    </div>
  );
}

/** The step in progress: add the authenticator, prove who is asking, or keep the new backup codes. */
function FlowPanel({
  copy,
  flow,
  busy,
  onSubmitCode,
  onSubmitPassword,
  onClose,
}: {
  copy: AccountSecurityCopy;
  flow: Exclude<
    AccountSecurityFlow,
    { kind: "password" } | { kind: "passwordChanged" } | { kind: "signOutThis" }
  >;
  busy: boolean;
  onSubmitCode?: (code: string) => void;
  onSubmitPassword?: (password: string) => void;
  onClose?: () => void;
}) {
  const [value, setValue] = React.useState("");
  const inputId = React.useId();
  const inputRef = React.useRef<HTMLInputElement>(null);
  const doneRef = React.useRef<HTMLButtonElement>(null);
  const t = copy.flow;
  // The step opens in place: focus lands on what the person does next (the code field, or the
  // button that confirms the backup codes were saved).
  const focusKey =
    flow.kind === "stepUp" ? `stepUp-${flow.purpose}` : flow.kind;
  const confirmsCodes = flow.kind === "codes";
  React.useEffect(() => {
    (confirmsCodes ? doneRef : inputRef).current?.focus();
  }, [focusKey, confirmsCodes]);
  if (flow.kind === "codes")
    return (
      <section
        aria-label={t.codesTitle}
        className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
      >
        <b className="block text-body font-semibold text-foreground">
          {t.codesTitle}
        </b>
        <p className="m-0 mt-1 text-crumb text-muted-foreground">
          {t.codesText}
        </p>
        <ul className="m-0 mt-2.5 grid list-none grid-cols-2 gap-1.5 p-0 font-mono text-nav">
          {flow.codes.map((code) => (
            <li key={code}>{code}</li>
          ))}
        </ul>
        <button
          ref={doneRef}
          type="button"
          onClick={onClose}
          className={cn(BTN_SM, FOCUS, "mt-3")}
        >
          {t.done}
        </button>
      </section>
    );
  const usesPassword = flow.kind === "stepUp" && flow.factor === "password";
  const failure =
    flow.error === "wrong"
      ? usesPassword
        ? t.wrongPassword
        : t.wrongCode
      : flow.error === "wait"
        ? fillCopy(t.wait, { seconds: flow.wait ?? 0 })
        : flow.error === "failed"
          ? copy.actionError
          : null;
  const title =
    flow.kind === "enroll"
      ? t.enrollTitle
      : flow.purpose === "disable"
        ? t.disableTitle
        : flow.purpose === "others"
          ? t.othersTitle
          : t.renewTitle;
  return (
    <section
      aria-label={title}
      className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
    >
      <b className="block text-body font-semibold text-foreground">{title}</b>
      {flow.kind === "enroll" ? (
        <>
          <p className="m-0 mt-1 text-crumb text-muted-foreground">
            {t.enrollText}
          </p>
          {flow.otpAuthUri ? (
            <AccountQrCode
              value={flow.otpAuthUri}
              label={t.qrLabel}
              className="mt-3 size-[168px] rounded-[8px] border border-line"
            />
          ) : null}
          <p className="m-0 mt-3 text-xs text-meta-foreground">
            {t.secretLabel}
          </p>
          <p
            data-f="secret"
            className="m-0 mt-1 font-mono text-nav break-all select-all"
          >
            {flow.secret}
          </p>
        </>
      ) : (
        <p className="m-0 mt-1 text-crumb text-muted-foreground">
          {usesPassword ? t.passwordText : t.codeText}
        </p>
      )}
      <form
        noValidate
        className="mt-2.5 flex flex-wrap items-end gap-2.5"
        onSubmit={(event) => {
          event.preventDefault();
          if (!value) return;
          if (usesPassword) onSubmitPassword?.(value);
          else onSubmitCode?.(value);
          setValue("");
        }}
      >
        <div className="flex flex-col gap-1">
          <label htmlFor={inputId} className="text-xs font-semibold">
            {usesPassword ? t.passwordLabel : t.codeLabel}
          </label>
          <input
            ref={inputRef}
            id={inputId}
            type={usesPassword ? "password" : "text"}
            autoComplete={usesPassword ? "current-password" : "one-time-code"}
            inputMode={usesPassword ? undefined : "numeric"}
            value={value}
            aria-invalid={failure ? true : undefined}
            onChange={(event) => setValue(event.target.value)}
            className={cn(INPUT, "min-w-[200px]")}
          />
        </div>
        <button
          type="submit"
          disabled={busy || !value || flow.error === "wait"}
          className={cn(BTN_PRIMARY, FOCUS)}
        >
          {t.confirm}
        </button>
        <button type="button" onClick={onClose} className={cn(BTN_SM, FOCUS)}>
          {t.cancel}
        </button>
      </form>
      {failure ? (
        <p role="alert" className="m-0 mt-2 text-crumb text-danger-text">
          {failure}
        </p>
      ) : null}
    </section>
  );
}

/** Signing out of the device in hand ends the session at once: ask first, with the safe answer focused. */
function SignOutThisPanel({
  copy,
  busy,
  onConfirm,
  onClose,
}: {
  copy: AccountSecurityCopy;
  busy: boolean;
  onConfirm?: () => void;
  onClose?: () => void;
}) {
  const cancelRef = React.useRef<HTMLButtonElement>(null);
  React.useEffect(() => {
    cancelRef.current?.focus();
  }, []);
  const t = copy.flow;
  return (
    <section
      aria-label={t.signOutThisTitle}
      className="rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
    >
      <b className="block text-body font-semibold text-foreground">
        {t.signOutThisTitle}
      </b>
      <p className="m-0 mt-1 text-crumb text-muted-foreground">
        {t.signOutThisText}
      </p>
      <div className="mt-3 flex flex-wrap items-center gap-2.5">
        <button
          type="button"
          disabled={busy}
          onClick={onConfirm}
          className={cn(BTN_DANGER, FOCUS)}
        >
          {t.signOutThisConfirm}
        </button>
        <button
          ref={cancelRef}
          type="button"
          onClick={onClose}
          className={cn(BTN_SM, FOCUS)}
        >
          {t.cancel}
        </button>
      </div>
    </section>
  );
}

function SecurityBody({
  copy,
  data,
  busy,
  actionError,
  notice,
  flow,
  onChangePassword,
  onSubmitNewPassword,
  onPasswordChangedContinue,
  onSetTwoFactor,
  onSubmitCode,
  onSubmitPassword,
  onCloseFlow,
  onConfirmSignOutThis,
  onLoadMoreActivity,
  onRenewBackupCodes,
  onSignOut,
  onSignOutOthers,
}: Omit<AccountSecuritySurfaceProps, "security" | "onRetry"> & {
  data: AccountSecurityData;
}) {
  const ids = {
    password: React.useId(),
    twoFactor: React.useId(),
    sessions: React.useId(),
    others: React.useId(),
    othersText: React.useId(),
    activity: React.useId(),
  };
  const others = data.sessions.filter((session) => !session.isCurrent);
  const current = data.sessions.find((session) => session.isCurrent);
  return (
    <>
      <Block
        titleId={ids.password}
        title={copy.password.title}
        aside={
          data.ssoProvider ? (
            <Tag>
              {fillCopy(copy.password.managed, {
                workspace: data.workspaceName,
              })}
            </Tag>
          ) : null
        }
      >
        <Row
          icon={Lock}
          title={
            data.ssoProvider
              ? fillCopy(copy.password.ssoTitle, {
                  provider: data.ssoProvider,
                })
              : copy.password.localTitle
          }
          text={
            data.ssoProvider
              ? fillCopy(copy.password.ssoText, {
                  email: data.email,
                  provider: data.ssoProvider,
                })
              : copy.password.localText
          }
          control={
            data.ssoProvider ? (
              <a
                href="https://mysignins.microsoft.com"
                target="_blank"
                rel="noopener"
                className={cn(
                  "text-compact leading-[1.45] font-semibold text-link underline underline-offset-2",
                  FOCUS,
                )}
              >
                {copy.password.ssoLink}
                <span className="sr-only">{copy.password.ssoLinkOpens}</span>
              </a>
            ) : onChangePassword ? (
              <button
                type="button"
                disabled={busy}
                onClick={onChangePassword}
                className={cn(BTN_SM, FOCUS)}
              >
                {copy.password.change}
              </button>
            ) : (
              <span className="text-xs text-meta-foreground">
                {copy.password.unavailable}
              </span>
            )
          }
        />
      </Block>

      <Block
        titleId={ids.twoFactor}
        title={copy.twoFactor.title}
        aside={
          <Tag tone={data.twoFactorEnabled ? "ok" : "neutral"}>
            {data.twoFactorEnabled
              ? copy.twoFactor.enabled
              : copy.twoFactor.disabled}
          </Tag>
        }
      >
        <Row
          icon={Smartphone}
          title={copy.twoFactor.appTitle}
          text={
            data.twoFactorEnabled
              ? copy.twoFactor.appText
              : copy.twoFactor.offText
          }
          control={
            data.twoFactorEnabled ? (
              <button
                type="button"
                disabled={busy}
                onClick={() => onSetTwoFactor(false)}
                className={cn(LINK_BUTTON_DANGER, FOCUS)}
              >
                {copy.twoFactor.disable}
              </button>
            ) : (
              <button
                type="button"
                disabled={busy}
                onClick={() => onSetTwoFactor(true)}
                className={cn(BTN_SM, FOCUS)}
              >
                {copy.twoFactor.enable}
              </button>
            )
          }
        />
        {data.twoFactorEnabled ? (
          <Row
            icon={KeyRound}
            title={copy.twoFactor.backupTitle}
            text={pluralCopy(
              {
                one: copy.twoFactor.backupText_one,
                other: copy.twoFactor.backupText_other,
              },
              data.backupCodesLeft,
            )}
            control={
              <button
                type="button"
                disabled={busy}
                onClick={onRenewBackupCodes}
                className={cn(BTN_SM, FOCUS)}
              >
                {copy.twoFactor.backupRenew}
              </button>
            }
          />
        ) : null}
      </Block>

      {flow?.kind === "password" ? (
        <AccountPasswordPanel
          copy={copy}
          needsCode={flow.needsCode}
          error={flow.error}
          wait={flow.wait}
          busy={busy}
          onSubmit={onSubmitNewPassword}
          onClose={onCloseFlow}
        />
      ) : flow?.kind === "passwordChanged" ? (
        <AccountPasswordChangedPanel
          copy={copy}
          onContinue={onPasswordChangedContinue}
        />
      ) : flow?.kind === "signOutThis" ? (
        <SignOutThisPanel
          copy={copy}
          busy={busy}
          onConfirm={onConfirmSignOutThis}
          onClose={onCloseFlow}
        />
      ) : flow ? (
        <FlowPanel
          copy={copy}
          flow={flow}
          busy={busy}
          onSubmitCode={onSubmitCode}
          onSubmitPassword={onSubmitPassword}
          onClose={onCloseFlow}
        />
      ) : null}

      <Block
        titleId={ids.sessions}
        title={copy.sessions.title}
        aside={
          <span className="text-crumb leading-[1.45] text-meta-foreground">
            {pluralCopy(
              {
                one: copy.sessions.count_one,
                other: copy.sessions.count_other,
              },
              data.sessions.length,
            )}
          </span>
        }
      >
        {data.sessions.map((session) => {
          const current = session.isCurrent;
          const where = current
            ? copy.sessions.active
            : fillCopy(copy.sessions.signedIn, {
                age: relativeAge(copy, session.signedInAt, data.now),
              });
          return (
            <Row
              key={session.id}
              icon={SESSION_ICON[session.kind]}
              title={
                <>
                  {session.device}
                  {current ? (
                    <Tag tone="accent" className="ml-1">
                      {copy.sessions.thisDevice}
                    </Tag>
                  ) : null}
                </>
              }
              text={where}
              control={
                <button
                  type="button"
                  disabled={busy}
                  aria-label={fillCopy(
                    current
                      ? copy.sessions.signOutThisLabel
                      : copy.sessions.signOutLabel,
                    { device: session.device },
                  )}
                  onClick={() => onSignOut(session.id)}
                  className={cn(BTN_SM, FOCUS)}
                >
                  {copy.sessions.signOut}
                </button>
              }
            />
          );
        })}
      </Block>

      {data.activity && data.activity.length > 0 ? (
        <Block titleId={ids.activity} title={copy.activity.title} aside={null}>
          {data.activity.map((line) => (
            <Row
              key={line.id}
              icon={KeyRound}
              title={
                copy.activity.types[
                  line.type as keyof typeof copy.activity.types
                ] ?? line.type
              }
              text={[
                relativeAge(copy, line.occurredAt, data.now),
                line.device,
                line.ipAddress,
              ]
                .filter(Boolean)
                .join(" · ")}
              control={null}
            />
          ))}
          {data.activityHasMore && onLoadMoreActivity ? (
            <div className="border-t border-line px-4 py-2.5">
              <button
                type="button"
                onClick={onLoadMoreActivity}
                className={cn(BTN_SM, FOCUS)}
              >
                {copy.activity.more}
              </button>
            </div>
          ) : null}
        </Block>
      ) : null}

      <div
        data-f="dz"
        className="flex items-center gap-3.5 rounded-[12px] border border-danger bg-danger-subtle px-4 py-[13px]"
      >
        <span className="min-w-0 flex-1">
          <b
            id={ids.others}
            className="block text-nav leading-[1.45] font-semibold text-danger-text"
          >
            {copy.others.title}
          </b>
          <small
            id={ids.othersText}
            className="block text-crumb leading-[1.45] text-foreground"
          >
            {others.length > 0
              ? fillCopy(copy.others.text, {
                  others: others
                    .map((session) => shortName(session))
                    .join(copy.others.and),
                  current: current ? shortName(current) : "",
                })
              : copy.others.nobody}
          </small>
        </span>
        <button
          type="button"
          disabled={busy || others.length === 0}
          aria-describedby={ids.othersText}
          onClick={onSignOutOthers}
          className={cn(BTN_DANGER, FOCUS)}
        >
          {copy.others.action}
        </button>
      </div>
      {actionError ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {copy.actionError}
        </p>
      ) : null}
      {notice ? <Toast>{notice}</Toast> : null}
    </>
  );
}

/** "iPhone 15" of "iPhone 15 · ứng dụng Notrelix". */
function shortName(session: AccountSessionView): string {
  return session.device.split(" · ")[0] ?? session.device;
}

/**
 * Account security (Final-Account `bao-mat`): how the password is managed,
 * two-factor and its backup codes, the signed-in devices and the sign-out of
 * every other device. The container owns the writes.
 */
export function AccountSecuritySurface(props: AccountSecuritySurfaceProps) {
  const { copy, security } = props;
  const titleId = React.useId();
  return (
    <section
      aria-labelledby={titleId}
      className="flex min-w-0 flex-col gap-[18px]"
    >
      <PageHead
        id={titleId}
        title={copy.title}
        description={copy.description}
      />
      {security.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.loading}
          className="flex flex-col gap-3"
        >
          <Skeleton className="h-24 w-full rounded-[12px]" />
          <Skeleton className="h-36 w-full rounded-[12px]" />
          <Skeleton className="h-44 w-full rounded-[12px]" />
        </div>
      ) : security.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={props.onRetry}
        />
      ) : (
        <SecurityBody {...props} data={security.data} />
      )}
    </section>
  );
}
