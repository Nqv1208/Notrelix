import * as React from "react";
import { AlertTriangle, Info } from "lucide-react";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Input,
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
  Label,
  RadioGroup,
  RadioGroupItem,
  cn,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import {
  describeProofRefusal,
  type OwnershipProofRefusal,
  type OwnershipProofRequest,
  type OwnershipStepUp,
} from "./ownership-step-up";
import type { WorkspaceSettingsCopy } from "./workspace-settings.copy";

/** A member who may become the owner: active, not a Guest, not already an Owner. */
export interface TransferCandidate {
  readonly userId: string;
  readonly name: string;
  readonly role: "admin" | "member";
  /** Job title, shown under the name when the person has one. */
  readonly detail: string | null;
}

/** Why a handover was refused; the dialog says so in place and the list can be reloaded. */
export type TransferFailure =
  | { readonly kind: "conflict" }
  /** No proof reached the producer (412), or it was stale, spent or not this person's (400): confirm again. */
  | { readonly kind: "step-up-required" }
  | { readonly kind: "step-up-invalid" }
  | { readonly kind: "rejected"; readonly message: string | null }
  | { readonly kind: "forbidden" }
  | { readonly kind: "network" }
  | { readonly kind: "unknown" };

export interface TransferOwnershipFlowProps {
  readonly copy: WorkspaceSettingsCopy;
  readonly workspaceName: string;
  readonly candidates: readonly TransferCandidate[];
  /** Off while the workspace version is unknown: a write without it would be refused. */
  readonly disabled: boolean;
  /** The host's proof of presence, asked after the person confirms the choice. */
  readonly stepUp: OwnershipStepUp;
  /** Resolves `null` on success, or why the producer refused. `stepUpToken` is the single-use proof just given. */
  readonly onTransfer: (
    userId: string,
    stepUpToken: string,
  ) => Promise<TransferFailure | null>;
  /** Re-reads the member list and the workspace after a conflict. */
  readonly onReload: () => void;
  /** Leads to the invitation page when nobody is eligible. */
  readonly onInvite: () => void;
}

function failureText(
  copy: WorkspaceSettingsCopy,
  failure: TransferFailure,
): string {
  switch (failure.kind) {
    case "conflict":
      return copy.danger.transferConflictBody;
    case "rejected":
      return failure.message ?? copy.danger.transferRejected;
    case "forbidden":
      return copy.toasts.forbidden;
    case "network":
      return copy.toasts.network;
    case "step-up-required":
    case "step-up-invalid":
      return copy.danger.stepUpRefusedBody;
    default:
      return copy.toasts.unknown;
  }
}

function proofRefusalText(
  copy: WorkspaceSettingsCopy,
  refusal: OwnershipProofRefusal,
  password: boolean,
): string {
  const d = copy.danger;
  switch (refusal.kind) {
    case "wrong":
      return password ? d.stepUpWrongPassword : d.stepUpWrongCode;
    case "wait":
      return formatCopy(d.stepUpWait, { seconds: String(refusal.seconds) });
    case "network":
      return copy.toasts.network;
    default:
      return copy.toasts.unknown;
  }
}

/**
 * The owner's handover: pick an active non-Guest member, confirm, and the person becomes Owner while the
 * caller becomes Admin. States: choose, nobody eligible, pending, refused (stale list, rule, no permission,
 * offline). Every refusal stays in the dialog and nothing is retried on its own.
 */
