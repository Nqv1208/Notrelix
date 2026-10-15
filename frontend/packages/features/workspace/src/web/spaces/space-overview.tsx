import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import type { WorkspaceDestination } from "../../core/model/navigation";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import { ShellLink } from "../shell/shell-link";
import {
  AvatarStack,
  FOCUS,
  Glyph,
  MarkSquare,
  BTN_SM,
  BTN_SM_PRIMARY,
  SpaceAvatar,
  type GlyphName,
} from "./space-page-parts";
import {
  spaceWhenLabel,
  type SpaceAccessView,
  type SpaceAttentionRowView,
  type SpaceBoardRowView,
  type SpaceChildView,
  type SpaceDocRowView,
  type SpaceTemplateView,
  type SpaceWorkloadRowView,
} from "./space-page-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/**
 * The overview of a department (Final-Space `tong-quan`, `phong-moi`): the
 * running Boards, the sub-departments and the recent Docs in the column, the
 * summary in the aside. A column or card the producers cannot fill is left
 * out, and the grid closes over it.
 */

const LIST =
  "overflow-hidden rounded-[12px] border border-line bg-popover dark:bg-card";
const NOTE = "mt-2 mb-0 text-crumb leading-[1.55] text-meta-foreground";

export function SectionHead({
  id,
  title,
  count,
  action,
}: {
  id: string;
  title: string;
  count?: number;
  action?: ReactNode;
}) {
  return (
    <div data-f="sh" className="mb-2.5 flex items-center gap-2">
      <h2
        id={id}
        data-f="sh-h2"
        className="m-0 text-item-heading leading-[1.45] text-foreground"
      >
        {title}
      </h2>
      {count === undefined ? null : (
        <span
          data-f="sh-n"
          className="text-compact leading-[1.45] text-meta-foreground"
        >
          {count}
        </span>
      )}
      {action}
    </div>
  );
}

const SECTION_LINK =
  "ml-auto text-crumb leading-[1.45] font-medium text-state-selected-foreground hover:underline";

const BOARD_COLUMNS = {
  progress: "170px",
  attention: "120px",
  people: "88px",
  updated: "96px",
} as const;

function boardGrid(shows: {
  progress: boolean;
  attention: boolean;
  people: boolean;
}): string {
  return [
    "minmax(0,1fr)",
    shows.progress ? BOARD_COLUMNS.progress : null,
    shows.attention ? BOARD_COLUMNS.attention : null,
    shows.people ? BOARD_COLUMNS.people : null,
    BOARD_COLUMNS.updated,
  ]
    .filter((part): part is string => part !== null)
    .join(" ");
}

function BoardProgress({ row }: { row: SpaceBoardRowView }) {
  const progress = row.progress;
  if (!progress) return <span />;
  if (progress.kind === "text") {
    return (
      <span
        data-f="pg"
        className="flex items-center gap-2 text-xs leading-[1.45] text-foreground tabular-nums"
      >
        {progress.text}
      </span>
    );
  }
  const pct = progress.total === 0 ? 0 : (progress.done / progress.total) * 100;
  return (
    <span
      data-f="pg"
      className="flex items-center gap-2 text-xs leading-[1.45] text-meta-foreground tabular-nums"
    >
      <span
        data-f="track"
        className="h-[5px] flex-1 overflow-hidden rounded-[3px] bg-state-hover-strong"
      >
        <span
          className="block h-full rounded-[3px] bg-work-status-done"
          style={{ width: `${Math.round(pct)}%` }}
        />
      </span>
      {progress.done}/{progress.total}
    </span>
  );
}

const ATTENTION_CLASS = {
  over: "text-danger-text font-semibold",
  warn: "text-warning-text font-semibold",
  ok: "text-meta-foreground font-normal",
} as const;

