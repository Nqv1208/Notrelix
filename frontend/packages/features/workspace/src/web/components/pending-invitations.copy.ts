/**
 * Pending invitations copy, vi and en, registered as the `workspace-pending-invitations` catalog
 * (`pending-invitations.catalog.ts`). Counts use the `_one`/`_other` forms (Vietnamese keeps both
 * complete); placeholders use `{{name}}`.
 */
export const en = {
  trigger: "Pending workspace invitations",
  label: "Invites",
  waiting_one: "{{count}} invitation is waiting",
  waiting_other: "{{count}} invitations are waiting",
  title: "Invitations",
  loading: "Loading invitations…",
  loadError: "We couldn't load your invitations.",
  retry: "Try again",
  emptyTitle: "No pending invitations",
  emptyText: "When someone invites you to a workspace, it will appear here.",
  invitedBy: "{{inviter}} invited you as {{role}}",
  invited: "You were invited as {{role}}",
  expires: "Expires {{date}}",
  roles: {
    owner: "Owner",
    admin: "Admin",
    member: "Member",
    guest: "Guest",
  },
  accept: "Accept",
  decline: "Decline",
  acceptLabel: "Accept the invitation to {{workspace}}",
  declineLabel: "Decline the invitation to {{workspace}}",
  working: "Working…",
  errors: {
    gone: "That invitation is no longer available.",
    refused: "We couldn't accept that invitation: {{reason}}",
    failed: "We couldn't do that. Try again.",
  },
};

export type PendingInvitationsCopy = typeof en;

export const vi: PendingInvitationsCopy = {
  trigger: "Lời mời vào workspace đang chờ",
  label: "Lời mời",
  // Vietnamese has no singular/plural distinction; both forms stay complete.
  waiting_one: "Có {{count}} lời mời đang chờ",
  waiting_other: "Có {{count}} lời mời đang chờ",
  title: "Lời mời",
  loading: "Đang tải lời mời…",
  loadError: "Không thể tải lời mời của bạn.",
  retry: "Thử lại",
  emptyTitle: "Chưa có lời mời nào",
  emptyText: "Khi có người mời bạn vào một workspace, lời mời sẽ hiện ở đây.",
  invitedBy: "{{inviter}} mời bạn với vai trò {{role}}",
  invited: "Bạn được mời với vai trò {{role}}",
  expires: "Hết hạn {{date}}",
  roles: {
    owner: "Chủ sở hữu",
    admin: "Quản trị viên",
    member: "Thành viên",
    guest: "Khách",
  },
  accept: "Chấp nhận",
  decline: "Từ chối",
  acceptLabel: "Chấp nhận lời mời vào {{workspace}}",
  declineLabel: "Từ chối lời mời vào {{workspace}}",
  working: "Đang xử lý…",
  errors: {
    gone: "Lời mời này không còn nữa.",
    refused: "Chưa chấp nhận được lời mời: {{reason}}",
    failed: "Chưa làm được việc này. Hãy thử lại.",
  },
};

export function pendingInvitationsCopy(
  locale: "vi" | "en",
): PendingInvitationsCopy {
  return locale === "vi" ? vi : en;
}
