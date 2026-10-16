import { useEffect, useRef, type KeyboardEvent, type ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import {
  initialsFor,
  type MarkToneId,
  type SpaceTab,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  MARK_TONE_CLASS,
  type ShellNavigation,
} from "../shell/shell-models";
import { ShellLink } from "../shell/shell-link";
import type { SpaceAvatarView, SpaceHeadPeople } from "./space-page-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/**
 * Parts of the department (Space) page the tabs share: glyphs, the avatar
 * stack, buttons, the head, the tab strip and the Create menu (Final-Space).
 */

export const FOCUS =
  "outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";

/** Final-Space `.btn`: 32 px, 1 px strong border, 13 px medium. */
export const BTN =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong aria-disabled:cursor-default aria-disabled:hover:bg-popover dark:bg-card dark:aria-disabled:hover:bg-card";
export const BTN_PRIMARY =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground hover:bg-primary/90 aria-disabled:cursor-default";
export const BTN_SM =
  "inline-flex h-7 items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong aria-disabled:cursor-default aria-disabled:hover:bg-popover dark:bg-card dark:aria-disabled:hover:bg-card";
export const BTN_SM_PRIMARY =
  "inline-flex h-7 items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-2.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground hover:bg-primary/90 aria-disabled:cursor-default";

/** The glyphs of the mockup, drawn at its path data. */
const GLYPH = {
  lock: ["M8 11V7a4 4 0 0 1 8 0v4", "rect:4,11,16,10,2"],
  userPlus: ["M2 21a7 7 0 0 1 14 0M19 8v6M16 11h6", "circle:9,8,4"],
  plus: ["M12 5v14M5 12h14"],
  chevronDown: ["m6 9 6 6 6-6"],
  chevronRight: ["m9 18 6-6-6-6"],
  chevronLeft: ["m15 18-6-6 6-6"],
  shield: ["M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z"],
  plug: ["M9 2v6M15 2v6M6 8h12v3a6 6 0 0 1-12 0z", "M12 17v5"],
  card: ["rect:2,5,20,14,2", "M2 10h20"],
  archive: [
    "rect:3,4,18,5,1",
    "M5 9v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V9",
    "M10 13h4",
  ],
  member: [
    "circle:9,8,4",
    "M2 21a7 7 0 0 1 14 0M17 3a4 4 0 0 1 0 8M22 21a7 7 0 0 0-5-6.7",
  ],
  gear: [
    "M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1 7 17M17 7l2.1-2.1",
    "circle:12,12,3",
  ],
  kanban: ["rect:3,4,18,16,2", "M9 4v16M15 4v10"],
  table: ["rect:3,4,18,16,2", "M3 10h18M3 15h18M9 4v16"],
  inbox: [
    "M22 12h-6l-2 3h-4l-2-3H2",
    "M5.5 5h13L22 12v6a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-6z",
  ],
  doc: [
    "M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z",
    "M14 3v6h6",
  ],
  docLines: [
    "M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z",
    "M14 3v6h6M8 13h8M8 17h5",
  ],
  calendar: ["rect:3,5,18,16,2", "M3 10h18M8 3v4M16 3v4"],
  folderPlus: [
    "M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z",
    "M12 11v5M9.5 13.5h5",
  ],
  folder: [
    "M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z",
  ],
  grid: [
    "rect:3,3,7,7,1.5",
    "rect:14,3,7,7,1.5",
    "rect:3,14,7,7,1.5",
    "rect:14,14,7,7,1.5",
  ],
  arrowDown: ["M12 3v12M7 10l5 5 5-5"],
  eye: ["M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z", "circle:12,12,3"],
  check: ["m5 12 5 5L20 7"],
  inboxTray: [
    "M22 12h-6l-2 3h-4l-2-3H2",
    "M5.5 5h13L22 12v6a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-6z",
  ],
  users: [
    "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2",
    "circle:9,7,4",
    "M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75",
  ],
  search: ["circle:11,11,7", "m20 20-3.5-3.5"],
  link: [
    "M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71",
    "M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71",
  ],
  x: ["M18 6 6 18M6 6l12 12"],
  more: ["circle:5,12,1", "circle:12,12,1", "circle:19,12,1"],
  info: ["circle:12,12,10", "M12 16v-4M12 8h.01"],
  infoCircle: ["circle:12,12,9", "M12 11v5M12 8h.01"],
  team: [
    "circle:9,8,3.5",
    "circle:17,9,2.5",
    "M2.5 20a6.5 6.5 0 0 1 13 0M15 20a5 5 0 0 1 6.5-4.7",
  ],
  person: ["circle:9,8,3.5", "M2.5 20a6.5 6.5 0 0 1 13 0"],
  alert: [
    "M12 9v4M12 17h.01",
    "M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z",
  ],
  xmark: ["M6 6l12 12M18 6 6 18"],
  mail: ["rect:2,4,20,16,2", "m22 7-10 6L2 7"],
} as const;

export type GlyphName = keyof typeof GLYPH;

/** An inline stroke glyph at the mockup's 24x24 path data. */
export function Glyph({
  name,
  className = "size-4",
  strokeWidth = 1.8,
}: {
  name: GlyphName;
  className?: string;
  strokeWidth?: number;
}) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={cn("shrink-0", className)}
    >
      {GLYPH[name].map((part, index) => {
        if (part.startsWith("rect:")) {
          const [x, y, w, h, r] = part.slice(5).split(",").map(Number);
          return <rect key={index} x={x} y={y} width={w} height={h} rx={r} />;
        }
        if (part.startsWith("circle:")) {
          const [cx, cy, r] = part.slice(7).split(",").map(Number);
          return <circle key={index} cx={cx} cy={cy} r={r} />;
        }
        return <path key={index} d={part} />;
      })}
    </svg>
  );
}

