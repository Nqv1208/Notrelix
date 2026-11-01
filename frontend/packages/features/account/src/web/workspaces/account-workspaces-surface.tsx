import * as React from "react";
import { Check, Plus } from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  BTN,
  BTN_SM,
  FOCUS,
  LINK_BUTTON,
  LINK_BUTTON_DANGER,
  LoadError,
  Note,
  PageHead,
  Tag,
  Toast,
} from "../shared/account-parts";
import type {
  AccountLeaveFailure,
  AccountWorkspaceRole,
} from "../../core/types/account";
import {
  fillCopy,
  pluralCopy,
  type AccountSection,
} from "../shared/account-surface-model";
import type { AccountWorkspacesCopy } from "./account-workspaces.copy";

export type AccountWorkspaceTone = "blue" | "sage" | "gold" | "coral" | "ember";

export interface AccountWorkspaceView {
  readonly id: string;
  readonly name: string;
  readonly initials: string;
  readonly tone: AccountWorkspaceTone;
  readonly role: AccountWorkspaceRole;
  /** `null` when the person may not see it (a guest). */
  readonly memberCount: number | null;
  /** `null` unless the person is an owner or admin. */
  readonly departmentCount: number | null;
  readonly isDefault: boolean;
  readonly canLeave: boolean;
  /** Archived: read-only, listed in its own group, only opened from here. */
  readonly isArchived: boolean;
  readonly open: { readonly href: string; readonly onNavigate: () => void };
}

export interface AccountWorkspacesSurfaceProps {
  readonly copy: AccountWorkspacesCopy;
  readonly workspaces: AccountSection<readonly AccountWorkspaceView[]>;
  /** An action is running: its buttons wait. */
  readonly busy: boolean;
  /** Why the last action did not happen; `null` = no error to show. */
  readonly actionError: AccountLeaveFailure | null;
  /** The text of the toast at the foot of the page; `null` = none. */
  readonly notice: string | null;
  readonly create: { readonly href: string; readonly onNavigate: () => void };
  readonly onSetDefault: (workspaceId: string) => void;
  /** Brings an archived workspace back (offered to its owners and admins). */
  readonly onUnarchive: (workspaceId: string) => void;
  readonly onLeave: (workspaceId: string) => void;
  readonly onRetry: () => void;
}

const MARK: Record<AccountWorkspaceTone, string> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  ember: "bg-mark-crimson text-mark-crimson-foreground",
};

function isPlainClick(event: React.MouseEvent): boolean {
  return (
    event.button === 0 &&
    !event.metaKey &&
    !event.ctrlKey &&
    !event.shiftKey &&
    !event.altKey
  );
}

function metaOf(copy: AccountWorkspacesCopy, view: AccountWorkspaceView) {
  const parts: string[] = [copy.roles[view.role]];
  if (view.memberCount !== null)
    parts.push(
      pluralCopy(
        { one: copy.people_one, other: copy.people_other },
        view.memberCount,
      ),
    );
  if (view.departmentCount !== null)
    parts.push(
      pluralCopy(
        { one: copy.departments_one, other: copy.departments_other },
        view.departmentCount,
      ),
    );
  return parts.join(" · ");
}

function noteOf(copy: AccountWorkspacesCopy, view: AccountWorkspaceView) {
  if (!view.canLeave && !view.isArchived)
    return {
      kind: "info" as const,
      text: fillCopy(copy.noteSoleOwner, { workspace: view.name }),
    };
  return null;
}

