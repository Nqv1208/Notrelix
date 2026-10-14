import { type ReactNode } from "react";
import {
  AtSign,
  Bell,
  Check,
  Clock,
  Mail,
  MessageSquare,
  Plus,
  SlidersHorizontal,
  Sparkles,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { initialsFor } from "../../core/model/navigation";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  isPlainClick,
} from "../shell/shell-models";
import { DepartmentMark } from "../home/work-row-parts";
import {
  INBOX_TABS,
  type InboxItemView,
  type InboxKind,
  type InboxNeed,
  type InboxSegment,
  type InboxTab,
} from "./inbox-models";
import type { WorkspaceInboxCopy } from "./workspace-inbox.copy";

/**
 * The left column of the Inbox (Final-Inbox `.list`): title row, the five
 * tabs, the unread toggle and the notifications by day.
 */

const KIND_BADGE_CLASS: Record<InboxKind, string> = {
  approve: "bg-warning text-warning-foreground",
  mention: "bg-primary text-primary-foreground",
  assign: "bg-success text-success-foreground",
  overdue: "bg-danger text-danger-foreground",
  invite: "bg-success text-success-foreground",
  comment: "bg-primary text-primary-foreground",
  change: "bg-meta-foreground text-primary-foreground",
  system: "bg-meta-foreground text-primary-foreground",
};

const NEED_CLASS: Record<InboxNeed, string> = {
  approve: "bg-status-chip-review text-warning-text",
  reply: "bg-state-selected text-state-selected-foreground",
  overdue: "bg-danger-subtle text-danger-text",
};

function KindGlyph({ kind }: { kind: InboxKind }) {
  const common = {
    "aria-hidden": true,
    className: "size-[9px]",
    strokeWidth: 2.6,
  } as const;
  switch (kind) {
    case "approve":
      return <Check {...common} />;
    case "mention":
      return <AtSign {...common} />;
    case "assign":
    case "invite":
      return <Plus {...common} />;
    case "comment":
      return <MessageSquare {...common} />;
    case "overdue":
      return <Clock {...common} />;
    case "change":
      return <Sparkles {...common} />;
    default:
      return <Mail {...common} />;
  }
}

/** The 30 px avatar of a notification with its corner badge. */
export function InboxAvatar({
  item,
}: {
  item: Pick<InboxItemView, "person" | "kind" | "avatarText">;
}) {
  const { person, kind } = item;
  if (!person && kind === "overdue") {
    return (
      <span
        data-f="av"
        aria-hidden="true"
        className="relative grid size-[30px] shrink-0 place-items-center rounded-full bg-danger-text text-danger-foreground"
      >
        <Clock className="size-[15px]" strokeWidth={1.8} />
      </span>
    );
  }
  return (
    <span
      data-f="av"
      aria-hidden="true"
      className={cn(
        "relative grid size-[30px] shrink-0 place-items-center rounded-full text-avatar-lg font-bold",
        person
          ? IDENTITY_MARK_CLASS[person.tone]
          : "bg-meta-foreground text-primary-foreground",
      )}
    >
      {person ? (
        initialsFor(person.name)
      ) : item.avatarText ? (
        item.avatarText
      ) : (
        <Bell className="size-[15px]" strokeWidth={1.8} />
      )}
      {person && kind !== "system" && kind !== "change" ? (
        <span
          data-f="kd"
          className={cn(
            "absolute -right-[3px] -bottom-[3px] box-border grid size-4 place-items-center rounded-full border-2 border-popover dark:border-card",
            KIND_BADGE_CLASS[kind],
          )}
        >
          <KindGlyph kind={kind} />
        </span>
      ) : null}
    </span>
  );
}

function Segments({ segments }: { segments: readonly InboxSegment[] }) {
  return (
    <>
      {segments.map((segment, index) =>
        segment.strong ? (
          <b key={index} className="font-semibold text-foreground">
            {segment.text}
          </b>
        ) : (
          <span key={index}>{segment.text}</span>
        ),
      )}
    </>
  );
}

export { Segments as InboxSegments };

