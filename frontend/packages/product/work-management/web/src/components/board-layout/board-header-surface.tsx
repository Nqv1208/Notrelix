import * as React from "react";
import {
  Archive,
  Building2,
  Eye,
  Link2,
  Lock,
  MoreHorizontal,
  PanelLeft,
  Search,
  Trash2,
  type LucideIcon,
} from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  TABLET_SLOT,
  TabletSlot,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
  useOptionalSidebar,
} from "@notrelix/ui-web";
import { identityMarkClass } from "../shared/item-display";
import { BOARD_BAND_BUTTON, BoardBand } from "./board-state-block";
import { fillBoardHeaderCopy, type BoardHeaderCopy } from "./board-header.copy";

/**
 * Department (Space) mark tones. Mirrors the mark tone ids of
 * `@notrelix/ui-tokens`; the route resolves the tone for the board's Space.
 */
export type BoardHeaderMarkTone =
  "blue" | "gold" | "coral" | "sage" | "crimson" | "violet";

export type BoardAccessLevel = "viewer" | "commenter" | "editor" | "manager";

/**
 * The caller's access to the board and who else can open it. Mirrors the
 * board access read (`GET /boards/{id}/access`). `peopleCount` counts every
 * person who can open the board, guests included; `guestCount` is the part of
 * it that are workspace guests. `sources` name only what the caller may see;
 * an unnamed source is shown as a count.
 */
export interface BoardHeaderAccess {
  readonly myLevel: BoardAccessLevel;
  readonly mySource: {
    readonly kind:
      | "owner"
      | "workspace-admin"
      | "workspace"
      | "space"
      | "team"
      | "explicit"
      | "guest";
    readonly grantedBy?: {
      readonly userId: string;
      readonly name: string;
    } | null;
  };
  readonly peopleCount: number;
  /**
   * False when `peopleCount` is a lower bound (sources the server could not
   * count); the stack then says "at least". Absent means complete.
   */
  readonly peopleCountComplete?: boolean;
  readonly guestCount: number;
  readonly sources: readonly {
    readonly kind: "space" | "team" | "user" | "guest";
    readonly id?: string | null;
    readonly name?: string | null;
    readonly count: number;
  }[];
}

export interface BoardHeaderSpace {
  readonly name: string;
  /** What the crumb and the access chip say ("Phòng Công nghệ"); defaults to `name`. */
  readonly label?: string;
  readonly initials: string;
  readonly markTone: BoardHeaderMarkTone;
  /** Link to the Space page; plain clicks go to `onOpenSpace` when given. */
  readonly href?: string;
}

export interface BoardHeaderPerson {
  readonly id: string;
  readonly name: string;
  readonly initials: string;
}

/**
 * Who the board is visible to, for the access chip: everyone in its Space,
 * the whole workspace, or only the people added to it.
 */
export type BoardHeaderVisibility = "space" | "workspace" | "private";

/**
 * An archived board: it reads, nobody writes. A Manager may unarchive it (`onUnarchive` present);
 * `state` tells the banner what the last attempt did.
 */
export interface BoardHeaderArchived {
  readonly onUnarchive?: () => void;
  readonly state?: "idle" | "pending" | "conflict" | "denied" | "failed";
}

