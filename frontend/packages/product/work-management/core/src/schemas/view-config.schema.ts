import { z } from "zod";

export const filterConfigSchema = z.object({
  fieldId: z.string(),
  operator: z.enum(["is", "is_not", "contains", "is_empty", "is_not_empty"]),
  value: z.unknown(),
});

export const sortConfigSchema = z.object({
  fieldId: z.string(),
  direction: z.enum(["asc", "desc"]),
});

/**
 * Cards a Kanban lane renders before "Xem thêm N việc". A lane scrolls inside
 * its own height, so this only pages very long lanes.
 */
export const DEFAULT_KANBAN_LANE_CARD_LIMIT = 10;
/** A lane's work-in-progress limit: a warning number, never a write rule. */
export const MIN_KANBAN_WIP_LIMIT = 1;
export const MAX_KANBAN_WIP_LIMIT = 999;
export const MIN_KANBAN_LANE_CARD_LIMIT = 1;
export const MAX_KANBAN_LANE_CARD_LIMIT = 20;

/** Board access levels that may be given the Kanban status controls. */
export const kanbanStatusManageRoleSchema = z.enum(["manager", "editor"]);
export type KanbanStatusManageRole = z.infer<
  typeof kanbanStatusManageRoleSchema
>;
export const DEFAULT_KANBAN_STATUS_MANAGE_ROLES: readonly KanbanStatusManageRole[] =
  ["manager", "editor"];

/**
 * Manager is always in: a manager can never be locked out of the status
 * controls. Order is canonical (manager first), duplicates dropped.
 */
export function normalizeStatusManageRoles(
  roles: readonly KanbanStatusManageRole[] | undefined,
): KanbanStatusManageRole[] {
  const editor = (roles ?? DEFAULT_KANBAN_STATUS_MANAGE_ROLES).includes(
    "editor",
  );
  return editor ? ["manager", "editor"] : ["manager"];
}

/** The Kanban settings of a view, every one resolved to a usable value. */
export interface KanbanViewSettings {
  readonly laneCardLimit: number;
  readonly statusManageRoles: readonly KanbanStatusManageRole[];
  readonly keyboardMove: boolean;
  /** Lane value id (status or Select option) to its WIP limit; absent: none. */
  readonly wipLimits: Readonly<Record<string, number>>;
  /** Field ids shown on the card face, in order. */
  readonly cardFieldIds?: string[];
  /** Face fields hidden while the card has no value. */
  readonly cardHideEmpty?: string[];
}

const wipLimitsSchema = z.record(
  z.string(),
  z.number().int().min(MIN_KANBAN_WIP_LIMIT).max(MAX_KANBAN_WIP_LIMIT),
);

/**
 * The Kanban settings of a saved view config. A config saved before these
 * keys existed, or one carrying a bad value, gives today's behaviour: four
 * cards per lane, managers and editors manage statuses, keyboard move on.
 * A limit outside 1..20 is clamped.
 */
export function resolveKanbanSettings(config: {
  readonly laneCardLimit?: unknown;
  readonly statusManageRoles?: unknown;
  readonly keyboardMove?: unknown;
  readonly wipLimits?: unknown;
  readonly cardFieldIds?: unknown;
  readonly cardHideEmpty?: unknown;
}): KanbanViewSettings {
  const ids = z.array(z.string());
  const faceIds = ids.safeParse(config.cardFieldIds);
  const hideEmpty = ids.safeParse(config.cardHideEmpty);
  const wip = wipLimitsSchema.safeParse(config.wipLimits);
  const limit = config.laneCardLimit;
  const roles = z
    .array(kanbanStatusManageRoleSchema)
    .safeParse(config.statusManageRoles);
  return {
    laneCardLimit:
      typeof limit === "number" && Number.isFinite(limit)
        ? Math.min(
            MAX_KANBAN_LANE_CARD_LIMIT,
            Math.max(MIN_KANBAN_LANE_CARD_LIMIT, Math.round(limit)),
          )
        : DEFAULT_KANBAN_LANE_CARD_LIMIT,
    statusManageRoles: normalizeStatusManageRoles(
      roles.success ? roles.data : undefined,
    ),
    keyboardMove:
      typeof config.keyboardMove === "boolean" ? config.keyboardMove : true,
    wipLimits: wip.success ? wip.data : {},
    cardFieldIds: faceIds.success ? faceIds.data : [],
    cardHideEmpty: hideEmpty.success ? hideEmpty.data : [],
  };
}

/** What a Timeline groups its rows by: the board's groups (default), the Status, the assignee or nothing. */
export const timelineGroupBySchema = z.enum([
  "group",
  "status",
  "assignee",
  "none",
]);
export type TimelineGroupBy = z.infer<typeof timelineGroupBySchema>;
/** Where a Timeline bar starts: the start day (default) or the day the item was created. */
export const timelineBarSpanSchema = z.enum(["start-due", "created-due"]);
export type TimelineBarSpan = z.infer<typeof timelineBarSpanSchema>;

/**
 * The Calendar's date basis and the Timeline's grouping and bar span of a
 * saved view config; a missing or bad value gives the default (Calendar:
 * start to due; Timeline: the board's groups, start to due).
 */
export function resolveCalendarDateField(config: {
  readonly calendarDateField?: unknown;
}): string {
  const value = config.calendarDateField;
  return typeof value === "string" && value.length > 0 ? value : "span";
}
export function resolveTimelinePresentation(config: {
  readonly timelineGroupBy?: unknown;
  readonly timelineBarSpan?: unknown;
}): { groupBy: TimelineGroupBy; barSpan: TimelineBarSpan } {
  return {
    groupBy: timelineGroupBySchema.catch("group").parse(config.timelineGroupBy),
    barSpan: timelineBarSpanSchema
      .catch("start-due")
      .parse(config.timelineBarSpan),
  };
}

export const viewConfigSchema = z.object({
  groupBy: z.string(),
  hiddenFields: z.array(z.string()),
  columnOrder: z.array(z.string()),
  columnWidths: z.record(z.string(), z.number()),
  collapsedGroups: z.record(z.string(), z.boolean()).default({}),
  filters: z.array(filterConfigSchema),
  sortBy: z.array(sortConfigSchema),
  density: z.enum(["compact", "comfortable"]).default("comfortable"),
  searchQuery: z.string().optional(),
  laneCardLimit: z
    .number()
    .int()
    .min(MIN_KANBAN_LANE_CARD_LIMIT)
    .max(MAX_KANBAN_LANE_CARD_LIMIT)
    .default(DEFAULT_KANBAN_LANE_CARD_LIMIT),
  statusManageRoles: z
    .array(kanbanStatusManageRoleSchema)
    .default([...DEFAULT_KANBAN_STATUS_MANAGE_ROLES])
    .transform(normalizeStatusManageRoles),
  keyboardMove: z.boolean().default(true),
  wipLimits: wipLimitsSchema.optional(),
  cardFieldIds: z.array(z.string()).optional(),
  cardHideEmpty: z.array(z.string()).optional(),
  calendarDateField: z.string().min(1).max(128).optional(),
  timelineGroupBy: timelineGroupBySchema.optional(),
  timelineBarSpan: timelineBarSpanSchema.optional(),
});

export type ViewConfigInput = z.infer<typeof viewConfigSchema>;
