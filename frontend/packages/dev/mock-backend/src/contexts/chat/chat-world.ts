/**
 * The Chat world of the default mock store: one Workspace (the primary one) with
 * public and private channels, two channel groups, an archived and a deleted
 * channel, a few hundred messages in `general` (paging), tombstones, edits, a
 * reply, system messages and read state (unread counts, one muted space).
 *
 * The people are the mock personas. A persona who is not a member of the
 * primary Workspace in this world (the `commenter` persona outside its own
 * world) is left out of every channel.
 */

import type { MockStore } from "../../state/mock-store";
import { mockIds } from "../../state/mock-ids";
import { chatGuid, type MockChatState } from "./chat-state";
import {
  CHAT_SYSTEM_TEXT,
  type ChatMembershipRole,
  type ChatNotificationLevel,
  type ChatSpaceKind,
} from "./chat-model";

/** Stable ids of the seeded rows (GUID-shaped, like the backend's). */
export const CHAT_SEED_IDS = {
  groups: {
    team: chatGuid("group", 1),
    projects: chatGuid("group", 2),
  },
  spaces: {
    general: chatGuid("space", 1),
    engineering: chatGuid("space", 2),
    leadership: chatGuid("space", 3),
    erpRollout: chatGuid("space", 4),
    random: chatGuid("space", 5),
    launch2026: chatGuid("space", 6),
    oldNotes: chatGuid("space", 7),
  },
} as const;

/** The number of messages seeded in `general`: enough pages to exercise paging. */
export const GENERAL_MESSAGE_COUNT = 320;

interface MembershipSeed {
  readonly userId: string;
  readonly role: ChatMembershipRole;
  /** The read boundary at the seed: messages after it are unread. */
  readonly read: number;
  readonly level?: ChatNotificationLevel;
  readonly mutedUntil?: string | null;
  readonly joinedDaysAgo?: number;
}

interface MessageSeed {
  readonly author: string;
  readonly text?: string;
  /** A system message announces a change; `text` is then the announcement. */
  readonly system?: boolean;
  /** The text the message was edited to (the edit happened a minute after it was sent). */
  readonly editedTo?: string;
  readonly deleted?: boolean;
  /** The sequence of the quoted message in the same space. */
  readonly replyToSequence?: number;
}

interface SpaceSeed {
  readonly id: string;
  readonly name: string;
  readonly topic: string | null;
  readonly kind: ChatSpaceKind;
  readonly groupId: string | null;
  readonly position: number;
  readonly version: number;
  readonly createdDaysAgo: number;
  readonly archivedMinutesAgo?: number;
  readonly deletedDaysAgo?: number;
  readonly members: readonly MembershipSeed[];
  readonly messages: readonly MessageSeed[];
}

const { owner, admin, member, viewer } = mockIds.users;

/** The authors of `general` in turn: the owner, the admin, the member and the Guest. */
const GENERAL_AUTHORS = [owner, admin, member, viewer] as const;

const GENERAL_LINES = [
  "Mình vừa cập nhật lịch họp tuần này.",
  "Ai đã xem tài liệu kế hoạch chưa?",
  "Bản build mới đã lên staging.",
  "Cảm ơn mọi người đã hỗ trợ hôm qua.",
  "Có ai rảnh chiều nay để review không?",
  "Mình đã đóng ticket số 128.",
  "Nhớ điền báo cáo trước 17:00 nhé.",
] as const;

/** The general channel's messages: 320 of them, with the edges the paging and the rules need. */
function generalMessages(): MessageSeed[] {
  const messages: MessageSeed[] = [];
  for (let sequence = 1; sequence <= GENERAL_MESSAGE_COUNT; sequence += 1) {
    const author = GENERAL_AUTHORS[(sequence - 1) % GENERAL_AUTHORS.length]!;
    const line = GENERAL_LINES[sequence % GENERAL_LINES.length]!;
    const base: MessageSeed = { author, text: `Tin nhắn ${sequence}. ${line}` };
    if (sequence === 1) {
      messages.push({
        author: owner,
        text: "Chào mọi người, đây là kênh chung của Minh An.",
      });
    } else if (sequence === 40 || sequence === 77 || sequence === 150) {
      messages.push({ ...base, deleted: true });
    } else if (sequence === 60) {
      messages.push({ ...base, editedTo: `${base.text} (đã sửa)` });
    } else if (sequence === 200) {
      messages.push({
        ...base,
        editedTo: `${base.text} Đã cập nhật nội dung.`,
      });
    } else if (sequence === 90) {
      messages.push({ ...base, replyToSequence: 88 });
    } else if (sequence === 100) {
      messages.push({
        author: admin,
        text: CHAT_SYSTEM_TEXT.memberJoined,
        system: true,
      });
    } else {
      messages.push(base);
    }
  }
  return messages;
}