function BoardRow({
  row,
  navigation,
  copy,
  now,
  shows,
}: {
  row: SpaceBoardRowView;
  navigation: ShellNavigation;
  copy: WorkspaceSpacesCopy;
  now: number;
  shows: { progress: boolean; attention: boolean; people: boolean };
}) {
  const o = copy.overview;
  const line = row.meta
    ? row.meta
    : row.isPrivate
      ? row.addedCount === null
        ? o.privateLineNoCount
        : formatCopy(o.privateLine, { count: row.addedCount })
      : null;
  return (
    <ShellLink
      data-f="br"
      destination={{ section: "board", boardId: row.id }}
      navigation={navigation}
      style={{ gridTemplateColumns: boardGrid(shows) }}
      className={cn(
        "box-content grid min-h-[58px] items-center gap-3.5 border-t border-line px-4 first:border-t-0 hover:bg-background",
        FOCUS,
        "focus-visible:ring-inset",
      )}
    >
      <span data-f="bnm" className="flex min-w-0 items-center gap-2.5">
        <span
          data-f="bi"
          className="grid size-[30px] shrink-0 place-items-center rounded-[8px] bg-state-selected text-state-selected-foreground"
        >
          <Glyph name={row.icon} />
        </span>
        <span className="min-w-0">
          <b
            data-f="bname"
            className="block truncate text-body leading-[1.45] font-semibold text-foreground"
          >
            {row.name}
          </b>
          {line ? (
            <small
              data-f="bsub"
              className="flex items-center gap-[5px] text-xs leading-[1.45] text-meta-foreground"
            >
              {row.isPrivate && !row.meta ? (
                <Glyph name="lock" className="size-3" />
              ) : null}
              {line}
            </small>
          ) : null}
        </span>
      </span>
      {shows.progress ? <BoardProgress row={row} /> : null}
      {shows.attention ? (
        row.attention ? (
          <span
            data-f="att"
            className={cn(
              "text-crumb leading-[1.45]",
              ATTENTION_CLASS[row.attention.tone],
            )}
          >
            {row.attention.text}
          </span>
        ) : (
          <span />
        )
      ) : null}
      {shows.people ? (
        row.people ? (
          <AvatarStack
            dataF="bstack"
            people={row.people}
            ringClass="ring-2 ring-popover dark:ring-card"
            avatarClass="size-[22px] text-avatar-sm"
          />
        ) : (
          <span />
        )
      ) : null}
      <span
        data-f="when"
        className="text-right text-xs leading-[1.45] text-meta-foreground"
      >
        {spaceWhenLabel(copy, row.updatedAt, now)}
      </span>
    </ShellLink>
  );
}

export function BoardsSection({
  copy,
  navigation,
  now,
  rows,
  total,
  libraryHref,
  note,
}: {
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  now: number;
  rows: readonly SpaceBoardRowView[];
  total: number;
  libraryHref: WorkspaceDestination | null;
  note: string | null;
}) {
  const o = copy.overview;
  const shows = {
    progress: rows.some((row) => row.progress),
    attention: rows.some((row) => row.attention),
    people: rows.some((row) => row.people),
  };
  const head = (label: string, align?: "right") => (
    <span className={align === "right" ? "text-right" : undefined}>
      {label}
    </span>
  );
  return (
    <section aria-labelledby="space-h-boards">
      <SectionHead
        id="space-h-boards"
        title={o.boards}
        count={rows.length}
        action={
          libraryHref && rows.length > 0 ? (
            <ShellLink
              data-f="sh-a"
              destination={libraryHref}
              navigation={navigation}
              className={cn(SECTION_LINK, FOCUS)}
            >
              {formatCopy(o.boardsAll, { count: total })}
            </ShellLink>
          ) : null
        }
      />
      {rows.length === 0 ? (
        <p
          data-f="empty"
          className="m-0 rounded-[12px] border border-dashed border-line px-4 py-4 text-compact leading-[1.45] text-meta-foreground"
        >
          {o.boardsEmpty}
        </p>
      ) : (
        <div data-f="list" className={LIST}>
          <div
            data-f="lh"
            aria-hidden="true"
            style={{ gridTemplateColumns: boardGrid(shows) }}
            className="box-content grid h-[34px] items-center gap-3.5 border-b border-line bg-background px-4 text-micro leading-[1.45] font-semibold text-meta-foreground"
          >
            {head(o.listHead.board)}
            {shows.progress ? head(o.listHead.progress) : null}
            {shows.attention ? head(o.listHead.attention) : null}
            {shows.people ? head(o.listHead.people) : null}
            {head(o.listHead.updated, "right")}
          </div>
          {rows.map((row) => (
            <BoardRow
              key={row.id}
              row={row}
              navigation={navigation}
              copy={copy}
              now={now}
              shows={shows}
            />
          ))}
        </div>
      )}
      {note ? (
        <p data-f="note" className={NOTE}>
          {note}
        </p>
      ) : null}
    </section>
  );
}