export function TransferOwnershipFlow({
  copy,
  workspaceName,
  candidates,
  disabled,
  stepUp,
  onTransfer,
  onReload,
  onInvite,
}: TransferOwnershipFlowProps) {
  const [open, setOpen] = React.useState(false);
  const [selected, setSelected] = React.useState("");
  const [pending, setPending] = React.useState(false);
  const [failure, setFailure] = React.useState<TransferFailure | null>(null);
  const [asked, setAsked] = React.useState<OwnershipProofRequest | null>(null);
  const [proofError, setProofError] =
    React.useState<OwnershipProofRefusal | null>(null);
  const [refused, setRefused] = React.useState(false);
  const [recovery, setRecovery] = React.useState(false);
  const [value, setValue] = React.useState("");
  const groupId = React.useId();
  const d = copy.danger;
  const values = { workspace: workspaceName };
  const choice = candidates.find((c) => c.userId === selected) ?? null;
  const stale = failure?.kind === "conflict";

  const proving = asked !== null;
  const usesPassword = asked?.factor === "password";
  const usesProvider = asked?.factor === "provider";
  const expired = proofError?.kind === "expired";
  const waiting = proofError?.kind === "wait";
  const ready = usesPassword
    ? value.length > 0
    : recovery
      ? value.trim().length > 0
      : value.length === 6;

  const reset = (next: boolean) => {
    setOpen(next);
    if (next) {
      setSelected(candidates[0]?.userId ?? "");
      setFailure(null);
      setAsked(null);
      setProofError(null);
      setRefused(false);
      setRecovery(false);
      setValue("");
    }
  };

  /** Asks the host which factor to satisfy; a fresh challenge every time, because each is spent by one answer. */
  const ask = async (): Promise<boolean> => {
    try {
      setAsked(await stepUp.request());
      setProofError(null);
      setRecovery(false);
      setValue("");
      return true;
    } catch (error) {
      setFailure(
        describeProofRefusal(error).kind === "network"
          ? { kind: "network" }
          : { kind: "unknown" },
      );
      return false;
    }
  };

  const confirmChoice = async () => {
    if (!choice || pending) return;
    setPending(true);
    setFailure(null);
    await ask();
    setPending(false);
  };

  const restart = async () => {
    setPending(true);
    setFailure(null);
    setRefused(false);
    await ask();
    setPending(false);
  };

  const prove = async () => {
    if (!choice || !asked || pending || !ready) return;
    setPending(true);
    setFailure(null);
    setProofError(null);
    setRefused(false);
    let token: string;
    try {
      token = usesPassword
        ? await stepUp.withPassword(value)
        : await stepUp.withCode(
            asked.challengeToken ?? "",
            recovery ? value.trim() : value.replace(/\s+/g, ""),
          );
    } catch (error) {
      setPending(false);
      setProofError(describeProofRefusal(error));
      setValue("");
      return;
    }
    const outcome = await onTransfer(choice.userId, token);
    if (outcome === null) {
      setPending(false);
      setOpen(false);
      return;
    }
    if (
      outcome.kind === "step-up-required" ||
      outcome.kind === "step-up-invalid"
    ) {
      setRefused(true);
      await ask();
    } else if (outcome.kind === "conflict") {
      setFailure(outcome);
    } else {
      // The proof is spent whatever the refusal: a fresh challenge waits behind the message.
      await ask();
      setFailure(outcome);
    }
    setPending(false);
  };

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-danger-subtle p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 flex-col gap-0.5">
        <h3 className="text-label text-foreground">{d.transferTitle}</h3>
        <p className="text-metadata text-muted-foreground">
          {formatCopy(d.transferDescription, values)}
        </p>
      </div>
      <Button
        variant="destructive"
        size="sm"
        disabled={disabled}
        onClick={() => reset(true)}
        className="shrink-0 max-md:h-touch!"
      >
        {d.transferAction}
      </Button>
      <AlertDialog open={open} onOpenChange={reset}>
        <AlertDialogContent>
          <form
            noValidate
            onSubmit={(event) => {
              event.preventDefault();
              if (stale) return;
              void (proving ? prove() : confirmChoice());
            }}
            className="flex flex-col gap-4"
          >
            <AlertDialogHeader>
              <AlertDialogTitle>
                {proving
                  ? d.stepUpTitle
                  : formatCopy(d.transferDialogTitle, values)}
              </AlertDialogTitle>
              <AlertDialogDescription
                className={proving ? undefined : "sr-only"}
              >
                {proving
                  ? formatCopy(d.stepUpText, {
                      ...values,
                      name: choice?.name ?? "",
                    })
                  : formatCopy(d.transferDescription, values)}
              </AlertDialogDescription>
            </AlertDialogHeader>

            {proving ? (
              <div className="flex flex-col gap-3">
                {refused ? (
                  <p
                    role="alert"
                    className="m-0 flex items-start gap-2 rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
                  >
                    <AlertTriangle
                      className="mt-0.5 size-4 shrink-0"
                      aria-hidden
                    />
                    <span>
                      <b>{d.stepUpRefusedTitle} </b>
                      {d.stepUpRefusedBody}
                    </span>
                  </p>
                ) : null}
                {expired ? (
                  <p
                    role="alert"
                    className="m-0 flex items-start gap-2 rounded-md bg-warning-subtle px-3 py-2 text-metadata text-warning-subtle-foreground"
                  >
                    <Info className="mt-0.5 size-4 shrink-0" aria-hidden />
                    <span>
                      <b>{d.stepUpExpiredTitle} </b>
                      {d.stepUpExpiredBody}
                    </span>
                  </p>
                ) : null}
                {usesProvider ? (
                  <div className="flex flex-col gap-1 rounded-lg border border-dashed border-line p-4">
                    <b className="text-label text-foreground">
                      {d.stepUpProviderTitle}
                    </b>
                    <p className="m-0 text-metadata text-muted-foreground">
                      {d.stepUpProviderBody}
                    </p>
                  </div>
                ) : usesPassword ? (
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor={`${groupId}-proof`}>
                      {d.stepUpPasswordLabel}
                    </Label>
                    <Input
                      id={`${groupId}-proof`}
                      type="password"
                      autoComplete="current-password"
                      autoFocus
                      value={value}
                      disabled={pending || expired}
                      aria-invalid={
                        proofError?.kind === "wrong" ? true : undefined
                      }
                      onChange={(event) => setValue(event.target.value)}
                    />
                  </div>
                ) : (
                  <div className="flex flex-col items-start gap-1.5">
                    <Label htmlFor={`${groupId}-proof`}>
                      {recovery ? d.stepUpRecoveryLabel : d.stepUpCodeLabel}
                    </Label>
                    {recovery ? (
                      <Input
                        id={`${groupId}-proof`}
                        autoComplete="off"
                        autoFocus
                        value={value}
                        disabled={pending || expired}
                        aria-invalid={
                          proofError?.kind === "wrong" ? true : undefined
                        }
                        onChange={(event) => setValue(event.target.value)}
                      />
                    ) : (
                      <InputOTP
                        id={`${groupId}-proof`}
                        maxLength={6}
                        pushPasswordManagerStrategy="none"
                        inputMode="numeric"
                        autoComplete="one-time-code"
                        autoFocus
                        value={value}
                        disabled={pending || expired}
                        aria-invalid={
                          proofError?.kind === "wrong" ? true : undefined
                        }
                        onChange={setValue}
                      >
                        <InputOTPGroup>
                          {[0, 1, 2, 3, 4, 5].map((index) => (
                            <InputOTPSlot key={index} index={index} />
                          ))}
                        </InputOTPGroup>
                      </InputOTP>
                    )}
                    {!expired ? (
                      <button
                        type="button"
                        className="text-metadata font-semibold text-primary hover:underline max-md:min-h-touch"
                        onClick={() => {
                          setRecovery((on) => !on);
                          setValue("");
                          setProofError(null);
                        }}
                      >
                        {recovery ? d.stepUpUseCode : d.stepUpUseRecovery}
                      </button>
                    ) : null}
                  </div>
                )}
                {proofError && !expired ? (
                  <p
                    role="alert"
                    className="m-0 flex items-start gap-2 text-metadata text-danger-subtle-foreground"
                  >
                    <AlertTriangle
                      className="mt-0.5 size-4 shrink-0"
                      aria-hidden
                    />
                    <span>
                      {proofRefusalText(copy, proofError, usesPassword)}
                    </span>
                  </p>
                ) : null}
              </div>
            ) : candidates.length === 0 ? (
              <div className="flex flex-col gap-1 rounded-lg border border-dashed border-line p-4 text-center">
                <b className="text-label text-foreground">
                  {d.transferNoneTitle}
                </b>
                <p className="m-0 text-metadata text-muted-foreground">
                  {d.transferNoneBody}
                </p>
              </div>
            ) : (
              <>
                <RadioGroup
                  aria-label={d.transferPick}
                  value={selected}
                  onValueChange={setSelected}
                  disabled={pending || stale}
                >
                  {candidates.map((person) => {
                    const id = `${groupId}-${person.userId}`;
                    return (
                      <label
                        key={person.userId}
                        htmlFor={id}
                        className={cn(
                          "flex min-h-11 cursor-pointer items-center gap-3 rounded-lg border border-line px-3 py-2",
                          selected === person.userId &&
                            "border-primary bg-state-hover-strong",
                        )}
                      >
                        <RadioGroupItem id={id} value={person.userId} />
                        <span className="flex min-w-0 flex-col">
                          <b className="truncate text-label text-foreground">
                            {person.name}
                          </b>
                          <small className="truncate text-metadata text-muted-foreground">
                            {person.role === "admin"
                              ? d.transferRoleAdmin
                              : d.transferRoleMember}
                            {person.detail ? ` · ${person.detail}` : ""}
                          </small>
                        </span>
                      </label>
                    );
                  })}
                </RadioGroup>
                {failure === null && choice ? (
                  <p className="m-0 flex items-start gap-2 rounded-md bg-state-hover-strong px-3 py-2 text-metadata text-muted-foreground">
                    <Info className="mt-0.5 size-4 shrink-0" aria-hidden />
                    <span>
                      {formatCopy(d.transferEffect, { name: choice.name })}
                    </span>
                  </p>
                ) : null}
              </>
            )}

            {failure ? (
              <p
                role="alert"
                className="m-0 flex items-start gap-2 rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
              >
                <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
                <span>
                  {stale ? <b>{d.transferConflictTitle} </b> : null}
                  {failureText(copy, failure)}
                </span>
              </p>
            ) : null}

            <AlertDialogFooter>
              <AlertDialogCancel type="button" className="max-md:h-touch">
                {expired || usesProvider ? d.stepUpClose : d.cancel}
              </AlertDialogCancel>
              {proving ? (
                usesProvider ? null : expired ? (
                  <Button
                    type="button"
                    variant="destructive"
                    disabled={pending}
                    className="max-md:h-touch!"
                    onClick={() => void restart()}
                  >
                    {d.stepUpRestart}
                  </Button>
                ) : stale ? (
                  <Button
                    type="button"
                    className="max-md:h-touch!"
                    onClick={() => {
                      setOpen(false);
                      onReload();
                    }}
                  >
                    {d.transferReload}
                  </Button>
                ) : (
                  <Button
                    type="submit"
                    variant="destructive"
                    disabled={!ready || pending || waiting}
                    className="max-md:h-touch!"
                  >
                    {pending ? d.stepUpChecking : d.stepUpConfirm}
                  </Button>
                )
              ) : candidates.length === 0 ? (
                <Button
                  type="button"
                  className="max-md:h-touch!"
                  onClick={() => {
                    setOpen(false);
                    onInvite();
                  }}
                >
                  {d.transferInvite}
                </Button>
              ) : stale ? (
                <Button
                  type="button"
                  className="max-md:h-touch!"
                  onClick={() => {
                    setOpen(false);
                    onReload();
                  }}
                >
                  {d.transferReload}
                </Button>
              ) : (
                <Button
                  type="submit"
                  variant="destructive"
                  disabled={!choice || pending}
                  className="max-md:h-touch!"
                >
                  {pending
                    ? d.transferPending
                    : choice
                      ? formatCopy(d.transferConfirm, { name: choice.name })
                      : d.transferAction}
                </Button>
              )}
            </AlertDialogFooter>
          </form>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
