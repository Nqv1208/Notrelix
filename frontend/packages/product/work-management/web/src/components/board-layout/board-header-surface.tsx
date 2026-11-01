import * as React from "react";
import {
  Archive,
  Building2,
  Eye,
  Link2,
  Lock,
  MoreHorizontal,
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
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@notrelix/ui-web";
import { identityMarkClass } from "../shared/item-display";
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

export interface BoardHeaderSurfaceProps extends BoardHeaderData {
  readonly title: string;
  readonly copy: BoardHeaderCopy;
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
  onOpenArchive,
  archived,
}: BoardHeaderSurfaceProps) {
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

  return (
    <div className="flex flex-col">
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
          <h1 className="truncate text-board-title text-foreground">{title}</h1>
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
                aria-describedby={settingsReason ? settingsReasonId : undefined}
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

        {onCopyLink ||
        onLeave ||
        (onDuplicate && canManage) ||
        (onImport && canEditItems) ||
        onExport ||
        canManage ||
        ((onOpenTrash || onOpenArchive) && canEditItems) ? (
          <DropdownMenu>
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
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
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
                <DropdownMenuItem onSelect={onOpenTrash}>
                  {h.deletedItems}
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
                  <DropdownMenuItem
                    variant="destructive"
                    onSelect={onOpenSettings}
                  >
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
        ) : null}

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

      {archived ? (
        <div className="mx-4 mt-3 flex flex-wrap items-center gap-2.5 rounded-[9px] border border-line bg-state-hover px-3 py-[9px] text-compact leading-[1.45] font-medium text-foreground sm:mx-6">
          <Archive aria-hidden="true" className="size-4 shrink-0" />
          <span className="min-w-0 flex-1">
            {archived.onUnarchive
              ? h.archived.bannerManager
              : h.archived.banner}
          </span>
          {archived.onUnarchive ? (
            <Button
              variant="outline"
              size="sm"
              disabled={archived.state === "pending"}
              onClick={archived.onUnarchive}
            >
              {archived.state === "pending"
                ? h.archived.unarchiving
                : h.archived.unarchive}
            </Button>
          ) : null}
          {archived.state &&
          archived.state !== "idle" &&
          archived.state !== "pending" ? (
            <span
              role="alert"
              className="basis-full text-crumb text-danger-text"
            >
              {h.archived[archived.state]}
            </span>
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