export function ChildrenSection({
  copy,
  navigation,
  spaceName,
  children,
  canCreate,
  onCreate,
}: {
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  spaceName: string;
  children: readonly SpaceChildView[];
  canCreate: boolean;
  onCreate: () => void;
}) {
  const o = copy.overview;
  return (
    <section aria-labelledby="space-h-subs">
      <SectionHead
        id="space-h-subs"
        title={o.children}
        count={children.length}
      />
      <div
        data-f="subs"
        className="grid grid-cols-[repeat(auto-fit,minmax(260px,1fr))] gap-3"
      >
        {children.map((child) => {
          const meta = [
            child.people === null
              ? null
              : formatCopy(copy.aside.people_other, { count: child.people }),
            child.boards === null
              ? null
              : formatCopy(
                  child.boards === 1 ? o.childMeta_one : o.childMeta_other,
                  { count: child.boards },
                ),
          ]
            .filter((part): part is string => part !== null)
            .join(" · ");
          return (
            <ShellLink
              key={child.id}
              data-f="sub"
              destination={{ section: "space", spaceId: child.id }}
              navigation={navigation}
              className={cn(
                "flex items-start gap-3 rounded-[12px] border border-line bg-popover p-3.5 hover:border-line-strong dark:bg-card",
                FOCUS,
              )}
            >
              <MarkSquare
                dataF="subdm"
                name={child.name}
                tone={child.markTone}
                className="size-8 rounded-[9px] text-mark-md"
              />
              <span className="min-w-0">
                <b
                  data-f="subname"
                  className="block text-body leading-[1.45] font-semibold text-foreground"
                >
                  {child.name}
                </b>
                {meta ? (
                  <small
                    data-f="submeta"
                    className="mt-0.5 block text-crumb leading-[1.45] text-meta-foreground"
                  >
                    {meta}
                  </small>
                ) : null}
                <span
                  data-f="why"
                  className="mt-2 flex items-center gap-[5px] text-xs leading-[1.45] text-muted-foreground"
                >
                  <Glyph
                    name={child.isPrivate ? "lock" : "arrowDown"}
                    className="size-3"
                  />
                  {child.isPrivate
                    ? o.childPrivate
                    : formatCopy(o.childOpen, { name: spaceName })}
                </span>
              </span>
            </ShellLink>
          );
        })}
        {canCreate ? (
          <button
            data-f="subadd"
            type="button"
            onClick={onCreate}
            className={cn(
              "flex items-center gap-3 rounded-[12px] border border-dashed border-line bg-transparent p-3.5 text-left text-nav leading-[1.45] font-medium text-muted-foreground hover:border-line-strong",
              FOCUS,
            )}
          >
            <Glyph name="plus" />
            {o.childAdd}
          </button>
        ) : null}
      </div>
      {children.length > 0 ? (
        <p data-f="note" className={NOTE}>
          {formatCopy(o.childNote, { name: spaceName })}
        </p>
      ) : null}
    </section>
  );
}

