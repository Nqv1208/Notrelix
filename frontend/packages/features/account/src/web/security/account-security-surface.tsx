import * as React from "react";
import { Key, Shield, Smartphone, type LucideIcon } from "lucide-react";
import { Button, Skeleton } from "@notrelix/ui-web";
import {
  fillCopy,
  type AccountFormatters,
  type AccountSection,
} from "../shared/account-surface-model";
import type { AccountSecurityCopy } from "./account-security.copy";

/** The read-only security facts the page shows. */
export interface AccountSecurityData {
  readonly twoFactorEnabled: boolean;
  /** ISO timestamp; `null` when the producer does not report one. */
  readonly lastPasswordChange: string | null;
  readonly activeSessions: number;
}

export interface AccountSecuritySurfaceProps {
  readonly copy: AccountSecurityCopy;
  readonly formatters: AccountFormatters;
  readonly security: AccountSection<AccountSecurityData>;
  readonly onRetry: () => void;
}

function SecurityCard({
  icon: Icon,
  title,
  detail,
  children,
}: {
  icon: LucideIcon;
  title: string;
  detail: string;
  children?: React.ReactNode;
}) {
  const titleId = React.useId();
  return (
    <section
      aria-labelledby={titleId}
      className="flex min-w-0 flex-col gap-3 rounded-lg border border-border p-4"
    >
      <div className="flex min-w-0 items-center gap-3">
        <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted">
          <Icon aria-hidden="true" className="size-4 text-muted-foreground" />
        </div>
        <div className="flex min-w-0 flex-col gap-0.5">
          <h2 id={titleId} className="text-label text-foreground">
            {title}
          </h2>
          <p className="text-metadata text-muted-foreground">{detail}</p>
        </div>
      </div>
      {children}
    </section>
  );
}

/** A disabled action with the reason it is unavailable. */
function UnavailableAction({
  label,
  reason,
}: {
  label: string;
  reason: string;
}) {
  const reasonId = React.useId();
  return (
    <div className="flex min-w-0 flex-col items-start gap-1.5">
      <Button
        variant="outline"
        size="sm"
        disabled
        aria-describedby={reasonId}
        className="h-auto min-h-8 max-w-full whitespace-normal text-left"
      >
        {label}
      </Button>
      <p id={reasonId} className="text-metadata text-muted-foreground">
        {reason}
      </p>
    </div>
  );
}

function SecuritySkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-4">
      {Array.from({ length: 3 }, (_, index) => (
        <Skeleton key={index} className="h-24 w-full rounded-lg" />
      ))}
    </div>
  );
}

/**
 * Account security: password, two-factor authentication and active sessions.
 * The facts are read-only; the change actions stay disabled with a stated
 * reason until the producer exposes them.
 */
export function AccountSecuritySurface({
  copy,
  formatters,
  security,
  onRetry,
}: AccountSecuritySurfaceProps) {
  return (
    <div className="mx-auto flex w-full min-w-0 max-w-2xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.title}</h1>
        <p className="text-body text-muted-foreground">{copy.description}</p>
      </header>

      {security.status === "loading" ? (
        <SecuritySkeleton label={copy.loading} />
      ) : security.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border px-3 py-2"
        >
          <p className="min-w-0 text-body text-muted-foreground">
            {copy.loadError}
          </p>
          <Button size="xs" variant="outline" onClick={onRetry}>
            {copy.retry}
          </Button>
        </div>
      ) : (
        <div className="flex min-w-0 flex-col gap-4">
          <SecurityCard
            icon={Key}
            title={copy.password.title}
            detail={
              security.data.lastPasswordChange
                ? fillCopy(copy.password.lastChanged, {
                    date: formatters.date(security.data.lastPasswordChange),
                  })
                : copy.password.lastChangedUnknown
            }
          >
            <UnavailableAction
              label={copy.password.change}
              reason={copy.password.unavailable}
            />
          </SecurityCard>
          <SecurityCard
            icon={Smartphone}
            title={copy.twoFactor.title}
            detail={
              security.data.twoFactorEnabled
                ? copy.twoFactor.enabled
                : copy.twoFactor.disabled
            }
          >
            <UnavailableAction
              label={
                security.data.twoFactorEnabled
                  ? copy.twoFactor.disable
                  : copy.twoFactor.enable
              }
              reason={copy.twoFactor.unavailable}
            />
          </SecurityCard>
          <SecurityCard
            icon={Shield}
            title={copy.sessions.title}
            detail={fillCopy(
              security.data.activeSessions === 1
                ? copy.sessions.count_one
                : copy.sessions.count_other,
              { count: formatters.number(security.data.activeSessions) },
            )}
          />
        </div>
      )}
    </div>
  );
}
