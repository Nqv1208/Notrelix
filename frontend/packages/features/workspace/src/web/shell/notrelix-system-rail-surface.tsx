import * as React from "react";
import { Inbox, Pin, Plus, Search, Settings2 } from "lucide-react";
import { cn, NotrelixLogoMark } from "@notrelix/ui-web";

import type { MarkToneId } from "../../core/model/navigation";
import type { NotrelixSystemRailCopy } from "./notrelix-system-rail.copy";
import { IdentityMark } from "./shell-link";
import { formatCopy, MARK_TONE_CLASS, type ShellUser } from "./shell-models";

/**
 * hidden: not rendered. overlay: over the layout with a scrim (`nav=hien-de`).
 * pinned: in flow, pushes the layout (`nav=hien-day`). drawer: narrow
 * screens, a row of the workspaces (and Add) at the top of the navigation
 * drawer, which owns focus and dismissal.
 */
export type SystemRailPresentation = "hidden" | "overlay" | "pinned" | "drawer";

export interface SystemRailWorkspace {
  readonly id: string;
  readonly name: string;
  /** Label under the mark; the full `name` stays the accessible name. */
  readonly shortName: string;
  readonly initials: string;
  /** Mark tone (WX-DEC-014); identity only, never status. */
  readonly tone: MarkToneId;
  /** The member is a workspace guest here. */
  readonly isGuest: boolean;
  /** Unread notifications; `null` when unknown (no badge, never "0"). */
  readonly unread: number | null;
  readonly current: boolean;
}

export interface NotrelixSystemRailSurfaceProps {
  readonly copy: NotrelixSystemRailCopy;
  readonly presentation: SystemRailPresentation;
  readonly workspaces: readonly SystemRailWorkspace[];
  readonly user: ShellUser | null;
  /** False until an all-workspace inbox exists; the entry is then disabled. */
  readonly allInboxAvailable: boolean;
  readonly allInboxUnread: number | null;
  readonly onSwitchWorkspace: (workspaceId: string) => void;
  readonly onCreateWorkspace: () => void;
  readonly onOpenAllInbox: () => void;
  /**
   * Cross-workspace search. Omitted until a producer exists: the entry is
   * then disabled with the visible reason (never a current-workspace search
   * under the "all workspaces" title).
   */
  readonly onOpenGlobalSearch?: () => void;
  readonly onOpenAccount: () => void;
  readonly onTogglePinned: () => void;
  readonly onClose: () => void;
}

const BADGE_CAP = 99;

function hasUnread(count: number | null): count is number {
  return count !== null && count > 0;
}

function withUnread(
  label: string,
  count: number | null,
  copy: NotrelixSystemRailCopy,
): string {
  return hasUnread(count)
    ? `${label}, ${formatCopy(copy.unread, { count })}`
    : label;
}

function UnreadBadge({ count }: { readonly count: number | null }) {
  if (!hasUnread(count)) return null;
  return (
    <span
      aria-hidden="true"
      data-slot="system-rail-badge"
      className="absolute top-0.5 right-2 flex h-4 min-w-4 items-center justify-center rounded-full bg-rail-badge px-[3px] text-mark-sm text-rail-badge-foreground tabular-nums"
    >
      {count > BADGE_CAP ? `${BADGE_CAP}+` : count}
    </span>
  );
}

/**
 * One 64 px rail entry (Final-Space `.app`): a 20 px icon or 40 px mark above
 * a 10.5 px label, 7 px above and 6 px below, 3 px between.
 */
const RAIL_ITEM_CLASS =
  "relative flex w-16 flex-col items-center gap-0.75 rounded-[10px] pt-1.75 pb-1.5 text-rail-label text-rail-foreground outline-none focus-visible:focus-ring [&_svg]:size-5 [&_svg]:stroke-[1.8]";
const RAIL_ACTION_CLASS = `${RAIL_ITEM_CLASS} hover:bg-rail-hover hover:text-rail-active-foreground`;

function RailLabel({ children }: { readonly children: React.ReactNode }) {
  return <span className="block max-w-full truncate">{children}</span>;
}

