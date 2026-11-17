import * as React from "react";
import {
  AlertTriangle,
  Check,
  Copy,
  KeyRound,
  Plus,
  ShieldCheck,
  Trash2,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import type { AccountApiToken } from "../../core/types/account";
import {
  BTN,
  BTN_DANGER,
  BTN_PRIMARY,
  BTN_SM,
  FOCUS,
  INPUT,
  LoadError,
  PageHead,
  Tag,
  Toast,
} from "../shared/account-parts";
import { fillCopy, type AccountSection } from "../shared/account-surface-model";
import type { AccountTokensCopy } from "./account-tokens.copy";

/** What the person can pick for a token's life; the producer allows at most 365. */
export const TOKEN_LIFETIMES = [30, 90, 365] as const;
export type TokenLifetime = (typeof TOKEN_LIFETIMES)[number];

type FlowError = "wrong" | "wait" | "failed" | "expired" | "lifetime" | null;

/** The step of issuing a token. The secret lives in the `reveal` step alone and nowhere else. */
export type AccountTokensIssueFlow =
  | {
      readonly kind: "form";
      readonly name: string;
      readonly days: TokenLifetime;
      readonly error: "failed" | null;
    }
  | {
      readonly kind: "proof";
      readonly factor: "code" | "password" | "provider";
      readonly providers: readonly string[];
      readonly error: FlowError;
      readonly wait?: number;
    }
  | {
      readonly kind: "reveal";
      readonly secret: string;
      readonly name: string;
      readonly expiresAt: string;
    };

export interface AccountTokensSurfaceProps {
  readonly copy: AccountTokensCopy;
  readonly locale: string;
  readonly section: AccountSection<{
    readonly tokens: readonly AccountApiToken[];
    readonly hasMore: boolean;
  }>;
  /** The person may not manage tokens here: the same page a missing workspace gives. */
  readonly denied: boolean;
  /** The producer's now (ms), so ages and expiries read against one clock. */
  readonly now: number;
  readonly flow: AccountTokensIssueFlow | null;
  readonly busy: boolean;
  readonly loadingMore: boolean;
  readonly notice: string | null;
  readonly actionError: boolean;
  readonly onStart: () => void;
  readonly onSubmitForm: (input: { name: string; days: TokenLifetime }) => void;
  readonly onSubmitProof: (value: string) => void;
  readonly onProvider: (provider: string) => void;

  readonly onCloseFlow: () => void;
  readonly onRevoke: (token: AccountApiToken) => Promise<void>;
  readonly onLoadMore: () => void;
  readonly onRetry: () => void;
}

/** The dialog buttons of the boards: 36 px high, 14 px padding. */
const DLG = "h-9 px-3.5 text-nav";

const COLS = "grid-cols-[minmax(0,1.6fr)_130px_130px_92px]";
const ROW =
  "grid items-center gap-3.5 px-4 max-md:flex max-md:flex-wrap max-md:gap-x-3 max-md:gap-y-2";

function dateOf(locale: string, value: string): string {
  return new Intl.DateTimeFormat(locale, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(value));
}

function ageOf(locale: string, at: string, now: number): string {
  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
  const minutes = Math.max(0, Math.round((now - Date.parse(at)) / 60_000));
  if (minutes < 60) return rtf.format(-minutes, "minute");
  const hours = Math.round(minutes / 60);
  if (hours < 24) return rtf.format(-hours, "hour");
  return rtf.format(-Math.round(hours / 24), "day");
}

/** Round icon tile of a dialog head or an empty state. */
function Tile({
  tone,
  large,
  children,
}: {
  tone: "acc" | "ok" | "warn" | "danger" | "plain";
  large?: boolean;
  children: React.ReactNode;
}) {
  return (
    <span
      data-f="ico"
      className={cn(
        "grid shrink-0 place-items-center rounded-[10px] [&_svg]:size-[18px]",
        large ? "size-[52px] [&_svg]:size-6" : "size-9",
        tone === "acc" && "bg-state-selected text-link",
        tone === "ok" && "bg-success-subtle text-success-subtle-foreground",
        tone === "warn" && "bg-warning-subtle text-warning-subtle-foreground",
        tone === "danger" && "bg-danger-subtle text-danger-text",
        tone === "plain" && "bg-state-hover-strong text-muted-foreground",
      )}
    >
      {children}
    </span>
  );
}

function Banner({
  tone,
  title,
  text,
}: {
  tone: "plain" | "warn";
  title: string;
  text: string;
}) {
  return (
    <div
      role={tone === "warn" ? "alert" : "status"}
      data-f="ban"
      className="flex items-start gap-2.5 rounded-[10px] bg-state-hover-strong px-3.5 py-3 text-compact leading-normal text-muted-foreground"
    >
      {tone === "warn" ? (
        <AlertTriangle
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0 text-warning-subtle-foreground"
        />
      ) : (
        <ShieldCheck aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
      )}
      <div className="min-w-0">
        <b className="block font-[650] text-foreground">{title}</b>
        <p className="m-0 mt-0.5">{text}</p>
      </div>
    </div>
  );
}

/** The dialog every step shares: head, body, footer (Idn-Var-Tokens-*). */
function Shell({
  label,
  icon,
  title,
  locked,
  describedBy,
  role = "dialog",
  small,
  lead,
  onClose,
  footer,
  children,
}: {
  label: string;
  icon: React.ReactNode;
  title: string;
  /** The secret step: only its button leaves it. */
  locked?: boolean;
  describedBy?: string;
  role?: "dialog" | "alertdialog";
  /** The 440 px dialog of a short question (`.dlg.sm`). */
  small?: boolean;
  /** The sentence under the head (`.dp`). */
  lead?: React.ReactNode;
  onClose: () => void;
  footer: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !locked) onClose();
      }}
    >
      <DialogContent
        role={role}
        aria-label={label}
        aria-describedby={describedBy}
        showCloseButton={false}
        onEscapeKeyDown={(event) => {
          if (locked) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (locked) event.preventDefault();
        }}
        className={cn(
          "max-h-[calc(100%-2rem)] w-[520px] gap-0 overflow-auto rounded-[14px] border-0 bg-popover p-0 sm:max-w-[520px] max-sm:top-auto max-sm:bottom-0 max-sm:max-h-[92%] max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none dark:bg-card",
          small && "w-[440px] sm:max-w-[440px]",
        )}
      >
        <div
          data-f="dh"
          className="flex items-center gap-2.5 px-5 pt-[18px] pb-1"
        >
          <span className="mr-0.5 flex">{icon}</span>
          <DialogTitle className="m-0 text-lg leading-[1.45] font-[650] text-foreground">
            {title}
          </DialogTitle>
        </div>
        {lead}
        <div data-f="db" className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
          {children}
        </div>
        <div
          data-f="df"
          className="flex flex-wrap items-center gap-2 border-t border-line bg-background px-5 py-3.5"
        >
          {footer}
        </div>
      </DialogContent>
    </Dialog>
  );
}