/** The route-composed header data (see `BoardHeaderDataContext`). */
export interface BoardHeaderData {
  /** The board's Space; `null` for a board at the workspace level. */
  readonly space?: BoardHeaderSpace | null;
  readonly visibility?: BoardHeaderVisibility;
  /** `null`/absent while the access summary is not loaded. */
  readonly access?: BoardHeaderAccess | null;
  /** Faces for the avatar stack (the first three are shown). */
  readonly people?: readonly BoardHeaderPerson[];
  readonly onOpenSpace?: () => void;
  /** Absent: Settings stays disabled with the stated reason. */
  readonly onOpenSettings?: () => void;
  /** Absent: the access chip, the stack and Share stay disabled with the reason. */
  readonly onOpenShare?: () => void;
  /** Absent: the More options menu is not shown. */
  readonly onCopyLink?: () => void;
  /** Opens the board's deleted items; absent: no "Deleted items" entry. */
  readonly onOpenTrash?: () => void;
  /** Items in the board's trash that can still be restored; shown on the entry when above zero. */
  readonly trashCount?: number;
  /** Opens the board's archived items; absent: no entry in the menu. */
  readonly onOpenArchive?: () => void;
  /** Opens the Leave board dialog; absent: no entry in the menu. */
  readonly onLeave?: () => void;
  /** Opens the Duplicate board dialog (a Manager); absent: no entry in the menu. */
  readonly onDuplicate?: () => void;
  /** Opens the Import items dialog (someone who may create items); absent: no entry in the menu. */
  readonly onImport?: () => void;
  /** Opens the Export dialog (anyone who sees the board, not a guest); absent: no entry in the menu. */
  readonly onExport?: () => void;
  /** Present when the board is archived: the read-only banner with Unarchive for a Manager. */
  readonly archived?: BoardHeaderArchived;
}

/** What the tablet band's 56 px header needs beyond the data every width has (`Final-T-*`). */
export interface BoardHeaderTablet {
  /** The 44 px segmented view switcher. */
  readonly tabs: React.ReactNode;
  /** Opens the workspace command palette ("Tìm hoặc chạy lệnh"). */
  readonly onSearch: () => void;
}

export interface BoardHeaderSurfaceProps extends BoardHeaderData {
  readonly title: string;
  readonly copy: BoardHeaderCopy;
  /** Present in the tablet band: the one-row header replaces the title row and the tab row. */
  readonly tablet?: BoardHeaderTablet;
}

/** The 44 px icon button of the tablet header and toolbar. */
export const TABLET_ICON_BUTTON =
  "grid size-11 shrink-0 cursor-pointer place-items-center rounded-[10px] border-0 bg-transparent p-0 text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";

/** The 44 px outline button of the tablet header (Chia sẻ, search). */
const TABLET_OUTLINE_BUTTON =
  "flex h-11 shrink-0 cursor-pointer items-center rounded-[10px] border border-line-strong bg-popover text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";

/** The access chip of the tablet toolbar ("Phòng Công nghệ · có 1 khách"): a 44 px outline chip that opens Share. */
export function TabletAccessChip({
  access,
  visibility,
  space,
  copy,
  onOpenShare,
}: {
  access: BoardHeaderAccess;
  visibility: BoardHeaderVisibility;
  space: BoardHeaderSpace | null | undefined;
  copy: BoardHeaderCopy;
  onOpenShare?: (() => void) | undefined;
}) {
  const ChipIcon: LucideIcon = visibility === "workspace" ? Building2 : Lock;
  return (
    <button
      type="button"
      data-slot="tablet-access-chip"
      onClick={onOpenShare}
      aria-disabled={onOpenShare ? undefined : true}
      className="inline-flex h-11 max-w-full shrink-0 cursor-pointer items-center gap-[7px] rounded-[10px] border border-line-strong bg-popover px-3.5 text-[14px] font-medium text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default"
    >
      <ChipIcon aria-hidden="true" className="size-3.75 shrink-0" />
      <span className="truncate">
        {accessChipLabel(visibility, space, access.guestCount, copy)}
      </span>
    </button>
  );
}

/** The avatar stack of the tablet toolbar (44 px, opens Share). */
export function TabletAvatarStack({
  access,
  people,
  copy,
  onOpenShare,
}: {
  access: BoardHeaderAccess;
  people: readonly BoardHeaderPerson[];
  copy: BoardHeaderCopy;
  onOpenShare?: (() => void) | undefined;
}) {
  const faces = people.slice(0, VISIBLE_FACES);
  const overflow = Math.max(0, access.peopleCount - faces.length);
  return (
    <button
      type="button"
      data-slot="tablet-avatar-stack"
      onClick={onOpenShare}
      aria-disabled={onOpenShare ? undefined : true}
      aria-label={boardAccessSentence(access, copy)}
      className="flex h-11 shrink-0 cursor-pointer items-center rounded-[10px] border-0 bg-transparent pr-2 pl-3.5 outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default"
    >
      {faces.map((person) => (
        <span
          key={person.id}
          aria-hidden="true"
          className={cn(
            "-ml-1.5 grid size-7 place-items-center rounded-full text-avatar-md ring-2 ring-background",
            identityMarkClass(person.id),
          )}
        >
          {person.initials}
        </span>
      ))}
      {overflow > 0 ? (
        <span
          aria-hidden="true"
          className="-ml-1.5 grid size-7 place-items-center rounded-full bg-state-hover-strong text-avatar-md text-muted-foreground ring-2 ring-background"
        >
          {fillBoardHeaderCopy(copy.header.moreAvatars, { count: overflow })}
        </span>
      ) : null}
    </button>
  );
}

