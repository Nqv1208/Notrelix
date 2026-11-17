import * as React from "react";
import { cn, Skeleton } from "@notrelix/ui-web";
import { Link2, Lock } from "lucide-react";
import {
  BTN_SM,
  FOCUS,
  LINK_BUTTON,
  LoadError,
  PageHead,
  Tag,
  Toast,
} from "../shared/account-parts";
import type { AccountSecurityCopy } from "../security/account-security.copy";
import {
  FlowPanel,
  type AccountSecurityFlow,
} from "../security/account-security-surface";

/** The page's own step: the proof before a link or an unlink. */
export type AccountProvidersFlow = Extract<
  AccountSecurityFlow,
  { kind: "stepUp" }
>;

/** The sign-in providers a person can link. */
const PROVIDERS = ["Google", "Microsoft"] as const;

const named = (text: string, provider: string) =>
  text.replaceAll("{{provider}}", provider);

export interface AccountProvidersSurfaceProps {
  readonly copy: AccountSecurityCopy;
  readonly status: "loading" | "ready" | "error";
  readonly linkedProviders: readonly string[];
  readonly hasPassword: boolean;
  readonly busy: boolean;
  readonly notice: string | null;
  readonly problem: string | null;
  readonly flow: AccountProvidersFlow | null;
  /** Leaves for the place where a first password is set. */
  readonly onSetPassword?: () => void;
  readonly onLink?: (provider: string) => void;
  readonly onUnlink?: (provider: string) => void;
  readonly onSubmitCode?: (code: string) => void;
  readonly onSubmitPassword?: (password: string) => void;
  readonly onStartProviderStepUp?: (provider: string) => void;
  readonly onCloseFlow?: () => void;
  readonly onRetry?: () => void;
}

function Glyph({ children }: { children: React.ReactNode }) {
  return (
    <span
      aria-hidden="true"
      className="grid size-9 shrink-0 place-items-center rounded-[10px] border border-line bg-background text-muted-foreground"
    >
      {children}
    </span>
  );
}

