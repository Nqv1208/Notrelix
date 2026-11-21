import * as React from "react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type {
  SsoDomain,
  SsoFailure,
  SsoProvider,
  SsoProviderInput,
  SsoProviderSaved,
  SsoSettings,
} from "../../core/sso";
import {
  BTN_PRIMARY,
  BTN_SM,
  FOCUS,
  INPUT,
  LINK_BUTTON_DANGER,
  LoadError,
  Note,
  Tag,
  Toggle,
} from "../shared/account-parts";
import { fillCopy, type AccountSection } from "../shared/account-surface-model";
import type { AccountSecurityCopy } from "./account-security.copy";

/** The page's view of the setup: a section that can also be refused to everyone but the Owner. */
export type AccountSsoState =
  AccountSection<SsoSettings> | { readonly status: "forbidden" };

export type SsoResult<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly failure: SsoFailure };

/** What the panel asks of its host; each answers when the server has. */
export interface AccountSsoActions {
  readonly saveProvider: (
    input: SsoProviderInput,
  ) => Promise<SsoResult<SsoProviderSaved>>;
  readonly removeProvider: () => Promise<SsoResult<null>>;
  readonly addDomain: (domain: string) => Promise<SsoResult<SsoDomain>>;
  readonly verifyDomain: (id: string) => Promise<SsoResult<SsoDomain>>;
  readonly removeDomain: (id: string) => Promise<SsoResult<null>>;
  readonly setRequired: (required: boolean) => Promise<SsoResult<null>>;
  readonly retry: () => void;
}

export interface AccountSsoPanelProps {
  readonly copy: AccountSecurityCopy;
  readonly state: AccountSsoState;
  readonly actions: AccountSsoActions;
}

type Message = { readonly tone: "ok" | "error"; readonly text: string };

const TITLE_CLASS =
  "m-0 text-body leading-[1.45] font-semibold text-foreground";
const SUB_CLASS = "border-t border-line px-4 py-3 first:border-t-0";
const HINT_CLASS = "text-xs leading-[1.45] text-meta-foreground";
const ERROR_CLASS = "text-xs leading-[1.45] text-danger-text";

function isHttpsUrl(value: string): boolean {
  try {
    return new URL(value.trim()).protocol === "https:";
  } catch {
    return false;
  }
}

function CopyButton({
  text,
  label,
  copy,
}: {
  text: string;
  label: string;
  copy: AccountSecurityCopy["sso"]["provider"];
}) {
  const [state, setState] = React.useState<"idle" | "copied" | "failed">(
    "idle",
  );
  React.useEffect(() => {
    if (state === "idle") return;
    const timer = setTimeout(() => setState("idle"), 2500);
    return () => clearTimeout(timer);
  }, [state]);
  return (
    <>
      <button
        type="button"
        aria-label={label}
        onClick={() => {
          const write = navigator.clipboard?.writeText(text);
          if (!write) {
            setState("failed");
            return;
          }
          write.then(
            () => setState("copied"),
            () => setState("failed"),
          );
        }}
        className={cn(BTN_SM, FOCUS)}
      >
        {state === "copied" ? copy.copied : copy.copy}
      </button>
      <span role="status" className="sr-only">
        {state === "copied"
          ? copy.copied
          : state === "failed"
            ? copy.copyFailed
            : ""}
      </span>
      {state === "failed" ? (
        <span className={ERROR_CLASS}>{copy.copyFailed}</span>
      ) : null}
    </>
  );
}

function Field({
  id,
  label,
  hint,
  error,
  children,
}: {
  id: string;
  label: string;
  hint?: string;
  error: string | null;
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-w-[220px] flex-1 flex-col gap-1">
      <label htmlFor={id} className="text-xs font-semibold">
        {label}
      </label>
      {children}
      {hint ? (
        <span id={`${id}-hint`} className={HINT_CLASS}>
          {hint}
        </span>
      ) : null}
      {error ? (
        <span id={`${id}-error`} role="alert" className={ERROR_CLASS}>
          {error}
        </span>
      ) : null}
    </div>
  );
}