function Card({
  copy,
  view,
  busy,
  onSetDefault,
  onUnarchive,
  onAskLeave,
}: {
  copy: AccountWorkspacesCopy;
  view: AccountWorkspaceView;
  busy: boolean;
  onSetDefault: (id: string) => void;
  onUnarchive: (id: string) => void;
  onAskLeave: (view: AccountWorkspaceView) => void;
}) {
  const titleId = React.useId();
  const noteId = React.useId();
  const note = noteOf(copy, view);
  return (
    <section
      aria-labelledby={titleId}
      data-f="wsc"
      className={cn(
        "flex flex-col gap-2.5 rounded-[12px] border bg-popover px-4 py-3.5 dark:bg-card",
        view.isDefault ? "border-line-strong" : "border-line",
        view.isArchived && "opacity-80",
      )}
    >
      <div className="flex items-center gap-3.5 max-md:flex-wrap">
        <span
          aria-hidden="true"
          className={cn(
            "grid size-11 shrink-0 place-items-center rounded-[12px] text-compact font-bold",
            MARK[view.tone],
          )}
        >
          {view.initials}
        </span>
        <div className="min-w-0">
          <h3
            id={titleId}
            className="m-0 flex flex-wrap items-center gap-2 text-item-heading leading-[1.45] font-[650] text-foreground"
          >
            {view.name}
            {view.isDefault ? (
              <Tag tone="ok">
                <Check
                  aria-hidden="true"
                  strokeWidth={1.8}
                  className="mr-[3px] size-3"
                />
                {copy.defaultTag}
              </Tag>
            ) : null}
            {view.isArchived ? <Tag>{copy.archivedTag}</Tag> : null}
          </h3>
          <span className="mt-0.5 block text-crumb leading-[1.45] text-meta-foreground">
            {metaOf(copy, view)}
          </span>
        </div>
        <div className="ml-auto flex shrink-0 items-center gap-2.5">
          {view.isDefault || view.isArchived ? null : (
            <button
              type="button"
              disabled={busy}
              aria-label={fillCopy(copy.makeDefaultLabel, { name: view.name })}
              onClick={() => onSetDefault(view.id)}
              className={cn(LINK_BUTTON, FOCUS)}
            >
              {copy.makeDefault}
            </button>
          )}
          {view.isArchived ? (
            <button
              type="button"
              disabled={busy}
              aria-label={fillCopy(copy.unarchiveLabel, { name: view.name })}
              onClick={() => onUnarchive(view.id)}
              className={cn(BTN_SM, FOCUS)}
            >
              {copy.unarchive}
            </button>
          ) : (
            <a
              href={view.open.href}
              aria-label={fillCopy(copy.openLabel, { name: view.name })}
              onClick={(event) => {
                if (!isPlainClick(event)) return;
                event.preventDefault();
                view.open.onNavigate();
              }}
              className={cn(BTN_SM, FOCUS)}
            >
              {copy.open}
            </a>
          )}
          {view.isArchived ? null : (
            <button
              type="button"
              disabled={busy || !view.canLeave}
              aria-label={fillCopy(copy.leaveLabel, { name: view.name })}
              aria-describedby={note ? noteId : undefined}
              onClick={() => onAskLeave(view)}
              className={cn(LINK_BUTTON_DANGER, FOCUS)}
            >
              {copy.leave}
            </button>
          )}
        </div>
      </div>
      {note ? <Note id={noteId}>{note.text}</Note> : null}
    </section>
  );
}

/**
 * Your workspaces (Final-Account `workspace`): each workspace the member
 * belongs to with the role, the people and departments they may see, the default that opens with
 * the app and the way out. Leaving asks first; the host reports the result.
 */
export function AccountWorkspacesSurface({
  copy,
  workspaces,
  busy,
  actionError,
  notice,
  create,
  onSetDefault,
  onUnarchive,
  onLeave,
  onRetry,
}: AccountWorkspacesSurfaceProps) {
  const titleId = React.useId();
  const archivedTitleId = React.useId();
  const [leaving, setLeaving] = React.useState<AccountWorkspaceView | null>(
    null,
  );
  return (
    <section aria-labelledby={titleId} className="flex min-w-0 flex-col gap-4">
      <PageHead
        id={titleId}
        title={copy.title}
        description={copy.description}
        action={
          <a
            href={create.href}
            onClick={(event) => {
              if (!isPlainClick(event)) return;
              event.preventDefault();
              create.onNavigate();
            }}
            className={cn(BTN, FOCUS)}
          >
            <Plus aria-hidden="true" className="size-4" strokeWidth={1.8} />
            {copy.create}
          </a>
        }
      />
      {workspaces.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.loading}
          className="flex flex-col gap-4"
        >
          <Skeleton className="h-28 w-full rounded-[12px]" />
          <Skeleton className="h-[74px] w-full rounded-[12px]" />
          <Skeleton className="h-[74px] w-full rounded-[12px]" />
        </div>
      ) : workspaces.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={onRetry}
        />
      ) : (
        <>
          {workspaces.data
            .filter((view) => !view.isArchived)
            .map((view) => (
              <Card
                key={view.id}
                copy={copy}
                view={view}
                busy={busy}
                onSetDefault={onSetDefault}
                onUnarchive={onUnarchive}
                onAskLeave={setLeaving}
              />
            ))}
          {workspaces.data.some((view) => view.isArchived) ? (
            <section
              aria-labelledby={archivedTitleId}
              className="mt-2 flex flex-col gap-3"
            >
              <h2
                id={archivedTitleId}
                className="m-0 text-item-heading leading-[1.45] font-[650] text-foreground"
              >
                {copy.archivedTitle}
              </h2>
              <Note>{copy.archivedNote}</Note>
              {workspaces.data
                .filter((view) => view.isArchived)
                .map((view) => (
                  <Card
                    key={view.id}
                    copy={copy}
                    view={view}
                    busy={busy}
                    onSetDefault={onSetDefault}
                    onUnarchive={onUnarchive}
                    onAskLeave={setLeaving}
                  />
                ))}
            </section>
          ) : null}
        </>
      )}
      {actionError ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {actionError === "last-owner"
            ? copy.lastOwnerError
            : copy.actionError}
        </p>
      ) : null}
      <AlertDialog
        open={leaving !== null}
        onOpenChange={(open) => {
          if (!open) setLeaving(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {fillCopy(copy.confirm.title, { name: leaving?.name ?? "" })}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {fillCopy(copy.confirm.text, { name: leaving?.name ?? "" })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{copy.confirm.cancel}</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => {
                if (leaving) onLeave(leaving.id);
                setLeaving(null);
              }}
            >
              {copy.confirm.action}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      {notice ? <Toast>{notice}</Toast> : null}
    </section>
  );
}
