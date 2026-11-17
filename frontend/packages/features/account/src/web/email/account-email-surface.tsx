import * as React from "react";
import { cn, Skeleton } from "@notrelix/ui-web";
import { AlertTriangle, Check } from "lucide-react";
import {
  BTN_PRIMARY,
  BTN_SM,
  FOCUS,
  INPUT,
  LoadError,
  PageHead,
  Toast,
} from "../shared/account-parts";
import { fillCopy } from "../shared/account-surface-model";
import type { AccountSecurityCopy } from "../security/account-security.copy";
import {
  FlowPanel,
  type AccountSecurityFlow,
} from "../security/account-security-surface";

/** What the last attempt ended in; `null` = nothing yet. */
export type AccountEmailError = "wait" | "failed";

/** The page's own steps: the proof for the change, then the link on its way. */
export type AccountEmailFlow =
  | Extract<AccountSecurityFlow, { kind: "stepUp" }>
  | { readonly kind: "emailSent"; readonly email: string };

const LOOKS_LIKE_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export interface AccountEmailSurfaceProps {
  readonly copy: AccountSecurityCopy;
  readonly status: "loading" | "ready" | "error";
  readonly currentEmail: string;
  readonly busy: boolean;
  readonly error: AccountEmailError | null;
  readonly wait?: number;
  readonly notice: string | null;
  readonly flow: AccountEmailFlow | null;
  /** The address the proof is being bought for (kept in the page, never sent before the proof). */
  readonly pendingEmail?: string;
  readonly onSubmitEmail?: (email: string) => void;
  readonly onSubmitCode?: (code: string) => void;
  readonly onSubmitPassword?: (password: string) => void;
  readonly onStartProviderStepUp?: (provider: string) => void;
  readonly onCloseFlow?: () => void;
  readonly onRetry?: () => void;
}

/** Idn-Email: the sign-in address, a new one, and the link sent to it. */
export function AccountEmailSurface(props: AccountEmailSurfaceProps) {
  const { copy, flow } = props;
  const t = copy.email;
  const titleId = React.useId();
  const currentId = React.useId();
  const newId = React.useId();
  const errorId = React.useId();
  const [value, setValue] = React.useState(props.pendingEmail ?? "");
  const [tried, setTried] = React.useState(false);
  const email = value.trim();
  const problem = !LOOKS_LIKE_EMAIL.test(email)
    ? t.invalid
    : email.toLowerCase() === props.currentEmail.toLowerCase()
      ? t.sameAsCurrent
      : null;
  const fieldError = tried ? problem : null;
  const failure =
    props.error === "wait"
      ? fillCopy(t.wait, { seconds: props.wait ?? 0 })
      : props.error === "failed"
        ? copy.actionError
        : null;
  return (
    <section aria-labelledby={titleId} className="flex min-w-0 flex-col gap-4">
      <PageHead id={titleId} title={t.title} description={t.pageText} />
      {props.status === "loading" ? (
        <div role="status" aria-label={copy.loading}>
          <Skeleton className="h-60 w-full max-w-[560px] rounded-[12px]" />
        </div>
      ) : props.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={props.onRetry ?? (() => undefined)}
        />
      ) : (
        <>
          {flow?.kind === "emailSent" ? (
            <div
              role="status"
              data-f="ban"
              className="flex items-start gap-2.5 rounded-[10px] bg-success-subtle px-3.5 py-3 text-compact leading-normal text-success-subtle-foreground"
            >
              <Check aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
              <div className="min-w-0">
                <b className="block font-[650]">{t.sentTitle}</b>
                <p className="m-0 mt-0.5">
                  {fillCopy(t.sentText, { email: flow.email })}
                </p>
                <p className="m-0 mt-0.5 text-xs">{t.sentNote}</p>
                <button
                  type="button"
                  onClick={props.onCloseFlow}
                  className={cn(BTN_SM, FOCUS, "mt-2")}
                >
                  {t.sentDone}
                </button>
              </div>
            </div>
          ) : null}
          <div
            data-f="card"
            className="max-w-[560px] rounded-[12px] border border-line bg-popover dark:bg-card"
          >
            <form
              noValidate
              className="flex flex-col gap-3.5 p-4"
              onSubmit={(event) => {
                event.preventDefault();
                setTried(true);
                if (props.busy || problem !== null) return;
                props.onSubmitEmail?.(email);
              }}
            >
              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor={currentId}
                  className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
                >
                  {t.current}
                </label>
                <input
                  id={currentId}
                  type="email"
                  readOnly
                  value={props.currentEmail}
                  className={INPUT}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor={newId}
                  className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
                >
                  {t.label}
                </label>
                <input
                  id={newId}
                  type="email"
                  autoComplete="email"
                  placeholder={t.placeholder}
                  value={value}
                  aria-invalid={fieldError ? true : undefined}
                  aria-describedby={fieldError ? errorId : undefined}
                  onChange={(event) => setValue(event.target.value)}
                  className={INPUT}
                />
                {fieldError ? (
                  <p
                    id={errorId}
                    role="alert"
                    className="m-0 flex items-start gap-1.5 text-compact leading-[1.45] font-medium text-danger-text"
                  >
                    <AlertTriangle
                      aria-hidden="true"
                      className="mt-0.5 size-[15px] shrink-0"
                    />
                    {fieldError}
                  </p>
                ) : null}
              </div>
              <div>
                <button
                  type="submit"
                  disabled={
                    props.busy ||
                    props.error === "wait" ||
                    flow?.kind === "stepUp"
                  }
                  className={cn(BTN_PRIMARY, FOCUS)}
                >
                  {t.submit}
                </button>
              </div>
              {failure ? (
                <p role="alert" className="m-0 text-crumb text-danger-text">
                  {failure}
                </p>
              ) : null}
            </form>
          </div>
          {flow?.kind === "stepUp" ? (
            <div className="max-w-[560px]">
              <FlowPanel
                copy={copy}
                flow={flow}
                busy={props.busy}
                onSubmitCode={props.onSubmitCode}
                onSubmitPassword={props.onSubmitPassword}
                onStartProvider={props.onStartProviderStepUp}
                onClose={props.onCloseFlow}
              />
            </div>
          ) : null}
        </>
      )}
      {props.notice ? <Toast>{props.notice}</Toast> : null}
    </section>
  );
}