const describedBy = (id: string, hint: boolean, error: boolean) =>
  [hint ? `${id}-hint` : null, error ? `${id}-error` : null]
    .filter(Boolean)
    .join(" ") || undefined;

/** The provider's summary, or the form that grows out of it in place. */
function ProviderBlock({
  copy,
  provider,
  redirectUri,
  requireSso,
  actions,
  onMessage,
}: {
  copy: AccountSecurityCopy;
  provider: SsoProvider | null;
  redirectUri: string;
  requireSso: boolean;
  actions: AccountSsoActions;
  onMessage: (message: Message) => void;
}) {
  const t = copy.sso.provider;
  const base = React.useId();
  const ids = {
    name: `${base}-name`,
    issuer: `${base}-issuer`,
    clientId: `${base}-client`,
    secret: `${base}-secret`,
    jit: `${base}-jit`,
    redirect: `${base}-redirect`,
  };
  const [editing, setEditing] = React.useState(false);
  const [name, setName] = React.useState("");
  const [issuerUrl, setIssuerUrl] = React.useState("");
  const [clientId, setClientId] = React.useState("");
  const [secret, setSecret] = React.useState("");
  const [jit, setJit] = React.useState(true);
  const [tried, setTried] = React.useState(false);
  const [busy, setBusy] = React.useState<"save" | "remove" | null>(null);
  const [serverError, setServerError] = React.useState<SsoFailure | null>(null);
  const [confirmRemove, setConfirmRemove] = React.useState(false);
  const nameRef = React.useRef<HTMLInputElement>(null);
  const issuerRef = React.useRef<HTMLInputElement>(null);
  const clientRef = React.useRef<HTMLInputElement>(null);
  const secretRef = React.useRef<HTMLInputElement>(null);
  const openerRef = React.useRef<HTMLButtonElement>(null);
  const wasEditing = React.useRef(false);

  // The form opens in place: focus lands on its first field; on close it returns to the opener.
  React.useEffect(() => {
    if (editing) nameRef.current?.focus();
    else if (wasEditing.current) openerRef.current?.focus();
    wasEditing.current = editing;
  }, [editing]);

  const open = () => {
    setName(provider?.name ?? "");
    setIssuerUrl(provider?.issuerUrl ?? "");
    setClientId(provider?.clientId ?? "");
    // Write-only: the stored secret is never read back, so the field always starts empty.
    setSecret("");
    setJit(provider?.jitProvisioning ?? true);
    setTried(false);
    setServerError(null);
    setEditing(true);
  };

  const problems = {
    name: name.trim() === "" ? t.nameRequired : null,
    issuer: isHttpsUrl(issuerUrl) ? null : t.issuerInvalid,
    clientId: clientId.trim() === "" ? t.clientIdRequired : null,
    secret:
      !provider?.hasClientSecret && secret.trim() === ""
        ? t.secretRequired
        : null,
  };
  // The server checks the same fields again; its refusal lands next to the field.
  const nameText =
    (tried ? problems.name : null) ??
    (serverError === "invalid-name" ? t.nameRequired : null);
  const clientText =
    (tried ? problems.clientId : null) ??
    (serverError === "invalid-client" ? t.clientIdRequired : null);
  const secretText =
    (tried ? problems.secret : null) ??
    (serverError === "client-secret-required" ? t.secretRequired : null);
  const issuerText =
    (tried ? problems.issuer : null) ??
    (serverError === "invalid-issuer" ? t.invalidIssuer : null);
  const formError =
    serverError === "invalid"
      ? t.invalid
      : serverError === "in-use"
        ? t.inUseReplace
        : serverError === "failed"
          ? copy.sso.failed
          : serverError === "forbidden"
            ? copy.sso.forbiddenAction
            : null;

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setTried(true);
    setServerError(null);
    if (problems.name) return void nameRef.current?.focus();
    if (problems.issuer) return void issuerRef.current?.focus();
    if (problems.clientId) return void clientRef.current?.focus();
    if (problems.secret) return void secretRef.current?.focus();
    setBusy("save");
    const result = await actions.saveProvider({
      name: name.trim(),
      issuerUrl: issuerUrl.trim(),
      clientId: clientId.trim(),
      ...(secret.trim() === "" ? {} : { clientSecret: secret }),
      jitProvisioning: jit,
    });
    setBusy(null);
    if (result.ok) {
      setSecret("");
      setEditing(false);
      onMessage({
        tone: "ok",
        text: result.value.status === "Active" ? t.savedActive : t.savedDraft,
      });
    } else {
      setServerError(result.failure);
      if (result.failure === "invalid-issuer") issuerRef.current?.focus();
      else if (result.failure === "invalid-name") nameRef.current?.focus();
      else if (result.failure === "invalid-client") clientRef.current?.focus();
      else if (result.failure === "client-secret-required")
        secretRef.current?.focus();
    }
  };

  const remove = async () => {
    setBusy("remove");
    const result = await actions.removeProvider();
    setBusy(null);
    setConfirmRemove(false);
    if (result.ok) onMessage({ tone: "ok", text: t.removed });
    else
      onMessage({
        tone: "error",
        text:
          result.failure === "in-use"
            ? t.inUse
            : result.failure === "forbidden"
              ? copy.sso.forbiddenAction
              : copy.sso.failed,
      });
    openerRef.current?.focus();
  };

  const statusText =
    provider?.status === "Active"
      ? t.statusActive
      : provider?.status === "Disabled"
        ? t.statusDisabled
        : t.statusDraft;

  return (
    <div className={SUB_CLASS} data-f="sso-provider">
      <div className="flex items-center gap-3 max-sm:flex-wrap">
        <h4 className={cn(TITLE_CLASS, "flex-1")}>{t.title}</h4>
        {provider && !editing ? (
          <>
            <button
              ref={openerRef}
              type="button"
              onClick={open}
              className={cn(BTN_SM, FOCUS)}
            >
              {t.edit}
            </button>
            {confirmRemove ? (
              <span className="flex items-center gap-2">
                <span className="text-crumb">{t.removeConfirm}</span>
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => void remove()}
                  className={cn(LINK_BUTTON_DANGER, FOCUS)}
                >
                  {t.removeYes}
                </button>
                <button
                  type="button"
                  onClick={() => setConfirmRemove(false)}
                  className={cn(BTN_SM, FOCUS)}
                >
                  {t.removeKeep}
                </button>
              </span>
            ) : (
              <button
                type="button"
                disabled={busy !== null}
                onClick={() => setConfirmRemove(true)}
                className={cn(LINK_BUTTON_DANGER, FOCUS)}
              >
                {t.remove}
              </button>
            )}
          </>
        ) : null}
      </div>

      {!provider && !editing ? (
        <div className="mt-1 flex items-center gap-3 max-sm:flex-wrap">
          <p className="m-0 flex-1 text-compact text-meta-foreground">
            {t.emptyText}
          </p>
          <button
            ref={openerRef}
            type="button"
            onClick={open}
            className={cn(BTN_PRIMARY, FOCUS)}
          >
            {t.setUp}
          </button>
        </div>
      ) : null}

      {provider && !editing ? (
        <div className="mt-1 flex flex-col gap-1.5">
          <p className="m-0 text-compact text-foreground">
            <b className="font-semibold">{provider.name}</b>
            {" · "}
            {fillCopy(t.issuerLine, {
              issuer: provider.issuerUrl,
              clientId: provider.clientId,
            })}
          </p>
          <p className="m-0 text-crumb text-muted-foreground">{statusText}</p>
          {provider.status !== "Active" && provider.validationError ? (
            <p className={cn("m-0", ERROR_CLASS)} role="alert">
              {fillCopy(t.draftReason, { reason: provider.validationError })}
            </p>
          ) : null}
          <p className="m-0 flex items-center gap-1.5 text-crumb text-muted-foreground">
            {provider.hasClientSecret ? (
              <Tag tone="ok">{t.secretSaved}</Tag>
            ) : null}
            {provider.jitProvisioning ? t.jitOn : t.jitOff}
          </p>
          <RedirectUri
            copy={copy}
            id={ids.redirect}
            redirectUri={redirectUri}
          />
          {requireSso ? (
            <p className={HINT_CLASS}>{copy.sso.require.inUse}</p>
          ) : null}
        </div>
      ) : null}

      {editing ? (
        <form
          noValidate
          onSubmit={(event) => void submit(event)}
          className="mt-2 flex flex-col gap-3"
        >
          <div className="flex flex-wrap gap-3">
            <Field
              id={ids.name}
              label={t.name}
              hint={t.nameHint}
              error={nameText}
            >
              <input
                ref={nameRef}
                id={ids.name}
                type="text"
                value={name}
                onChange={(event) => setName(event.target.value)}
                aria-invalid={nameText ? true : undefined}
                aria-describedby={describedBy(ids.name, true, !!nameText)}
                className={cn(INPUT, FOCUS)}
              />
            </Field>
            <Field
              id={ids.issuer}
              label={t.issuer}
              hint={t.issuerHint}
              error={issuerText}
            >
              <input
                ref={issuerRef}
                id={ids.issuer}
                type="url"
                inputMode="url"
                autoComplete="off"
                value={issuerUrl}
                onChange={(event) => setIssuerUrl(event.target.value)}
                aria-invalid={issuerText ? true : undefined}
                aria-describedby={describedBy(ids.issuer, true, !!issuerText)}
                className={cn(INPUT, FOCUS)}
              />
            </Field>
          </div>
          <div className="flex flex-wrap gap-3">
            <Field id={ids.clientId} label={t.clientId} error={clientText}>
              <input
                ref={clientRef}
                id={ids.clientId}
                type="text"
                autoComplete="off"
                value={clientId}
                onChange={(event) => setClientId(event.target.value)}
                aria-invalid={clientText ? true : undefined}
                aria-describedby={describedBy(
                  ids.clientId,
                  false,
                  !!clientText,
                )}
                className={cn(INPUT, FOCUS)}
              />
            </Field>
            <Field
              id={ids.secret}
              label={t.secret}
              hint={provider?.hasClientSecret ? t.secretKeep : t.secretHint}
              error={secretText}
            >
              <input
                ref={secretRef}
                id={ids.secret}
                type="password"
                autoComplete="new-password"
                value={secret}
                onChange={(event) => setSecret(event.target.value)}
                aria-invalid={secretText ? true : undefined}
                aria-describedby={describedBy(ids.secret, true, !!secretText)}
                className={cn(INPUT, FOCUS)}
              />
            </Field>
          </div>
          <div className="flex items-start gap-3">
            <Toggle
              id={ids.jit}
              checked={jit}
              onChange={setJit}
              aria-labelledby={`${ids.jit}-label`}
              aria-describedby={`${ids.jit}-text`}
            />
            <span className="min-w-0 flex-1">
              <b
                id={`${ids.jit}-label`}
                className="block text-nav leading-[1.45] font-semibold"
              >
                {t.jit}
              </b>
              <small id={`${ids.jit}-text`} className={cn("block", HINT_CLASS)}>
                {t.jitText}
              </small>
            </span>
          </div>
          <RedirectUri
            copy={copy}
            id={ids.redirect}
            redirectUri={redirectUri}
          />
          {formError ? (
            <p role="alert" className={cn("m-0", ERROR_CLASS)}>
              {formError}
            </p>
          ) : null}
          <div className="flex items-center gap-2">
            <button
              type="submit"
              disabled={busy !== null}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              {busy === "save" ? t.saving : t.save}
            </button>
            <button
              type="button"
              disabled={busy !== null}
              onClick={() => setEditing(false)}
              className={cn(BTN_SM, FOCUS)}
            >
              {t.cancel}
            </button>
          </div>
        </form>
      ) : null}
    </div>
  );
}