export function DocsSection({
  copy,
  navigation,
  now,
  rows,
  total,
  libraryHref,
}: {
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  now: number;
  rows: readonly SpaceDocRowView[];
  total: number;
  libraryHref: WorkspaceDestination | null;
}) {
  const o = copy.overview;
  const editor = rows.some((row) => row.editor);
  const grid = editor ? "minmax(0,1fr) 180px 96px" : "minmax(0,1fr) 96px";
  return (
    <section aria-labelledby="space-h-docs">
      <SectionHead
        id="space-h-docs"
        title={o.docs}
        action={
          libraryHref && rows.length > 0 ? (
            <ShellLink
              data-f="sh-a"
              destination={libraryHref}
              navigation={navigation}
              className={cn(SECTION_LINK, FOCUS)}
            >
              {formatCopy(o.docsAll, { count: total })}
            </ShellLink>
          ) : null
        }
      />
      {rows.length === 0 ? (
        <p
          data-f="empty"
          className="m-0 rounded-[12px] border border-dashed border-line px-4 py-4 text-compact leading-[1.45] text-meta-foreground"
        >
          {o.docsEmpty}
        </p>
      ) : (
        <div data-f="list" className={LIST}>
          {rows.map((row) => (
            <ShellLink
              key={row.id}
              data-f="br"
              destination={{ section: "doc", docId: row.id }}
              navigation={navigation}
              style={{ gridTemplateColumns: grid }}
              className={cn(
                "box-content grid min-h-[58px] items-center gap-3.5 border-t border-line px-4 first:border-t-0 hover:bg-background",
                FOCUS,
                "focus-visible:ring-inset",
              )}
            >
              <span data-f="bnm" className="flex min-w-0 items-center gap-2.5">
                <span
                  data-f="bi"
                  className="grid size-[30px] shrink-0 place-items-center rounded-[8px] bg-state-hover-strong text-muted-foreground"
                >
                  <Glyph name="doc" />
                </span>
                <span className="min-w-0">
                  <b
                    data-f="bname"
                    className="block truncate text-body leading-[1.45] font-semibold text-foreground"
                  >
                    {row.name}
                  </b>
                  {row.meta ? (
                    <small
                      data-f="bsub"
                      className="flex items-center gap-[5px] text-xs leading-[1.45] text-meta-foreground"
                    >
                      {row.meta}
                    </small>
                  ) : null}
                </span>
              </span>
              {editor ? (
                <span
                  data-f="editor"
                  className="text-crumb leading-[1.45] text-meta-foreground"
                >
                  {row.editor}
                </span>
              ) : null}
              <span
                data-f="when"
                className="text-right text-xs leading-[1.45] text-meta-foreground"
              >
                {spaceWhenLabel(copy, row.updatedAt, now)}
              </span>
            </ShellLink>
          ))}
        </div>
      )}
    </section>
  );
}

const ASIDE_H3 =
  "m-0 mb-2 text-compact leading-[1.45] font-semibold text-muted-foreground";

const NUM_CLASS = {
  over: "bg-danger-subtle text-danger-text",
  warn: "bg-warning-subtle text-warning-text",
  mute: "bg-state-hover-strong text-muted-foreground",
} as const;

export function AttentionBlock({
  copy,
  rows,
}: {
  copy: WorkspaceSpacesCopy;
  rows: readonly SpaceAttentionRowView[];
}) {
  return (
    <div data-f="ab">
      <h3 data-f="abh" className={ASIDE_H3}>
        {copy.aside.attention}
      </h3>
      <div data-f="attl">
        {rows.map((row) => (
          <a
            key={row.id}
            data-f="attrow"
            href={row.href ?? undefined}
            aria-disabled={row.href ? undefined : true}
            className={cn(
              "-mx-2 flex min-h-9 items-center gap-2.5 rounded-[8px] px-2 text-nav leading-[1.45] font-normal text-foreground hover:bg-state-hover-strong",
              FOCUS,
            )}
          >
            <span
              data-f="num"
              className={cn(
                "grid h-[22px] min-w-[26px] place-items-center rounded-[6px] text-xs leading-[1.45] font-bold tabular-nums",
                NUM_CLASS[row.tone],
              )}
            >
              {row.count}
            </span>
            {row.label}
            <Glyph
              name="chevronRight"
              className="ml-auto size-3.5 text-meta-foreground"
            />
          </a>
        ))}
      </div>
    </div>
  );
}

