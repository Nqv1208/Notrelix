import * as React from "react";
import { ArrowUpRight, Check, LayoutGrid, Plus } from "lucide-react";
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  BTN_PRIMARY,
  BTN_SM,
  FOCUS,
  LINK_BUTTON,
  LoadError,
  Tag,
  Toast,
} from "../shared/account-parts";
import {
  fillCopy,
  pluralCopy,
  type AccountSection,
} from "../shared/account-surface-model";
import type {
  AccountWorkspaceTone,
  AccountWorkspaceView,
  DeletedWorkspaceView,
} from "./account-workspaces-surface";
import type { WorkspacesHomeCopy } from "./workspaces-home.copy";

export interface WorkspacesHomeSurfaceProps {
  readonly copy: WorkspacesHomeCopy;
  /** The person's first name or full name; empty = a plain welcome. */
  readonly name: string;
  readonly workspaces: AccountSection<readonly AccountWorkspaceView[]>;
  readonly deleted: AccountSection<readonly DeletedWorkspaceView[]>;
  /** An action is running: its buttons wait. */
  readonly busy: boolean;
  readonly actionError: "failed" | "restore" | null;
  readonly notice: string | null;
  readonly create: { readonly href: string; readonly onNavigate: () => void };
  readonly account: { readonly href: string; readonly onNavigate: () => void };
  readonly onSetDefault: (workspaceId: string) => void;
  readonly onUnarchive: (workspaceId: string) => void;
  readonly onRestore: (workspaceId: string) => void;
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

function Mark({
  tone,
  initials,
  className,
}: {
  tone: AccountWorkspaceTone;
  initials: string;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-12 shrink-0 place-items-center rounded-[12px] text-item-heading font-bold",
        MARK[tone],
        className,
      )}
    >
      {initials}
    </span>
  );
}

function metaOf(copy: WorkspacesHomeCopy, view: AccountWorkspaceView): string {
  const parts: string[] = [copy.roles[view.role]];
  if (view.memberCount !== null)
    parts.push(
      pluralCopy(
        { one: copy.people_one, other: copy.people_other },
        view.memberCount,
      ),
    );
  return parts.join(" · ");
}

/** One workspace: the whole card opens it; "make default" is its own button above the link. */
function WorkspaceCard({
  copy,
  view,
  busy,
  onSetDefault,
}: {
  copy: WorkspacesHomeCopy;
  view: AccountWorkspaceView;
  busy: boolean;
  onSetDefault: (id: string) => void;
}) {
  return (
    <li
      data-f="wh-card"
      className={cn(
        "group relative flex min-h-[132px] flex-col gap-4 rounded-[14px] border bg-popover p-4 transition-[border-color,box-shadow] hover:border-line-strong hover:shadow-sm dark:bg-card",
        view.isDefault ? "border-line-strong" : "border-line",
      )}
    >
      <div className="flex items-start gap-3.5">
        <Mark tone={view.tone} initials={view.initials} />
        <div className="min-w-0 flex-1">
          <h3 className="m-0 text-item-heading leading-[1.4] font-[650] text-foreground">
            <a
              href={view.open.href}
              aria-label={fillCopy(copy.openLabel, { name: view.name })}
              onClick={(event) => {
                if (!isPlainClick(event)) return;
                event.preventDefault();
                view.open.onNavigate();
              }}
              className={cn(
                "block truncate rounded-[6px] after:absolute after:inset-0 after:rounded-[14px] after:content-['']",
                FOCUS,
              )}
            >
              {view.name}
            </a>
          </h3>
          <span className="mt-0.5 block truncate text-crumb leading-[1.45] text-meta-foreground">
            {metaOf(copy, view)}
          </span>
        </div>
        <ArrowUpRight
          aria-hidden="true"
          strokeWidth={1.8}
          className="size-4 shrink-0 text-meta-foreground opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100"
        />
      </div>
      <div className="mt-auto flex min-h-6 items-center gap-2">
        {view.isDefault ? (
          <Tag tone="ok">
            <Check
              aria-hidden="true"
              strokeWidth={1.8}
              className="mr-[3px] size-3"
            />
            {copy.defaultTag}
          </Tag>
        ) : (
          <button
            type="button"
            disabled={busy}
            aria-label={fillCopy(copy.makeDefaultLabel, { name: view.name })}
            onClick={() => onSetDefault(view.id)}
            className={cn(
              LINK_BUTTON,
              FOCUS,
              "relative z-10 rounded-[4px] opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100 max-md:opacity-100",
            )}
          >
            {copy.makeDefault}
          </button>
        )}
      </div>
    </li>
  );
}