function FormStep({
  copy,
  flow,
  busy,
  onSubmit,
  onClose,
}: {
  copy: AccountTokensCopy;
  flow: Extract<AccountTokensIssueFlow, { kind: "form" }>;
  busy: boolean;
  onSubmit: AccountTokensSurfaceProps["onSubmitForm"];
  onClose: () => void;
}) {
  const [name, setName] = React.useState(flow.name);
  const [days, setDays] = React.useState<TokenLifetime>(flow.days);
  const [missing, setMissing] = React.useState(false);
  const nameId = React.useId();
  const daysId = React.useId();
  const t = copy.form;
  const submit = () => {
    if (name.trim() === "") {
      setMissing(true);
      return;
    }
    onSubmit({ name: name.trim(), days });
  };
  return (
    <Shell
      label={t.title}
      icon={
        <Tile tone="acc">
          <KeyRound aria-hidden="true" />
        </Tile>
      }
      title={t.title}
      onClose={onClose}
      footer={
        <>
          <button
            type="button"
            onClick={onClose}
            className={cn(BTN, DLG, FOCUS)}
          >
            {t.cancel}
          </button>
          <span className="flex-1" />
          <button
            type="button"
            disabled={busy}
            onClick={submit}
            className={cn(BTN_PRIMARY, DLG, FOCUS)}
          >
            {t.submit}
          </button>
        </>
      }
    >
      <form
        className="contents"
        onSubmit={(event) => {
          event.preventDefault();
          submit();
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor={nameId}
            className="text-crumb font-semibold text-muted-foreground"
          >
            {t.name}
          </label>
          <input
            id={nameId}
            type="text"
            value={name}
            maxLength={256}
            placeholder={t.placeholder}
            aria-invalid={missing && name.trim() === "" ? true : undefined}
            onChange={(event) => {
              setName(event.target.value);
              setMissing(false);
            }}
            className={cn(INPUT, FOCUS)}
          />
          {missing ? (
            <p role="alert" className="m-0 text-crumb text-danger-text">
              {t.nameRequired}
            </p>
          ) : null}
        </div>
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor={daysId}
            className="text-crumb font-semibold text-muted-foreground"
          >
            {t.expiry}
          </label>
          <select
            id={daysId}
            value={days}
            onChange={(event) =>
              setDays(Number(event.target.value) as TokenLifetime)
            }
            className={cn(INPUT, FOCUS)}
          >
            {TOKEN_LIFETIMES.map((value) => (
              <option key={value} value={value}>
                {t[`days_${value}`]}
              </option>
            ))}
          </select>
          <p className="m-0 text-crumb text-meta-foreground">{t.hint}</p>
        </div>
        {flow.error ? (
          <p role="alert" className="m-0 text-crumb text-danger-text">
            {copy.proof.failed}
          </p>
        ) : null}
        <button type="submit" hidden />
      </form>
    </Shell>
  );
}

function ProofStep({
  copy,
  flow,
  busy,
  onSubmit,
  onProvider,
  onClose,
}: {
  copy: AccountTokensCopy;
  flow: Extract<AccountTokensIssueFlow, { kind: "proof" }>;
  busy: boolean;
  onSubmit: (value: string) => void;
  onProvider: (provider: string) => void;
  onClose: () => void;
}) {
  const [value, setValue] = React.useState("");
  const inputId = React.useId();
  const t = copy.proof;
  const usesProvider = flow.factor === "provider";
  const failure =
    flow.error === "wrong"
      ? flow.factor === "password"
        ? t.wrongPassword
        : t.wrongCode
      : flow.error === "wait"
        ? fillCopy(t.wait, { seconds: flow.wait ?? 0 })
        : flow.error === "expired"
          ? t.expired
          : flow.error === "lifetime"
            ? t.lifetime
            : flow.error === "failed"
              ? t.failed
              : null;
  return (
    <Shell
      label={t.title}
      icon={
        <Tile tone="acc">
          <ShieldCheck aria-hidden="true" />
        </Tile>
      }
      title={t.title}
      small
      lead={
        <p className="m-0 px-5 text-nav leading-[1.55] text-muted-foreground">
          {" "}
          {usesProvider ? t.providerText : t.text}{" "}
        </p>
      }
      onClose={onClose}
      footer={
        <>
          {" "}
          <button
            type="button"
            onClick={onClose}
            className={cn(BTN, DLG, FOCUS)}
          >
            {" "}
            {copy.form.cancel}{" "}
          </button>
          <span className="flex-1" />
          {usesProvider ? null : (
            <button
              type="button"
              disabled={busy || value.trim() === ""}
              onClick={() => onSubmit(value)}
              className={cn(BTN_PRIMARY, DLG, FOCUS)}
            >
              {t.confirm}
            </button>
          )}
        </>
      }
    >
      {usesProvider ? (
        <div className="flex flex-wrap gap-2">
          {flow.providers.map((provider) => (
            <button
              key={provider}
              type="button"
              disabled={busy}
              onClick={() => onProvider(provider)}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              {fillCopy(t.provider, { provider })}
            </button>
          ))}
        </div>
      ) : (
        <form
          className="flex flex-col gap-1.5"
          onSubmit={(event) => {
            event.preventDefault();
            if (value.trim() !== "" && !busy) onSubmit(value);
          }}
        >
          <label
            htmlFor={inputId}
            className="text-crumb font-semibold text-muted-foreground"
          >
            {flow.factor === "password" ? t.password : t.code}
          </label>
          <input
            id={inputId}
            autoFocus
            type={flow.factor === "password" ? "password" : "text"}
            inputMode={flow.factor === "password" ? undefined : "numeric"}
            autoComplete={
              flow.factor === "password" ? "current-password" : "one-time-code"
            }
            value={value}
            onChange={(event) => setValue(event.target.value)}
            aria-invalid={failure ? true : undefined}
            className={cn(INPUT, FOCUS)}
          />
        </form>
      )}
      {failure ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {failure}
        </p>
      ) : null}
    </Shell>
  );
}

function RevealStep({
  copy,
  locale,
  flow,
  onClose,
}: {
  copy: AccountTokensCopy;
  locale: string;
  flow: Extract<AccountTokensIssueFlow, { kind: "reveal" }>;
  onClose: () => void;
}) {
  const t = copy.reveal;
  const [copyFailed, setCopyFailed] = React.useState(false);
  const [copied, setCopied] = React.useState(false);
  const warnId = React.useId();
  const copyIt = async (): Promise<boolean> => {
    try {
      await navigator.clipboard.writeText(flow.secret);
      setCopied(true);
      setCopyFailed(false);
      return true;
    } catch {
      setCopyFailed(true);
      return false;
    }
  };
  return (
    <Shell
      label={t.title}
      icon={
        <Tile tone="ok">
          <Check aria-hidden="true" />
        </Tile>
      }
      title={t.title}
      describedBy={warnId}
      locked
      onClose={onClose}
      footer={
        <>
          <span className="flex-1" />
          <button
            type="button"
            onClick={() => {
              // A failed copy keeps the secret on screen once; the second press closes.
              if (copyFailed) return onClose();
              void copyIt().then((done) => {
                if (done) onClose();
              });
            }}
            className={cn(BTN_PRIMARY, DLG, FOCUS)}
          >
            {copyFailed ? (
              t.close
            ) : (
              <>
                <Copy aria-hidden="true" className="size-4" />
                {t.copyClose}
              </>
            )}
          </button>
        </>
      }
    >
      <div id={warnId}>
        <Banner tone="warn" title={t.warnTitle} text={t.warnText} />
      </div>
      <div
        data-f="tok-rev"
        className="flex items-center gap-2 rounded-[8px] border border-primary bg-state-selected px-3 py-2.5 font-mono text-crumb break-all"
      >
        <span data-testid="token-secret" className="min-w-0 select-all">
          {flow.secret}
        </span>
        <button
          type="button"
          aria-label={t.copy}
          onClick={() => void copyIt()}
          className={cn(
            "ml-auto grid size-8 shrink-0 place-items-center rounded-[7px] hover:bg-state-hover-strong",
            FOCUS,
          )}
        >
          {copied ? (
            <Check aria-hidden="true" className="size-4" />
          ) : (
            <Copy aria-hidden="true" className="size-4" />
          )}
        </button>
      </div>
      {copyFailed ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {t.copyFailed}
        </p>
      ) : null}
      <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
        {fillCopy(t.detail, {
          name: flow.name,
          date: dateOf(locale, flow.expiresAt),
        })}
      </p>
    </Shell>
  );
}