function RedirectUri({
  copy,
  id,
  redirectUri,
}: {
  copy: AccountSecurityCopy;
  id: string;
  redirectUri: string;
}) {
  const t = copy.sso.provider;
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-xs font-semibold">
        {t.redirect}
      </label>
      <div className="flex items-center gap-2">
        <input
          id={id}
          readOnly
          value={redirectUri}
          aria-describedby={`${id}-hint`}
          onFocus={(event) => event.currentTarget.select()}
          className={cn(INPUT, FOCUS, "font-mono")}
        />
        <CopyButton
          text={redirectUri}
          label={`${t.copy}: ${t.redirect}`}
          copy={t}
        />
      </div>
      <span id={`${id}-hint`} className={HINT_CLASS}>
        {t.redirectText}
      </span>
    </div>
  );
}

type RowNote = "pending" | "claimed" | "failed" | "forbidden" | "in-use";

function DomainRow({
  copy,
  domain,
  busy,
  note,
  rowRef,
  onVerify,
  onRemove,
}: {
  copy: AccountSecurityCopy;
  domain: SsoDomain;
  busy: "verify" | "remove" | null;
  note: RowNote | null;
  rowRef: (node: HTMLDivElement | null) => void;
  onVerify: () => void;
  onRemove: () => void;
}) {
  const t = copy.sso.domains;
  const [confirm, setConfirm] = React.useState(false);
  const values = { domain: domain.domain };
  const claimed = domain.claimed && domain.status !== "Verified";
  const tag =
    domain.status === "Verified"
      ? { tone: "ok" as const, text: t.verified }
      : claimed
        ? { tone: "neutral" as const, text: t.claimed }
        : domain.status === "Rejected"
          ? { tone: "neutral" as const, text: t.rejected }
          : { tone: "neutral" as const, text: t.pending };
  const showTxt =
    domain.status === "Pending" && !claimed && domain.txtValue !== null;
  const noteText =
    note === "pending"
      ? fillCopy(t.stillPending, values)
      : note === "claimed"
        ? fillCopy(t.claimedRow, values)
        : note === "in-use"
          ? t.inUse
          : note === "forbidden"
            ? copy.sso.forbiddenAction
            : note === "failed"
              ? copy.sso.failed
              : null;
  return (
    <li className={SUB_CLASS} data-f="sso-domain">
      <div className="flex items-center gap-3 max-sm:flex-wrap">
        {/* Focus lands here after a row is added or verified: the button that was pressed may be gone. */}
        <div
          ref={rowRef}
          tabIndex={-1}
          className={cn("flex min-w-0 flex-1 items-center gap-2", FOCUS)}
        >
          <b className="truncate text-nav font-semibold">{domain.domain}</b>
          <Tag tone={tag.tone}>{tag.text}</Tag>
        </div>
        {showTxt ? (
          <button
            type="button"
            disabled={busy !== null}
            aria-label={fillCopy(t.verifyLabel, values)}
            onClick={onVerify}
            className={cn(BTN_SM, FOCUS)}
          >
            {busy === "verify" ? t.verifying : t.verify}
          </button>
        ) : null}
        {confirm ? (
          <span className="flex items-center gap-2">
            <span className="text-crumb">
              {fillCopy(t.removeConfirm, values)}
            </span>
            <button
              type="button"
              disabled={busy !== null}
              onClick={() => {
                setConfirm(false);
                onRemove();
              }}
              className={cn(LINK_BUTTON_DANGER, FOCUS)}
            >
              {t.removeYes}
            </button>
            <button
              type="button"
              onClick={() => setConfirm(false)}
              className={cn(BTN_SM, FOCUS)}
            >
              {t.removeKeep}
            </button>
          </span>
        ) : (
          <button
            type="button"
            disabled={busy !== null}
            aria-label={fillCopy(t.removeLabel, values)}
            onClick={() => setConfirm(true)}
            className={cn(LINK_BUTTON_DANGER, FOCUS)}
          >
            {t.remove}
          </button>
        )}
      </div>
      {showTxt ? (
        <div className="mt-2 flex flex-col gap-1.5 rounded-[8px] border border-line bg-background px-3 py-2.5">
          <b className="text-crumb font-semibold">{t.txtTitle}</b>
          <dl className="m-0 grid grid-cols-[auto_1fr] items-center gap-x-3 gap-y-1.5 text-crumb max-sm:grid-cols-1">
            <dt className="text-meta-foreground">{t.txtHost}</dt>
            <dd className="m-0 flex items-center gap-2">
              <code className="min-w-0 break-all font-mono">
                {domain.txtHost}
              </code>
              <CopyButton
                text={domain.txtHost}
                label={fillCopy(t.copyHost, values)}
                copy={copy.sso.provider}
              />
            </dd>
            <dt className="text-meta-foreground">{t.txtValue}</dt>
            <dd className="m-0 flex items-center gap-2">
              <code className="min-w-0 break-all font-mono">
                {domain.txtValue}
              </code>
              <CopyButton
                text={domain.txtValue ?? ""}
                label={fillCopy(t.copyValue, values)}
                copy={copy.sso.provider}
              />
            </dd>
          </dl>
          <span className={HINT_CLASS}>{t.txtHint}</span>
        </div>
      ) : null}
      {claimed ? (
        <p className={cn("m-0 mt-1.5", ERROR_CLASS)}>
          {fillCopy(t.claimedRow, values)} {t.claimedHint}
        </p>
      ) : domain.status === "Rejected" ? (
        <p className={cn("m-0 mt-1.5", ERROR_CLASS)}>{t.rejectedHint}</p>
      ) : null}
      {noteText && !(note === "claimed" && claimed) ? (
        <p
          role={note === "pending" ? "status" : "alert"}
          className={cn(
            "m-0 mt-1.5",
            note === "pending" ? HINT_CLASS : ERROR_CLASS,
          )}
        >
          {noteText}
        </p>
      ) : null}
    </li>
  );
}