function CreateCard({
  copy,
  create,
}: {
  copy: WorkspacesHomeCopy;
  create: WorkspacesHomeSurfaceProps["create"];
}) {
  return (
    <li className="list-none">
      <a
        href={create.href}
        onClick={(event) => {
          if (!isPlainClick(event)) return;
          event.preventDefault();
          create.onNavigate();
        }}
        className={cn(
          "flex h-full min-h-[132px] flex-col items-start justify-center gap-2 rounded-[14px] border border-dashed border-line-strong p-4 text-foreground transition-colors hover:bg-state-hover-strong",
          FOCUS,
        )}
      >
        <span className="grid size-12 place-items-center rounded-[12px] border border-line-strong text-meta-foreground">
          <Plus aria-hidden="true" className="size-5" strokeWidth={1.8} />
        </span>
        <span className="text-item-heading leading-[1.4] font-[650]">
          {copy.createCard}
        </span>
        <span className="text-crumb leading-[1.45] text-meta-foreground">
          {copy.createHint}
        </span>
      </a>
    </li>
  );
}

function SideRow({
  mark,
  title,
  meta,
  action,
}: {
  mark: React.ReactNode;
  title: string;
  meta: string;
  action: React.ReactNode;
}) {
  return (
    <li className="flex items-center gap-3 rounded-[12px] border border-line bg-popover px-3.5 py-3 max-sm:flex-wrap dark:bg-card">
      {mark}
      <div className="min-w-0 flex-1">
        <span className="block truncate text-compact leading-[1.45] font-[650] text-foreground">
          {title}
        </span>
        <span className="block truncate text-crumb leading-[1.45] text-meta-foreground">
          {meta}
        </span>
      </div>
      {action}
    </li>
  );
}

/**
 * Home (`/home`): where a signed-in person lands before choosing where to
 * work. The workspaces are a launcher: one card opens one workspace; the
 * default is marked; archived and recently deleted ones sit below. Managing
 * roles and leaving stay on the account's "Your workspaces" page.
 */