function RevokeStep({
  copy,
  token,
  locale,
  now,
  busy,
  onConfirm,
  onClose,
}: {
  copy: AccountTokensCopy;
  token: AccountApiToken;
  locale: string;
  now: number;
  busy: boolean;
  onConfirm: () => void;
  onClose: () => void;
}) {
  const t = copy.confirmRevoke;
  return (
    <Shell
      role="alertdialog"
      label={t.title}
      icon={
        <Tile tone="danger">
          <Trash2 aria-hidden="true" />
        </Tile>
      }
      title={t.title}
      small
      lead={
        <DialogDescription className="m-0 px-5 text-nav leading-[1.55] text-muted-foreground">
          {" "}
          {t.text}{" "}
        </DialogDescription>
      }
      onClose={onClose}
      footer={
        <>
          {" "}
          <button
            type="button"
            onClick={onClose}
            className={cn(BTN, DLG, FOCUS)}
          >
            {" "}
            {t.keep}
          </button>
          <span className="flex-1" />
          <button
            type="button"
            disabled={busy}
            onClick={onConfirm}
            className={cn(BTN_DANGER, DLG, FOCUS)}
          >
            {t.action}
          </button>
        </>
      }
    >
      <TokenIdentity token={token} copy={copy} locale={locale} now={now} />
      <Banner tone="warn" title={t.warnTitle} text={t.warnText} />
    </Shell>
  );
}