const SPACES: readonly SpaceSeed[] = [
  {
    id: CHAT_SEED_IDS.spaces.general,
    name: "general",
    topic: "Thông báo và trao đổi chung",
    kind: "channel_public",
    groupId: null,
    position: 1,
    version: 3,
    createdDaysAgo: 30,
    members: [
      // The member who is the owner has read 20 messages of general short of the end.
      { userId: owner, role: "manager", read: 300, joinedDaysAgo: 30 },
      { userId: admin, role: "member", read: 310, joinedDaysAgo: 29 },
      {
        userId: member,
        role: "member",
        read: GENERAL_MESSAGE_COUNT,
        joinedDaysAgo: 20,
      },
      { userId: viewer, role: "member", read: 0, joinedDaysAgo: 10 },
    ],
    messages: generalMessages(),
  },
  {
    id: CHAT_SEED_IDS.spaces.engineering,
    name: "engineering",
    topic: "Kỹ thuật",
    kind: "channel_public",
    groupId: CHAT_SEED_IDS.groups.team,
    position: 2,
    version: 2,
    createdDaysAgo: 25,
    members: [
      // The owner muted engineering for good: no badge, no unread frame (CHT-019).
      {
        userId: owner,
        role: "manager",
        read: 4,
        level: "none",
        mutedUntil: null,
        joinedDaysAgo: 25,
      },
      { userId: admin, role: "member", read: 6, joinedDaysAgo: 24 },
      { userId: member, role: "member", read: 2, joinedDaysAgo: 20 },
    ],
    messages: [
      { author: owner, text: "Build đã xanh chưa?" },
      { author: admin, text: "Xanh rồi, merge được." },
      { author: member, text: "Mình sửa lỗi kiểm thử rồi." },
      { author: owner, text: "Bản nháp cũ", deleted: true },
      { author: admin, text: "Ai review PR giúp mình?" },
      { author: member, text: "Mình review." },
    ],
  },
  {
    id: CHAT_SEED_IDS.spaces.leadership,
    name: "leadership",
    topic: null,
    kind: "channel_private",
    groupId: null,
    position: 3,
    version: 1,
    createdDaysAgo: 18,
    // The Owner is not a member: a private channel is not seen through the role (CHT-004).
    members: [
      { userId: admin, role: "manager", read: 4, joinedDaysAgo: 18 },
      { userId: member, role: "member", read: 2, joinedDaysAgo: 18 },
    ],
    messages: [
      { author: admin, text: "Họp ban lãnh đạo lúc 14:00 thứ Năm." },
      { author: member, text: "Đã ghi nhận." },
      { author: admin, text: "Tài liệu ở trang Kế hoạch quý 4." },
      { author: admin, text: CHAT_SYSTEM_TEXT.memberJoined, system: true },
    ],
  },
  {
    id: CHAT_SEED_IDS.spaces.erpRollout,
    name: "erp-rollout",
    topic: "Triển khai ERP Q4",
    kind: "channel_public",
    groupId: CHAT_SEED_IDS.groups.projects,
    position: 4,
    version: 1,
    createdDaysAgo: 14,
    members: [
      { userId: owner, role: "manager", read: 2, joinedDaysAgo: 14 },
      { userId: member, role: "member", read: 1, joinedDaysAgo: 14 },
      // A Guest added to a public channel: the one channel the Guest sees besides general.
      { userId: viewer, role: "member", read: 0, joinedDaysAgo: 5 },
    ],
    messages: [
      { author: owner, text: "Kickoff ERP: 16 mục trên bảng." },
      { author: member, text: "Mình nhận nhóm Kế toán." },
      {
        author: owner,
        text: "Đã chốt lịch demo thứ Sáu.",
        editedTo: "Đã chốt lịch demo thứ Sáu tuần sau.",
      },
      { author: viewer, text: "Có cần mình chuẩn bị gì không?" },
      { author: member, text: "Chuẩn bị bản nháp giúp mình nhé." },
    ],
  },
  {
    id: CHAT_SEED_IDS.spaces.random,
    name: "random",
    topic: null,
    kind: "channel_public",
    groupId: null,
    position: 5,
    version: 1,
    createdDaysAgo: 30,
    members: [
      { userId: owner, role: "manager", read: 3, joinedDaysAgo: 30 },
      { userId: admin, role: "member", read: 1, joinedDaysAgo: 29 },
    ],
    messages: [
      { author: owner, text: "Ai có ảnh nào hay không?" },
      { author: admin, text: CHAT_SYSTEM_TEXT.memberJoined, system: true },
      { author: admin, text: "Có này." },
    ],
  },
  {
    id: CHAT_SEED_IDS.spaces.launch2026,
    name: "launch-2026",
    topic: "Ra mắt quý 4",
    kind: "channel_public",
    groupId: null,
    position: 6,
    version: 4,
    createdDaysAgo: 40,
    archivedMinutesAgo: 120,
    members: [
      { userId: admin, role: "manager", read: 0, joinedDaysAgo: 40 },
      { userId: owner, role: "member", read: 4, joinedDaysAgo: 40 },
    ],
    messages: [
      { author: admin, text: "Kế hoạch ra mắt." },
      { author: owner, text: "Đã xem." },
      { author: admin, text: "Mốc ngày 30/11." },
      { author: admin, text: CHAT_SYSTEM_TEXT.archived, system: true },
    ],
  },
  {
    id: CHAT_SEED_IDS.spaces.oldNotes,
    name: "old-notes",
    topic: null,
    kind: "channel_public",
    groupId: null,
    position: 7,
    version: 1,
    createdDaysAgo: 60,
    deletedDaysAgo: 2,
    members: [{ userId: owner, role: "manager", read: 2, joinedDaysAgo: 60 }],
    messages: [
      { author: owner, text: "Ghi chú cũ" },
      { author: owner, text: "Đã xoá", deleted: true },
    ],
  },
];

