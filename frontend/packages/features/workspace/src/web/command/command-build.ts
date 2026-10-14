import {
  PALETTE_KINDS,
  highlightSegments,
  matchesTerm,
  paletteCounts,
  parsePaletteQuery,
  type PaletteFilter,
  type PaletteKind,
  type ParsedPaletteQuery,
} from "../../core/model/command-palette";
import type { WorkspaceDestination } from "../../core/model/navigation";
import { formatCopy, type ShellPaletteSelection } from "../shell/shell-models";
import type {
  PaletteBoardEntry,
  PaletteDepartment,
  PaletteDocEntry,
  PalettePerson,
  PaletteRecentEntry,
  PaletteWorkEntry,
} from "./command-models";
import type { WorkspaceCommandCopy } from "./workspace-command.copy";

/** The commands the palette can list. */
export const PALETTE_COMMAND_IDS = [
  "set-status",
  "set-assignee",
  "move-board",
  "home",
  "my-work",
  "inbox",
  "search",
  "library",
  "reports",
  "members",
  "teams",
  "settings",
  "switch-workspace",
  "create-doc",
  "create-item",
  "theme",
  "shortcuts",
  "create-workspace",
] as const;
export type PaletteCommandId = (typeof PALETTE_COMMAND_IDS)[number];

/** The commands listed under "Lệnh nhanh" when nothing is typed. */
export const PALETTE_QUICK_IDS: readonly PaletteCommandId[] = [
  "create-item",
  "create-doc",
  "inbox",
  "theme",
];

export type PaletteIcon =
  | "work"
  | "doc"
  | "board"
  | "person"
  | "plus"
  | "home"
  | "my-work"
  | "inbox"
  | "search"
  | "library"
  | "reports"
  | "members"
  | "teams"
  | "settings"
  | "theme-dark"
  | "theme-light"
  | "keyboard"
  | "status"
  | "assignee"
  | "arrow"
  | "workspaces";

/** One piece of a result's second line. */
export type PaletteCtxPiece =
  | { readonly type: "mark"; readonly department: PaletteDepartment }
  | { readonly type: "text"; readonly text: string; readonly hit?: boolean }
  | { readonly type: "mono"; readonly text: string };

export interface PaletteRow {
  readonly id: string;
  readonly kind: PaletteKind;
  readonly icon: PaletteIcon;
  /** The icon tile draws in the accent tone (a "create" command). */
  readonly accent: boolean;
  readonly title: string;
  /** The identity tone of a person's initials (kind `person`). */
  readonly person?: PalettePerson;
  readonly ctx: readonly PaletteCtxPiece[];
  readonly status?: PaletteWorkEntry["status"];
  readonly due?: PaletteWorkEntry["due"];
  /** Real shortcut keys (`["G", "H"]`); only shortcuts that work. */
  readonly keys: readonly string[];
  /** Drawn like any row but inert: why. */
  readonly unavailable: string | null;
  readonly selection: ShellPaletteSelection | null;
  /** "↵ Mở" shows on this row while it is the selected one. */
  readonly openHint: boolean;
}

export interface PaletteGroup {
  readonly id: string;
  readonly label: string;
  /** The number next to the label; `null` draws none. */
  readonly count: number | null;
  readonly rows: readonly PaletteRow[];
}

export type PaletteView =
  | { readonly state: "recent"; readonly groups: readonly PaletteGroup[] }
  | {
      readonly state: "results";
      readonly groups: readonly PaletteGroup[];
      readonly counts: Readonly<Partial<Record<PaletteFilter, number>>>;
      readonly filtered: boolean;
    }
  | { readonly state: "commands"; readonly groups: readonly PaletteGroup[] }
  | { readonly state: "none"; readonly groups: readonly PaletteGroup[] };

export interface PaletteSources {
  readonly work: readonly PaletteWorkEntry[];
  readonly docs: readonly PaletteDocEntry[];
  readonly boards: readonly PaletteBoardEntry[];
  readonly people: readonly PalettePerson[];
  readonly recents: readonly PaletteRecentEntry[];
}

export interface PaletteCapabilities {
  readonly appearance: "light" | "dark";
  readonly canCreateWorkspace: boolean;
  readonly canCreateDoc: boolean;
  readonly canSwitchWorkspace: boolean;
  /** The commands the host offers; absent = every command it can run. */
  readonly commandIds?: readonly PaletteCommandId[];
  readonly otherWorkspaces: readonly string[];
}