/**
 * The Notrelix system rail (Final-Space `nav=hien-de` / `nav=hien-day`):
 * the member's workspaces, account-level inbox and search, and settings.
 * Entries without a producer yet (all-workspace inbox and search) keep the
 * active look of the design but do nothing; their reason is the tooltip and
 * the accessible description.
 * It always renders on the dark rail roles (`--rail-*`) in every appearance.
 * Overlay sits at `z-sticky` (below dialogs) with a scrim; Escape, the
 * scrim, focus leaving the rail and the current workspace call `onClose`,
 * and focus moves to the current workspace on open.
 */
export function NotrelixSystemRailSurface({
  copy,
  presentation,
  workspaces,
  user,
  allInboxAvailable,
  allInboxUnread,
  onSwitchWorkspace,
  onCreateWorkspace,
  onOpenAllInbox,
  onOpenGlobalSearch,
  onOpenAccount,
  onTogglePinned,
  onClose,
}: NotrelixSystemRailSurfaceProps) {
  const overlay = presentation === "overlay";
  const drawer = presentation === "drawer";
  const navRef = React.useRef<HTMLElement>(null);
  const reasonId = React.useId();
  const searchReasonId = React.useId();
  const globalSearchAvailable = onOpenGlobalSearch !== undefined;

  React.useEffect(() => {
    if (!overlay) return;
    const target =
      navRef.current?.querySelector<HTMLElement>('[aria-current="page"]') ??
      navRef.current?.querySelector<HTMLElement>("button:not([disabled])");
    target?.focus();
  }, [overlay]);

  if (presentation === "hidden") return null;

  const nav = (
    <nav
      ref={navRef}
      aria-label={copy.label}
      data-presentation={presentation}
      // Overlay only: clicks on the rail background keep focus on the nav,
      // so only focus leaving the rail (Tab past the last entry) closes it.
      tabIndex={overlay ? -1 : undefined}
      onKeyDown={(event) => {
        if (overlay && event.key === "Escape") {
          event.stopPropagation();
          onClose();
        }
      }}
      onBlur={(event) => {
        if (!overlay) return;
        const next = event.relatedTarget;
        if (next instanceof Node && navRef.current?.contains(next)) return;
        onClose();
      }}
      className={cn(
        // The app focus ring is < 3:1 on the dark rail in light and high
        // contrast: scope the rail's own ring for every focus-ring below.
        "flex gap-1 bg-rail text-rail-foreground outline-none [--focus-ring:var(--rail-focus-ring)]",
        drawer
          ? "w-full shrink-0 flex-row flex-wrap items-start p-2"
          : "w-19 shrink-0 flex-col items-center overflow-y-auto py-3",
        overlay &&
          "fixed inset-y-0 left-0 z-sticky shadow-modal motion-safe:animate-in motion-safe:slide-in-from-left motion-safe:duration-180 motion-safe:ease-out",
        presentation === "pinned" && "relative h-full",
      )}
    >
      {drawer ? null : (
        <NotrelixLogoMark
          role="img"
          aria-label={copy.logo}
          className="mb-3 size-8.5"
        />
      )}

      {workspaces.map((workspace) => {
        const name = workspace.isGuest
          ? `${workspace.name} ${copy.guest}`
          : workspace.name;
        return (
          <button
            key={workspace.id}
            type="button"
            aria-label={withUnread(name, workspace.unread, copy)}
            aria-current={workspace.current ? "page" : undefined}
            title={name}
            onClick={() => {
              if (!workspace.current) onSwitchWorkspace(workspace.id);
              else if (overlay) onClose();
            }}
            className={cn(
              RAIL_ACTION_CLASS,
              workspace.current && "text-rail-active-foreground",
            )}
          >
            <span
              aria-hidden="true"
              className={cn(
                "flex size-10 items-center justify-center rounded-[11px] text-mark-lg",
                MARK_TONE_CLASS[workspace.tone],
                workspace.current &&
                  "ring-2 ring-rail-active-foreground ring-offset-2 ring-offset-rail",
              )}
            >
              {workspace.initials}
            </span>
            <RailLabel>{workspace.shortName}</RailLabel>
            <UnreadBadge count={workspace.unread} />
          </button>
        );
      })}

      <button
        type="button"
        title={copy.create.title}
        onClick={onCreateWorkspace}
        className={RAIL_ACTION_CLASS}
      >
        <span
          aria-hidden="true"
          className="flex h-8.5 w-10 items-center justify-center rounded-[9px] border-[1.5px] border-dashed border-rail-outline text-rail-foreground"
        >
          <Plus />
        </span>
        <RailLabel>{copy.create.label}</RailLabel>
      </button>

      {drawer ? null : (
        <>
          <span
            aria-hidden="true"
            className="my-1.5 h-px w-9 bg-rail-separator"
          />

          {/* No producer yet: the entry keeps the design's look, does nothing
              and explains why through its tooltip and description. */}
          <button
            type="button"
            title={
              allInboxAvailable
                ? copy.allInbox.title
                : `${copy.allInbox.title}. ${copy.allInbox.unavailable}`
            }
            aria-label={
              allInboxAvailable
                ? withUnread(copy.allInbox.label, allInboxUnread, copy)
                : copy.allInbox.label
            }
            aria-disabled={allInboxAvailable ? undefined : true}
            aria-describedby={allInboxAvailable ? undefined : reasonId}
            onClick={allInboxAvailable ? onOpenAllInbox : undefined}
            className={allInboxAvailable ? RAIL_ACTION_CLASS : RAIL_ITEM_CLASS}
          >
            <Inbox aria-hidden="true" />
            <RailLabel>{copy.allInbox.label}</RailLabel>
            {allInboxAvailable ? <UnreadBadge count={allInboxUnread} /> : null}
            {allInboxAvailable ? null : (
              <span id={reasonId} className="sr-only">
                {copy.allInbox.unavailable}
              </span>
            )}
          </button>

          <button
            type="button"
            title={
              globalSearchAvailable
                ? copy.search.title
                : `${copy.search.title}. ${copy.search.unavailable}`
            }
            aria-label={copy.search.label}
            aria-disabled={globalSearchAvailable ? undefined : true}
            aria-describedby={
              globalSearchAvailable ? undefined : searchReasonId
            }
            onClick={onOpenGlobalSearch}
            className={
              globalSearchAvailable ? RAIL_ACTION_CLASS : RAIL_ITEM_CLASS
            }
          >
            <Search aria-hidden="true" />
            <RailLabel>{copy.search.label}</RailLabel>
            {globalSearchAvailable ? null : (
              <span id={searchReasonId} className="sr-only">
                {copy.search.unavailable}
              </span>
            )}
          </button>

          {/* The pin control sits in the free space above Settings so every
              entry keeps the design's position. */}
          <div className="flex flex-1 flex-col justify-end">
            <button
              type="button"
              aria-label={copy.pin.label}
              aria-pressed={presentation === "pinned"}
              title={copy.pin.label}
              onClick={onTogglePinned}
              className="flex size-8 items-center justify-center rounded-lg text-rail-foreground outline-none hover:bg-rail-hover hover:text-rail-active-foreground focus-visible:focus-ring aria-pressed:bg-rail-hover aria-pressed:text-rail-active-foreground [&_svg]:size-4"
            >
              <Pin aria-hidden="true" />
            </button>
          </div>

          <button
            type="button"
            title={copy.settings.title}
            onClick={onOpenAccount}
            className={RAIL_ACTION_CLASS}
          >
            <Settings2 aria-hidden="true" />
            <RailLabel>{copy.settings.label}</RailLabel>
          </button>

          {user ? (
            <span
              role="img"
              aria-label={formatCopy(copy.user, { name: user.name })}
              title={user.name}
              className="mt-1.5"
            >
              <IdentityMark
                name={user.name}
                tone={user.identity}
                imageUrl={user.avatarUrl}
                shape="round"
                className="text-rail-label font-bold"
              />
            </span>
          ) : null}
        </>
      )}
    </nav>
  );

  // One tree shape for every presentation (scrim slot first, nav second):
  // switching overlay ↔ pinned keeps the nav mounted and its focus.
  return (
    <>
      {overlay ? (
        <div
          aria-hidden="true"
          data-slot="system-rail-scrim"
          onClick={onClose}
          className="fixed inset-0 z-sticky bg-rail-scrim motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-180"
        />
      ) : null}
      {nav}
    </>
  );
}