/** A person's initials on an identity colour (Final-Space `.av`). */
export function SpaceAvatar({
  person,
  className,
}: {
  person: SpaceAvatarView;
  className?: string;
}) {
  return (
    <span
      data-f="av"
      aria-hidden="true"
      className={cn(
        "grid size-[26px] shrink-0 place-items-center rounded-full text-mark-sm font-bold",
        IDENTITY_MARK_CLASS[person.tone],
        className,
      )}
    >
      {initialsFor(person.name)}
    </span>
  );
}

/** Overlapping avatars; `more` draws the "+20" bubble. */
export function AvatarStack({
  people,
  more = 0,
  label,
  ringClass,
  avatarClass,
  dataF = "stack",
}: {
  people: readonly SpaceAvatarView[];
  more?: number;
  label?: string;
  ringClass: string;
  avatarClass?: string;
  dataF?: string;
}) {
  return (
    <div
      data-f={dataF}
      role={label ? "img" : undefined}
      aria-label={label}
      className="flex"
    >
      {people.map((person) => (
        <SpaceAvatar
          key={person.id}
          person={person}
          className={cn("-ml-1.5 first:ml-0", ringClass, avatarClass)}
        />
      ))}
      {more > 0 ? (
        <span
          data-f="av"
          aria-hidden="true"
          className={cn(
            "-ml-1.5 grid size-[26px] shrink-0 place-items-center rounded-full bg-state-hover-strong text-mark-sm font-bold text-muted-foreground",
            ringClass,
            avatarClass,
            people.length === 0 && "ml-0",
          )}
        >
          +{more}
        </span>
      ) : null}
    </div>
  );
}

export function MarkSquare({
  name,
  tone,
  className,
  dataF,
}: {
  name: string;
  tone: MarkToneId;
  className?: string;
  dataF?: string;
}) {
  return (
    <span
      data-f={dataF}
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center font-bold",
        MARK_TONE_CLASS[tone],
        className,
      )}
    >
      {initialsFor(name)}
    </span>
  );
}

export interface SpaceCreateMenuProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly spaceName: string;
  readonly peopleTotal: number | null;
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly onBoard: () => void;
  readonly onDoc: () => void;
  readonly onChild: (() => void) | null;
  /** A plain button without the chevron (the new-department head). */
  readonly secondary?: boolean;
}