const NAV: Readonly<
  Record<
    | "home"
    | "my-work"
    | "inbox"
    | "search"
    | "library"
    | "reports"
    | "members"
    | "teams"
    | "settings",
    { destination: WorkspaceDestination; keys: readonly string[] }
  >
> = {
  home: { destination: { section: "home" }, keys: ["G", "H"] },
  "my-work": { destination: { section: "my-work" }, keys: ["G", "M"] },
  inbox: { destination: { section: "inbox" }, keys: ["G", "I"] },
  search: { destination: { section: "search" }, keys: [] },
  library: { destination: { section: "library" }, keys: [] },
  reports: { destination: { section: "reports" }, keys: [] },
  members: { destination: { section: "members" }, keys: [] },
  teams: { destination: { section: "teams" }, keys: [] },
  settings: { destination: { section: "settings" }, keys: [] },
};

interface CommandDef {
  readonly id: PaletteCommandId;
  readonly label: string;
  readonly quickLabel?: string;
  readonly ctx: string | null;
  readonly icon: PaletteIcon;
  readonly accent: boolean;
  readonly keys: readonly string[];
  readonly selection: ShellPaletteSelection | null;
  readonly unavailable: string | null;
}

function commandDefs(
  copy: WorkspaceCommandCopy,
  caps: PaletteCapabilities,
): readonly CommandDef[] {
  const c = copy.commands;
  const dark = caps.appearance === "dark";
  const nav = (
    id: keyof typeof NAV,
    label: string,
    icon: PaletteIcon,
    quickLabel?: string,
  ): CommandDef => ({
    id,
    label,
    quickLabel,
    ctx: null,
    icon,
    accent: false,
    keys: NAV[id].keys,
    selection: { type: "navigate", destination: NAV[id].destination },
    unavailable: null,
  });
  const inert = (
    id: PaletteCommandId,
    label: string,
    icon: PaletteIcon,
    reason: string,
    extra: Partial<CommandDef> = {},
  ): CommandDef => ({
    id,
    label,
    ctx: null,
    icon,
    accent: false,
    keys: [],
    selection: null,
    unavailable: reason,
    ...extra,
  });
  const defs: CommandDef[] = [
    inert("set-status", c.setStatus, "status", c.unavailableItem, {
      ctx: c.setStatusCtx,
      accent: true,
    }),
    inert("set-assignee", c.setAssignee, "assignee", c.unavailableItem),
    inert("move-board", c.moveToBoard, "arrow", c.unavailableItem, {
      ctx: c.moveToBoardCtx,
    }),
    nav("home", c.home, "home"),
    nav("my-work", c.myWork, "my-work"),
    nav("inbox", c.inbox, "inbox", c.quickInbox),
    nav("search", c.search, "search"),
    nav("library", c.library, "library"),
    nav("reports", c.reports, "reports"),
    nav("members", c.members, "members"),
    nav("teams", c.teams, "teams"),
    nav("settings", c.settings, "settings"),
    caps.canSwitchWorkspace
      ? {
          id: "switch-workspace",
          label: c.switchWorkspace,
          ctx: caps.otherWorkspaces.length
            ? caps.otherWorkspaces.join(" · ")
            : c.switchWorkspaceCtx,
          icon: "workspaces",
          accent: false,
          keys: [],
          selection: { type: "action", action: "switch-workspace" },
          unavailable: null,
        }
      : inert(
          "switch-workspace",
          c.switchWorkspace,
          "workspaces",
          c.unavailable,
        ),
    caps.canCreateDoc
      ? {
          id: "create-doc",
          label: c.createDoc,
          ctx: null,
          icon: "doc",
          accent: false,
          keys: [],
          selection: { type: "action", action: "create-doc" },
          unavailable: null,
        }
      : inert("create-doc", c.createDoc, "doc", c.unavailable),
    inert("create-item", c.createItem, "plus", c.unavailableCreateItem, {
      accent: true,
    }),
    {
      id: "theme",
      label: dark ? c.toggleThemeLight : c.toggleThemeDark,
      ctx: null,
      icon: dark ? "theme-light" : "theme-dark",
      accent: false,
      keys: [],
      selection: { type: "action", action: "toggle-theme" },
      unavailable: null,
    },
    {
      id: "shortcuts",
      label: c.showShortcuts,
      ctx: null,
      icon: "keyboard",
      accent: false,
      keys: ["?"],
      selection: { type: "action", action: "show-shortcuts" },
      unavailable: null,
    },
  ];
  if (caps.canCreateWorkspace) {
    defs.push({
      id: "create-workspace",
      label: c.createWorkspace,
      ctx: null,
      icon: "plus",
      accent: false,
      keys: [],
      selection: { type: "action", action: "create-workspace" },
      unavailable: null,
    });
  }
  const allowed = caps.commandIds;
  return allowed ? defs.filter((def) => allowed.includes(def.id)) : defs;
}

