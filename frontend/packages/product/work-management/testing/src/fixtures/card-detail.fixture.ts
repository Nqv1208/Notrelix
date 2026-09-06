import type {
  CardActivity,
  CardComment,
  CardDetail,
  CardFile,
  CardUpdate,
} from "@notrelix/work-management-core";
import { cardFixture } from "./card.fixture";
import { memberFixture } from "./member.fixture";
import { fixedIso } from "../support/fixed-clock";

export function commentFixture(
  overrides: Partial<CardComment> = {},
): CardComment {
  return {
    id: "comment-test",
    cardId: "card-test",
    author: "Avery Stone",
    body: "Looks ready for review.",
    createdAt: fixedIso(),
    ...overrides,
  };
}

export function updateFixture(overrides: Partial<CardUpdate> = {}): CardUpdate {
  return {
    id: "update-test",
    cardId: "card-test",
    author: memberFixture(),
    body: "Updated the owner-local UI scenario.",
    mentionUserIds: [],
    attachmentIds: [],
    createdAt: fixedIso(),
    ...overrides,
  };
}

export function activityFixture(
  overrides: Partial<CardActivity> = {},
): CardActivity {
  return {
    id: "activity-test",
    cardId: "card-test",
    actor: "Avery Stone",
    action: "changed status",
    type: "updated",
    createdAt: fixedIso(),
    ...overrides,
  };
}

export function fileFixture(overrides: Partial<CardFile> = {}): CardFile {
  const member = memberFixture();
  return {
    id: "file-test",
    cardId: "card-test",
    name: "brief.pdf",
    size: 42_000,
    contentType: "application/pdf",
    url: "https://example.invalid/brief.pdf",
    source: "link",
    createdBy: member,
    createdAt: fixedIso(),
    ...overrides,
  };
}

export function cardDetailFixture(
  overrides: Partial<CardDetail> = {},
): CardDetail {
  return {
    ...cardFixture({
      descriptionMd: "Detailed execution task",
      members: [memberFixture()],
      _count: { comments: 1, attachments: 1, checklistItems: 1 },
    }),
    boardTitle: "Execution Board",
    watchers: [memberFixture({ id: "watcher-test", userId: "user-watcher" })],
    isWatched: true,
    updates: [updateFixture()],
    files: [fileFixture()],
    activity: [activityFixture()],
    ...overrides,
  };
}