/** Final-Space `.menu`: Board, Doc, sub-department, then a template. */
export function SpaceCreateMenu({
  copy,
  spaceName,
  peopleTotal,
  open,
  onOpenChange,
  onBoard,
  onDoc,
  onChild,
  secondary = false,
}: SpaceCreateMenuProps) {
  const m = copy.createMenu;
  const trigger = useRef<HTMLButtonElement>(null);
  const menu = useRef<HTMLDivElement>(null);

  // Escape and a press outside close it; focus returns to the trigger.
  useEffect(() => {
    if (!open) return;
    const onPointer = (event: PointerEvent) => {
      const target = event.target as Node;
      if (trigger.current?.contains(target) || menu.current?.contains(target))
        return;
      onOpenChange(false);
    };
    window.addEventListener("pointerdown", onPointer);
    return () => window.removeEventListener("pointerdown", onPointer);
  }, [open, onOpenChange]);

  useEffect(() => {
    if (open)
      menu.current?.querySelector<HTMLElement>("[role=menuitem]")?.focus();
  }, [open]);

  const close = (focusTrigger: boolean) => {
    onOpenChange(false);
    if (focusTrigger) trigger.current?.focus();
  };

  const items = (): HTMLElement[] =>
    Array.from(
      menu.current?.querySelectorAll<HTMLElement>("[role=menuitem]") ?? [],
    );

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Escape") {
      event.stopPropagation();
      close(true);
      return;
    }
    if (event.key === "Tab") {
      close(false);
      return;
    }
    const list = items();
    const index = list.indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      list[(index + step + list.length) % list.length]?.focus();
    } else if (event.key === "Home") {
      event.preventDefault();
      list[0]?.focus();
    } else if (event.key === "End") {
      event.preventDefault();
      list[list.length - 1]?.focus();
    } else if (event.key.toLowerCase() === "b" && !event.metaKey) {
      event.preventDefault();
      close(true);
      onBoard();
    } else if (event.key.toLowerCase() === "d" && !event.metaKey) {
      event.preventDefault();
      close(true);
      onDoc();
    }
  };

  const itemClass =
    "mx-1 box-border flex min-h-11 w-[calc(100%-8px)] items-center gap-2.5 rounded-[7px] border-0 bg-transparent px-2.5 text-left text-nav leading-[1.45] text-foreground hover:bg-state-hover-strong focus-visible:bg-state-hover-strong aria-disabled:cursor-default aria-disabled:hover:bg-transparent";
  const iconClass = (accent: boolean) =>
    cn(
      "grid size-7 shrink-0 place-items-center rounded-[7px]",
      accent
        ? "bg-state-selected text-state-selected-foreground"
        : "bg-state-hover-strong text-muted-foreground",
    );
  const text = (title: string, hint: string) => (
    <span className="min-w-0">
      <b data-f="mt" className="block text-nav leading-[1.45] font-semibold">
        {title}
      </b>
      <small
        data-f="mh"
        className="block text-xs leading-[1.45] font-normal text-meta-foreground"
      >
        {hint}
      </small>
    </span>
  );
  const key = (letter: string) => (
    <span
      data-f="kbd"
      className="ml-auto rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] text-muted-foreground"
    >
      {letter}
    </span>
  );

  return (
    <>
      <button
        ref={trigger}
        data-f="btn-new"
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => onOpenChange(!open)}
        className={cn(secondary ? BTN : BTN_PRIMARY, FOCUS)}
      >
        <Glyph name="plus" />
        {copy.page.create}
        {secondary ? null : (
          <Glyph name="chevronDown" className="size-[13px]" />
        )}
      </button>
      {open ? (
        <div
          ref={menu}
          data-f="menu"
          role="menu"
          aria-label={formatCopy(m.label, { name: spaceName })}
          onKeyDown={onKeyDown}
          className="absolute top-10 right-10 z-dropdown box-content w-[300px] rounded-[10px] border border-line-strong bg-popover py-1 text-nav font-normal shadow-popover dark:bg-card"
        >
          <button
            data-f="mi"
            type="button"
            role="menuitem"
            onClick={() => {
              close(true);
              onBoard();
            }}
            className={cn(itemClass, FOCUS, "bg-state-hover-strong")}
          >
            <span data-f="mic" className={iconClass(true)}>
              <Glyph name="kanban" />
            </span>
            {text(m.board, m.boardHint)}
            {key("B")}
          </button>
          <button
            data-f="mi"
            type="button"
            role="menuitem"
            onClick={() => {
              close(true);
              onDoc();
            }}
            className={cn(itemClass, FOCUS)}
          >
            <span data-f="mic" className={iconClass(true)}>
              <Glyph name="doc" />
            </span>
            {text(m.doc, m.docHint)}
            {key("D")}
          </button>
          <button
            data-f="mi"
            type="button"
            role="menuitem"
            aria-disabled={onChild ? undefined : true}
            title={onChild ? undefined : m.childUnavailable}
            onClick={
              onChild
                ? () => {
                    close(true);
                    onChild();
                  }
                : undefined
            }
            className={cn(itemClass, FOCUS)}
          >
            <span data-f="mic" className={iconClass(false)}>
              <Glyph name="folderPlus" />
            </span>
            {text(m.child, formatCopy(m.childHint, { name: spaceName }))}
          </button>
          <div data-f="msep" role="separator" className="my-1 h-px bg-line" />
          <button
            data-f="mi"
            type="button"
            role="menuitem"
            aria-disabled="true"
            title={m.templateUnavailable}
            className={cn(itemClass, FOCUS)}
          >
            <span data-f="mic" className={iconClass(false)}>
              <Glyph name="grid" />
            </span>
            {text(m.template, m.templateHint)}
          </button>
          <div
            data-f="mfoot"
            className="mt-1 border-t border-line px-3.5 pt-2 pb-1.5 text-xs leading-normal text-meta-foreground"
          >
            {peopleTotal === null
              ? formatCopy(m.footer, { name: spaceName })
              : formatCopy(m.footerCount, {
                  name: spaceName,
                  count: peopleTotal,
                })}
          </div>
        </div>
      ) : null}
    </>
  );
}