function commandRow(def: CommandDef, quick: boolean): PaletteRow {
  const title = quick && def.quickLabel ? def.quickLabel : def.label;
  return {
    id: `command:${def.id}`,
    kind: "command",
    icon: def.icon,
    accent: def.accent,
    title,
    ctx: def.ctx ? [{ type: "text", text: def.ctx }] : [],
    keys: def.keys,
    unavailable: def.unavailable,
    selection: def.selection,
    openHint: false,
  };
}

interface CtxPart {
  readonly text: string;
  readonly mono?: boolean;
  /** Free text that may hold the typed term: the match is highlighted. */
  readonly note?: boolean;
}

/**
 * The pieces of a second line as the design sets them: a department mark,
 * then runs of text and mono keys. Consecutive text parts are one run joined
 * with " · "; a run before a key ends with " ·" and a run after it starts
 * with "· ". A note's matches are split out as highlighted pieces.
 */
function joinCtx(
  department: PaletteDepartment | null | undefined,
  parts: readonly (CtxPart | null | undefined)[],
  term: string,
): PaletteCtxPiece[] {
  const pieces: PaletteCtxPiece[] = [];
  if (department) pieces.push({ type: "mark", department });
  type Seg = { text: string; hit: boolean };
  const runs: { mono: boolean; segs: Seg[] }[] = [];
  for (const part of parts) {
    if (!part || !part.text) continue;
    if (part.mono) {
      runs.push({ mono: true, segs: [{ text: part.text, hit: false }] });
      continue;
    }
    const segs: Seg[] = part.note
      ? [...highlightSegments(part.text, term)]
      : [{ text: part.text, hit: false }];
    const last = runs[runs.length - 1];
    if (last && !last.mono) {
      last.segs.push({ text: " · ", hit: false }, ...segs);
    } else {
      runs.push({
        mono: false,
        segs: last?.mono ? [{ text: "· ", hit: false }, ...segs] : segs,
      });
    }
  }
  runs.forEach((run, index) => {
    if (!run.mono && runs[index + 1]?.mono) {
      run.segs.push({ text: " ·", hit: false });
    }
  });
  for (const run of runs) {
    if (run.mono) {
      pieces.push({ type: "mono", text: run.segs[0]?.text ?? "" });
      continue;
    }
    let pending = "";
    for (const seg of run.segs) {
      if (seg.hit) {
        if (pending) pieces.push({ type: "text", text: pending });
        pending = "";
        pieces.push({ type: "text", text: seg.text, hit: true });
      } else {
        pending += seg.text;
      }
    }
    if (pending) pieces.push({ type: "text", text: pending });
  }
  return pieces;
}

function workRow(
  entry: PaletteWorkEntry,
  copy: WorkspaceCommandCopy,
  term: string,
): PaletteRow {
  return {
    id: `work:${entry.id}`,
    kind: "work",
    icon: "work",
    accent: false,
    title: entry.title,
    ctx: joinCtx(
      entry.department,
      [
        entry.boardName ? { text: entry.boardName, note: true } : null,
        entry.key ? { text: entry.key, mono: true } : null,
        entry.mine
          ? { text: copy.ctx.mine }
          : entry.assignee
            ? { text: entry.assignee }
            : null,
        entry.matchNote ? { text: entry.matchNote, note: true } : null,
      ],
      term,
    ),
    status: entry.status ?? null,
    due: entry.due ?? null,
    keys: [],
    unavailable: null,
    selection: { type: "navigate", destination: entry.destination },
    openHint: true,
  };
}

function docRow(
  entry: PaletteDocEntry,
  copy: WorkspaceCommandCopy,
  term: string,
): PaletteRow {
  const place = [entry.department?.name, entry.location]
    .filter(Boolean)
    .join(" / ");
  return {
    id: `doc:${entry.id}`,
    kind: "doc",
    icon: "doc",
    accent: false,
    title: entry.title,
    ctx: joinCtx(
      entry.department,
      [
        entry.matchNote ? null : { text: place || copy.ctx.doc },
        entry.editedLabel
          ? {
              text: entry.editedBy
                ? formatCopy(copy.ctx.editedBy, {
                    name: entry.editedBy,
                    when: entry.editedLabel,
                  })
                : formatCopy(copy.ctx.edited, { when: entry.editedLabel }),
            }
          : null,
        entry.matchNote ? { text: entry.matchNote, note: true } : null,
      ],
      term,
    ),
    keys: [],
    unavailable: null,
    selection: {
      type: "navigate",
      destination: { section: "doc", docId: entry.id },
    },
    openHint: false,
  };
}

