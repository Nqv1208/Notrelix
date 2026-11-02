import { useState, type FormEvent } from "react";
import {
  Clock,
  Eye,
  Hourglass,
  Lock,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import { cn, useIsMobile } from "@notrelix/ui-web";
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
 * A guest invitation to one board (the Auth mockup): who invited, what the
 * guest may do and until when, the form that creates the account, and the
 * pages for an invitation that cannot be used. Pure: the invitation, the copy
 * and the callbacks are injected; the typed name and password are its own.
 */
export interface AuthInvite {
  readonly inviterName: string;
  readonly inviterInitials: string;
  readonly orgName: string;
  readonly boardTitle: string;
  readonly level: "viewer" | "commenter" | "editor";
  /** The day the access ends, as shown ("31/01/2027"); null = it does not end. */
  readonly until: string | null;
  readonly email: string;
  readonly name: string;
  /** The email already has an account: no password to create. */
  readonly hasAccount: boolean;
  /** Set for an invitation to the whole workspace (it names a role, not a board). */
  readonly workspaceRole?: "member" | "guest" | null;
}

export type AuthInviteView =
  | {
      readonly kind: "invite";
      readonly invite: AuthInvite;
      readonly error?: "unreachable" | null;
      /** Server's word on a field, shown under it. */
      readonly fieldErrors?: {
        readonly name?: string;
        readonly password?: string;
      };
    }
  | {
      readonly kind: "gone";
      readonly reason: "expired" | "used" | "missing";
      readonly orgName: string | null;
    }
  | { readonly kind: "declined"; readonly inviterName: string };

export interface AuthInviteSurfaceProps {
  readonly copy: AuthCopy;
  readonly view: AuthInviteView;
  readonly pending?: "accept" | "decline" | null;
  readonly onAccept: (input: { name: string; password: string }) => void;
  readonly onDecline: () => void;
  readonly onSignIn: () => void;
  readonly onToSignIn: () => void;
}

export function AuthInviteSurface(props: AuthInviteSurfaceProps) {
  const { view } = props;
  if (view.kind === "invite") return <InviteForm {...props} view={view} />;
  if (view.kind === "declined") return <DeclinedPage {...props} view={view} />;
  return <GonePage {...props} view={view} />;
}

function InviteForm({
  copy,
  view,
  pending,
  onAccept,
  onDecline,
  onSignIn,
}: AuthInviteSurfaceProps & {
  view: Extract<AuthInviteView, { kind: "invite" }>;
}) {
  const t = copy.invite;
  const { invite } = view;
  const role = invite.workspaceRole ?? null;
  const [name, setName] = useState(invite.name);
  const [password, setPassword] = useState("");
  const [tried, setTried] = useState(false);
  // On a phone the invitation opens as a card; a new person types their name
  // and password only after pressing accept (the form grows in place).
  const mobile = useIsMobile();
  const [revealed, setRevealed] = useState(false);
  // An email that has an account signs in and accepts: no name is created.
  const nameRequired = !invite.hasAccount;
  const nameError =
    view.fieldErrors?.name ??
    (tried && nameRequired && !name.trim() ? t.nameRequired : null);
  const passwordError = invite.hasAccount
    ? null
    : (view.fieldErrors?.password ??
      (tried && password.length < PASSWORD_MIN_LENGTH
        ? fillAuthCopy(t.passwordShort, { count: PASSWORD_MIN_LENGTH })
        : null));
  const busy = pending != null;
  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTried(true);
    if (nameRequired && !name.trim()) return;
    if (!invite.hasAccount && password.length < PASSWORD_MIN_LENGTH) return;
    onAccept({ name: name.trim(), password });
  };
  const title = role
    ? fillAuthCopy(t.workspaceTitle, {
        inviter: invite.inviterName,
        org: invite.orgName,
      })
    : fillAuthCopy(t.title, {
        inviter: invite.inviterName,
        board: invite.boardTitle,
        org: invite.orgName,
      });
  const level = role
    ? t.workspaceRoles[role]
    : `${t.guest} · ${t.levels[invite.level]}`;
  const until = invite.until
    ? ` · ${fillAuthCopy(t.until, { date: invite.until })}`
    : "";
  const fields = (
    <>
      <div className="flex flex-col gap-1.5">
        <label htmlFor="auth-invite-email" className={LABEL_CLASS}>
          {t.emailLabel}
        </label>
        <input
          id="auth-invite-email"
          type="email"
          readOnly
          value={invite.email}
          className={INPUT_CLASS}
        />
        <p className="m-0 text-crumb leading-normal text-meta-foreground">
          {t.emailHint}
        </p>
      </div>
      <div className="flex flex-col gap-1.5">
        <label htmlFor="auth-invite-name" className={LABEL_CLASS}>
          {t.nameLabel}
        </label>
        <input
          id="auth-invite-name"
          name="name"
          type="text"
          autoComplete="name"
          autoFocus={revealed}
          value={name}
          aria-invalid={nameError ? true : undefined}
          aria-describedby={nameError ? "auth-invite-name-error" : undefined}
          onChange={(event) => setName(event.target.value)}
          className={INPUT_CLASS}
        />
        {nameError ? (
          <FieldError id="auth-invite-name-error">{nameError}</FieldError>
        ) : null}
      </div>
      {invite.hasAccount ? null : (
        <div className="flex flex-col gap-1.5">
          <label htmlFor="auth-invite-password" className={LABEL_CLASS}>
            {t.passwordLabel}
          </label>
          <PasswordField
            id="auth-invite-password"
            value={password}
            onChange={setPassword}
            invalid={Boolean(passwordError)}
            describedBy={
              passwordError
                ? "auth-invite-password-error"
                : "auth-invite-password-hint"
            }
            autoComplete="new-password"
            showLabel={t.show}
            hideLabel={t.hide}
          />
          {passwordError ? (
            <FieldError id="auth-invite-password-error">
              {passwordError}
            </FieldError>
          ) : (
            <p
              id="auth-invite-password-hint"
              className="m-0 text-crumb leading-normal text-meta-foreground"
            >
              {fillAuthCopy(t.passwordHint, { count: PASSWORD_MIN_LENGTH })}
            </p>
          )}
        </div>
      )}
      {view.error === "unreachable" ? (
        <FieldError>{t.unreachable}</FieldError>
      ) : null}
    </>
  );
  if (mobile) {
    const [before, after] = t.mobile.sentTo.split("{{email}}");
    const needsFields = !invite.hasAccount;
    return (
      <form noValidate className="contents" onSubmit={submit}>
        <span
          aria-hidden="true"
          className="grid size-12 place-items-center rounded-full bg-identity-sage text-doc-check font-bold text-identity-sage-foreground"
        >
          {invite.inviterInitials}
        </span>
        <h1 className="m-0 text-account-title leading-[1.3] font-bold">
          {title}
        </h1>
        <div className="overflow-hidden rounded-[14px] border border-border bg-surface-panel leading-[1.45]">
          <div className="flex items-center gap-3 px-4 py-3.5">
            <span
              aria-hidden="true"
              className="grid size-9 shrink-0 place-items-center rounded-[10px] bg-primary text-metadata font-bold text-primary-foreground"
            >
              {(role ? invite.orgName : invite.boardTitle)
                .split(/\s+/)
                .slice(0, 2)
                .map((word) => word.charAt(0).toUpperCase())
                .join("")}
            </span>
            <span className="min-w-0">
              <b className="block text-card-title">
                {role ? invite.orgName : invite.boardTitle}
              </b>
              {role ? null : (
                <small className="block text-item-small text-meta-foreground">
                  {invite.orgName}
                </small>
              )}
            </span>
          </div>
          <div className="flex items-start gap-3 border-t border-line px-4 py-3 text-body text-muted-foreground leading-[1.45]">
            <UserRound
              aria-hidden="true"
              className="mt-px size-5 shrink-0 text-meta-foreground"
            />
            <span className="min-w-0 flex-1">
              <b className="block text-item-heading font-semibold text-foreground leading-[1.45]">
                {t.mobile.yourAccess}
              </b>
              <span className="mt-1.5 inline-flex min-h-8 items-center rounded-[8px] bg-warning-subtle px-2.5 py-0.5 text-item-small font-semibold text-warning-subtle-foreground">
                {level}
                {until}
              </span>
            </span>
          </div>
          <div className="flex items-start gap-3 border-t border-line px-4 py-3 text-body text-muted-foreground leading-[1.45]">
            <Lock
              aria-hidden="true"
              className="mt-px size-5 shrink-0 text-meta-foreground"
            />
            <span className="min-w-0 flex-1">
              <b className="block text-item-heading font-semibold text-foreground leading-[1.45]">
                {role ? t.seeTitle : t.mobile.boardOnlyTitle}
              </b>
              {role
                ? t.workspaceSee[role]
                : fillAuthCopy(t.mobile.boardOnlyBody, { org: invite.orgName })}
            </span>
          </div>
        </div>
        <p className="-mt-0.5 mb-0 text-item-small leading-normal text-meta-foreground">
          {before}
          <b className="font-semibold text-foreground">{invite.email}</b>
          {after}
        </p>
        {needsFields && revealed ? (
          <div className="flex animate-in flex-col gap-4.5 duration-300 fade-in-0 slide-in-from-bottom-1 motion-reduce:animate-none">
            {fields}
          </div>
        ) : view.error === "unreachable" ? (
          <FieldError>{t.unreachable}</FieldError>
        ) : null}
        <div className="flex gap-2.5">
          <button
            type="button"
            disabled={busy}
            onClick={onDecline}
            className={cn(BUTTON_CLASS.outline, "h-12.5 rounded-[12px] flex-1")}
          >
            {pending === "decline" ? t.declining : t.decline}
          </button>
          <button
            type={needsFields && !revealed ? "button" : "submit"}
            disabled={busy}
            onClick={
              needsFields && !revealed ? () => setRevealed(true) : undefined
            }
            className={cn(BUTTON_CLASS.primary, "h-12.5 rounded-[12px] flex-1")}
          >
            {pending === "accept" ? t.accepting : t.mobile.accept}
          </button>
        </div>
      </form>
    );
  }
  return (
    <>
      <div className="flex flex-col gap-3">
        <span
          aria-hidden="true"
          className="grid size-8 place-items-center rounded-full bg-identity-sage text-mark-md font-bold text-identity-sage-foreground"
        >
          {invite.inviterInitials}
        </span>
        <AuthHeading medium title={title} />
        <span className="inline-flex h-7 items-center gap-1.5 self-start rounded-full bg-brand-subtle px-2.75 text-item-small font-semibold text-brand-subtle-foreground">
          <UserRound aria-hidden="true" className="size-3.5" />
          {level}
          {until}
        </span>
      </div>
      <section
        aria-labelledby="auth-see"
        className="flex flex-col gap-1.5 rounded-[12px] border border-line px-4 py-3.5"
      >
        <h2
          id="auth-see"
          className="m-0 flex items-center gap-2 text-item-ui leading-normal font-semibold"
        >
          <Eye aria-hidden="true" className="size-3.75" />
          {t.seeTitle}
        </h2>
        <p className="m-0 text-item-ui leading-normal text-muted-foreground">
          {role ? t.workspaceSee[role] : t.seeBody}
        </p>
      </section>
      <form noValidate className="flex flex-col gap-4.5" onSubmit={submit}>
        {fields}
        <div className="flex gap-2.5">
          <button
            type="submit"
            disabled={busy}
            className={cn(BUTTON_CLASS.primary, "flex-1")}
          >
            {pending === "accept"
              ? t.accepting
              : invite.hasAccount
                ? t.acceptExisting
                : t.accept}
          </button>
          <button
            type="button"
            disabled={busy}
            onClick={onDecline}
            className={cn(BUTTON_CLASS.outline, "w-auto flex-none px-4.5")}
          >
            {pending === "decline" ? t.declining : t.decline}
          </button>
        </div>
      </form>
      <p className="m-0 text-crumb leading-normal text-meta-foreground">
        {t.safe}
      </p>
      <p className="m-0 text-center text-item-ui leading-normal text-muted-foreground">
        {t.hasAccount}{" "}
        <button
          type="button"
          onClick={onSignIn}
          className={cn(LINK_BUTTON_CLASS, "py-0 leading-[18px]")}
        >
          {t.signInToAccept}
        </button>
      </p>
    </>
  );
}