export interface SpaceHeadProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly name: string;
  readonly workspaceName: string;
  readonly markTone: MarkToneId;
  readonly isPrivate: boolean;
  readonly archived: boolean;
  readonly description: string | null;
  readonly people: SpaceHeadPeople | null;
  readonly peopleTotal: number | null;
  /** Whether the member manages the Space (Add members, Create, Settings). */
  readonly canManage: boolean;
  readonly canCreate: boolean;
  readonly tab: SpaceTab;
  readonly spaceId: string;
  readonly menuOpen: boolean;
  readonly onMenuOpenChange: (open: boolean) => void;
  readonly onToggleSettings: () => void;
  readonly onCreateBoard: () => void;
  readonly onCreateDoc: () => void;
  readonly onCreateChild: (() => void) | null;
  /** Highlight the Add members button (a new, empty department, or the members tab). */
  readonly primaryAdd: boolean;
  /** On the members tab Add members opens the add dialog instead of the tab. */
  readonly onAdd: (() => void) | null;
}

/** Final-Space `.head`: mark, crumb and title, the chip, the actions, the lede. */
export function SpaceHead({
  copy,
  navigation,
  name,
  workspaceName,
  markTone,
  isPrivate,
  archived,
  description,
  people,
  peopleTotal,
  canManage,
  canCreate,
  tab,
  spaceId,
  menuOpen,
  onMenuOpenChange,
  onToggleSettings,
  onCreateBoard,
  onCreateDoc,
  onCreateChild,
  primaryAdd,
  onAdd,
}: SpaceHeadProps) {
  const settingsActive = tab === "settings";
  const membersDestination: WorkspaceDestination = {
    section: "space",
    spaceId,
    tab: "members",
  };
  return (
    <div data-f="head" className="px-7 pt-5">
      <div data-f="tr" className="flex flex-wrap items-center gap-3.5">
        <MarkSquare
          dataF="dm"
          name={name}
          tone={markTone}
          className="size-10 rounded-[11px] text-mark-lg"
        />
        <div data-f="crumbt" className="flex flex-col">
          <small data-f="crumb" className="text-crumb text-meta-foreground">
            {workspaceName}
          </small>
          <h1 data-f="h1" className="m-0 text-list-title text-foreground">
            {name}
          </h1>
        </div>
        <span
          data-f="chip"
          className="box-content inline-flex h-6 items-center gap-[5px] rounded-[6px] border border-line bg-popover px-2 text-xs leading-[1.45] font-medium text-muted-foreground dark:bg-card"
        >
          {isPrivate ? (
            <>
              <Glyph name="lock" className="size-[13px]" />
              {copy.tree.private}
            </>
          ) : (
            copy.visibility.Workspace
          )}
        </span>
        {archived ? (
          <span
            data-f="chip"
            className="box-content inline-flex h-6 items-center rounded-[6px] border border-line bg-popover px-2 text-xs leading-[1.45] font-medium text-muted-foreground dark:bg-card"
          >
            {copy.tree.archived}
          </span>
        ) : null}
        <div data-f="acts" className="relative ml-auto flex items-center gap-2">
          {people ? (
            <AvatarStack
              people={people.avatars}
              more={people.more}
              label={people.label}
              ringClass="ring-2 ring-background"
            />
          ) : null}
          {canManage && onAdd ? (
            <button
              data-f="btn-add"
              type="button"
              onClick={onAdd}
              className={cn(primaryAdd ? BTN_PRIMARY : BTN, FOCUS)}
            >
              <Glyph name="userPlus" />
              {copy.page.addMember}
            </button>
          ) : canManage ? (
            <ShellLink
              data-f="btn-add"
              destination={membersDestination}
              navigation={navigation}
              className={cn(primaryAdd ? BTN_PRIMARY : BTN, FOCUS)}
            >
              <Glyph name="userPlus" />
              {copy.page.addMember}
            </ShellLink>
          ) : null}
          {canCreate && !archived && tab === "overview" ? (
            <SpaceCreateMenu
              secondary={primaryAdd}
              copy={copy}
              spaceName={name}
              peopleTotal={peopleTotal}
              open={menuOpen}
              onOpenChange={onMenuOpenChange}
              onBoard={onCreateBoard}
              onDoc={onCreateDoc}
              onChild={onCreateChild}
            />
          ) : null}
          {canManage ? (
            <button
              data-f="ib"
              type="button"
              aria-label={copy.page.settingsButton}
              aria-pressed={settingsActive}
              onClick={onToggleSettings}
              className={cn(
                "grid size-8 shrink-0 place-items-center rounded-[7px] border-0 text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
                settingsActive && "bg-state-hover-strong text-foreground",
                FOCUS,
              )}
            >
              <Glyph name="gear" />
            </button>
          ) : null}
        </div>
      </div>
      {description && tab !== "members" ? (
        <p
          data-f="lede"
          className="mt-2 mr-0 mb-0 ml-[54px] max-w-[72ch] text-body leading-[1.45] text-muted-foreground"
        >
          {description}
        </p>
      ) : null}
    </div>
  );
}

