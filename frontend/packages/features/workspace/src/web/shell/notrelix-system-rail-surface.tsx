import * as React from "react";
import { Inbox, Pin, Plus, Search, SlidersHorizontal } from "lucide-react";
import { cn, NotrelixLogoMark } from "@notrelix/ui-web";

import type { MarkToneId } from "../../core/model/navigation";
import type { NotrelixSystemRailCopy } from "./notrelix-system-rail.copy";
import { IdentityMark } from "./shell-link";
import { formatCopy, MARK_TONE_CLASS, type ShellUser } from "./shell-models";

/**
 * hidden: not rendered. overlay: over the layout with a scrim (`nav=hien-de`).
 * pinned: in flow, pushes the layout (`nav=hien-day`).
 */
export type SystemRailPresentation = "hidden" | "overlay" | "pinned";

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
  readonly onOpenGlobalSearch: () => void;
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
      className="absolute top-0.5 right-2 flex h-4 min-w-4 items-center justify-center rounded-full bg-rail-badge px-1 text-caption leading-none font-bold text-rail-badge-foreground tabular-nums"
    >
      {count > BADGE_CAP ? `${BADGE_CAP}+` : count}
    </span>
  );
}

/** One 64 px rail entry: icon or mark above a short label. */
const RAIL_ITEM_CLASS =
  "relative flex w-16 flex-col items-center gap-1 rounded-lg px-0.5 pt-2 pb-1.5 text-caption font-medium text-rail-foreground outline-none hover:bg-rail-hover hover:text-rail-active-foreground focus-visible:focus-ring disabled:pointer-events-none disabled:opacity-60 [&_svg]:size-5";

function RailLabel({ children }: { readonly children: React.ReactNode }) {
  return <span className="block max-w-full truncate">{children}</span>;
}

/**
 * The Notrelix system rail (Final-Space `nav=hien-de` / `nav=hien-day`):
 * the member's workspaces, account-level inbox and search, and settings.
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
  const navRef = React.useRef<HTMLElement>(null);
  const reasonId = React.useId();

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
        "flex w-19 shrink-0 flex-col items-center gap-1 overflow-y-auto bg-rail py-3 text-rail-foreground outline-none",
        overlay
          ? "fixed inset-y-0 left-0 z-sticky shadow-modal motion-safe:animate-in motion-safe:slide-in-from-left motion-safe:duration-180 motion-safe:ease-out"
          : "relative h-full",
      )}
    >
      <NotrelixLogoMark
        role="img"
        aria-label={copy.logo}
        className="mb-3 h-7 w-9"
      />

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
              RAIL_ITEM_CLASS,
              workspace.current && "text-rail-active-foreground",
            )}
          >
            <span
              aria-hidden="true"
              className={cn(
                "flex size-10 items-center justify-center rounded-xl text-label font-bold",
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
        className={RAIL_ITEM_CLASS}
      >
        <span
          aria-hidden="true"
          className="flex size-10 items-center justify-center rounded-xl border-2 border-dashed border-rail-separator"
        >
          <Plus />
        </span>
        <RailLabel>{copy.create.label}</RailLabel>
      </button>

      <span aria-hidden="true" className="my-1.5 h-px w-9 bg-rail-separator" />

      <button
        type="button"
        title={copy.allInbox.title}
        aria-label={
          allInboxAvailable
            ? withUnread(copy.allInbox.label, allInboxUnread, copy)
            : copy.allInbox.label
        }
        aria-describedby={allInboxAvailable ? undefined : reasonId}
        disabled={!allInboxAvailable}
        onClick={onOpenAllInbox}
        className={RAIL_ITEM_CLASS}
      >
        <Inbox aria-hidden="true" />
        <RailLabel>{copy.allInbox.label}</RailLabel>
        {allInboxAvailable ? <UnreadBadge count={allInboxUnread} /> : null}
      </button>
      {allInboxAvailable ? null : (
        <span
          id={reasonId}
          className="-mt-1 w-16 text-center text-caption text-rail-foreground"
        >
          {copy.allInbox.unavailable}
        </span>
      )}

      <button
        type="button"
        title={copy.search.title}
        onClick={onOpenGlobalSearch}
        className={RAIL_ITEM_CLASS}
      >
        <Search aria-hidden="true" />
        <RailLabel>{copy.search.label}</RailLabel>
      </button>

      <span aria-hidden="true" className="flex-1" />

      <button
        type="button"
        title={copy.settings.title}
        onClick={onOpenAccount}
        className={RAIL_ITEM_CLASS}
      >
        <SlidersHorizontal aria-hidden="true" />
        <RailLabel>{copy.settings.label}</RailLabel>
      </button>

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
          />
        </span>
      ) : null}
    </nav>
  );

  if (!overlay) return nav;

  return (
    <>
      <div
        aria-hidden="true"
        data-slot="system-rail-scrim"
        onClick={onClose}
        className="fixed inset-0 z-sticky bg-black/15 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-180 dark:bg-black/40"
      />
      {nav}
    </>
  );
}
