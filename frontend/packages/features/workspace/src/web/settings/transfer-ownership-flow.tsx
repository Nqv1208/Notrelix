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
  RadioGroup,
  RadioGroupItem,
  cn,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
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
  /** Resolves `null` on success, or why the producer refused. */
  readonly onTransfer: (userId: string) => Promise<TransferFailure | null>;
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
  onTransfer,
  onReload,
  onInvite,
}: TransferOwnershipFlowProps) {
  const [open, setOpen] = React.useState(false);
  const [selected, setSelected] = React.useState("");
  const [pending, setPending] = React.useState(false);
  const [failure, setFailure] = React.useState<TransferFailure | null>(null);
  const groupId = React.useId();
  const d = copy.danger;
  const values = { workspace: workspaceName };
  const choice = candidates.find((c) => c.userId === selected) ?? null;
  const stale = failure?.kind === "conflict";

  const reset = (next: boolean) => {
    setOpen(next);
    if (next) {
      setSelected(candidates[0]?.userId ?? "");
      setFailure(null);
    }
  };

  const submit = async () => {
    if (!choice || pending) return;
    setPending(true);
    setFailure(null);
    const outcome = await onTransfer(choice.userId);
    setPending(false);
    if (outcome === null) setOpen(false);
    else setFailure(outcome);
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
              void submit();
            }}
            className="flex flex-col gap-4"
          >
            <AlertDialogHeader>
              <AlertDialogTitle>
                {formatCopy(d.transferDialogTitle, values)}
              </AlertDialogTitle>
              <AlertDialogDescription className="sr-only">
                {formatCopy(d.transferDescription, values)}
              </AlertDialogDescription>
            </AlertDialogHeader>

            {candidates.length === 0 ? (
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
                {d.cancel}
              </AlertDialogCancel>
              {candidates.length === 0 ? (
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
