import { useMemo } from "react";
import {
  buildSpaceIndex,
  childrenOf,
  identityToneFor,
  markToneFor,
} from "@notrelix/features-workspace/core";
import {
  formatCopy,
  type HomeAgenda,
  type HomeReplyView,
  type MyWorkDepartment,
  type MyWorkGroupView,
  type MyWorkItemView,
  type WorkspaceHomeCopy,
} from "@notrelix/features-workspace/web";
import { groupMyWorkItems } from "@notrelix/features-workspace/core";
import type {
  CalendarEventDto,
  HomeReplyDto,
  MyWorkItemExtDto,
} from "./work-feed-bridge";
import { useSpaceHooks } from "./use-workspace-spaces";

/** Departments (top-level Spaces) by Space id, for the feed's `departmentId`. */
export function useDepartmentsById(
  workspaceId: string,
): ReadonlyMap<string, MyWorkDepartment> {
  const { useSpaceTree } = useSpaceHooks();
  const tree = useSpaceTree(workspaceId);
  return useMemo(() => {
    const index = buildSpaceIndex(tree.data ?? []);
    return new Map(
      childrenOf(index, null).map((space) => [
        space.id,
        {
          name: space.name,
          markTone: markToneFor(space.id),
          destination: { section: "space", spaceId: space.id },
        } as MyWorkDepartment,
      ]),
    );
  }, [tree.data]);
}

/** One item of the extended My work read as the rows draw it. */
export function toExtItemView(
  item: MyWorkItemExtDto,
  boardNames: ReadonlyMap<string, string>,
  departmentOfBoard: (boardId: string) => MyWorkDepartment | null,
): MyWorkItemView {
  const reason = item.relations.includes("approver")
    ? "approve"
    : item.relations.includes("assigned")
      ? "mine"
      : "follow";
  return {
    id: item.id,
    boardId: item.boardId,
    groupId: item.groupId,
    name: item.name,
    dueAt: item.dueAt,
    startedAt: item.startedAt,
    updatedAt: item.updatedAt,
    version: item.version,
    boardName: boardNames.get(item.boardId) ?? null,
    department: departmentOfBoard(item.boardId),
    key: item.key,
    priority: item.priority,
    status: item.status
      ? { bucket: item.status.bucket, label: item.status.label }
      : null,
    reason,
    assignee: item.assignee
      ? { name: item.assignee.name, tone: identityToneFor(item.assignee.id) }
      : null,
    approval: item.approval
      ? {
          actorName: item.approval.requestedBy.name,
          actorTone: identityToneFor(item.approval.requestedBy.id),
          time: clock(item.approval.requestedAt),
        }
      : null,
    completedAt: item.completedAt,
  };
}

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

function clock(iso: string): string {
  const date = new Date(iso);
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function toExtGroups(
  items: readonly MyWorkItemView[],
  now: number,
): MyWorkGroupView[] {
  return groupMyWorkItems(items, now).map((group) => ({
    range: group.range,
    items: group.items as readonly MyWorkItemView[],
  }));
}

/** "12 phút trước"-style label already formatted by the host. */
export function toReplyViews(
  replies: readonly HomeReplyDto[],
  relativeTime: (value: string) => string,
): HomeReplyView[] {
  return replies.map((reply) => ({
    id: reply.id,
    kind: reply.kind === "approval" ? "approve" : "mention",
    actorName: reply.actor.name,
    actorTone: identityToneFor(reply.actor.id),
    when: relativeTime(reply.createdAt),
    text: reply.text,
    key: reply.target.key,
    target:
      reply.target.kind === "item" && reply.target.boardId
        ? {
            section: "board",
            boardId: reply.target.boardId,
            item: reply.target.id,
          }
        : reply.target.kind === "doc"
          ? { section: "doc", docId: reply.target.id }
          : undefined,
  }));
}

function durationWords(
  copy: WorkspaceHomeCopy,
  startsAt: string,
  endsAt: string,
): string {
  const minutes = Math.round(
    (Date.parse(endsAt) - Date.parse(startsAt)) / 60000,
  );
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  if (hours === 0) return formatCopy(copy.home.agenda.minutes, { count: rest });
  if (rest === 0) return formatCopy(copy.home.agenda.hours, { count: hours });
  return formatCopy(copy.home.agenda.hoursMinutes, { hours, minutes: rest });
}

export function toAgenda(
  copy: WorkspaceHomeCopy,
  events: readonly CalendarEventDto[],
  now: number,
): HomeAgenda {
  return {
    nowTime: clock(new Date(now).toISOString()),
    entries: events.map((event) => ({
      id: event.id,
      time: clock(event.startsAt),
      title: event.title,
      detail: [
        durationWords(copy, event.startsAt, event.endsAt),
        event.place,
        event.attendeeCount
          ? formatCopy(copy.home.agenda.people, { count: event.attendeeCount })
          : null,
      ]
        .filter(Boolean)
        .join(" · "),
      past: Date.parse(event.endsAt) <= now,
    })),
  };
}