function DomainsBlock({
  copy,
  domains,
  actions,
  onMessage,
}: {
  copy: AccountSecurityCopy;
  domains: readonly SsoDomain[];
  actions: AccountSsoActions;
  onMessage: (message: Message) => void;
}) {
  const t = copy.sso.domains;
  const inputId = React.useId();
  const [value, setValue] = React.useState("");
  const [adding, setAdding] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [rowBusy, setRowBusy] = React.useState<{
    id: string;
    kind: "verify" | "remove";
  } | null>(null);
  const [notes, setNotes] = React.useState<Record<string, RowNote>>({});
  const [focusId, setFocusId] = React.useState<string | null>(null);
  const rows = React.useRef(new Map<string, HTMLDivElement>());
  const inputRef = React.useRef<HTMLInputElement>(null);

  // After an add or a verify the row itself takes focus: the Verify button may be gone.
  React.useEffect(() => {
    if (focusId) {
      rows.current.get(focusId)?.focus();
      setFocusId(null);
    }
  }, [focusId, domains]);

  const add = async (event: React.FormEvent) => {
    event.preventDefault();
    const domain = value.trim().toLowerCase();
    if (domain === "" || !/^[^\s@/]+\.[^\s@/]+$/.test(domain)) {
      setError(t.invalid);
      inputRef.current?.focus();
      return;
    }
    setError(null);
    setAdding(true);
    const result = await actions.addDomain(domain);
    setAdding(false);
    if (result.ok) {
      setValue("");
      setFocusId(result.value.id);
      onMessage({
        tone: "ok",
        text: fillCopy(
          result.value.status === "Verified" ? t.verifiedNow : t.added,
          { domain: result.value.domain },
        ),
      });
      return;
    }
    setError(
      result.failure === "claimed"
        ? t.claimedAdd
        : result.failure === "public-provider"
          ? t.publicProvider
          : result.failure === "invalid-domain" || result.failure === "invalid"
            ? t.invalid
            : result.failure === "forbidden"
              ? copy.sso.forbiddenAction
              : copy.sso.failed,
    );
    inputRef.current?.focus();
  };

  const setNote = (id: string, note: RowNote | null) =>
    setNotes((current) => {
      const next = { ...current };
      if (note) next[id] = note;
      else delete next[id];
      return next;
    });

  const verify = async (domain: SsoDomain) => {
    setRowBusy({ id: domain.id, kind: "verify" });
    setNote(domain.id, null);
    const result = await actions.verifyDomain(domain.id);
    setRowBusy(null);
    if (!result.ok) {
      // Another Account has proved the domain first: 409 `accounts.domain.claimed`.
      setNote(
        domain.id,
        result.failure === "claimed"
          ? "claimed"
          : result.failure === "forbidden"
            ? "forbidden"
            : "failed",
      );
      return;
    }
    const row = result.value;
    if (row.status === "Verified")
      onMessage({
        tone: "ok",
        text: fillCopy(t.verifiedNow, { domain: row.domain }),
      });
    else setNote(domain.id, row.claimed ? "claimed" : "pending");
    setFocusId(domain.id);
  };

  const remove = async (domain: SsoDomain) => {
    setRowBusy({ id: domain.id, kind: "remove" });
    setNote(domain.id, null);
    const result = await actions.removeDomain(domain.id);
    setRowBusy(null);
    if (result.ok) {
      onMessage({
        tone: "ok",
        text: fillCopy(t.removed, { domain: domain.domain }),
      });
      inputRef.current?.focus();
    } else
      setNote(
        domain.id,
        result.failure === "in-use"
          ? "in-use"
          : result.failure === "forbidden"
            ? "forbidden"
            : "failed",
      );
  };

  return (
    <div className={SUB_CLASS} data-f="sso-domains">
      <h4 className={TITLE_CLASS}>{t.title}</h4>
      <p className="m-0 mt-0.5 text-crumb text-meta-foreground">{t.text}</p>
      {domains.length === 0 ? (
        <p className="m-0 mt-2 text-compact text-muted-foreground">{t.empty}</p>
      ) : (
        <ul className="m-0 mt-2 list-none rounded-[10px] border border-line p-0">
          {domains.map((domain) => (
            <DomainRow
              key={domain.id}
              copy={copy}
              domain={domain}
              busy={rowBusy?.id === domain.id ? rowBusy.kind : null}
              note={notes[domain.id] ?? null}
              rowRef={(node) => {
                if (node) rows.current.set(domain.id, node);
                else rows.current.delete(domain.id);
              }}
              onVerify={() => void verify(domain)}
              onRemove={() => void remove(domain)}
            />
          ))}
        </ul>
      )}
      <form
        noValidate
        onSubmit={(event) => void add(event)}
        className="mt-3 flex items-end gap-2 max-sm:flex-wrap"
      >
        <Field id={inputId} label={t.label} error={error}>
          <input
            ref={inputRef}
            id={inputId}
            type="text"
            inputMode="url"
            autoComplete="off"
            autoCapitalize="none"
            spellCheck={false}
            placeholder={t.placeholder}
            value={value}
            onChange={(event) => {
              setValue(event.target.value);
              setError(null);
            }}
            aria-invalid={error ? true : undefined}
            aria-describedby={describedBy(inputId, false, !!error)}
            className={cn(INPUT, FOCUS)}
          />
        </Field>
        <button
          type="submit"
          disabled={adding}
          className={cn(BTN_SM, FOCUS, "mb-px")}
        >
          {adding ? t.adding : t.add}
        </button>
      </form>
    </div>
  );
}

