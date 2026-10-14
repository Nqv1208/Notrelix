import {
  inboxDayLabel,
  type InboxItemView,
  type InboxKind,
  type WorkspaceInboxCopy,
} from "@notrelix/features-workspace/web";

/**
 * A notification as the producer returns it. The generated contract has none
 * (gap register), the mock and the app's own type disagree on the text field
 * (`message` and `body`), so both are read; `type` and `link` are optional.
 */
export interface InboxNotification {
  readonly id: string;
  readonly title: string;
  readonly body?: string | null;
  readonly message?: string | null;
  readonly isRead: boolean;
  readonly createdAt: string;
  readonly type?: string | null;
}

const KIND_BY_TYPE: Readonly<Record<string, InboxKind>> = {
  mention: "mention",
  assignment: "assign",
  comment: "comment",
  status_change: "change",
  invitation: "invite",
  system: "system",
};

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

/**
 * The member's notifications as the Inbox draws them. The read carries a
 * title, a text, a read flag and a time, so a row has a bold title and the
 * text, no person, no item context, no "needs" chip and no reading-pane card;
 * everything else the design draws is left out rather than guessed.
 */
export function toInboxItems(
  notifications: readonly InboxNotification[],
  copy: WorkspaceInboxCopy,
  now: number,
): InboxItemView[] {
  return [...notifications]
    .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
    .map((notification) => {
      const text = (notification.body ?? notification.message ?? "").trim();
      const segments = [
        { text: notification.title, strong: true },
        ...(text ? [{ text: ` ${text}` }] : []),
      ];
      const date = new Date(notification.createdAt);
      const when = Number.isNaN(date.getTime())
        ? ""
        : `${inboxDayLabel(copy, notification.createdAt, now)} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
      return {
        id: notification.id,
        unread: !notification.isRead,
        at: notification.createdAt,
        kind: KIND_BY_TYPE[notification.type ?? ""] ?? "system",
        person: null,
        segments,
        context: null,
        need: null,
        following: false,
        bundle: null,
        href: null,
        detail: {
          crumb: null,
          event: { person: null, segments, when },
          card: null,
          comment: null,
          replyTo: null,
          openHref: null,
        },
      };
    });
}
