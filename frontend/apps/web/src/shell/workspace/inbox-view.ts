import { identityToneFor } from "@notrelix/features-workspace/core";
import {
  formatCopy,
  inboxDayLabel,
  type InboxChildView,
  type InboxItemView,
  type InboxKind,
  type InboxSegment,
  type MyWorkDepartment,
  type WorkStatusBucket,
  type WorkspaceInboxCopy,
} from "@notrelix/features-workspace/web";
import type {
  InboxBundleChildDto,
  InboxEntryDto,
  PersonDto,
} from "./work-feed-bridge";

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

function clock(iso: string): string {
  const date = new Date(iso);
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function shortDate(iso: string): string {
  const date = new Date(iso);
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}

function startOfDay(time: number): number {
  const date = new Date(time);
  date.setHours(0, 0, 0, 0);
  return date.getTime();
}

const person = (dto: PersonDto) => ({
  name: dto.name,
  tone: identityToneFor(dto.id),
});

const strong = (text: string): InboxSegment => ({ text, strong: true });

export interface InboxViewDeps {
  readonly copy: WorkspaceInboxCopy;
  readonly now: number;
  /** Top-level Space (department) by Space id. */
  readonly departments: ReadonlyMap<string, MyWorkDepartment>;
  /** Where a target opens: the item's Board, a Doc. */
  readonly hrefFor: (target: InboxEntryDto["target"]) => string | null;
  /** `thứ Tư`-style weekday of an instant. */
  readonly weekday: (iso: string) => string;
}

function sentence(
  copy: WorkspaceInboxCopy,
  template: string,
  values: Record<string, string>,
  strongKeys: readonly string[],
): InboxSegment[] {
  // Splits the template on {{key}} so the named parts can be bold.
  const out: InboxSegment[] = [];
  let rest = template;
  const pattern = /\{\{(\w+)\}\}/;
  for (;;) {
    const match = pattern.exec(rest);
    if (!match) break;
    if (match.index > 0) out.push({ text: rest.slice(0, match.index) });
    const key = match[1] ?? "";
    const text = values[key] ?? "";
    out.push(strongKeys.includes(key) ? strong(text) : { text });
    rest = rest.slice(match.index + match[0].length);
  }
  if (rest) out.push({ text: rest });
  void copy;
  return out;
}

function dueWords(deps: InboxViewDeps, dueAt: string | null): string | null {
  if (!dueAt) return null;
  const today = startOfDay(deps.now);
  const due = startOfDay(Date.parse(dueAt));
  const date = new Date(dueAt);
  const hasTime = date.getHours() !== 0 || date.getMinutes() !== 0;
  if (due === today)
    return hasTime
      ? formatCopy(deps.copy.feed.todayDue, { time: clock(dueAt) })
      : deps.copy.days.today.toLowerCase();
  return shortDate(dueAt);
}

function daysOverdue(deps: InboxViewDeps, dueAt: string | null): number {
  if (!dueAt) return 0;
  return Math.max(
    0,
    Math.round(
      (startOfDay(deps.now) - startOfDay(Date.parse(dueAt))) / 86_400_000,
    ),
  );
}

function plural(copy: WorkspaceInboxCopy, count: number): string {
  return formatCopy(
    count === 1 ? copy.feed.overdueDays_one : copy.feed.overdueDays_other,
    { count },
  );
}

function childView(
  child: InboxBundleChildDto,
  copy: WorkspaceInboxCopy,
): InboxChildView {
  const template =
    child.kind === "status"
      ? copy.feed.childStatus
      : child.kind === "comment"
        ? copy.feed.childComment
        : copy.feed.childDue;
  return {
    id: child.id,
    at: child.createdAt,
    person: person(child.actor),
    segments: [
      strong(child.actor.name),
      {
        text: ` ${formatCopy(template, { key: child.itemKey, detail: child.detail })}`,
      },
    ],
  };
}

const KIND: Record<InboxEntryDto["type"], InboxKind> = {
  approval_requested: "approve",
  mention: "mention",
  overdue: "overdue",
  assigned: "assign",
  changes_bundle: "change",
  board_invited: "system",
  followed_status: "change",
  followed_comment: "change",
  followed_assignee: "change",
  automation: "system",
  due_tomorrow: "overdue",
  system: "system",
};

/** The producer's inbox as the Inbox page draws it. */
export function toInboxEntries(
  entries: readonly InboxEntryDto[],
  deps: InboxViewDeps,
): InboxItemView[] {
  const { copy } = deps;
  return entries.map((entry): InboxItemView => {
    const actor = entry.actor;
    const target = entry.target;
    const department = target.departmentId
      ? (deps.departments.get(target.departmentId) ?? null)
      : null;
    const actorName = actor?.name ?? "";
    let segments: InboxSegment[] = [];
    let context: InboxItemView["context"] = null;
    let avatarText: string | undefined;
    const keyLabel = [target.boardName, target.key].filter(Boolean).join(" · ");

    switch (entry.type) {
      case "approval_requested":
        segments = sentence(
          copy,
          copy.feed.approval,
          { actor: actorName, title: target.title },
          ["actor", "title"],
        );
        context = { department, label: keyLabel };
        break;
      case "mention":
        segments = sentence(
          copy,
          copy.feed.mention,
          {
            actor: actorName,
            title: target.title,
            excerpt: entry.excerpt ?? "",
          },
          ["actor", "title"],
        );
        context = {
          department,
          label: formatCopy(copy.feed.docContext, {
            department: department?.name ?? "",
          }),
        };
        break;
      case "overdue":
        segments = sentence(
          copy,
          entry.dueAt ? copy.feed.overdue : copy.feed.overduePlain,
          {
            title: target.title,
            days: plural(copy, daysOverdue(deps, entry.dueAt)),
          },
          ["title"],
        );
        context = {
          department,
          label: [
            keyLabel,
            target.owner
              ? formatCopy(copy.feed.ownerInCharge, { name: target.owner.name })
              : null,
          ]
            .filter(Boolean)
            .join(" · "),
        };
        break;
      case "assigned": {
        const due = dueWords(deps, entry.dueAt);
        segments = sentence(
          copy,
          due ? copy.feed.assigned : copy.feed.assignedNoDue,
          { actor: actorName, title: target.title, due: due ?? "" },
          ["actor", "title"],
        );
        context = { department, label: keyLabel };
        break;
      }
      case "automation":
        segments = sentence(
          copy,
          entry.excerpt ? copy.feed.automation : copy.feed.automationPlain,
          { title: target.title, excerpt: entry.excerpt ?? "" },
          ["title"],
        );
        context = { department, label: keyLabel };
        break;
      case "due_tomorrow":
        segments = sentence(
          copy,
          copy.feed.dueTomorrow,
          { title: target.title },
          ["title"],
        );
        context = { department, label: keyLabel };
        break;
      case "system":
        // A type this page does not know: its own text when it has one.
        segments = sentence(
          copy,
          entry.excerpt ? copy.feed.system : copy.feed.systemPlain,
          { excerpt: entry.excerpt ?? "" },
          [],
        );
        context = null;
        break;
      case "followed_status":
      case "followed_comment":
      case "followed_assignee": {
        const template =
          entry.type === "followed_status"
            ? entry.excerpt
              ? copy.feed.followedStatus
              : copy.feed.followedStatusPlain
            : entry.type === "followed_comment"
              ? entry.excerpt
                ? copy.feed.followedComment
                : copy.feed.followedCommentPlain
              : copy.feed.followedAssignee;
        segments = sentence(
          copy,
          template,
          {
            actor: actorName,
            title: target.title,
            status: entry.excerpt ?? "",
            excerpt: entry.excerpt ?? "",
          },
          ["actor", "title"],
        );
        context = { department, label: keyLabel };
        break;
      }
      case "changes_bundle": {
        const bundle = entry.bundle;
        const names = (bundle?.actors ?? []).map((a) => a.name).join(", ");
        const more = bundle?.moreActors
          ? formatCopy(copy.feed.bundleMore, { count: bundle.moreActors })
          : "";
        const count = bundle?.children.length ?? 0;
        segments = sentence(
          copy,
          copy.feed.bundle,
          {
            changes: formatCopy(copy.feed.bundleChanges, { count }),
            board: target.boardName ?? target.title,
            names: `${names}${more}`,
          },
          ["changes", "board"],
        );
        const parts = [
          bundle?.statusChanges
            ? formatCopy(copy.feed.bundleStatus, {
                count: bundle.statusChanges,
              })
            : null,
          bundle?.comments
            ? formatCopy(copy.feed.bundleComments, { count: bundle.comments })
            : null,
          bundle?.dueChanges
            ? formatCopy(copy.feed.bundleDue, { count: bundle.dueChanges })
            : null,
        ].filter(Boolean);
        context = { department: null, label: parts.join(" · ") };
        avatarText = `+${(bundle?.actors.length ?? 0) + (bundle?.moreActors ?? 0)}`;
        break;
      }
      case "board_invited":
        segments = sentence(
          copy,
          copy.feed.invited,
          {
            actor: actorName,
            title: target.title,
            permission: copy.feed.permissions[entry.permission ?? "view"],
          },
          ["actor", "title"],
        );
        context = {
          department,
          label: formatCopy(copy.feed.privateBoard, {
            department: department?.name ?? "",
          }),
        };
        break;
    }

    // ── Reading pane ───────────────────────────────────────────────
    const detailDto = entry.detail;
    const day = inboxDayLabel(copy, entry.createdAt, deps.now);
    const when = [
      formatCopy(copy.feed.whenOwner, { day, time: clock(entry.createdAt) }),
      entry.type === "approval_requested" && detailDto?.approval?.soleApprover
        ? copy.read.soleApprover
        : null,
      entry.type === "approval_requested" && detailDto?.item?.dueAt
        ? formatCopy(copy.read.approveBefore, {
            when: `${deps.weekday(detailDto.item.dueAt)} ${shortDate(detailDto.item.dueAt)}`,
          })
        : null,
      entry.type === "mention" && entry.need === "reply"
        ? copy.feed.needReply
        : null,
      entry.type === "overdue" && entry.dueAt ? copy.feed.followingNote : null,
    ]
      .filter(Boolean)
      .join(" · ");

    let eventSegments: InboxSegment[] = segments;
    if (entry.type === "approval_requested") {
      const summary = detailDto?.approval?.summary;
      eventSegments = sentence(
        copy,
        summary ? copy.feed.approvalEvent : copy.feed.approvalEventPlain,
        { actor: actorName, summary: summary ?? "" },
        ["actor"],
      );
    } else if (entry.type === "mention") {
      eventSegments = sentence(
        copy,
        copy.feed.mentionEvent,
        { actor: actorName, title: target.title },
        ["actor", "title"],
      );
    } else if (entry.type === "overdue") {
      eventSegments = sentence(
        copy,
        entry.dueAt ? copy.feed.overdueEvent : copy.feed.overduePlain,
        {
          title: target.title,
          days: plural(copy, daysOverdue(deps, entry.dueAt)),
        },
        ["title"],
      );
    }

    const item = detailDto?.item;
    const card =
      entry.type === "approval_requested" && item
        ? {
            title: target.title,
            status: item.status
              ? {
                  bucket: item.status.bucket as WorkStatusBucket,
                  label: item.status.label,
                }
              : undefined,
            priority: item.priority
              ? copy.read.priorities[item.priority]
              : undefined,
            due: item.dueAt ? shortDate(item.dueAt) : undefined,
            comments: item.commentCount,
            changes: detailDto?.approval?.changes ?? [],
            approvable: Boolean(detailDto?.approval),
          }
        : null;
    const latest =
      entry.type === "approval_requested" || entry.type === "mention"
        ? detailDto?.latestComment
        : null;
    const replyTarget = latest?.author.name ?? actor?.name ?? null;
    const detail: InboxItemView["detail"] = {
      crumb:
        entry.type === "board_invited" || entry.type === "changes_bundle"
          ? null
          : {
              department,
              board:
                target.kind === "doc"
                  ? target.title
                  : (target.boardName ?? target.title),
              key: target.key,
            },
      event: {
        person: actor ? person(actor) : null,
        segments: eventSegments,
        when,
      },
      card,
      comment: latest
        ? {
            person: person(latest.author),
            time: clock(latest.createdAt),
            text: latest.text,
          }
        : null,
      replyTo:
        entry.type === "approval_requested" || entry.type === "mention"
          ? replyTarget
          : null,
      openHref: deps.hrefFor(target),
    };

    return {
      id: entry.id,
      unread: entry.unread,
      at: entry.createdAt,
      kind: KIND[entry.type],
      person: actor ? person(actor) : null,
      ...(avatarText ? { avatarText } : {}),
      segments,
      context,
      need: entry.need,
      following: entry.following,
      muted: entry.muted,
      done: entry.done,
      itemId: target.kind === "item" ? target.id : null,
      bundle: entry.bundle
        ? {
            count: entry.bundle.children.length,
            summary: "",
            children: entry.bundle.children.map((child) =>
              childView(child, copy),
            ),
          }
        : null,
      href: null,
      detail: entry.type === "changes_bundle" ? null : detail,
    };
  });
}

/**
 * When a snoozed notification returns: an hour from now, tomorrow 08:00 or the
 * next Monday 08:00, in the member's own time zone.
 */
export function snoozeUntil(
  option: "hour" | "tomorrow" | "next-week",
  now: number,
): string {
  if (option === "hour") return new Date(now + 60 * 60 * 1000).toISOString();
  const date = new Date(now);
  date.setHours(8, 0, 0, 0);
  if (option === "tomorrow") {
    date.setDate(date.getDate() + 1);
  } else {
    // Monday is 1; the next Monday is always after today.
    const ahead = (8 - date.getDay()) % 7 || 7;
    date.setDate(date.getDate() + ahead);
  }
  return date.toISOString();
}