function boardRow(
  entry: PaletteBoardEntry,
  copy: WorkspaceCommandCopy,
  term: string,
): PaletteRow {
  return {
    id: `board:${entry.id}`,
    kind: "board",
    icon: "board",
    accent: false,
    title: entry.name,
    ctx: joinCtx(
      entry.department,
      [
        {
          text: entry.department
            ? formatCopy(copy.ctx.department, { name: entry.department.name })
            : copy.ctx.board,
        },
        entry.matchNote ? { text: entry.matchNote, note: true } : null,
      ],
      term,
    ),
    keys: [],
    unavailable: null,
    selection: {
      type: "navigate",
      destination: { section: "board", boardId: entry.id },
    },
    openHint: false,
  };
}

function personRow(person: PalettePerson): PaletteRow {
  return {
    id: `person:${person.id}`,
    kind: "person",
    icon: "person",
    accent: false,
    title: person.name,
    person,
    ctx: [{ type: "text", text: person.roleLabel }],
    keys: [],
    unavailable: null,
    selection: { type: "navigate", destination: { section: "members" } },
    openHint: false,
  };
}

function recentRow(
  entry: PaletteRecentEntry,
  copy: WorkspaceCommandCopy,
  term: string,
): PaletteRow {
  const label = entry.kind === "doc" ? copy.ctx.doc : copy.ctx.board;
  const when = entry.whenLabel
    ? { text: formatCopy(copy.ctx.opened, { when: entry.whenLabel }) }
    : null;
  const ctx =
    entry.kind === "work"
      ? joinCtx(
          entry.department,
          [
            entry.key ? { text: entry.key, mono: true } : null,
            entry.statusLabel ? { text: entry.statusLabel } : null,
            when,
          ],
          term,
        )
      : joinCtx(entry.department, [{ text: label }, when], term);
  return {
    id: `recent:${entry.kind}:${entry.id}`,
    kind: entry.kind === "work" ? "work" : entry.kind,
    icon: entry.kind,
    accent: false,
    title: entry.name,
    ctx,
    keys: [],
    unavailable: null,
    selection: {
      type: "navigate",
      destination:
        entry.destination ??
        (entry.kind === "board"
          ? { section: "board", boardId: entry.id }
          : { section: "doc", docId: entry.id }),
    },
    openHint: false,
  };
}

/** Rows listed per kind while "Tất cả" is the filter. */
export const PALETTE_ALL_LIMIT = 6;
/** Rows listed per kind while one kind is the filter. */
export const PALETTE_KIND_LIMIT = 50;

export interface BuildPaletteInput {
  readonly copy: WorkspaceCommandCopy;
  readonly query: string;
  readonly filter: PaletteFilter;
  readonly sources: PaletteSources;
  readonly capabilities: PaletteCapabilities;
  readonly workspaceName: string;
}

const hit = (text: string, term: string) => matchesTerm(text, term);