export function InboxRow({
  item,
  copy,
  selected,
  timeLabel,
  expanded,
  onSelect,
  onToggleBundle,
}: {
  item: InboxItemView;
  copy: WorkspaceInboxCopy;
  selected: boolean;
  timeLabel: string;
  expanded: boolean;
  onSelect: () => void;
  onToggleBundle: () => void;
}) {
  const className = cn(
    "relative flex gap-[11px] border-b border-border px-4 py-[11px] text-left no-underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-inset",
    selected ? "bg-row-selected" : "bg-transparent hover:bg-state-hover",
    item.bundle ? "" : "cursor-pointer",
  );
  const body = (
    <>
      {item.unread ? (
        <span
          data-f="ud"
          aria-hidden="true"
          className="absolute top-[22px] left-[5px] size-[7px] rounded-full bg-primary"
        />
      ) : null}
      <InboxAvatar item={item} />
      <span data-f="body" className="min-w-0 flex-1">
        <p
          data-f="what"
          className={cn(
            "m-0 text-nav leading-[1.45] font-normal",
            item.unread ? "text-foreground" : "text-muted-foreground",
          )}
        >
          {item.unread ? <span className="sr-only">{copy.unread}</span> : null}
          <Segments segments={item.segments} />
        </p>
        {item.context ? (
          <span
            data-f="res"
            className="mt-[3px] flex items-center gap-1.5 overflow-hidden text-xs leading-[1.45] whitespace-nowrap text-meta-foreground"
          >
            {item.context.department ? (
              <DepartmentMark
                small
                name={item.context.department.name}
                markTone={item.context.department.markTone}
              />
            ) : null}
            <span className="truncate">{item.context.label}</span>
          </span>
        ) : null}
        {item.need ? (
          <span
            data-f="need"
            className={cn(
              "mt-[5px] inline-flex h-5 items-center rounded-full px-[7px] text-micro leading-[1.45] font-semibold",
              NEED_CLASS[item.need],
            )}
          >
            {copy.need[item.need]}
          </span>
        ) : null}
        {item.bundle ? (
          <button
            data-f="bundle"
            type="button"
            aria-expanded={expanded}
            onClick={onToggleBundle}
            className="mt-1 block cursor-pointer border-0 bg-transparent p-0 text-left text-xs leading-[normal] font-semibold text-state-selected-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {formatCopy(expanded ? copy.bundleClose : copy.bundleOpen, {
              count: item.bundle.count,
            })}
          </button>
        ) : null}
      </span>
      <time
        data-f="time"
        dateTime={item.at}
        className="text-xs leading-[1.45] whitespace-nowrap text-meta-foreground"
      >
        {timeLabel}
      </time>
    </>
  );
  if (item.bundle) {
    return (
      <div data-f="it" data-item-id={item.id} className={className}>
        {body}
      </div>
    );
  }
  if (item.href) {
    return (
      <a
        data-f="it"
        data-item-id={item.id}
        href={item.href}
        aria-current={selected ? "true" : undefined}
        onClick={(event) => {
          if (!isPlainClick(event)) return;
          event.preventDefault();
          onSelect();
        }}
        className={className}
      >
        {body}
      </a>
    );
  }
  return (
    <button
      data-f="it"
      data-item-id={item.id}
      type="button"
      aria-current={selected ? "true" : undefined}
      onClick={onSelect}
      className={cn(className, "w-full border-x-0 border-t-0")}
    >
      {body}
    </button>
  );
}