function RequireRow({
  copy,
  settings,
  actions,
  onMessage,
}: {
  copy: AccountSecurityCopy;
  settings: SsoSettings;
  actions: AccountSsoActions;
  onMessage: (message: Message) => void;
}) {
  const t = copy.sso.require;
  const base = React.useId();
  const [busy, setBusy] = React.useState(false);
  const ready =
    settings.provider?.status === "Active" &&
    settings.domains.some((domain) => domain.status === "Verified");
  // Turning it off stays possible even if the setup is no longer ready.
  const blocked = !ready && !settings.requireSso;
  const change = async (required: boolean) => {
    setBusy(true);
    const result = await actions.setRequired(required);
    setBusy(false);
    if (result.ok)
      onMessage({ tone: "ok", text: required ? t.savedOn : t.savedOff });
    else
      onMessage({
        tone: "error",
        text:
          result.failure === "not-ready"
            ? t.notReady
            : result.failure === "in-use"
              ? t.inUse
              : result.failure === "forbidden"
                ? copy.sso.forbiddenAction
                : copy.sso.failed,
      });
  };
  return (
    <div
      className={cn(SUB_CLASS, "flex items-start gap-3")}
      data-f="sso-require"
    >
      <Toggle
        checked={settings.requireSso}
        disabled={busy || blocked}
        onChange={(next) => void change(next)}
        aria-labelledby={`${base}-title`}
        aria-describedby={`${base}-text${blocked ? ` ${base}-reason` : ""}`}
      />
      <span className="min-w-0 flex-1">
        <b
          id={`${base}-title`}
          className="block text-nav leading-[1.45] font-semibold"
        >
          {t.title}
        </b>
        <small id={`${base}-text`} className={cn("block", HINT_CLASS)}>
          {t.text}
        </small>
        {blocked ? (
          <small id={`${base}-reason`} className={cn("mt-1 block", HINT_CLASS)}>
            {t.reason}
          </small>
        ) : null}
      </span>
    </div>
  );
}