/** What the palette lists for the typed text and the filter tab. */
export function buildPalette(input: BuildPaletteInput): PaletteView {
  const { copy, filter, sources, capabilities } = input;
  const parsed: ParsedPaletteQuery = parsePaletteQuery(input.query);
  const defs = commandDefs(copy, capabilities);

  if (!input.query.trim()) {
    const recents = sources.recents.map((entry) => recentRow(entry, copy, ""));
    const quick = PALETTE_QUICK_IDS.flatMap((id) => {
      const def = defs.find((d) => d.id === id);
      return def ? [commandRow(def, true)] : [];
    });
    const groups: PaletteGroup[] = [];
    if (recents.length > 0) {
      groups.push({
        id: "recent",
        label: copy.groups.recent,
        count: null,
        rows: recents,
      });
    }
    if (quick.length > 0) {
      groups.push({
        id: "quick",
        label: copy.groups.quick,
        count: null,
        rows: quick,
      });
    }
    return { state: "recent", groups };
  }

  const term = parsed.term;
  const commandRows = defs
    .filter((def) => hit(def.label, term) || hit(def.quickLabel ?? "", term))
    .map((def) => commandRow(def, false));

  if (parsed.scope === "command") {
    return {
      state: "commands",
      groups: [
        {
          id: "command",
          label: term
            ? formatCopy(copy.match.command, { term })
            : copy.match.commandAll,
          count: null,
          rows: commandRows,
        },
      ],
    };
  }

  const people = sources.people
    .filter((person) => hit(person.name, term))
    .map(personRow);
  const boards = sources.boards
    .filter(
      (board) => hit(board.name, term) || hit(board.matchNote ?? "", term),
    )
    .map((board) => boardRow(board, copy, term));

  if (parsed.scope === "person") {
    return {
      state: "commands",
      groups: [
        {
          id: "person",
          label: term
            ? formatCopy(copy.match.person, { term })
            : copy.match.personAll,
          count: null,
          rows: people,
        },
      ],
    };
  }
  if (parsed.scope === "board") {
    return {
      state: "commands",
      groups: [
        {
          id: "board",
          label: term
            ? formatCopy(copy.match.board, { term })
            : copy.match.boardAll,
          count: null,
          rows: boards,
        },
      ],
    };
  }

  const work = sources.work
    .filter(
      (entry) =>
        hit(entry.title, term) ||
        hit(entry.key ?? "", term) ||
        hit(entry.boardName ?? "", term) ||
        hit(entry.matchNote ?? "", term),
    )
    .map((entry) => workRow(entry, copy, term));
  const docs = sources.docs
    .filter(
      (entry) => hit(entry.title, term) || hit(entry.matchNote ?? "", term),
    )
    .map((entry) => docRow(entry, copy, term));

  // The typed text offers an Item of that name; the host has no create-Item
  // producer for it, so the row is drawn inert.
  const createNamed: PaletteRow = {
    id: "command:create-item-named",
    kind: "command",
    icon: "plus",
    accent: true,
    title: formatCopy(copy.commands.createItemNamed, { name: term }),
    ctx: [{ type: "text", text: copy.ctx.createHint }],
    keys: [],
    unavailable: copy.commands.unavailableCreateItem,
    selection: null,
    openHint: false,
  };
  const commands = [...commandRows, createNamed];

  const byKind: Record<PaletteKind, readonly PaletteRow[]> = {
    work,
    doc: docs,
    board: boards,
    person: people,
    command: commands,
  };
  const counts = paletteCounts(
    PALETTE_KINDS.flatMap((kind) => byKind[kind].map(() => kind)),
  );

  // Nothing but the create row: the "not found" state with its suggestions.
  const matched =
    work.length +
    docs.length +
    boards.length +
    people.length +
    commandRows.length;
  if (matched === 0) {
    const others = capabilities.otherWorkspaces;
    const lastBoard = sources.recents.find((entry) => entry.kind === "board");
    const suggestions: PaletteRow[] = [
      {
        ...createNamed,
        ctx: lastBoard
          ? [
              {
                type: "text",
                text: formatCopy(copy.ctx.createIn, { board: lastBoard.name }),
              },
            ]
          : [],
      },
      {
        id: "command:search-all-workspaces",
        kind: "command",
        icon: "workspaces",
        accent: false,
        title: copy.commands.searchAllWorkspaces,
        ctx: others.length ? [{ type: "text", text: others.join(", ") }] : [],
        keys: [],
        unavailable: copy.commands.unavailableAllWorkspaces,
        selection: null,
        openHint: false,
      },
    ];
    return {
      state: "none",
      groups: [
        { id: "suggestions", label: "", count: null, rows: suggestions },
      ],
    };
  }

  const kinds: readonly PaletteKind[] =
    filter === "all" ? PALETTE_KINDS : [filter];
  const limit = filter === "all" ? PALETTE_ALL_LIMIT : PALETTE_KIND_LIMIT;
  const groups: PaletteGroup[] = [];
  for (const kind of kinds) {
    const rows = byKind[kind];
    if (rows.length === 0) continue;
    groups.push({
      id: kind,
      label: copy.groups[kind],
      count: kind === "command" ? null : rows.length,
      rows: rows.slice(0, limit),
    });
  }
  return {
    state: "results",
    groups,
    counts,
    filtered: filter !== "all",
  };
}

/** The first row a selection starts on: the first one that can run. */
export function firstEnabledId(groups: readonly PaletteGroup[]): string | null {
  const rows = groups.flatMap((group) => group.rows);
  return (rows.find((row) => !row.unavailable) ?? rows[0])?.id ?? null;
}

export function flattenRows(groups: readonly PaletteGroup[]): PaletteRow[] {
  return groups.flatMap((group) => [...group.rows]);
}