export function WorkloadBlock({
  copy,
  rows,
}: {
  copy: WorkspaceSpacesCopy;
  rows: readonly SpaceWorkloadRowView[];
}) {
  const a = copy.aside;
  return (
    <div data-f="ab">
      <h3 data-f="abh" className={ASIDE_H3}>
        {a.workload}
      </h3>
      <div data-f="wl" className="flex flex-col gap-[9px]">
        {rows.map((row) => (
          <div
            key={row.id}
            data-f="wr"
            className="grid grid-cols-[24px_104px_1fr_82px] items-center gap-2 text-compact leading-[1.45]"
          >
            <SpaceAvatar person={row.person} className="size-6 text-mark-xs" />
            <span data-f="wname" className="text-foreground">
              {row.person.name}
            </span>
            <span
              data-f="bar"
              className="flex h-2 overflow-hidden rounded bg-state-hover-strong"
            >
              <span
                className="bg-work-status-doing"
                style={{ width: `${row.activePct}%` }}
              />
              {row.overdue > 0 ? (
                <span
                  className="bg-danger-text"
                  style={{ width: `${row.overduePct}%` }}
                />
              ) : null}
            </span>
            <span
              data-f="wc"
              className="text-right text-xs leading-[1.45] text-meta-foreground tabular-nums"
            >
              {row.overdue > 0
                ? formatCopy(a.workloadOverdue, {
                    count: row.active,
                    overdue: row.overdue,
                  })
                : formatCopy(a.workloadCount, { count: row.active })}
            </span>
          </div>
        ))}
      </div>
      <div
        data-f="wlg"
        className="mt-2 flex gap-3 text-micro leading-[1.45] text-meta-foreground"
      >
        <span className="inline-flex items-center gap-[5px]">
          <i className="inline-block size-2 rounded-[2px] bg-work-status-doing" />
          {a.legendDoing}
        </span>
        <span className="inline-flex items-center gap-[5px]">
          <i className="inline-block size-2 rounded-[2px] bg-danger-text" />
          {a.legendOverdue}
        </span>
        <span className="ml-auto">{a.legendNote}</span>
      </div>
    </div>
  );
}

export function AccessBlock({
  copy,
  navigation,
  spaceId,
  access,
  canManage,
  hint,
  showLink = true,
}: {
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  spaceId: string;
  access: SpaceAccessView;
  canManage: boolean;
  /** Replaces the visibility sentence (the new-department state). */
  hint?: string;
  showLink?: boolean;
}) {
  const a = copy.aside;
  const isPrivate = access.visibility === "Private";
  const members: WorkspaceDestination = {
    section: "space",
    spaceId,
    tab: "members",
  };
  return (
    <div data-f="ab">
      <h3 data-f="abh" className={ASIDE_H3}>
        {a.access}
      </h3>
      <div
        data-f="acc"
        className="flex flex-col gap-3 rounded-[12px] border border-line bg-background p-3.5"
      >
        <div data-f="vis" className="flex items-start gap-2.5">
          <span
            data-f="vic"
            className="box-content grid size-[30px] shrink-0 place-items-center rounded-[8px] border border-line bg-popover text-muted-foreground dark:bg-card"
          >
            <Glyph name={isPrivate ? "lock" : "users"} />
          </span>
          <span>
            <b className="block text-nav leading-[1.45] font-semibold text-foreground">
              {isPrivate ? a.private : a.open}
            </b>
            <small className="block text-crumb leading-normal text-meta-foreground">
              {hint ?? (isPrivate ? a.privateHint : a.openHint)}
            </small>
          </span>
        </div>
        {access.summary ? (
          <div
            data-f="accsum"
            className="text-crumb leading-[1.45] text-muted-foreground"
          >
            {access.summary}
          </div>
        ) : null}
        {access.rows.length > 0 ? (
          <dl
            data-f="accdl"
            className="m-0 grid grid-cols-[96px_1fr] gap-x-2.5 gap-y-2 text-crumb leading-[1.45]"
          >
            {access.rows.map((row) => (
              <div key={row.key} className="contents">
                <dt data-f="dt" className="text-meta-foreground">
                  {row.label}
                </dt>
                <dd
                  data-f="dd"
                  className={cn(
                    "m-0",
                    row.empty ? "text-meta-foreground" : "text-foreground",
                  )}
                >
                  {row.destination ? (
                    <ShellLink
                      destination={row.destination}
                      navigation={navigation}
                      className={cn("text-foreground hover:underline", FOCUS)}
                    >
                      {row.value}
                    </ShellLink>
                  ) : (
                    row.value
                  )}
                  {row.note ? (
                    <small className="block text-note-small text-meta-foreground">
                      {row.note}
                    </small>
                  ) : null}
                </dd>
              </div>
            ))}
          </dl>
        ) : null}
        {showLink ? (
          <ShellLink
            data-f="more"
            destination={members}
            navigation={navigation}
            className={cn(
              "text-crumb leading-[1.45] font-semibold text-state-selected-foreground hover:underline",
              FOCUS,
            )}
          >
            {canManage ? a.manage : a.seeMembers}
          </ShellLink>
        ) : null}
      </div>
    </div>
  );
}