/** Literal strings so Tailwind generates them. Never status colours. */
const MARK_TONE_CLASS: Readonly<Record<BoardHeaderMarkTone, string>> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};

const VISIBLE_FACES = 3;

/** The mockup's Settings glyph: a hub with eight rays. */
function SettingsGlyph() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <circle cx="12" cy="12" r="3" />
      <path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1 7 17M17 7l2.1-2.1" />
    </svg>
  );
}

/** Settings and Share: 34 px outline buttons of the mockup header. */
const HEADER_BUTTON_CLASS =
  "h-8.5 gap-1.5 rounded-[7px] border-line-strong bg-popover px-3 text-compact font-medium text-foreground shadow-none hover:bg-state-hover-strong";

function countForm(one: string, other: string, count: number): string {
  return count === 1 ? one : other;
}

function joinList(parts: readonly string[], copy: BoardHeaderCopy): string {
  const { listSeparator, listPairSeparator, listLastSeparator } = copy.header;
  if (parts.length <= 1) return parts[0] ?? "";
  if (parts.length === 2) return `${parts[0]}${listPairSeparator}${parts[1]}`;
  return `${parts.slice(0, -1).join(listSeparator)}${listLastSeparator}${parts[parts.length - 1]}`;
}

/**
 * The avatar stack's accessible name: the total and each source, e.g.
 * "32 người vào được board: 24 người Phòng Công nghệ, …, Đỗ Khánh Linh và 1
 * khách". A source the caller may not see by name is stated as a count.
 * When the count is not complete the total reads "Ít nhất 32 người…".
 */
export function boardAccessSentence(
  access: BoardHeaderAccess,
  copy: BoardHeaderCopy,
): string {
  const h = copy.header;
  const parts = access.sources.map((source) => {
    const { count } = source;
    switch (source.kind) {
      case "space":
      case "team":
        return source.name
          ? fillBoardHeaderCopy(
              countForm(h.sourceGroup_one, h.sourceGroup_other, count),
              { count, name: source.name },
            )
          : fillBoardHeaderCopy(
              countForm(
                h.sourceGroupUnnamed_one,
                h.sourceGroupUnnamed_other,
                count,
              ),
              { count },
            );
      case "user":
        return (
          source.name ??
          fillBoardHeaderCopy(
            countForm(
              h.sourceUserUnnamed_one,
              h.sourceUserUnnamed_other,
              count,
            ),
            { count },
          )
        );
      case "guest":
        return fillBoardHeaderCopy(
          countForm(h.sourceGuest_one, h.sourceGuest_other, count),
          { count },
        );
    }
  });
  const total = access.peopleCount;
  const atLeast = access.peopleCountComplete === false;
  if (parts.length === 0) {
    return fillBoardHeaderCopy(
      atLeast
        ? countForm(h.peopleAtLeast_one, h.peopleAtLeast_other, total)
        : countForm(h.people_one, h.people_other, total),
      { count: total },
    );
  }
  return fillBoardHeaderCopy(
    atLeast
      ? countForm(
          h.peopleAtLeastWithSources_one,
          h.peopleAtLeastWithSources_other,
          total,
        )
      : countForm(h.peopleWithSources_one, h.peopleWithSources_other, total),
    { count: total, sources: joinList(parts, copy) },
  );
}

