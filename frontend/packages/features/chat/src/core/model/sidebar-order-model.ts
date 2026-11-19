import type {
  ChatChannel,
  ChatChannelGroup,
  ChatChannelPlacement,
} from "../types/chat";

/** The gap a fresh end position keeps from its neighbour, and the spacing of a renumbered run. */
export const POSITION_STEP = 1024;

/** A position between two neighbours (a double): the midpoint, or one step past the only neighbour. */
export function positionBetween(
  prev: number | null,
  next: number | null,
): number {
  if (prev !== null && next !== null) return (prev + next) / 2;
  if (prev !== null) return prev + POSITION_STEP;
  if (next !== null) return next - POSITION_STEP;
  return 0;
}

/** Where a channel lands: a category (null for none) and the channel it goes before (null for the end of the run). */
export interface ChannelDrop {
  readonly groupId: string | null;
  readonly beforeId: string | null;
}

const groupOf = (channel: ChatChannel) => channel.groupId ?? null;

function byName(a: string, b: string): number {
  return a < b ? -1 : a > b ? 1 : 0;
}

/**
 * The placements that move one channel to a drop. One placement while the midpoint still fits between its
 * neighbours; when neighbours share a position (or the doubles ran out) the whole run is renumbered.
 * Null when the drop is where the channel already is.
 */
export function placeChannel(
  channels: readonly ChatChannel[],
  movedId: string,
  drop: ChannelDrop,
): ChatChannelPlacement[] | null {
  const moved = channels.find((channel) => channel.id === movedId);
  if (!moved || drop.beforeId === movedId) return null;
  const run = channels.filter(
    (channel) => channel.id !== movedId && groupOf(channel) === drop.groupId,
  );
  const found =
    drop.beforeId === null
      ? run.length
      : run.findIndex((channel) => channel.id === drop.beforeId);
  const index = found < 0 ? run.length : found;
  const original = channels
    .filter((channel) => groupOf(channel) === groupOf(moved))
    .findIndex((channel) => channel.id === movedId);
  if (groupOf(moved) === drop.groupId && original === index) return null;
  const prev = run[index - 1]?.position ?? null;
  const next = run[index]?.position ?? null;
  const position = positionBetween(prev, next);
  const fits =
    (prev === null || position > prev) && (next === null || position < next);
  if (fits) {
    return [
      {
        channelId: moved.id,
        groupId: drop.groupId,
        position,
        expectedVersion: moved.version,
      },
    ];
  }
  return [...run.slice(0, index), moved, ...run.slice(index)].map(
    (channel, i) => ({
      channelId: channel.id,
      groupId: drop.groupId,
      position: (i + 1) * POSITION_STEP,
      expectedVersion: channel.version,
    }),
  );
}

/**
 * The drop one step up or down for the keyboard path: past the first row of a category it goes to the end of the
 * one above, past the last it goes to the start of the one below. `groupIds` is the category order (no entry for
 * the ungrouped run, which always comes first). Null at either end.
 */
export function stepChannel(
  channels: readonly ChatChannel[],
  groupIds: readonly string[],
  movedId: string,
  delta: -1 | 1,
): ChannelDrop | null {
  const moved = channels.find((channel) => channel.id === movedId);
  if (!moved) return null;
  const sections: (string | null)[] = [null, ...groupIds];
  const section = sections.indexOf(groupOf(moved));
  const runOf = (groupId: string | null) =>
    channels.filter((channel) => groupOf(channel) === groupId);
  const run = runOf(groupOf(moved));
  const i = run.findIndex((channel) => channel.id === movedId);
  if (delta === -1) {
    if (i > 0) return { groupId: groupOf(moved), beforeId: run[i - 1]!.id };
    if (section > 0) return { groupId: sections[section - 1]!, beforeId: null };
    return null;
  }
  if (i < run.length - 1) {
    return { groupId: groupOf(moved), beforeId: run[i + 2]?.id ?? null };
  }
  if (section >= 0 && section < sections.length - 1) {
    const target = sections[section + 1]!;
    return { groupId: target, beforeId: runOf(target)[0]?.id ?? null };
  }
  return null;
}

/** The cache after a placement lands, in sidebar order (position, then name). The version is bumped as the server does. */
export function applyPlacements(
  channels: readonly ChatChannel[],
  placements: readonly ChatChannelPlacement[],
): ChatChannel[] {
  const byId = new Map(placements.map((p) => [p.channelId, p]));
  return channels
    .map((channel) => {
      const placement = byId.get(channel.id);
      return placement
        ? {
            ...channel,
            groupId: placement.groupId,
            position: placement.position,
            version: channel.version + 1,
          }
        : channel;
    })
    .sort((a, b) => a.position - b.position || byName(a.name, b.name));
}

/** A category and its new position. */
export interface GroupMove {
  readonly group: ChatChannelGroup;
  readonly position: number;
}

