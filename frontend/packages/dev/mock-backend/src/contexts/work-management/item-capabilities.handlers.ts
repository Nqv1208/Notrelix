/**
 * Work Management mock — the card popup's item capabilities: cover, repeat, reminder, votes, viewers,
 * copying a checklist and clearing every link. State lives next to the store (one set per store), so a
 * reset store starts clean.
 */
import type { operations } from "@notrelix/contracts";
import { defineMockOperation } from "../../operations/types";
import {
  forbidden,
  noContent,
  notFound,
  ok,
} from "../../transport/create-response";
import { preconditionFailed } from "../workspace/space-target";
import type { MockStore } from "../../state/mock-store";
import { canReadBoard, canWriteBoardItems } from "./boards.handlers";

interface CardSettings {
  coverColor: string | null;
  coverAttachmentId: string | null;
  coverStyle: string | null;
  repeatRule: string | null;
  reminderOffsetMinutes: number;
}

interface CapabilityState {
  readonly settings: Map<string, CardSettings>;
  /** `${itemId}:${fieldId}` to the people who voted and when. */
  readonly votes: Map<string, Map<string, string>>;
  /** Item id to the people who opened it and when. */
  readonly views: Map<string, Map<string, string>>;
}

const states = new WeakMap<MockStore, CapabilityState>();

function stateOf(store: MockStore): CapabilityState {
  let state = states.get(store);
  if (!state) {
    state = { settings: new Map(), votes: new Map(), views: new Map() };
    states.set(store, state);
  }
  return state;
}

const COLORS = [
  "blue",
  "teal",
  "violet",
  "amber",
  "rose",
  "gray",
  "orange",
  "green",
  "indigo",
  "red",
];
const RULES = ["daily", "weekdays", "weekly", "biweekly", "monthly"];
const OFFSETS = [-1, 0, 15, 60, 1440, 2880, 10080];

function settingsOf(store: MockStore, itemId: string): CardSettings {
  const state = stateOf(store);
  let settings = state.settings.get(itemId);
  if (!settings) {
    settings = {
      coverColor: null,
      coverAttachmentId: null,
      coverStyle: null,
      repeatRule: null,
      reminderOffsetMinutes: 1440,
    };
    state.settings.set(itemId, settings);
  }
  return settings;
}

/** The cover, repeat rule and reminder as the item reads carry them. */
export function projectCardSettings(store: MockStore, itemId: string) {
  return { ...settingsOf(store, itemId) };
}

/** The Vote field values of an item: `{ count, voted }` by field id. */
export function projectVoteValues(
  store: MockStore,
  itemId: string,
  boardId: string,
): Record<string, { count: number; voted: boolean }> {
  const me = store.getCurrentUser().id;
  const values: Record<string, { count: number; voted: boolean }> = {};
  for (const column of store.getColumns(boardId)) {
    if (column.fieldType.toLowerCase() !== "vote") continue;
    const voters = stateOf(store).votes.get(`${itemId}:${column.id}`);
    values[column.id] = {
      count: voters?.size ?? 0,
      voted: voters?.has(me) ?? false,
    };
  }
  return values;
}

function badRequest(errors: Record<string, string[]>) {
  return {
    status: 400,
    body: { message: "Validation failed", code: "VALIDATION_ERROR", errors },
  };
}

function missingKey(request: { headers: Headers }) {
  return request.headers.get("Idempotency-Key")
    ? null
    : badRequest({ "Idempotency-Key": ["The header is required."] });
}

type VersionBody = { expectedVersion?: number };

function versionConflict(
  store: MockStore,
  itemId: string,
  body: VersionBody | undefined,
) {
  const item = store.getCard(itemId);
  const current = item?.version ?? 1;
  return body?.expectedVersion !== undefined && body.expectedVersion !== current
    ? preconditionFailed()
    : null;
}

function bump(store: MockStore, itemId: string) {
  const item = store.getCard(itemId);
  store.updateCard(itemId, { version: (item?.version ?? 1) + 1 });
}

/** The first occurrence of a repeat rule after `due` that is also after `after` (the producer's `ItemRepeat.NextDue`). */
function nextDue(due: Date, rule: string, after: Date): Date {
  let next = new Date(due);
  do {
    const day = next.getUTCDay();
    if (rule === "daily") next.setUTCDate(next.getUTCDate() + 1);
    else if (rule === "weekdays")
      next.setUTCDate(next.getUTCDate() + (day === 5 ? 3 : day === 6 ? 2 : 1));
    else if (rule === "weekly") next.setUTCDate(next.getUTCDate() + 7);
    else if (rule === "biweekly") next.setUTCDate(next.getUTCDate() + 14);
    else next.setUTCMonth(next.getUTCMonth() + 1);
    next = new Date(next);
  } while (next <= after);
  return next;
}

/**
 * Completing a repeating item with a due date does not close it: the dates move to the next occurrence and
 * the item stays open, in one change. Returns whether it did so; false leaves the caller to complete the item.
 * `optionId` is the status being written (absent for the complete endpoint): only a done status that follows
 * an open one rolls over.
 */