function GonePage({
  copy,
  view,
  onToSignIn,
}: AuthInviteSurfaceProps & {
  view: Extract<AuthInviteView, { kind: "gone" }>;
}) {
  const t = copy.gone;
  const org = view.orgName ?? "";
  const [title, body, help] =
    view.reason === "expired"
      ? [t.expiredTitle, fillAuthCopy(t.expiredBody, { org }), t.expiredHelp]
      : view.reason === "used"
        ? [t.usedTitle, fillAuthCopy(t.usedBody, { org }), t.usedHelp]
        : [t.missingTitle, t.missingBody, t.missingHelp];
  return (
    <>
      <span
        aria-hidden="true"
        className="grid size-13 place-items-center rounded-[14px] bg-state-hover text-muted-foreground"
      >
        {view.reason === "expired" ? (
          <Hourglass className="size-6" />
        ) : (
          <Clock className="size-6" />
        )}
      </span>
      <AuthHeading title={title} lead={body} />
      <p className="m-0 text-item-prose leading-normal text-muted-foreground">
        {help}
      </p>
      <button
        type="button"
        onClick={onToSignIn}
        className={cn(BUTTON_CLASS.outline, "text-brand-subtle-foreground")}
      >
        {t.toSignIn}
      </button>
    </>
  );
}

function DeclinedPage({
  copy,
  view,
  onToSignIn,
}: AuthInviteSurfaceProps & {
  view: Extract<AuthInviteView, { kind: "declined" }>;
}) {
  const t = copy.invite.declined;
  return (
    <>
      <span
        aria-hidden="true"
        className="grid size-13 place-items-center rounded-[14px] bg-state-hover text-muted-foreground"
      >
        <ShieldCheck className="size-6" />
      </span>
      <AuthHeading title={t.title} />
      <p className="m-0 text-item-prose text-muted-foreground">
        {fillAuthCopy(t.body, { inviter: view.inviterName })}
      </p>
      <button
        type="button"
        onClick={onToSignIn}
        className={BUTTON_CLASS.outline}
      >
        {copy.gone.toSignIn}
      </button>
    </>
  );
}
