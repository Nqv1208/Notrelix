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
  BTN_SM,
  FOCUS,
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
import type { AccountSecurityCopy } from "./account-security.copy";

export interface AccountSessionView {
  readonly id: string;
  readonly device: string;
  readonly kind: "desktop" | "phone" | "tablet";
  readonly place: string;
  /** ISO timestamp; `null` = the device in your hands, in use right now. */
  readonly lastActiveAt: string | null;
}

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
}

export interface AccountSecuritySurfaceProps {
  readonly copy: AccountSecurityCopy;
  readonly security: AccountSection<AccountSecurityData>;
  /** An action is running: its buttons wait. */
  readonly busy: boolean;
  readonly actionError: boolean;
  /** The text of the toast at the foot of the page; `null` = none. */
  readonly notice: string | null;
  /** Where a password of this account is changed; `null` = nowhere yet. */
  readonly passwordHref: string | null;
  readonly onSetTwoFactor: (enabled: boolean) => void;
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

function SecurityBody({
  copy,
  data,
  busy,
  actionError,
  notice,
  passwordHref,
  onSetTwoFactor,
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
  };
  const others = data.sessions.filter((session) => session.lastActiveAt);
  const current = data.sessions.find((session) => !session.lastActiveAt);
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
            ) : passwordHref ? (
              <a href={passwordHref} className={cn(BTN_SM, FOCUS)}>
                {copy.password.change}
              </a>
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
          const current = session.lastActiveAt === null;
          const where = current
            ? `${session.place} · ${copy.sessions.active}`
            : `${session.place} · ${relativeAge(copy, session.lastActiveAt ?? data.now, data.now)}`;
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