/** Idn-Providers: the providers that can sign this person in, and the ways in they have. */
export function AccountProvidersSurface(props: AccountProvidersSurfaceProps) {
  const { copy, flow } = props;
  const t = copy.providers;
  const titleId = React.useId();
  const linkedOf = (provider: string) =>
    props.linkedProviders.some(
      (name) => name.toLowerCase() === provider.toLowerCase(),
    );
  // The last way to sign in cannot go: the producer refuses it, and the page says so first.
  const last =
    !props.hasPassword && props.linkedProviders.length === 1
      ? (PROVIDERS.find(linkedOf) ?? null)
      : null;
  const rowClass =
    "flex items-center gap-[18px] border-t border-line px-4 py-3.5 first:border-t-0 max-sm:flex-wrap max-sm:items-start max-sm:gap-2.5";
  return (
    <section
      aria-labelledby={titleId}
      className="flex min-w-0 flex-col gap-4 max-sm:gap-3.5"
    >
      <PageHead
        id={titleId}
        title={t.title}
        description={last ? t.pageTextLast : t.pageText}
      />
      {props.status === "loading" ? (
        <div role="status" aria-label={copy.loading}>
          <Skeleton className="h-36 w-full rounded-[12px]" />
        </div>
      ) : props.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={props.onRetry ?? (() => undefined)}
        />
      ) : (
        <>
          {last ? (
            <div
              data-f="ban"
              className="flex items-start gap-2.5 rounded-[10px] bg-warning-subtle px-3.5 py-3 text-compact leading-normal text-foreground"
            >
              <div className="min-w-0">
                <b className="block font-[650]">{named(t.lastTitle, last)}</b>
                <p className="m-0 mt-0.5">{t.lastMethod}</p>
                <button
                  type="button"
                  onClick={props.onSetPassword}
                  className={cn(LINK_BUTTON, FOCUS, "mt-2 py-0.5 text-compact")}
                >
                  {t.setPassword}
                </button>
              </div>
            </div>
          ) : null}
          <div
            data-f="card"
            className="rounded-[12px] border border-line bg-popover dark:bg-card"
          >
            {PROVIDERS.map((provider) => {
              const linked = linkedOf(provider);
              return (
                <div key={provider} data-f="row" className={rowClass}>
                  <Glyph>
                    <Link2 className="size-4" strokeWidth={1.8} />
                  </Glyph>
                  <span className="min-w-0 flex-1 max-sm:basis-[calc(100%-46px)]">
                    <b className="block text-body leading-[1.45] font-semibold text-foreground">
                      {provider}
                      {linked ? (
                        <>
                          {" "}
                          <Tag tone="ok">{t.connected}</Tag>
                        </>
                      ) : null}
                    </b>
                    <small className="mt-1 block text-xs leading-[1.45] text-meta-foreground">
                      {linked ? t.connectedText : t.notConnectedText}
                    </small>
                  </span>
                  <div className="flex items-center gap-2 max-sm:w-full max-sm:[&>button]:flex-1">
                    {linked ? (
                      <button
                        type="button"
                        disabled={props.busy || last === provider}
                        aria-label={`${t.disconnect} ${provider}`}
                        onClick={() => props.onUnlink?.(provider)}
                        className={cn(
                          BTN_SM,
                          FOCUS,
                          last !== provider &&
                            "border-danger-text text-danger-text",
                        )}
                      >
                        {t.disconnect}
                      </button>
                    ) : (
                      <button
                        type="button"
                        disabled={props.busy}
                        aria-label={`${t.connect} ${provider}`}
                        onClick={() => props.onLink?.(provider)}
                        className={cn(BTN_SM, FOCUS)}
                      >
                        {t.connect}
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
            {props.problem ? (
              <p
                role="alert"
                className="m-0 border-t border-line px-4 py-3.5 text-crumb text-danger-text"
              >
                {props.problem}
              </p>
            ) : null}
          </div>
          {last === null ? (
            <div
              data-f="card2"
              className="rounded-[12px] border border-line bg-popover dark:bg-card"
            >
              <h3 className="m-0 border-b border-line px-4 py-3 text-compact leading-[1.45] font-[650] text-foreground">
                {t.methodsTitle}
              </h3>
              <div data-f="row" className={rowClass}>
                <Glyph>
                  <Lock className="size-4" strokeWidth={1.8} />
                </Glyph>
                <span className="min-w-0 flex-1">
                  <b className="block text-body leading-[1.45] font-semibold text-foreground">
                    {t.passwordTitle}
                  </b>
                  <small className="mt-1 block text-xs leading-[1.45] text-meta-foreground">
                    {props.hasPassword ? t.passwordSet : t.passwordNone}
                  </small>
                </span>
              </div>
              {props.linkedProviders.map((provider) => (
                <div key={provider} data-f="row" className={rowClass}>
                  <Glyph>
                    <Link2 className="size-4" strokeWidth={1.8} />
                  </Glyph>
                  <span className="min-w-0 flex-1">
                    <b className="block text-body leading-[1.45] font-semibold text-foreground">
                      {provider}
                    </b>
                    <small className="mt-1 block text-xs leading-[1.45] text-meta-foreground">
                      {t.connected}
                    </small>
                  </span>
                </div>
              ))}
            </div>
          ) : null}
          {flow ? (
            <FlowPanel
              copy={copy}
              flow={flow}
              busy={props.busy}
              onSubmitCode={props.onSubmitCode}
              onSubmitPassword={props.onSubmitPassword}
              onStartProvider={props.onStartProviderStepUp}
              onClose={props.onCloseFlow}
            />
          ) : null}
        </>
      )}
      {props.notice ? <Toast>{props.notice}</Toast> : null}
    </section>
  );
}
