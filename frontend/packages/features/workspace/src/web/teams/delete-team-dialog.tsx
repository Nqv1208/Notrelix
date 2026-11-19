import * as React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import type { TeamReferencesRefusal } from "../../core/model/errors";
import { formatCopy } from "../shell/shell-models";
import { BTN, BTN_PRIMARY, FOCUS } from "../spaces/space-page-parts";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

/** What deleting the Team answered: each failure is shown in its own words. */
export type TeamDeleteOutcome =
  | { readonly kind: "deleted" }
  | { readonly kind: "refused"; readonly refusal: TeamReferencesRefusal }
  | { readonly kind: "conflict" }
  | { readonly kind: "denied" }
  | { readonly kind: "failed" };

/**
 * Delete a Team (`Wsp-Var-Teams-Refuse-T`). A Team that Spaces still list is refused (LD-16) and the dialog
 * becomes the refusal: the Spaces the caller can see, then "and others you cannot see" (no number) when some
 * are hidden. Nothing is removed for the owner or admin; they take the entries out first.
 */
export function DeleteTeamDialog({
  copy,
  open,
  teamName,
  onDelete,
  onReload,
  onClose,
}: {
  copy: WorkspaceTeamsCopy;
  open: boolean;
  teamName: string;
  onDelete: () => Promise<TeamDeleteOutcome>;
  onReload: () => void;
  onClose: () => void;
}) {
  const d = copy.deleteDialog;
  const [pending, setPending] = React.useState(false);
  const [outcome, setOutcome] = React.useState<TeamDeleteOutcome | null>(null);
  React.useEffect(() => {
    if (open) {
      setPending(false);
      setOutcome(null);
    }
  }, [open]);

  const refused = outcome?.kind === "refused" ? outcome.refusal : null;
  const problem =
    outcome?.kind === "conflict"
      ? d.conflict
      : outcome?.kind === "denied"
        ? d.denied
        : outcome?.kind === "failed"
          ? d.failed
          : null;
  const values = { team: teamName };

  const confirm = async () => {
    if (pending) return;
    setPending(true);
    setOutcome(null);
    const result = await onDelete();
    setPending(false);
    if (result.kind === "deleted") onClose();
    else setOutcome(result);
  };

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="dlg"
        className="top-[110px] box-border flex w-[470px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div className="px-5 pt-[18px] pb-1.5">
          <DialogTitle className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground">
            {formatCopy(refused ? d.refusedTitle : d.title, values)}
          </DialogTitle>
        </div>
        <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
          <DialogDescription className="m-0 text-nav leading-[1.55] font-normal text-foreground">
            {refused ? d.refusedText : formatCopy(d.text, values)}
          </DialogDescription>
          {refused ? (
            <>
              {refused.references.length > 0 ? (
                <ul
                  aria-label={d.refusedTitle.replace("{{team}}", teamName)}
                  className="m-0 flex list-none flex-col overflow-hidden rounded-[10px] border border-line p-0 text-compact"
                >
                  {refused.references.map((reference) => (
                    <li
                      key={reference.id}
                      className="flex items-center gap-2.5 border-t border-line px-3 py-2 first:border-t-0"
                    >
                      <span className="min-w-0 flex-1 truncate font-medium text-foreground">
                        {reference.name}
                      </span>
                      <small className="text-xs text-meta-foreground">
                        {d.spaceKind}
                      </small>
                    </li>
                  ))}
                </ul>
              ) : null}
              {refused.moreHidden ? (
                <p className="m-0 text-crumb text-meta-foreground">
                  {d.others}
                </p>
              ) : null}
            </>
          ) : null}
          {problem ? (
            <div
              role="alert"
              className="flex flex-wrap items-center gap-3 rounded-[8px] bg-danger-subtle px-3 py-2 text-compact text-danger-subtle-foreground"
            >
              <span className="min-w-0 flex-1">{problem}</span>
              {outcome?.kind === "conflict" ? (
                <button
                  type="button"
                  onClick={() => {
                    setOutcome(null);
                    onReload();
                  }}
                  className={cn(BTN, FOCUS)}
                >
                  {d.reload}
                </button>
              ) : null}
            </div>
          ) : null}
        </div>
        <div className="flex justify-end gap-2 px-5 pb-[18px]">
          {refused ? (
            <button
              type="button"
              onClick={onClose}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              {d.understood}
            </button>
          ) : (
            <>
              <button
                type="button"
                onClick={onClose}
                className={cn(BTN, FOCUS)}
              >
                {d.cancel}
              </button>
              <button
                type="button"
                disabled={pending}
                aria-busy={pending || undefined}
                onClick={() => void confirm()}
                className={cn(
                  BTN_PRIMARY,
                  "bg-danger text-danger-foreground hover:bg-danger/90",
                  FOCUS,
                )}
              >
                {pending ? d.confirming : d.confirm}
              </button>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
