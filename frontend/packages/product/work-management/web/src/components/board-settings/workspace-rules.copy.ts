/**
 * Copy of the workspace automation rules in the board settings, vi and en. It
 * is the `automations.rules` part of the board settings copy (one catalog,
 * `board-settings`). Placeholders use the catalog `{{name}}` form.
 */
export const workspaceRulesEn = {
  title: "Automations",
  intro:
    "A rule does a step for you when something happens on this board. It acts on the item that triggered it and runs with the permission of the person who created it.",
  scope: "Applies to this board",
  create: "New rule",
  loading: "Loading the rules",
  error: "The rules could not be loaded.",
  retry: "Try again",
  forbidden: {
    title: "Only workspace admins manage automations",
    body: "Ask a workspace admin to create or change a rule.",
  },
  empty: "No rule yet.",
  running: "Running",
  off: "Off",
  toggle: "Turn on the rule {{name}}",
  line: "When {{trigger}}, {{action}}",
  triggers: {
    ItemCreated: "an item is created",
    ItemAssigned: "a person is assigned to an item",
    ItemMovedToGroup: "an item is moved to a group",
  },
  actions: {
    move: "move it to {{group}}",
    moveUnknown: "move an item to a group",
    other: "run the action {{action}}",
  },
  triggerMovedTo: "an item is moved to {{group}}",
  groupUnknown: "a group of another board",
  disabledBy: "Turned off by the system: {{reason}}",
  form: {
    title: "New rule",
    name: "Rule name",
    namePlaceholder: "For example: Park the idea when a task is created",
    trigger: "When",
    action: "Then",
    actionMove: "Move an item to a group",
    triggerGroup: "Moved to the group",
    group: "Move it to the group",
    choose: "Choose",
    submit: "Create rule",
    cancel: "Cancel",
    hint: "A new rule is off until you turn it on. The groups are of this board.",
  },
  history: {
    show: "Run history",
    hide: "Hide the run history",
    loading: "Loading the runs",
    error: "The runs could not be loaded.",
    empty: "This rule has not run yet.",
    attempts: "{{count}} attempts",
    status: {
      queued: "Waiting",
      running: "Running",
      succeeded: "Succeeded",
      failed: "Failed",
      cancelled: "Cancelled",
    },
  },
  feedback: {
    limit: "The limit of automation rules in this workspace's plan is reached.",
    invalid: "The rule is not valid. Check the name and the groups.",
    forbidden: "Only workspace admins change automations.",
    failed: "The change could not be saved. Try again.",
  },
} as const;

export const workspaceRulesVi: WorkspaceRulesCopy = {
  title: "Tự động hoá",
  intro:
    "Quy tắc làm thay một bước khi có việc xảy ra trên board này. Quy tắc tác động lên việc đã kích hoạt nó và chạy với quyền của người tạo ra nó.",
  scope: "Áp dụng cho board này",
  create: "Quy tắc mới",
  loading: "Đang tải các quy tắc",
  error: "Không tải được các quy tắc.",
  retry: "Thử lại",
  forbidden: {
    title: "Chỉ quản trị viên workspace quản lý tự động hoá",
    body: "Hãy nhờ một quản trị viên workspace tạo hoặc đổi quy tắc.",
  },
  empty: "Chưa có quy tắc nào.",
  running: "Đang chạy",
  off: "Đang tắt",
  toggle: "Bật quy tắc {{name}}",
  line: "Khi {{trigger}}, {{action}}",
  triggers: {
    ItemCreated: "một việc được tạo",
    ItemAssigned: "một người được giao việc",
    ItemMovedToGroup: "một việc được chuyển sang nhóm khác",
  },
  actions: {
    move: "chuyển việc đó sang {{group}}",
    moveUnknown: "chuyển một việc sang một nhóm",
    other: "chạy hành động {{action}}",
  },
  triggerMovedTo: "một việc được chuyển sang {{group}}",
  groupUnknown: "một nhóm của board khác",
  disabledBy: "Hệ thống đã tắt: {{reason}}",
  form: {
    title: "Quy tắc mới",
    name: "Tên quy tắc",
    namePlaceholder: "Ví dụ: Cất ý tưởng khi có việc mới",
    trigger: "Khi",
    action: "Thì",
    actionMove: "Chuyển một việc sang một nhóm",
    triggerGroup: "Được chuyển sang nhóm",
    group: "Chuyển việc sang nhóm",
    choose: "Chọn",
    submit: "Tạo quy tắc",
    cancel: "Huỷ",
    hint: "Quy tắc mới đang tắt cho đến khi bạn bật. Các nhóm thuộc board này.",
  },
  history: {
    show: "Lịch sử chạy",
    hide: "Ẩn lịch sử chạy",
    loading: "Đang tải các lần chạy",
    error: "Không tải được các lần chạy.",
    empty: "Quy tắc này chưa chạy lần nào.",
    attempts: "{{count}} lần thử",
    status: {
      queued: "Đang chờ",
      running: "Đang chạy",
      succeeded: "Thành công",
      failed: "Thất bại",
      cancelled: "Đã huỷ",
    },
  },
  feedback: {
    limit: "Gói của workspace đã hết số quy tắc tự động hoá được tạo.",
    invalid: "Quy tắc chưa hợp lệ. Kiểm tra tên và các nhóm.",
    forbidden: "Chỉ quản trị viên workspace đổi được tự động hoá.",
    failed: "Chưa lưu được thay đổi. Hãy thử lại.",
  },
};

type Widen<T> = T extends string
  ? string
  : T extends object
    ? { readonly [K in keyof T]: Widen<T[K]> }
    : T;

export type WorkspaceRulesCopy = Widen<typeof workspaceRulesEn>;