export function WorkspacesHomeSurface({
  copy,
  name,
  workspaces,
  deleted,
  busy,
  actionError,
  notice,
  create,
  account,
  onSetDefault,
  onUnarchive,
  onRestore,
  onRetry,
}: WorkspacesHomeSurfaceProps) {
  const titleId = React.useId();
  const listId = React.useId();
  const archivedId = React.useId();
  const deletedId = React.useId();
  const active =
    workspaces.status === "ready"
      ? workspaces.data.filter((view) => !view.isArchived)
      : [];
  const archived =
    workspaces.status === "ready"
      ? workspaces.data.filter((view) => view.isArchived)
      : [];
  const hasAny = workspaces.status === "ready" && workspaces.data.length > 0;
  const deletedList = deleted.status === "ready" ? deleted.data : [];
  const empty = workspaces.status === "ready" && !hasAny;

  return (
    <div className="mx-auto flex w-full max-w-[1040px] flex-col gap-8 px-6 pt-10 pb-16 max-sm:px-4 max-sm:pt-6">
      <header className="flex flex-wrap items-end gap-x-6 gap-y-4">
        <div className="min-w-0">
          <h1
            id={titleId}
            className="m-0 text-xl leading-[1.3] font-[650] text-foreground"
          >
            {name ? fillCopy(copy.hello, { name }) : copy.helloAnon}
          </h1>
          <p className="m-0 mt-1 text-compact leading-normal text-meta-foreground">
            {workspaces.status === "ready"
              ? hasAny
                ? pluralCopy(
                    { one: copy.summary_one, other: copy.summary_other },
                    active.length,
                  )
                : copy.summaryNone
              : " "}
          </p>
        </div>
        <div className="ml-auto flex items-center gap-2.5">
          <a
            href={account.href}
            onClick={(event) => {
              if (!isPlainClick(event)) return;
              event.preventDefault();
              account.onNavigate();
            }}
            className={cn(LINK_BUTTON, FOCUS, "rounded-[4px]")}
          >
            {copy.accountLink}
          </a>
          <a
            href={create.href}
            onClick={(event) => {
              if (!isPlainClick(event)) return;
              event.preventDefault();
              create.onNavigate();
            }}
            className={cn(BTN_PRIMARY, FOCUS)}
          >
            <Plus aria-hidden="true" className="size-4" strokeWidth={1.8} />
            {copy.create}
          </a>
        </div>
      </header>

      {workspaces.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.loading}
          className="grid grid-cols-[repeat(auto-fill,minmax(260px,1fr))] gap-4"
        >
          <Skeleton className="h-[132px] rounded-[14px]" />
          <Skeleton className="h-[132px] rounded-[14px]" />
          <Skeleton className="h-[132px] rounded-[14px]" />
        </div>
      ) : workspaces.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={onRetry}
        />
      ) : empty ? (
        <Empty className="border-0">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <LayoutGrid aria-hidden="true" className="text-icon" />
            </EmptyMedia>
            <EmptyTitle>
              <h2 className="text-section-heading text-foreground">
                {copy.emptyTitle}
              </h2>
            </EmptyTitle>
            <EmptyDescription className="text-body">
              {copy.emptyDescription}
            </EmptyDescription>
          </EmptyHeader>
          <EmptyContent>
            <a
              href={create.href}
              onClick={(event) => {
                if (!isPlainClick(event)) return;
                event.preventDefault();
                create.onNavigate();
              }}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              {copy.createCard}
            </a>
          </EmptyContent>
        </Empty>
      ) : (
        <section aria-labelledby={listId} className="flex flex-col gap-3">
          <h2
            id={listId}
            className="m-0 text-item-heading leading-[1.45] font-[650] text-foreground"
          >
            {copy.yourWorkspaces}
          </h2>
          <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(260px,1fr))] gap-4 p-0">
            {active.map((view) => (
              <WorkspaceCard
                key={view.id}
                copy={copy}
                view={view}
                busy={busy}
                onSetDefault={onSetDefault}
              />
            ))}
            <CreateCard copy={copy} create={create} />
          </ul>
        </section>
      )}

      {archived.length > 0 ? (
        <section aria-labelledby={archivedId} className="flex flex-col gap-3">
          <div>
            <h2
              id={archivedId}
              className="m-0 text-item-heading leading-[1.45] font-[650] text-foreground"
            >
              {copy.archivedTitle}
            </h2>
            <p className="m-0 text-crumb leading-normal text-meta-foreground">
              {copy.archivedNote}
            </p>
          </div>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {archived.map((view) => (
              <SideRow
                key={view.id}
                mark={
                  <Mark
                    tone={view.tone}
                    initials={view.initials}
                    className="size-9 rounded-[10px] text-compact opacity-70"
                  />
                }
                title={view.name}
                meta={metaOf(copy, view)}
                action={
                  <button
                    type="button"
                    disabled={busy}
                    aria-label={fillCopy(copy.unarchiveLabel, {
                      name: view.name,
                    })}
                    onClick={() => onUnarchive(view.id)}
                    className={cn(BTN_SM, FOCUS)}
                  >
                    {copy.unarchive}
                  </button>
                }
              />
            ))}
          </ul>
        </section>
      ) : null}

      {deletedList.length > 0 ? (
        <section aria-labelledby={deletedId} className="flex flex-col gap-3">
          <div>
            <h2
              id={deletedId}
              className="m-0 text-item-heading leading-[1.45] font-[650] text-foreground"
            >
              {copy.deletedTitle}
            </h2>
            <p className="m-0 text-crumb leading-normal text-meta-foreground">
              {copy.deletedNote}
            </p>
          </div>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {deletedList.map((view) => (
              <SideRow
                key={view.id}
                mark={
                  <Mark
                    tone={view.tone}
                    initials={view.initials}
                    className="size-9 rounded-[10px] text-compact opacity-70"
                  />
                }
                title={view.name}
                meta={fillCopy(copy.deletedMeta, {
                  date: view.deletedOn,
                  left: pluralCopy(
                    { one: copy.daysLeft_one, other: copy.daysLeft_other },
                    view.daysLeft,
                  ),
                })}
                action={
                  <button
                    type="button"
                    disabled={busy}
                    aria-label={fillCopy(copy.restoreLabel, {
                      name: view.name,
                    })}
                    onClick={() => onRestore(view.id)}
                    className={cn(BTN_SM, FOCUS)}
                  >
                    {copy.restore}
                  </button>
                }
              />
            ))}
          </ul>
        </section>
      ) : deleted.status === "error" ? (
        <LoadError
          message={copy.deletedLoadError}
          retry={copy.retry}
          onRetry={onRetry}
        />
      ) : null}

      {actionError ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {actionError === "restore" ? copy.restoreError : copy.actionError}
        </p>
      ) : null}
      {notice ? <Toast>{notice}</Toast> : null}
    </div>
  );
}