export function rollRepeatOnCompletion(
  store: MockStore,
  itemId: string,
  optionId?: string,
): boolean {
  const item = store.getCard(itemId);
  const rule = settingsOf(store, itemId).repeatRule;
  if (!item || !rule || !item.dueAt || item.completedAt) return false;
  const statusColumn = store
    .getColumns(item.boardId)
    .find((column) => column.fieldType === "status");
  const category = (id: string | undefined) =>
    statusColumn?.options?.find((option) => option.id === id)?.category;
  const current = store.getFieldValues(itemId)[statusColumn?.id ?? ""];
  if (optionId !== undefined && category(optionId) !== "done") return false;
  if (category(current as string | undefined) === "done") return false;
  const previousDue = new Date(item.dueAt);
  const due = nextDue(previousDue, rule, new Date(store.getClock().isoNow()));
  const shift = due.getTime() - previousDue.getTime();
  store.updateCard(itemId, {
    dueAt: due.toISOString(),
    ...(item.startedAt
      ? {
          startedAt: new Date(
            new Date(item.startedAt).getTime() + shift,
          ).toISOString(),
        }
      : {}),
    version: (item.version ?? 1) + 1,
  });
  return true;
}

/** Every item of a board, across its groups. */
function boardCards(store: MockStore, boardId: string) {
  return store.getLists(boardId).flatMap((list) => store.getCards(list.id));
}

const contract = (operationId: keyof operations) =>
  ({ kind: "openapi", operationId }) as const;

