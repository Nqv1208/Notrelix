/**
 * Board header and view bar copy, vi and en, registered as the `board-header`
 * catalog (`board-header.catalog.ts`). The pure surfaces take a `copy` prop;
 * `useBoardHeaderCopy` resolves it for the active locale. Counts use the
 * `_one`/`_other` forms (Vietnamese keeps both complete); placeholders use the
 * catalog `{{name}}` form and are filled by `fillBoardHeaderCopy`.
 */
export const en = {
  /** Accessible name of the view tab list. */
  boardViews: "Views",
  views: {
    table: "Table",
    kanban: "Kanban",
    calendar: "Calendar",
    timeline: "Timeline",
  },
  /** Heading shown while the board title is not loaded. */
  untitledBoard: "Board",
  addView: "Add view",
  /** Marks a saved view only its creator sees. */
  privateView: "Private view",
  header: {
    settings: "Settings",
    settingsUnavailable: "Board settings aren't available yet.",
    share: "Share",
    shareUnavailable: "Sharing this board isn't available yet.",
    moreOptions: "More options",
    copyLink: "Copy board link",
    deletedItems: "Deleted items",
    formResponses: "Form responses",
    renameBoard: "Rename board…",
    archiveBoard: "Archive board…",
    deleteBoard: "Delete board…",
    /** Access chip wording by board visibility. */
    scope: {
      space: "{{space}}",
      workspace: "Whole workspace",
      private: "Private",
    },
    guests_one: "{{scope}} · {{count}} guest",
    guests_other: "{{scope}} · {{count}} guests",
    /** Accessible name of the avatar stack. */
    people_one: "{{count}} person can open this board",
    people_other: "{{count}} people can open this board",
    peopleWithSources_one: "{{count}} person can open this board: {{sources}}",
    peopleWithSources_other:
      "{{count}} people can open this board: {{sources}}",
    /** When the count is a lower bound (not every source could be counted). */
    peopleAtLeast_one: "At least {{count}} person can open this board",
    peopleAtLeast_other: "At least {{count}} people can open this board",
    peopleAtLeastWithSources_one:
      "At least {{count}} person can open this board: {{sources}}",
    peopleAtLeastWithSources_other:
      "At least {{count}} people can open this board: {{sources}}",
    sourceGroup_one: "{{count}} person in {{name}}",
    sourceGroup_other: "{{count}} people in {{name}}",
    sourceGroupUnnamed_one: "{{count}} other person",
    sourceGroupUnnamed_other: "{{count}} other people",
    sourceUserUnnamed_one: "{{count}} person added directly",
    sourceUserUnnamed_other: "{{count}} people added directly",
    sourceGuest_one: "{{count}} guest",
    sourceGuest_other: "{{count}} guests",
    listSeparator: ", ",
    listPairSeparator: " and ",
    listLastSeparator: ", and ",
    moreAvatars: "+{{count}}",
    /** The archived-board banner: read-only for everyone, Unarchive for a manager. */
    archived: {
      banner:
        "This board is archived. It is read-only until a manager unarchives it.",
      bannerManager:
        "This board is archived and read-only. Unarchive it to let people write again.",
      unarchive: "Unarchive",
      unarchiving: "Unarchiving…",
      conflict:
        "Someone changed the board first. It was reloaded, check it and try again.",
      denied: "Only a manager can unarchive this board.",
      failed: "The board could not be unarchived. Nothing changed, try again.",
    },
    /** Read-only banner by effective level. */
    banner: {
      commenter: "You can comment on this board",
      viewer: "You can view this board",
      grantedBy: "{{level}} · added directly by {{name}}",
    },
  },
};

export type BoardHeaderCopy = typeof en;

export const vi: BoardHeaderCopy = {
  boardViews: "Chế độ xem",
  views: {
    table: "Bảng",
    kanban: "Kanban",
    calendar: "Lịch",
    timeline: "Dòng thời gian",
  },
  untitledBoard: "Board",
  addView: "Thêm chế độ xem",
  privateView: "Chế độ xem riêng tư",
  header: {
    settings: "Thiết lập",
    settingsUnavailable: "Chưa có trang thiết lập board.",
    share: "Chia sẻ",
    shareUnavailable: "Chưa có trang chia sẻ board.",
    moreOptions: "Thêm tuỳ chọn",
    copyLink: "Sao chép liên kết board",
    deletedItems: "Việc đã xoá",
    formResponses: "Phản hồi biểu mẫu",
    renameBoard: "Đổi tên board…",
    archiveBoard: "Lưu trữ board…",
    deleteBoard: "Xoá board…",
    scope: {
      space: "{{space}}",
      workspace: "Cả workspace",
      private: "Riêng tư",
    },
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    guests_one: "{{scope}} · có {{count}} khách",
    guests_other: "{{scope}} · có {{count}} khách",
    people_one: "{{count}} người vào được board",
    people_other: "{{count}} người vào được board",
    peopleWithSources_one: "{{count}} người vào được board: {{sources}}",
    peopleWithSources_other: "{{count}} người vào được board: {{sources}}",
    peopleAtLeast_one: "Ít nhất {{count}} người vào được board",
    peopleAtLeast_other: "Ít nhất {{count}} người vào được board",
    peopleAtLeastWithSources_one:
      "Ít nhất {{count}} người vào được board: {{sources}}",
    peopleAtLeastWithSources_other:
      "Ít nhất {{count}} người vào được board: {{sources}}",
    sourceGroup_one: "{{count}} người {{name}}",
    sourceGroup_other: "{{count}} người {{name}}",
    sourceGroupUnnamed_one: "{{count}} người khác",
    sourceGroupUnnamed_other: "{{count}} người khác",
    sourceUserUnnamed_one: "{{count}} người được thêm riêng",
    sourceUserUnnamed_other: "{{count}} người được thêm riêng",
    sourceGuest_one: "{{count}} khách",
    sourceGuest_other: "{{count}} khách",
    listSeparator: ", ",
    listPairSeparator: " và ",
    listLastSeparator: " và ",
    moreAvatars: "+{{count}}",
    archived: {
      banner:
        "Board này đã được lưu trữ. Board chỉ đọc cho đến khi quản lý bỏ lưu trữ.",
      bannerManager:
        "Board này đã lưu trữ và chỉ đọc. Bỏ lưu trữ để mọi người ghi lại được.",
      unarchive: "Bỏ lưu trữ",
      unarchiving: "Đang bỏ lưu trữ…",
      conflict:
        "Người khác vừa thay đổi board trước. Board đã được tải lại, hãy xem rồi thử lại.",
      denied: "Chỉ quản lý mới bỏ lưu trữ được board này.",
      failed: "Chưa bỏ lưu trữ được board. Chưa có gì thay đổi, hãy thử lại.",
    },
    banner: {
      commenter: "Bạn có quyền bình luận trên board này",
      viewer: "Bạn có quyền xem board này",
      grantedBy: "{{level}} · được {{name}} thêm riêng",
    },
  },
};

export function boardHeaderCopy(locale: "vi" | "en"): BoardHeaderCopy {
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; unknown names stay as written. */
export function fillBoardHeaderCopy(
  template: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return template.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}