export function InboxListHeader({
  copy,
  onMarkAllRead,
  settingsHref,
  onOpenSettings,
  scope,
}: {
  copy: WorkspaceInboxCopy;
  /** `null` draws "mark all read" inert. */
  onMarkAllRead: (() => void) | null;
  settingsHref: string | null;
  onOpenSettings: () => void;
  /** The "Chỉ workspace X · Xem mọi workspace" line; `null` leaves it out. */
  scope: { workspaceName: string; allHref: string | null } | null;
}) {
  return (
    <>
      <div data-f="lh" className="flex items-center gap-2 px-4 pt-4">
        <h1
          data-f="h1"
          className="m-0 text-sheet-title leading-[1.45] text-foreground"
        >
          {copy.title}
        </h1>
        <span className="flex-1" />
        <button
          data-f="markall"
          type="button"
          aria-disabled={onMarkAllRead ? undefined : true}
          onClick={onMarkAllRead ?? undefined}
          className="box-border inline-flex h-[30px] shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border border-transparent bg-transparent px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-muted-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed"
        >
          {copy.markAllRead}
        </button>
        {settingsHref ? (
          <a
            data-f="ib"
            href={settingsHref}
            aria-label={copy.settings}
            onClick={(event) => {
              if (!isPlainClick(event)) return;
              event.preventDefault();
              onOpenSettings();
            }}
            className="grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <SlidersHorizontal
              aria-hidden="true"
              className="size-4"
              strokeWidth={1.8}
            />
          </a>
        ) : (
          <button
            data-f="ib"
            type="button"
            aria-label={copy.settings}
            aria-disabled="true"
            title={copy.settingsUnavailable}
            className="grid size-8 shrink-0 cursor-not-allowed place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground outline-none"
          >
            <SlidersHorizontal
              aria-hidden="true"
              className="size-4"
              strokeWidth={1.8}
            />
          </button>
        )}
      </div>
      {scope ? (
        <div
          data-f="scope"
          className="px-4 pt-0.5 text-xs leading-[1.45] text-meta-foreground"
        >
          {formatCopy(copy.scope, { name: scope.workspaceName })}
          {scope.allHref ? (
            <a
              href={scope.allHref}
              className="font-medium text-state-selected-foreground no-underline hover:underline"
            >
              {copy.scopeAll}
            </a>
          ) : (
            <span
              aria-disabled="true"
              title={copy.settingsUnavailable}
              className="font-medium text-state-selected-foreground"
            >
              {copy.scopeAll}
            </span>
          )}
        </div>
      ) : null}
    </>
  );
}

export function InboxTabs({
  copy,
  tab,
  counts,
  available,
  onTab,
}: {
  copy: WorkspaceInboxCopy;
  tab: InboxTab;
  counts: Partial<Record<InboxTab, number>>;
  available: Record<InboxTab, boolean>;
  onTab: (tab: InboxTab) => void;
}) {
  return (
    <div
      data-f="tabs"
      role="tablist"
      aria-label={copy.tabsLabel}
      className="flex flex-wrap gap-0.5 border-b border-border px-3 pt-2.5"
    >
      {INBOX_TABS.map((id) => {
        const on = tab === id;
        const usable = available[id];
        const count = counts[id];
        return (
          <button
            key={id}
            data-f="tab"
            type="button"
            role="tab"
            id={`inbox-tab-${id}`}
            aria-selected={on}
            aria-controls="inbox-panel"
            aria-disabled={usable ? undefined : true}
            title={usable ? undefined : copy.tabUnavailable}
            tabIndex={on ? 0 : -1}
            onClick={usable ? () => onTab(id) : undefined}
            className={cn(
              "-mb-px inline-flex h-9 items-center gap-1.5 border-0 border-b-2 border-transparent bg-transparent px-[9px] text-compact leading-[1.45] font-medium whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              on
                ? "border-primary font-semibold text-foreground"
                : "text-muted-foreground",
              usable ? "cursor-pointer" : "cursor-not-allowed",
            )}
          >
            {copy.tabs[id]}
            {usable && count !== undefined ? (
              <span
                data-f="tabc"
                className={cn(
                  "text-micro leading-[1.45] font-semibold",
                  on
                    ? "text-state-selected-foreground"
                    : "text-meta-foreground",
                )}
              >
                {count}
              </span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}

export function InboxToolbar({
  copy,
  unreadOnly,
  onToggleUnread,
}: {
  copy: WorkspaceInboxCopy;
  unreadOnly: boolean;
  onToggleUnread: () => void;
}) {
  return (
    <div
      data-f="lbar"
      className="flex items-center gap-1.5 border-b border-border px-3 py-2"
    >
      <button
        data-f="tog"
        type="button"
        aria-pressed={unreadOnly}
        onClick={onToggleUnread}
        className={cn(
          "box-border inline-flex h-7 cursor-pointer items-center gap-1.5 rounded-[7px] border px-[9px] text-crumb leading-[1.45] outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
          unreadOnly
            ? "border-transparent bg-state-selected font-semibold text-state-selected-foreground"
            : "border-line-strong bg-popover font-medium text-muted-foreground dark:bg-card",
        )}
      >
        {copy.unreadOnly}
      </button>
      <span className="flex-1" />
      <span className="text-xs leading-[1.45] text-meta-foreground">
        {copy.grouped}
      </span>
    </div>
  );
}

export function InboxDayHeader({ children }: { children: ReactNode }) {
  return (
    <div
      data-f="day"
      className="px-4 pt-3 pb-1 text-xs leading-[1.45] font-semibold text-meta-foreground"
    >
      {children}
    </div>
  );
}