/** The moves that put one category before another (null for the end); more than one when positions tie. Null when nothing changes. */
export function placeGroup(
  groups: readonly ChatChannelGroup[],
  movedId: string,
  beforeId: string | null,
): GroupMove[] | null {
  const moved = groups.find((group) => group.id === movedId);
  if (!moved || beforeId === movedId) return null;
  const run = groups.filter((group) => group.id !== movedId);
  const found =
    beforeId === null ? run.length : run.findIndex((g) => g.id === beforeId);
  const index = found < 0 ? run.length : found;
  if (groups.findIndex((group) => group.id === movedId) === index) return null;
  const prev = run[index - 1]?.position ?? null;
  const next = run[index]?.position ?? null;
  const position = positionBetween(prev, next);
  if (
    (prev === null || position > prev) &&
    (next === null || position < next)
  ) {
    return [{ group: moved, position }];
  }
  return [...run.slice(0, index), moved, ...run.slice(index)].map(
    (group, i) => ({ group, position: (i + 1) * POSITION_STEP }),
  );
}

/** The category before which a step up or down puts it (null for the end), or `undefined` at either end. */
export function stepGroup(
  groups: readonly ChatChannelGroup[],
  movedId: string,
  delta: -1 | 1,
): { readonly beforeId: string | null } | undefined {
  const i = groups.findIndex((group) => group.id === movedId);
  if (i < 0) return undefined;
  if (delta === -1) return i > 0 ? { beforeId: groups[i - 1]!.id } : undefined;
  return i < groups.length - 1
    ? { beforeId: groups[i + 2]?.id ?? null }
    : undefined;
}

/** The categories after a move, in order (position, then name). */
export function applyGroupMoves(
  groups: readonly ChatChannelGroup[],
  moves: readonly GroupMove[],
): ChatChannelGroup[] {
  const byId = new Map(moves.map((move) => [move.group.id, move.position]));
  return groups
    .map((group) =>
      byId.has(group.id)
        ? {
            ...group,
            position: byId.get(group.id)!,
            version: group.version + 1,
          }
        : group,
    )
    .sort((a, b) => a.position - b.position || byName(a.name, b.name));
}

/** One heading of the sidebar with the channels under it. `groupId` is null for the channels outside any category. */
export interface SidebarSection {
  readonly groupId: string | null;
  /** The category's real name, or null while the names are unread or the id is not in them (the numbered fallback). */
  readonly name: string | null;
  readonly version: number | null;
  readonly channels: readonly ChatChannel[];
}

/**
 * The ungrouped channels first (when there are any), then every category in position order, empty ones included,
 * then any group id a channel names that the category list does not (read late or failed).
 */
export function sidebarSections(
  channels: readonly ChatChannel[],
  groups: readonly ChatChannelGroup[] | undefined,
): SidebarSection[] {
  const sections: SidebarSection[] = [];
  const ungrouped = channels.filter((channel) => groupOf(channel) === null);
  if (ungrouped.length > 0) {
    sections.push({
      groupId: null,
      name: null,
      version: null,
      channels: ungrouped,
    });
  }
  const known = new Set<string>();
  for (const group of groups ?? []) {
    known.add(group.id);
    sections.push({
      groupId: group.id,
      name: group.name,
      version: group.version,
      channels: channels.filter((channel) => channel.groupId === group.id),
    });
  }
  const orphans = new Map<string, ChatChannel[]>();
  for (const channel of channels) {
    const id = channel.groupId;
    if (!id || known.has(id)) continue;
    const run = orphans.get(id);
    if (run) run.push(channel);
    else orphans.set(id, [channel]);
  }
  for (const [groupId, run] of orphans) {
    sections.push({ groupId, name: null, version: null, channels: run });
  }
  return sections;
}

/** What the pointer is over while a channel row or a category heading is dragged. */
export type DropOver =
  | {
      readonly type: "channel";
      readonly channelId: string;
      readonly groupId: string | null;
    }
  | { readonly type: "section"; readonly groupId: string | null };

/** The half of the target the dragged item's centre is in. */
export type DropEdge = "before" | "after";

/** The half of a rect (top and height) a vertical centre is in. */
export function edgeOf(
  centerY: number | null,
  rect: { readonly top: number; readonly height: number },
): DropEdge {
  return centerY !== null && centerY >= rect.top + rect.height / 2
    ? "after"
    : "before";
}

/** The drop a channel gets from what it is over: before or after a row, or the end of a category (its heading or its empty body). */
export function channelDropFor(
  over: DropOver,
  edge: DropEdge,
  channels: readonly ChatChannel[],
  movedId: string,
): ChannelDrop {
  if (over.type === "section") return { groupId: over.groupId, beforeId: null };
  if (edge === "before") {
    return { groupId: over.groupId, beforeId: over.channelId };
  }
  const run = channels.filter(
    (channel) => channel.id !== movedId && groupOf(channel) === over.groupId,
  );
  const at = run.findIndex((channel) => channel.id === over.channelId);
  return { groupId: over.groupId, beforeId: run[at + 1]?.id ?? null };
}

/** The category a dragged category goes before when it is over another one (null for the end). */
export function groupDropFor(
  overId: string,
  edge: DropEdge,
  groups: readonly ChatChannelGroup[],
  movedId: string,
): string | null {
  if (edge === "before") return overId;
  const run = groups.filter((group) => group.id !== movedId);
  const at = run.findIndex((group) => group.id === overId);
  return run[at + 1]?.id ?? null;
}