function accessChipLabel(
  visibility: BoardHeaderVisibility,
  space: BoardHeaderSpace | null | undefined,
  guestCount: number,
  copy: BoardHeaderCopy,
): string {
  const h = copy.header;
  const scope =
    visibility === "space" && space
      ? fillBoardHeaderCopy(h.scope.space, { space: space.label ?? space.name })
      : visibility === "private"
        ? h.scope.private
        : h.scope.workspace;
  if (guestCount <= 0) return scope;
  return fillBoardHeaderCopy(
    countForm(h.guests_one, h.guests_other, guestCount),
    { scope, count: guestCount },
  );
}

function bannerText(
  access: BoardHeaderAccess,
  copy: BoardHeaderCopy,
): string | null {
  const { banner } = copy.header;
  const level =
    access.myLevel === "commenter"
      ? banner.commenter
      : access.myLevel === "viewer"
        ? banner.viewer
        : null;
  if (!level) return null;
  const grantedBy = access.mySource.grantedBy;
  if (access.mySource.kind === "explicit" && grantedBy) {
    return fillBoardHeaderCopy(banner.grantedBy, {
      level,
      name: grantedBy.name,
    });
  }
  return level;
}

/** True for a plain primary click that the host router should handle. */
function isPlainClick(event: React.MouseEvent): boolean {
  return (
    event.button === 0 &&
    !event.metaKey &&
    !event.ctrlKey &&
    !event.shiftKey &&
    !event.altKey
  );
}

/** Shows the reason an unavailable control is disabled on hover and focus. */
function ReasonHint({
  reason,
  children,
}: {
  reason: string | null;
  children: React.ReactElement;
}) {
  if (!reason) return children;
  return (
    <Tooltip>
      <TooltipTrigger asChild>{children}</TooltipTrigger>
      <TooltipContent>{reason}</TooltipContent>
    </Tooltip>
  );
}

function SpaceCrumb({
  space,
  onOpenSpace,
}: {
  space: BoardHeaderSpace;
  onOpenSpace: (() => void) | undefined;
}) {
  const className =
    "self-start truncate rounded-sm text-crumb text-meta-foreground outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring";
  if (space.href) {
    return (
      <a
        href={space.href}
        className={className}
        onClick={(event) => {
          if (!onOpenSpace || !isPlainClick(event)) return;
          event.preventDefault();
          onOpenSpace();
        }}
      >
        {space.label ?? space.name}
      </a>
    );
  }
  if (onOpenSpace) {
    return (
      <button type="button" className={className} onClick={onOpenSpace}>
        {space.label ?? space.name}
      </button>
    );
  }
  return (
    <span className="truncate text-crumb text-meta-foreground">
      {space.label ?? space.name}
    </span>
  );
}

/** The Space's 28 px mark in a 44 px target: the tablet header's crumb (it opens the Space). */
function TabletSpaceMark({
  space,
  onOpenSpace,
}: {
  space: BoardHeaderSpace;
  onOpenSpace: (() => void) | undefined;
}) {
  const label = space.label ?? space.name;
  const mark = (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-7 place-items-center rounded-[8px] text-[10.5px] font-bold",
        MARK_TONE_CLASS[space.markTone],
      )}
    >
      {space.initials}
    </span>
  );
  if (space.href) {
    return (
      <a
        href={space.href}
        aria-label={label}
        className={TABLET_ICON_BUTTON}
        onClick={(event) => {
          if (!onOpenSpace || !isPlainClick(event)) return;
          event.preventDefault();
          onOpenSpace();
        }}
      >
        {mark}
      </a>
    );
  }
  return (
    <button
      type="button"
      aria-label={label}
      className={TABLET_ICON_BUTTON}
      onClick={onOpenSpace}
    >
      {mark}
    </button>
  );
}

/**
 * The board's title row: department mark, Space crumb, title, access chip,
 * avatar stack, Settings, Share and More options, then the read-only banner
 * for a commenter or viewer. Settings and Share are shown to managers only;
 * a link whose page does not exist yet stays focusable, disabled, with the
 * reason as its description.
 */