function tagOf(copy: AccountSecurityCopy, settings: SsoSettings) {
  const t = copy.sso.tag;
  if (settings.requireSso) return { tone: "ok" as const, text: t.required };
  const status = settings.provider?.status;
  if (!status) return { tone: "neutral" as const, text: t.notSet };
  if (status === "Active") return { tone: "ok" as const, text: t.active };
  return {
    tone: "neutral" as const,
    text: status === "Draft" ? t.draft : t.disabled,
  };
}

/**
 * The Account's single sign-on setup (Owner only): the identity provider, the domains
 * that route sign-in to it and the switch that makes it required. Every state of the
 * three areas is drawn here; the host only answers the calls.
 */
export function AccountSsoPanel({
  copy,
  state,
  actions,
}: AccountSsoPanelProps) {
  const titleId = React.useId();
  const [message, setMessage] = React.useState<Message | null>(null);
  const settings = state.status === "ready" ? state.data : null;
  const aside = settings ? tagOf(copy, settings) : null;

  return (
    <section
      aria-labelledby={titleId}
      data-f="sso"
      className="rounded-[12px] border border-line bg-popover dark:bg-card"
    >
      <div className="flex items-center gap-2.5 border-b border-line px-4 py-[13px] max-sm:flex-wrap max-sm:gap-y-1">
        <h3 id={titleId} className={TITLE_CLASS}>
          {copy.sso.title}
        </h3>
        {aside ? <Tag tone={aside.tone}>{aside.text}</Tag> : null}
      </div>

      {state.status === "loading" ? (
        <div role="status" className="flex flex-col gap-2 px-4 py-3">
          <span className="sr-only">{copy.sso.loading}</span>
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-4 w-1/2" />
        </div>
      ) : state.status === "error" ? (
        <div className="px-4 py-3">
          <LoadError
            message={copy.sso.loadError}
            retry={copy.retry}
            onRetry={actions.retry}
          />
        </div>
      ) : state.status === "forbidden" ? (
        <div className="px-4 py-3">
          <Note>{copy.sso.forbidden}</Note>
        </div>
      ) : (
        <>
          <p className={cn("m-0 px-4 pt-3", "text-crumb text-meta-foreground")}>
            {copy.sso.intro}
          </p>
          <ProviderBlock
            copy={copy}
            provider={state.data.provider}
            redirectUri={state.data.redirectUri}
            requireSso={state.data.requireSso}
            actions={actions}
            onMessage={setMessage}
          />
          <DomainsBlock
            copy={copy}
            domains={state.data.domains}
            actions={actions}
            onMessage={setMessage}
          />
          <RequireRow
            copy={copy}
            settings={state.data}
            actions={actions}
            onMessage={setMessage}
          />
          <div
            aria-live="polite"
            className={cn("px-4", message && "border-t border-line py-2.5")}
          >
            {message ? (
              <p
                role={message.tone === "error" ? "alert" : undefined}
                className={cn(
                  "m-0 text-crumb",
                  message.tone === "error"
                    ? "text-danger-text"
                    : "text-muted-foreground",
                )}
              >
                {message.text}
              </p>
            ) : null}
          </div>
        </>
      )}
    </section>
  );
}