const TEMPLATE_ICON: Record<SpaceTemplateView["icon"], GlyphName> = {
  contract: "docLines",
  intake: "inbox",
  calendar: "calendar",
};

/** Final-Space `phong-moi`: the three first steps and the templates. */
export function SetupSection({
  copy,
  name,
  isPrivate,
  templates,
  onAddMembers,
  onCreateBoard,
}: {
  copy: WorkspaceSpacesCopy;
  name: string;
  isPrivate: boolean;
  templates: readonly SpaceTemplateView[];
  onAddMembers: () => void;
  onCreateBoard: (() => void) | null;
}) {
  const s = copy.setup;
  const step = (
    key: string,
    title: string,
    hint: string,
    done: boolean,
    action: ReactNode,
  ) => (
    <div
      key={key}
      data-f="step"
      className="flex items-center gap-3 border-t border-line px-[18px] py-3.5"
    >
      <span
        data-f="ck"
        className={cn(
          "box-border grid size-5 shrink-0 place-items-center rounded-full border-[1.5px] text-primary-foreground",
          done
            ? "border-work-status-done bg-work-status-done"
            : "border-line-strong",
        )}
      >
        {done ? (
          <Glyph name="check" className="size-3" strokeWidth={3} />
        ) : null}
      </span>
      <span data-f="stext">
        <b
          className={cn(
            "block text-body leading-[1.45]",
            done
              ? "font-medium text-meta-foreground line-through"
              : "font-semibold text-foreground",
          )}
        >
          {title}
        </b>
        <small className="mt-px block text-crumb leading-[1.45] text-meta-foreground">
          {hint}
        </small>
      </span>
      {action ? <span className="ml-auto">{action}</span> : null}
    </div>
  );
  return (
    <>
      <div
        data-f="setup"
        className="overflow-hidden rounded-[12px] border border-line bg-popover dark:bg-card"
      >
        <div data-f="setup-top" className="border-b border-line px-[18px] py-4">
          <b className="block text-item-heading leading-[1.45] text-foreground">
            {formatCopy(s.title, { name })}
          </b>
          <small className="mt-0.5 block text-compact leading-[1.45] text-meta-foreground">
            {s.subtitle}
          </small>
        </div>
        {step(
          "created",
          s.created,
          isPrivate ? s.createdHint : s.createdHintOpen,
          true,
          null,
        )}
        {step(
          "members",
          s.members,
          s.membersHint,
          false,
          <button
            data-f="btn-sm"
            type="button"
            onClick={onAddMembers}
            className={cn(BTN_SM_PRIMARY, FOCUS)}
          >
            {s.membersAction}
          </button>,
        )}
        {step(
          "board",
          s.board,
          s.boardHint,
          false,
          <button
            data-f="btn-sm"
            type="button"
            aria-disabled={onCreateBoard ? undefined : true}
            onClick={onCreateBoard ?? undefined}
            className={cn(BTN_SM, FOCUS)}
          >
            {s.boardAction}
          </button>,
        )}
      </div>
      {templates.length > 0 ? (
        <section aria-labelledby="space-h-tpl">
          <SectionHead
            id="space-h-tpl"
            title={formatCopy(s.templates, { name })}
          />
          {templates.map((template) => (
            <a
              key={template.id}
              data-f="tpl"
              aria-disabled="true"
              title={s.templateUnavailable}
              className="flex items-center gap-3 rounded-[10px] px-3.5 py-3 hover:bg-state-hover-strong"
            >
              <span
                data-f="tpl-i"
                className="grid size-[34px] shrink-0 place-items-center rounded-[9px] bg-state-hover-strong text-muted-foreground"
              >
                <Glyph name={TEMPLATE_ICON[template.icon]} />
              </span>
              <span>
                <b className="block text-body leading-[1.45] font-semibold text-foreground">
                  {template.title}
                </b>
                <small className="block text-crumb leading-[1.45] text-meta-foreground">
                  {template.hint}
                </small>
              </span>
              <span
                data-f="tpl-use"
                className="ml-auto text-crumb leading-[1.45] font-semibold text-state-selected-foreground"
              >
                {s.useTemplate}
              </span>
            </a>
          ))}
        </section>
      ) : null}
    </>
  );
}