export function BoardHeaderSurface({
  title,
  copy,
  space,
  visibility,
  access,
  people = [],
  onOpenSpace,
  onOpenSettings,
  onOpenShare,
  onCopyLink,
  onLeave,
  onDuplicate,
  onImport,
  onExport,
  onOpenTrash,
  trashCount = 0,
  onOpenArchive,
  archived,
  tablet,
}: BoardHeaderSurfaceProps) {
  const sidebar = useOptionalSidebar();
  const shareReasonId = React.useId();
  const settingsReasonId = React.useId();
  const h = copy.header;
  const canManage = access?.myLevel === "manager";
  const canEditItems = canManage || access?.myLevel === "editor";
  const shareReason = onOpenShare ? null : h.shareUnavailable;
  const settingsReason = onOpenSettings ? null : h.settingsUnavailable;
  const faces = people.slice(0, VISIBLE_FACES);
  const overflow = access ? Math.max(0, access.peopleCount - faces.length) : 0;
  const banner = access ? bannerText(access, copy) : null;
  const ChipIcon: LucideIcon = visibility === "workspace" ? Building2 : Lock;
  const showsShareControl = Boolean(access);

  const hasMoreMenu = Boolean(
    onCopyLink ||
    onLeave ||
    (onDuplicate && canManage) ||
    (onImport && canEditItems) ||
    onExport ||
    canManage ||
    ((onOpenTrash || onOpenArchive) && canEditItems),
  );
  const renderMoreMenu = (trigger: React.ReactNode) => (
    <DropdownMenu>
      {trigger}
      <DropdownMenuContent align="end" className="w-60">
        {canManage ? (
          <DropdownMenuItem
            className="xl:hidden"
            disabled={Boolean(settingsReason)}
            onSelect={onOpenSettings}
          >
            <SettingsGlyph />
            {h.settings}
          </DropdownMenuItem>
        ) : null}
        {onCopyLink ? (
          <DropdownMenuItem onSelect={onCopyLink}>
            <Link2 aria-hidden="true" />
            {h.copyLink}
          </DropdownMenuItem>
        ) : null}
        {onOpenArchive && canEditItems ? (
          <DropdownMenuItem onSelect={onOpenArchive}>
            {h.archivedItems}
          </DropdownMenuItem>
        ) : null}
        {onOpenTrash && canEditItems ? (
          <DropdownMenuItem
            className="gap-2.5 rounded-[7px] px-2.5 text-[13.5px] font-medium"
            onSelect={onOpenTrash}
            aria-label={
              trashCount > 0
                ? `${h.deletedItems}, ${fillBoardHeaderCopy(h.deletedItemsCount, { count: trashCount })}`
                : undefined
            }
          >
            <Trash2 aria-hidden="true" />
            {h.deletedItems}
            {trashCount > 0 ? (
              <span
                aria-hidden="true"
                className="ml-auto grid h-[18px] min-w-[18px] place-items-center rounded-[9px] bg-state-hover-strong px-[5px] text-[11px] leading-none font-bold text-muted-foreground"
              >
                {trashCount}
              </span>
            ) : null}
          </DropdownMenuItem>
        ) : null}
        {canManage && onOpenSettings ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={onOpenSettings}>
              {h.renameBoard}
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={onOpenSettings}>
              {h.archiveBoard}
            </DropdownMenuItem>
            <DropdownMenuItem variant="destructive" onSelect={onOpenSettings}>
              {h.deleteBoard}
            </DropdownMenuItem>
          </>
        ) : null}
        {onDuplicate && canManage ? (
          <DropdownMenuItem onSelect={onDuplicate}>
            {h.duplicateBoard}
          </DropdownMenuItem>
        ) : null}
        {onImport && canEditItems ? (
          <DropdownMenuItem onSelect={onImport}>
            {h.importItems}
          </DropdownMenuItem>
        ) : null}
        {onExport ? (
          <DropdownMenuItem onSelect={onExport}>
            {h.exportItems}
          </DropdownMenuItem>
        ) : null}
        {onLeave ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={onLeave}>
              {h.leaveBoard}
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );

  return (
    <div className="flex flex-col">
      {tablet ? (
        <div
          data-slot="tablet-board-header"
          className="flex h-14 items-center gap-1.5 border-b border-line pr-3 pl-1.5"
        >
          {sidebar ? (
            <button
              type="button"
              onClick={sidebar.toggleSidebar}
              aria-expanded={sidebar.open}
              aria-label={
                sidebar.open
                  ? copy.tablet.collapseSidebar
                  : copy.tablet.expandSidebar
              }
              className={TABLET_ICON_BUTTON}
            >
              <PanelLeft aria-hidden="true" className="size-5" />
            </button>
          ) : null}
          <div className="flex min-w-0 items-center gap-0.5">
            {space ? (
              <TabletSpaceMark space={space} onOpenSpace={onOpenSpace} />
            ) : null}
            {space ? (
              <span
                aria-hidden="true"
                className="mx-0.5 text-[14.5px] text-meta-foreground"
              >
                /
              </span>
            ) : null}
            <h1 className="mr-1.5 ml-0.5 truncate text-[17px] font-[650] tracking-[-0.01em] text-foreground">
              {title}
            </h1>
          </div>
          {tablet.tabs}
          <span className="min-w-0 flex-1" />
          <button
            type="button"
            onClick={tablet.onSearch}
            aria-label={copy.tablet.search}
            aria-keyshortcuts="Meta+K Control+K"
            className={cn(
              TABLET_OUTLINE_BUTTON,
              "gap-2 pr-2.5 pl-3 text-[14px] text-meta-foreground",
            )}
          >
            <Search aria-hidden="true" className="size-5" />
            <span
              aria-hidden="true"
              className="rounded-[5px] border border-line-strong bg-state-hover px-[5px] font-mono text-[11.5px] leading-[18px] text-muted-foreground"
            >
              ⌘K
            </span>
          </button>
          <TabletSlot slot={TABLET_SLOT.headerAdd} className="contents" />
          {canManage ? (
            <ReasonHint reason={shareReason}>
              <button
                type="button"
                onClick={onOpenShare}
                aria-disabled={shareReason ? true : undefined}
                aria-describedby={shareReason ? shareReasonId : undefined}
                className={cn(
                  TABLET_OUTLINE_BUTTON,
                  "justify-center px-3.5 text-[14.5px] font-medium aria-disabled:cursor-default aria-disabled:text-muted-foreground",
                )}
              >
                {h.share}
              </button>
            </ReasonHint>
          ) : null}
          {hasMoreMenu
            ? renderMoreMenu(
                <DropdownMenuTrigger asChild>
                  <button
                    type="button"
                    aria-label={h.moreOptions}
                    className={TABLET_ICON_BUTTON}
                  >
                    <MoreHorizontal aria-hidden="true" className="size-5" />
                  </button>
                </DropdownMenuTrigger>,
              )
            : null}
        </div>
      ) : (
        <div className="flex flex-wrap items-center gap-3 px-4 pt-4 sm:px-6">
          {space ? (
            <span
              aria-hidden="true"
              className={cn(
                "flex size-8.5 shrink-0 items-center justify-center rounded-[9px] text-metadata font-bold",
                MARK_TONE_CLASS[space.markTone],
              )}
            >
              {space.initials}
            </span>
          ) : null}
          <div className="flex min-w-0 flex-col">
            {space ? (
              <SpaceCrumb space={space} onOpenSpace={onOpenSpace} />
            ) : null}
            <h1 className="truncate text-board-title text-foreground">
              {title}
            </h1>
          </div>

          {access && visibility ? (
            <ReasonHint reason={shareReason}>
              <button
                type="button"
                onClick={onOpenShare}
                aria-disabled={shareReason ? true : undefined}
                aria-describedby={shareReason ? shareReasonId : undefined}
                className="inline-flex h-6.5 max-w-full items-center gap-[5px] rounded-sm border border-line bg-popover px-2 text-metadata font-medium tracking-normal text-muted-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default"
              >
                <ChipIcon aria-hidden="true" className="size-3.25 shrink-0" />
                <span className="truncate">
                  {accessChipLabel(visibility, space, access.guestCount, copy)}
                </span>
              </button>
            </ReasonHint>
          ) : null}

          {access ? (
            <ReasonHint reason={shareReason}>
              <button
                type="button"
                onClick={onOpenShare}
                aria-disabled={shareReason ? true : undefined}
                aria-describedby={shareReason ? shareReasonId : undefined}
                aria-label={boardAccessSentence(access, copy)}
                className="ml-auto flex rounded-full pl-1.5 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default"
              >
                {faces.map((person) => (
                  <span
                    key={person.id}
                    aria-hidden="true"
                    className={cn(
                      "-ml-1.5 grid size-6.5 place-items-center rounded-full text-avatar-md ring-2 ring-background",
                      identityMarkClass(person.id),
                    )}
                  >
                    {person.initials}
                  </span>
                ))}
                {overflow > 0 ? (
                  <span
                    aria-hidden="true"
                    className="-ml-1.5 grid size-6.5 place-items-center rounded-full bg-state-hover-strong text-avatar-md text-muted-foreground ring-2 ring-background"
                  >
                    {fillBoardHeaderCopy(h.moreAvatars, { count: overflow })}
                  </span>
                ) : null}
              </button>
            </ReasonHint>
          ) : null}

          {canManage ? (
            <>
              <ReasonHint reason={settingsReason}>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={onOpenSettings}
                  aria-disabled={settingsReason ? true : undefined}
                  aria-describedby={
                    settingsReason ? settingsReasonId : undefined
                  }
                  className={cn(
                    HEADER_BUTTON_CLASS,
                    "hidden xl:inline-flex",
                    "aria-disabled:cursor-default aria-disabled:text-muted-foreground",
                  )}
                >
                  <SettingsGlyph />
                  {h.settings}
                </Button>
              </ReasonHint>
              <ReasonHint reason={shareReason}>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={onOpenShare}
                  aria-disabled={shareReason ? true : undefined}
                  aria-describedby={shareReason ? shareReasonId : undefined}
                  className={cn(
                    HEADER_BUTTON_CLASS,
                    "aria-disabled:cursor-default aria-disabled:text-muted-foreground",
                  )}
                >
                  {h.share}
                </Button>
              </ReasonHint>
            </>
          ) : null}

          {hasMoreMenu
            ? renderMoreMenu(
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    aria-label={h.moreOptions}
                    className={cn(
                      !access ? "ml-auto" : undefined,
                      "rounded-[7px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
                    )}
                  >
                    <MoreHorizontal aria-hidden="true" />
                  </Button>
                </DropdownMenuTrigger>,
              )
            : null}

          {showsShareControl && shareReason ? (
            <span id={shareReasonId} hidden>
              {shareReason}
            </span>
          ) : null}
          {canManage && settingsReason ? (
            <span id={settingsReasonId} hidden>
              {settingsReason}
            </span>
          ) : null}
        </div>
      )}

      {archived ? (
        <div className="mx-4 mt-3 sm:mx-6" data-slot="board-archived">
          <BoardBand
            tone="info"
            icon={<Archive />}
            title={h.archived.title}
            description={
              archived.onUnarchive
                ? h.archived.bannerManager
                : h.archived.banner
            }
            action={
              archived.onUnarchive ? (
                <button
                  type="button"
                  className={BOARD_BAND_BUTTON}
                  disabled={archived.state === "pending"}
                  onClick={archived.onUnarchive}
                >
                  {archived.state === "pending"
                    ? h.archived.unarchiving
                    : h.archived.unarchive}
                </button>
              ) : undefined
            }
          />
          {archived.state &&
          archived.state !== "idle" &&
          archived.state !== "pending" ? (
            <p role="alert" className="mt-1.5 text-crumb text-danger-text">
              {h.archived[archived.state]}
            </p>
          ) : null}
        </div>
      ) : null}

      {banner ? (
        <div
          role="note"
          className="mx-4 mt-3 flex items-center gap-2.5 rounded-[9px] bg-brand-subtle px-3 py-[9px] text-compact leading-[1.45] font-medium text-brand-subtle-foreground sm:mx-6"
        >
          <Eye aria-hidden="true" className="size-4 shrink-0" />
          {banner}
        </div>
      ) : null}
    </div>
  );
}
