/**
 * Boards and Docs directory copy (WX-DIR-001, WX-DEC-008), vi and en.
 * Registered as the `workspace-library` catalog.
 */
export const en = {
  title: "Boards & Docs",
  description: "Every Board and Doc you can open in {{workspace}}.",
  descriptionLoading: "Every Board and Doc you can open in this workspace.",
  newBoard: "New Board",
  newDoc: "New Doc",
  search: {
    label: "Filter by name or location",
    placeholder: "Filter Boards and Docs…",
    clear: "Clear filter",
  },
  types: {
    label: "Show",
    all: "All",
    board: "Boards",
    doc: "Docs",
  },
  spaces: {
    label: "Space",
    all: "All Spaces",
    none: "No Space",
  },
  columns: {
    select: "Select",
    name: "Name",
    type: "Type",
    location: "Location",
    updated: "Updated",
    pinned: "Pinned",
  },
  kind: {
    board: "Board",
    doc: "Doc",
  },
  sort: {
    ascending: "sorted ascending",
    descending: "sorted descending",
    by: "Sort by {{column}}",
  },
  row: {
    select: "Select {{name}}",
    pin: "Pin {{name}}",
    unpin: "Unpin {{name}}",
    move: "Move {{name}} to a Space",
    pinning: "Updating pin for {{name}}…",
    rootLocation: "Workspace",
    unknownUpdated: "Not reported",
  },
  selection: {
    selectAll: "Select all shown",
    count_one: "{{count}} selected",
    count_other: "{{count}} selected",
    pin: "Pin",
    unpin: "Unpin",
    clear: "Clear selection",
  },
  results_one: "{{count}} result",
  results_other: "{{count}} results",
  keyboardHint:
    "Use ↑ and ↓ to move between rows, Space to select, Enter to open.",
  empty: {
    title: "No Boards or Docs yet",
    description:
      "Create a Board to track work or a Doc to write things down. Everything you can open in this workspace is listed here.",
    readOnly: "Nothing has been shared with you in this workspace yet.",
  },
  noResults: {
    title: "Nothing matches",
    description: "No Board or Doc matches the current filter.",
    clear: "Clear filters",
  },
  error: {
    title: "Boards and Docs couldn't load",
    description:
      "Your data is unchanged. Check your connection, then try again.",
    retry: "Try again",
  },
  toasts: {
    pinned_one: "Pinned {{count}} item.",
    pinned_other: "Pinned {{count}} items.",
    unpinned_one: "Unpinned {{count}} item.",
    unpinned_other: "Unpinned {{count}} items.",
    pinFailed: "Some pins couldn't be updated.",
    pinFailedHint: "The pin list shows what was saved. Try again.",
  },
};

export type WorkspaceLibraryCopy = typeof en;

export const vi: WorkspaceLibraryCopy = {
  title: "Bảng & Tài liệu",
  description: "Mọi Bảng và Tài liệu bạn mở được trong {{workspace}}.",
  descriptionLoading:
    "Mọi Bảng và Tài liệu bạn mở được trong không gian làm việc này.",
  newBoard: "Bảng mới",
  newDoc: "Tài liệu mới",
  search: {
    label: "Lọc theo tên hoặc vị trí",
    placeholder: "Lọc Bảng và Tài liệu…",
    clear: "Xóa bộ lọc",
  },
  types: {
    label: "Hiển thị",
    all: "Tất cả",
    board: "Bảng",
    doc: "Tài liệu",
  },
  spaces: {
    label: "Không gian",
    all: "Mọi Không gian",
    none: "Không thuộc Không gian",
  },
  columns: {
    select: "Chọn",
    name: "Tên",
    type: "Loại",
    location: "Vị trí",
    updated: "Cập nhật",
    pinned: "Đã ghim",
  },
  kind: {
    board: "Bảng",
    doc: "Tài liệu",
  },
  sort: {
    ascending: "sắp xếp tăng dần",
    descending: "sắp xếp giảm dần",
    by: "Sắp xếp theo {{column}}",
  },
  row: {
    select: "Chọn {{name}}",
    pin: "Ghim {{name}}",
    unpin: "Bỏ ghim {{name}}",
    move: "Chuyển {{name}} vào một Không gian",
    pinning: "Đang cập nhật ghim cho {{name}}…",
    rootLocation: "Không gian làm việc",
    unknownUpdated: "Chưa có thông tin",
  },
  selection: {
    selectAll: "Chọn tất cả đang hiển thị",
    count_one: "Đã chọn {{count}}",
    count_other: "Đã chọn {{count}}",
    pin: "Ghim",
    unpin: "Bỏ ghim",
    clear: "Bỏ chọn",
  },
  results_one: "{{count}} kết quả",
  results_other: "{{count}} kết quả",
  keyboardHint:
    "Dùng ↑ và ↓ để di chuyển giữa các hàng, Space để chọn, Enter để mở.",
  empty: {
    title: "Chưa có Bảng hoặc Tài liệu nào",
    description:
      "Tạo một Bảng để theo dõi công việc hoặc một Tài liệu để ghi chép. Mọi thứ bạn mở được trong không gian làm việc này đều được liệt kê ở đây.",
    readOnly:
      "Chưa có nội dung nào được chia sẻ với bạn trong không gian làm việc này.",
  },
  noResults: {
    title: "Không có kết quả phù hợp",
    description: "Không có Bảng hoặc Tài liệu nào khớp với bộ lọc hiện tại.",
    clear: "Xóa bộ lọc",
  },
  error: {
    title: "Không tải được Bảng và Tài liệu",
    description:
      "Dữ liệu của bạn không thay đổi. Kiểm tra kết nối rồi thử lại.",
    retry: "Thử lại",
  },
  toasts: {
    pinned_one: "Đã ghim {{count}} mục.",
    pinned_other: "Đã ghim {{count}} mục.",
    unpinned_one: "Đã bỏ ghim {{count}} mục.",
    unpinned_other: "Đã bỏ ghim {{count}} mục.",
    pinFailed: "Không cập nhật được một số ghim.",
    pinFailedHint: "Danh sách ghim hiển thị những gì đã lưu. Hãy thử lại.",
  },
};

export function workspaceLibraryCopy(
  locale: "vi" | "en",
): WorkspaceLibraryCopy {
  return locale === "en" ? en : vi;
}
