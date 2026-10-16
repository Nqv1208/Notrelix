import { useState } from "react";
import { Clock, Eye, ShieldCheck, UserRound } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillAuthCopy } from "../copy/fill-auth-copy";
import type { AuthCopy } from "../copy/auth.copy";
import {
  AuthHeading,
  BUTTON_CLASS,
  FieldError,
  INPUT_CLASS,
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
  const [name, setName] = useState(invite.name);
  const [password, setPassword] = useState("");
  const [tried, setTried] = useState(false);
  const nameError =
    view.fieldErrors?.name ?? (tried && !name.trim() ? t.nameRequired : null);
  const passwordError = invite.hasAccount
    ? null
    : (view.fieldErrors?.password ??
      (tried && password.length < 12 ? t.passwordShort : null));
  const busy = pending != null;
  return (
    <>
      <span
        aria-hidden="true"
        className="grid size-8 place-items-center rounded-full bg-identity-sage text-mark-lg font-bold text-identity-sage-foreground"
      >
        {invite.inviterInitials}
      </span>
      <AuthHeading
        medium
        title={fillAuthCopy(t.title, {
          inviter: invite.inviterName,
          board: invite.boardTitle,
          org: invite.orgName,
        })}
      />
      <span className="inline-flex h-7 items-center gap-1.5 self-start rounded-full bg-brand-subtle px-2.75 text-item-small font-semibold text-brand-subtle-foreground">
        <UserRound aria-hidden="true" className="size-3.5" />
        {t.guest} · {t.levels[invite.level]}
        {invite.until
          ? ` · ${fillAuthCopy(t.until, { date: invite.until })}`
          : ""}
      </span>
      <section
        aria-labelledby="auth-see"
        className="flex flex-col gap-1.5 rounded-[12px] border border-line px-4 py-3.5"
      >
        <h2
          id="auth-see"
          className="m-0 flex items-center gap-2 text-item-ui font-semibold"
        >
          <Eye aria-hidden="true" className="size-4" />
          {t.seeTitle}
        </h2>
        <p className="m-0 text-item-ui text-muted-foreground">{t.seeBody}</p>
      </section>
      <form
        noValidate
        className="flex flex-col gap-4.5"
        onSubmit={(event) => {
          event.preventDefault();
          setTried(true);
          if (!name.trim()) return;
          if (!invite.hasAccount && password.length < 12) return;
          onAccept({ name: name.trim(), password });
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="auth-invite-email"
            className="text-item-ui font-semibold"
          >
            {t.emailLabel}
          </label>
          <input
            id="auth-invite-email"
            type="email"
            readOnly
            value={invite.email}
            className={INPUT_CLASS}
          />
          <p className="m-0 text-item-note text-meta-foreground">
            {t.emailHint}
          </p>
        </div>
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="auth-invite-name"
            className="text-item-ui font-semibold"
          >
            {t.nameLabel}
          </label>
          <input
            id="auth-invite-name"
            name="name"
            type="text"
            autoComplete="name"
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
            <label
              htmlFor="auth-invite-password"
              className="text-item-ui font-semibold"
            >
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
                className="m-0 text-item-note text-meta-foreground"
              >
                {t.passwordHint}
              </p>
            )}
          </div>
        )}
        {view.error === "unreachable" ? (
          <FieldError>{t.unreachable}</FieldError>
        ) : null}
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
      <p className="m-0 flex items-start gap-2 text-item-small leading-normal text-meta-foreground">
        <ShieldCheck aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
        {t.safe}
      </p>
      <p className="m-0 text-center text-item-ui text-muted-foreground">
        {t.hasAccount}{" "}
        <button type="button" onClick={onSignIn} className={LINK_BUTTON_CLASS}>
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
        <Clock className="size-6" />
      </span>
      <AuthHeading title={title} />
      <p className="m-0 text-item-prose text-muted-foreground">{body}</p>
      <p className="m-0 text-item-prose text-muted-foreground">{help}</p>
      <button
        type="button"
        onClick={onToSignIn}
        className={BUTTON_CLASS.outline}
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