const GROUPS = [
  { id: CHAT_SEED_IDS.groups.team, name: "Team", position: 1 },
  { id: CHAT_SEED_IDS.groups.projects, name: "Dự án", position: 2 },
] as const;

/**
 * Writes the Chat world into a freshly seeded store. Runs after the Workspace
 * memberships exist, so only people who are members of the primary Workspace
 * get a channel membership.
 */
export function seedChatWorld(state: MockChatState, store: MockStore): void {
  const clock = store.getClock();
  const workspaceId = mockIds.workspaces.primary;
  const workspaceMembers = new Set(
    store.getWorkspaceMembers(workspaceId).map((row) => row.userId),
  );

  for (const group of GROUPS) {
    state.groups.set(group.id, {
      id: group.id,
      workspaceId,
      name: group.name,
      position: group.position,
      version: 1,
      createdAt: clock.offsetDays(-45),
    });
  }

  for (const seed of SPACES) {
    const messageCount = seed.messages.length;
    const ids: string[] = [];
    state.spaces.set(seed.id, {
      id: seed.id,
      workspaceId,
      kind: seed.kind,
      name: seed.name,
      topic: seed.topic,
      groupId: seed.groupId,
      position: seed.position,
      archivedAt:
        seed.archivedMinutesAgo === undefined
          ? null
          : clock.offsetMinutes(-seed.archivedMinutesAgo),
      deletedAt:
        seed.deletedDaysAgo === undefined
          ? null
          : clock.offsetDays(-seed.deletedDaysAgo),
      lastSequence: messageCount,
      version: seed.version,
      createdAt: clock.offsetDays(-seed.createdDaysAgo),
      createdBy: seed.members[0]?.userId ?? owner,
    });

    seed.messages.forEach((message, index) => {
      const sequence = index + 1;
      // The row is numbered by its position in the seed; the clock runs a few minutes before now.
      const createdAt = clock.offsetMinutes(
        -(messageCount - sequence) * 3 - 60,
      );
      const id = state.nextId("message");
      ids.push(id);
      const isSystem = message.system === true;
      const edited = message.editedTo !== undefined && !message.deleted;
      const deleted = message.deleted === true;
      state.appendMessage({
        id,
        workspaceId,
        spaceId: seed.id,
        sequence,
        authorId: message.author,
        kind: isSystem ? "system" : "user",
        content: deleted
          ? null
          : edited
            ? message.editedTo!
            : (message.text ?? null),
        clientMessageId: isSystem ? null : `seed-${seed.name}-${sequence}`,
        replyToId:
          message.replyToSequence === undefined
            ? null
            : (ids[message.replyToSequence - 1] ?? null),
        createdAt,
        editedAt: edited
          ? clock.offsetMinutes(-(messageCount - sequence) * 3 - 59)
          : null,
        deletedAt: deleted
          ? clock.offsetMinutes(-(messageCount - sequence) * 3 - 58)
          : null,
        deletedBy: deleted ? message.author : null,
        version: edited || deleted ? 2 : 1,
      });
    });

    for (const row of seed.members) {
      if (!workspaceMembers.has(row.userId)) continue;
      state.memberships.set(state.membershipKey(seed.id, row.userId), {
        spaceId: seed.id,
        workspaceId,
        userId: row.userId,
        role: row.role,
        notificationLevel: row.level ?? "all",
        mutedUntil: row.mutedUntil ?? null,
        readSequence: row.read,
        mentionCount: 0,
        joinedAt: clock.offsetDays(-(row.joinedDaysAgo ?? seed.createdDaysAgo)),
        leftAt: null,
      });
    }
  }

  state.reserveIds("space", SPACES.length);
  state.reserveIds("group", GROUPS.length);
}