export interface SpaceTabsProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly name: string;
  readonly spaceId: string;
  readonly tab: SpaceTab;
  readonly contentCount: number | null;
  readonly membersCount: number | null;
  readonly showSettings: boolean;
}

/** Final-Space `.tabs`: overview, Boards and Docs, members, settings. */
export function SpaceTabs({
  copy,
  navigation,
  name,
  spaceId,
  tab,
  contentCount,
  membersCount,
  showSettings,
}: SpaceTabsProps) {
  const base =
    "relative -mb-px box-content inline-flex h-[38px] items-center gap-[7px] border-0 border-b-2 border-transparent px-3 text-nav leading-[1.45] font-medium text-muted-foreground hover:text-foreground";
  const on = "border-primary font-semibold text-foreground";
  const count = (value: number | null): ReactNode =>
    value === null ? null : (
      <span data-f="tabn" className="font-normal text-meta-foreground">
        {value}
      </span>
    );
  const tabLink = (target: SpaceTab, label: string, value: number | null) => (
    <ShellLink
      key={target}
      data-f="tab"
      destination={{
        section: "space",
        spaceId,
        tab: target === "overview" ? undefined : target,
      }}
      navigation={navigation}
      aria-current={tab === target ? "page" : undefined}
      className={cn(base, tab === target && on, FOCUS)}
    >
      {label}
      {count(value)}
    </ShellLink>
  );
  return (
    <nav
      data-f="tabs"
      aria-label={formatCopy(copy.page.tabsLabel, { name })}
      className="flex items-end gap-0.5 border-b border-line px-7 pt-3.5"
    >
      {tabLink("overview", copy.page.tabs.overview, null)}
      <span
        data-f="tab"
        role="link"
        aria-disabled="true"
        title={copy.page.tabUnavailable}
        className={cn(base, "cursor-default hover:text-muted-foreground")}
      >
        {copy.page.tabs.content}
        {count(contentCount)}
      </span>
      {tabLink("members", copy.page.tabs.members, membersCount)}
      {showSettings ? tabLink("settings", copy.page.tabs.settings, null) : null}
    </nav>
  );
}