export const itemCapabilitiesOperations = [
  defineMockOperation<
    { id: string },
    {
      color?: string | null;
      attachmentId?: string | null;
      style?: string | null;
    } & VersionBody,
    void
  >({
    id: "wm.boardItems.setCover",
    contract: contract("WorkManagement.BoardItems.SetCover"),
    method: "PUT",
    route: "/board-items/:id/cover",
    async handle({ params, body, store, request }) {
      const missing = missingKey(request);
      if (missing) return missing;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const conflict = versionConflict(store, params.id, body);
      if (conflict) return conflict;
      const color = body?.color ?? null;
      const attachmentId = body?.attachmentId ?? null;
      const clearing = color === null && attachmentId === null;
      if (
        !clearing &&
        ((color !== null && attachmentId !== null) ||
          (color !== null && !COLORS.includes(color)) ||
          (body?.style !== "strip" && body?.style !== "full"))
      )
        return badRequest({ color: ["A cover is one colour or one image."] });
      if (
        attachmentId &&
        !store
          .getAttachments(params.id)
          .some(
            (file) =>
              file.id === attachmentId && file.contentType.startsWith("image/"),
          )
      )
        return badRequest({
          attachmentId: ["The cover image must be attached to this item."],
        });
      const settings = settingsOf(store, params.id);
      const style = clearing ? null : (body?.style ?? null);
      if (
        settings.coverColor !== color ||
        settings.coverAttachmentId !== attachmentId ||
        settings.coverStyle !== style
      ) {
        Object.assign(settings, {
          coverColor: color,
          coverAttachmentId: attachmentId,
          coverStyle: style,
        });
        bump(store, params.id);
      }
      return noContent();
    },
  }),
  defineMockOperation<
    { id: string },
    { rule?: string | null } & VersionBody,
    void
  >({
    id: "wm.boardItems.setRepeat",
    contract: contract("WorkManagement.BoardItems.SetRepeat"),
    method: "PUT",
    route: "/board-items/:id/repeat",
    async handle({ params, body, store, request }) {
      const missing = missingKey(request);
      if (missing) return missing;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const conflict = versionConflict(store, params.id, body);
      if (conflict) return conflict;
      const rule = body?.rule ?? null;
      if (rule !== null && !RULES.includes(rule))
        return badRequest({ rule: ["Unknown repeat rule."] });
      if (rule !== null && !item.dueAt)
        return badRequest({
          rule: ["Set a due date before the item repeats."],
        });
      const settings = settingsOf(store, params.id);
      if (settings.repeatRule !== rule) {
        settings.repeatRule = rule;
        bump(store, params.id);
      }
      return noContent();
    },
  }),
  defineMockOperation<
    { id: string },
    { offsetMinutes?: number } & VersionBody,
    void
  >({
    id: "wm.boardItems.setReminder",
    contract: contract("WorkManagement.BoardItems.SetReminder"),
    method: "PUT",
    route: "/board-items/:id/reminder",
    async handle({ params, body, store, request }) {
      const missing = missingKey(request);
      if (missing) return missing;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const conflict = versionConflict(store, params.id, body);
      if (conflict) return conflict;
      const offset = body?.offsetMinutes;
      if (offset === undefined || !OFFSETS.includes(offset))
        return badRequest({ offsetMinutes: ["Unknown reminder offset."] });
      const settings = settingsOf(store, params.id);
      if (settings.reminderOffsetMinutes !== offset) {
        settings.reminderOffsetMinutes = offset;
        bump(store, params.id);
      }
      return noContent();
    },
  }),
  defineMockOperation<
    { id: string; fieldId: string },
    { voted?: boolean },
    { count: number; voted: boolean }
  >({
    id: "wm.boardItems.setVote",
    contract: contract("WorkManagement.BoardItems.SetVote"),
    method: "PUT",
    route: "/board-items/:id/votes/:fieldId",
    async handle({ params, body, store, request }) {
      const missing = missingKey(request);
      if (missing) return missing;
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store))
        return notFound("Card not found");
      const isVote = store
        .getColumns(item.boardId)
        .some(
          (column) =>
            column.id === params.fieldId &&
            column.fieldType.toLowerCase() === "vote",
        );
      if (!isVote) return notFound("Field not found");
      const me = store.getCurrentUser().id;
      const votes = stateOf(store).votes;
      const key = `${params.id}:${params.fieldId}`;
      const voters = votes.get(key) ?? new Map<string, string>();
      if (body?.voted) {
        if (!voters.has(me)) voters.set(me, new Date().toISOString());
      } else voters.delete(me);
      votes.set(key, voters);
      return ok({ count: voters.size, voted: voters.has(me) });
    },
  }),
  defineMockOperation<{ id: string; fieldId: string }, never, unknown>({
    id: "wm.boardItems.listVotes",
    contract: contract("WorkManagement.BoardItems.ListVotes"),
    method: "GET",
    route: "/board-items/:id/votes/:fieldId",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store))
        return notFound("Card not found");
      const me = store.getCurrentUser().id;
      const voters =
        stateOf(store).votes.get(`${params.id}:${params.fieldId}`) ??
        new Map<string, string>();
      return ok({
        count: voters.size,
        voted: voters.has(me),
        voters: [...voters].map(([userId, votedAt]) => ({
          userId,
          name: store.getUser(userId)?.name ?? "",
          avatarUrl: null,
          votedAt,
        })),
      });
    },
  }),
  defineMockOperation<{ id: string }, never, void>({
    id: "wm.boardItems.recordView",
    contract: contract("WorkManagement.BoardItems.RecordView"),
    method: "POST",
    route: "/board-items/:id/views",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store))
        return notFound("Card not found");
      const views = stateOf(store).views;
      const row = views.get(params.id) ?? new Map<string, string>();
      const me = store.getCurrentUser().id;
      const last = row.get(me);
      if (!last || Date.now() - Date.parse(last) >= 5 * 60_000)
        row.set(me, new Date().toISOString());
      views.set(params.id, row);
      return noContent();
    },
  }),
  defineMockOperation<{ id: string }, never, unknown>({
    id: "wm.boardItems.listViewers",
    contract: contract("WorkManagement.BoardItems.ListViewers"),
    method: "GET",
    route: "/board-items/:id/viewers",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store))
        return notFound("Card not found");
      const row =
        stateOf(store).views.get(params.id) ?? new Map<string, string>();
      const viewers = [...row]
        .sort((a, b) => b[1].localeCompare(a[1]))
        .map(([userId, lastViewedAt]) => ({
          userId,
          name: store.getUser(userId)?.name ?? "",
          avatarUrl: null,
          lastViewedAt,
        }));
      return ok({ count: viewers.length, viewers });
    },
  }),
  defineMockOperation<{ id: string }, never, number>({
    id: "wm.boardItems.clearLinks",
    contract: contract("WorkManagement.BoardItems.ClearLinks"),
    method: "DELETE",
    route: "/board-items/:id/links",
    async handle({ params, store, request }) {
      const missing = missingKey(request);
      if (missing) return missing;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const links = store.getItemLinks(params.id);
      for (const link of links) store.removeItemLink(link.id);
      return ok(links.length);
    },
  }),
  defineMockOperation<
    { id: string },
    { sourceChecklistId?: string; title?: string | null },
    string
  >({
    id: "wm.checklists.copy",
    contract: contract("WorkManagement.Checklists.Copy"),
    method: "POST",
    route: "/board-items/:id/checklists/copy",
    async handle({ params, body, store, request }) {
      const missing = missingKey(request);
      if (missing) return missing;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const sources = boardCards(store, item.boardId).flatMap((card) =>
        store.getCardChecklists(card.id),
      );
      const source = sources.find(
        (checklist) => checklist.id === body?.sourceChecklistId,
      );
      if (!source) return notFound("Checklist not found");
      const created = store.createChecklist(
        params.id,
        body?.title?.trim() || source.title,
      );
      for (const entry of source.items)
        store.createChecklistItem(created.id, entry.title);
      return ok(created.id);
    },
  }),
  defineMockOperation<{ id: string }, never, unknown>({
    id: "wm.checklists.listForBoard",
    contract: contract("WorkManagement.Checklists.ListForBoard"),
    method: "GET",
    route: "/boards/:id/checklists",
    async handle({ params, store }) {
      if (!canReadBoard(params.id, store)) return notFound("Board not found");
      return ok(
        boardCards(store, params.id).flatMap((card) =>
          store.getCardChecklists(card.id).map((checklist) => ({
            id: checklist.id,
            title: checklist.title,
            itemId: card.id,
            itemKey: card.itemKey ?? null,
            itemName: card.title,
            itemCount: checklist.items.length,
          })),
        ),
      );
    },
  }),
];