function TokenIdentity({
  token,
  copy,
  locale,
  now,
}: {
  token: AccountApiToken;
  copy: AccountTokensCopy;
  locale: string;
  now: number;
}) {
  return (
    <div data-f="who" className="flex min-w-0 items-center gap-[11px]">
      <Tile tone="plain">
        <KeyRound aria-hidden="true" />
      </Tile>
      <div className="min-w-0">
        <b className="block truncate text-nav font-semibold text-foreground">
          {token.name}
        </b>
        <small className="block truncate font-mono text-xs leading-[1.5] text-meta-foreground">
          {token.suffix ? `ntk_v1.••••••${token.suffix}` : "ntk_v1.••••••"}
          {" · "}
          {token.lastUsedAt
            ? ageOf(locale, token.lastUsedAt, now)
            : copy.neverUsed}
        </small>
      </div>
    </div>
  );
}

export function AccountTokensSurface(props: AccountTokensSurfaceProps) {
  const {
    copy,
    locale,
    section,
    denied,
    now,
    flow,
    busy,
    loadingMore,
    notice,
    actionError,
    onStart,
    onSubmitForm,
    onSubmitProof,
    onProvider,
    onCloseFlow,
    onRevoke,
    onLoadMore,
    onRetry,
  } = props;
  const [revoking, setRevoking] = React.useState<AccountApiToken | null>(null);
  const [revokeBusy, setRevokeBusy] = React.useState(false);
  const tokens = section.status === "ready" ? section.data.tokens : [];
  const empty = section.status === "ready" && tokens.length === 0;

  const confirmRevoke = async () => {
    if (!revoking) return;
    setRevokeBusy(true);
    try {
      await onRevoke(revoking);
    } finally {
      setRevokeBusy(false);
      setRevoking(null);
    }
  };

  return (
    <section
      aria-labelledby="tokens-title"
      className="flex flex-col gap-4"
      data-f="tokens"
    >
      <PageHead
        id="tokens-title"
        title={copy.title}
        description={denied ? "" : copy.description}
        action={
          denied ? undefined : (
            <button
              type="button"
              onClick={onStart}
              disabled={section.status !== "ready"}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              <Plus aria-hidden="true" className="size-4" />
              {copy.create}
            </button>
          )
        }
      />
      {denied ? (
        <EmptyCard
          tone="warn"
          title={copy.denied.title}
          text={copy.denied.text}
        />
      ) : section.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={onRetry}
        />
      ) : section.status === "loading" ? (
        <div
          aria-busy="true"
          aria-label={copy.loading}
          className="flex flex-col gap-2"
        >
          <Skeleton className="h-[60px] w-full rounded-[12px]" />
          <Skeleton className="h-[60px] w-full rounded-[12px]" />
        </div>
      ) : empty ? (
        <EmptyCard
          tone="acc"
          title={copy.empty.title}
          text={copy.empty.text}
          action={
            <button
              type="button"
              onClick={onStart}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              <Plus aria-hidden="true" className="size-4" />
              {copy.createFirst}
            </button>
          }
        />
      ) : (
        <>
          <div
            role="table"
            aria-label={copy.title}
            data-f="card"
            className="overflow-hidden rounded-[12px] border border-line bg-popover dark:bg-card"
          >
            <div role="rowgroup">
              <div
                role="row"
                data-f="th"
                className={cn(
                  ROW,
                  COLS,
                  "h-9 text-crumb font-semibold text-meta-foreground max-md:hidden",
                )}
              >
                <span role="columnheader">{copy.columns.name}</span>
                <span role="columnheader">{copy.columns.lastUsed}</span>
                <span role="columnheader">{copy.columns.expires}</span>
                <span role="columnheader" aria-hidden="true" />
              </div>
            </div>
            <div role="rowgroup">
              {tokens.map((token) => (
                <div
                  key={token.id}
                  role="row"
                  data-f="tr"
                  className={cn(
                    ROW,
                    COLS,
                    "min-h-[60px] border-t border-line py-2 hover:bg-background max-md:py-3",
                  )}
                >
                  <div role="cell" className="max-md:basis-full">
                    <TokenName token={token} copy={copy} />
                  </div>
                  <div
                    role="cell"
                    className="min-w-0 truncate text-compact max-md:hidden"
                  >
                    {token.lastUsedAt
                      ? ageOf(locale, token.lastUsedAt, now)
                      : copy.neverUsed}
                  </div>
                  <div
                    role="cell"
                    className="min-w-0 truncate text-compact max-md:hidden"
                  >
                    {token.expiresAt ? (
                      dateOf(locale, token.expiresAt)
                    ) : (
                      <Tag tone="neutral" className="text-danger-text">
                        {copy.noExpiry}
                      </Tag>
                    )}
                  </div>
                  <div
                    role="cell"
                    className="flex items-center justify-self-end max-md:ml-auto"
                  >
                    {token.status === "active" ? (
                      <button
                        type="button"
                        aria-label={fillCopy(copy.revokeLabel, {
                          name: token.name,
                        })}
                        onClick={() => setRevoking(token)}
                        className={cn(
                          BTN_SM,
                          "border-danger-text text-danger-text",
                          FOCUS,
                        )}
                      >
                        {copy.revoke}
                      </button>
                    ) : (
                      <Tag>
                        {token.status === "revoked"
                          ? copy.revokedTag
                          : copy.expiredTag}
                      </Tag>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
          {section.status === "ready" && section.data.hasMore ? (
            <button
              type="button"
              disabled={loadingMore}
              onClick={onLoadMore}
              className={cn(BTN, "self-center", FOCUS)}
            >
              {copy.loadMore}
            </button>
          ) : null}
          <Banner
            tone="plain"
            title={copy.banner.title}
            text={copy.banner.text}
          />
        </>
      )}
      {actionError ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {copy.actionError}
        </p>
      ) : null}
      {flow?.kind === "form" ? (
        <FormStep
          copy={copy}
          flow={flow}
          busy={busy}
          onSubmit={onSubmitForm}
          onClose={onCloseFlow}
        />
      ) : flow?.kind === "proof" ? (
        <ProofStep
          copy={copy}
          flow={flow}
          busy={busy}
          onSubmit={onSubmitProof}
          onProvider={onProvider}
          onClose={onCloseFlow}
        />
      ) : flow?.kind === "reveal" ? (
        <RevealStep
          copy={copy}
          locale={locale}
          flow={flow}
          onClose={onCloseFlow}
        />
      ) : null}
      {revoking ? (
        <RevokeStep
          copy={copy}
          token={revoking}
          locale={locale}
          now={now}
          busy={revokeBusy}
          onConfirm={() => void confirmRevoke()}
          onClose={() => setRevoking(null)}
        />
      ) : null}
      {notice ? <Toast>{notice}</Toast> : null}
    </section>
  );
}

function TokenName({
  token,
  copy,
}: {
  token: AccountApiToken;
  copy: AccountTokensCopy;
}) {
  return (
    <div data-f="who" className="flex min-w-0 items-center gap-[11px]">
      <Tile tone="plain">
        <KeyRound aria-hidden="true" />
      </Tile>
      <div className="min-w-0">
        <b className="block truncate text-nav font-semibold text-foreground">
          {token.name}
        </b>
        <small className="block truncate font-mono text-xs leading-[1.5] text-meta-foreground">
          {token.suffix ? `ntk_v1.••••••${token.suffix}` : "ntk_v1.••••••"}
          {token.expiresAt === null ? ` · ${copy.noExpiryHint}` : ""}
        </small>
      </div>
    </div>
  );
}

function EmptyCard({
  tone,
  title,
  text,
  action,
}: {
  tone: "acc" | "warn";
  title: string;
  text: string;
  action?: React.ReactNode;
}) {
  return (
    <div
      data-f="card"
      className="rounded-[12px] border border-line bg-popover dark:bg-card"
    >
      <div className="flex flex-col items-center gap-2.5 px-5 py-9 text-center">
        <Tile tone={tone} large>
          <KeyRound aria-hidden="true" />
        </Tile>
        <h3 className="m-0 text-body font-[650] text-foreground">{title}</h3>
        <p className="m-0 max-w-[44ch] text-compact leading-[1.55] text-muted-foreground">
          {text}
        </p>
        {action}
      </div>
    </div>
  );
}
