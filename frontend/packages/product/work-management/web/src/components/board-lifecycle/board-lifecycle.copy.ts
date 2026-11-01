/**
 * Board lifecycle dialogs copy (move with access impact, duplicate, import,
 * export), vi and en. Plain data with no imports; `board-lifecycle.catalog.ts`
 * registers it as the `board-lifecycle` catalog. `{{name}}` placeholders are
 * filled with `fillLifecycleCopy`.
 */
export const en = {
  move: {
    loading: "Working out who gains and who loses access…",
    failed: "The impact could not be read. Nothing was moved.",
    retry: "Try again",
    losing_one: "{{count}} person loses access to the board",
    losing_other: "{{count}} people lose access to the board",
    losingEveryone:
      "Everyone outside {{target}} who was not added to the board loses access",
    losingBody:
      "They reach the board only through {{from}}. After the move the board is “Not found” to them.",
    losingBodyRoot:
      "They reach the board only because it sits at the top of the workspace. After the move the board is “Not found” to them.",
    strandedTitle_one: "{{count}} person who loses access has unfinished items",
    strandedTitle_other:
      "{{count}} people who lose access have unfinished items",
    strandedBody:
      "{{items}} items, {{overdue}} overdue. Add them to the board before the move so the work is not left behind; nobody is removed from an item.",
    stranded_one: "{{count}} unfinished item",
    stranded_other: "{{count}} unfinished items",
    strandedOverdue: "{{count}} overdue",
    noOwnAccess: "no access of their own",
    moreStranded: "and {{count}} more",
    noneStranded: "Nobody with unfinished items loses access",
    noneStrandedBody: "You can move it now.",
    gaining_one: "{{count}} person gains access to the board",
    gaining_other: "{{count}} people gain access to the board",
    gainingEveryone:
      "Everyone in the workspace who is not a guest reaches the board",
    gainingBody:
      "People in {{target}} reach it by their role there: Admin is Manager, Member is Editor, Viewer is Viewer.",
    kept: "Access given on the board itself stays",
    keptBody: "{{names}}. Their items stay as they are.",
    keptMore: "{{names}} and {{count}} more",
    noChange: "Nobody gains or loses access.",
    moveWithout: "Move without adding anyone",
    moveAdding_one: "Add {{count}} person, then move",
    moveAdding_other: "Add {{count}} people, then move",
    moving: "Moving…",
    moved: "{{name}} was moved.",
    movedAdded_one: "{{name}} was moved and {{count}} person was added.",
    movedAdded_other: "{{name}} was moved and {{count}} people were added.",
  },
  duplicate: {
    title: "Duplicate board",
    intro: "Copy the structure of “{{name}}” into a new board.",
    nameLabel: "Name of the copy",
    defaultName: "{{name}} (copy)",
    nameRequired: "Give the copy a name.",
    placeLabel: "Place in",
    placeRoot: "No Space (workspace level)",
    placeHint: "Defaults to where the original board sits.",
    alwaysTitle: "Always copied",
    alwaysBody:
      "Fields, groups, views, labels and the board settings. The copy is its own board with its own key and item numbers.",
    itemsTitle: "Copy the items too",
    itemsCount_one: "{{count}} item",
    itemsCount_other: "{{count}} items",
    itemsHint: "Off by default: the copy has the structure but no items.",
    itemsOn:
      "The items come with their group and order; each gets a new key in the copy. Comments, history and files are not copied. At most {{max}} items.",
    tooMany:
      "This board has {{count}} items, over the limit of {{max}} for a copy with items. Duplicate without items, then use Import to bring the items over.",
    dropped_one:
      "{{count}} assignment goes: that person has no access to the new board and is removed from the copied item.",
    dropped_other:
      "{{count}} assignments go: those people have no access to the new board and are removed from the copied items.",
    membersTitle: "Members",
    membersBody:
      "Not copied. You are the Manager of the copy; invite others with Share.",
    automationsTitle: "Automations",
    automationsBody: "Not copied. The copy starts with no rules.",
    fixed: "Fixed",
    footer:
      "The copy is an independent board with its own key and item numbers.",
    cancel: "Cancel",
    submit: "Duplicate board",
    working: "Copying…",
    workingItems:
      "Copying {{count}} items… nothing is created unless all of it is copied.",
    failed: "The board could not be copied. Nothing was created; try again.",
    done: "{{name}} was created.",
    doneDropped_one: "{{name}} was created; {{count}} assignment was left out.",
    doneDropped_other:
      "{{name}} was created; {{count}} assignments were left out.",
    open: "Open the copy",
    denied: "You cannot place a board there.",
    previewFailed: "The size of the copy could not be read.",
  },
  import: {
    title: "Import items from a file",
    intro:
      "Each row of the file becomes an item in “{{name}}”. At most 5,000 rows, all or nothing: if any error is left, no item is created.",
    stepsLabel: "Steps",
    stepFile: "File",
    stepMap: "Map columns",
    stepCheck: "Check",
    stepRun: "Import",
    dropTitle: "Drop a CSV or XLSX file here",
    dropOr: "or",
    choose: "Choose file",
    dropHint: "The first row holds the column names. At most 5,000 rows.",
    reading: "Reading the file",
    readingBody:
      "No items are created yet. Reading is only to map columns and check.",
    fileMeta: "{{rows}} rows · {{columns}} columns · {{size}}",
    changeFile: "Change file",
    chooseOther: "Choose another file",
    cancel: "Cancel",
    close: "Close",
    next: "Continue",
    back: "Back",
    reject_unsupportedType_title: "The file {{file}} cannot be used",
    reject_unsupportedType:
      "Only CSV or XLSX files are accepted. Nothing was read from this file.",
    reject_tooLarge_title: "The file {{file}} is too large",
    reject_tooLarge:
      "The largest file is 10 MB. Nothing was read from this file.",
    reject_noRows_title: "The file has no data rows",
    reject_noRows:
      "Add at least one row under the column names and choose again.",
    reject_tooManyRows_title:
      "The file has {{rows}} rows, over the 5,000 row limit",
    reject_tooManyRows:
      "Nothing was imported. Split the file into parts under 5,000 rows and import each part.",
    reject_tooManyColumns_title: "The file has too many columns",
    reject_tooManyColumns: "At most 100 columns. Remove some and choose again.",
    reject_unsafe_title: "The file {{file}} has unsafe content",
    reject_unsafe:
      "The XLSX holds macros, embedded objects or links to other workbooks. Save it as a plain XLSX or CSV and choose again.",
    reject_malformed_title: "The file {{file}} cannot be read",
    reject_malformed:
      "The file is damaged or has a cell that is too long. Check it and choose again.",
    mapColumn: "Column in the file",
    mapField: "Board field",
    mapCheck: "Check",
    mapSkip: "Do not import this column",
    mapChoose: "Choose a field…",
    mapRequired: "Required",
    targetTitle: "Item title",
    targetGroup: "Group",
    checkOk: "Matches",
    checkUnknown_one: "{{count}} unknown value",
    checkUnknown_other: "{{count}} unknown values",
    checkIncompatible: "Cannot map",
    incompatibleBody:
      "The column holds text (for example “{{example}}”) but the field {{field}} is {{type}}. Choose another field, or skip this column.",
    type_Text: "Text",
    type_LongText: "Long text",
    type_Number: "Number",
    type_Date: "Date",
    type_Select: "Select",
    type_Status: "Status",
    type_MultiSelect: "Multi-select",
    type_Checkbox: "Checkbox",
    type_Link: "Link",
    type_Person: "Person",
    locked_person: "Cannot be imported from a file yet",
    locked_computed: "Calculated, cannot be imported",
    locked_vote: "Cast by the voter, cannot be imported",
    missingTitle: "No column for the item title",
    missingTitleBody:
      "Every item needs a title. Choose the column that holds it.",
    mapProblem_one: "One column cannot be mapped",
    mapProblem_other: "{{count}} columns cannot be mapped",
    mapProblemBody:
      "Fix it before the Check step. Continue opens when no column reports an error.",
    checkingTitle: "Checking the file",
    allValid: "All {{rows}} rows are valid",
    allValidBody:
      "Will create {{rows}} items in “{{name}}”, in the board's default group. Items show on the board only when the whole file is done.",
    hasErrors: "{{errors}} errors in {{rows}} rows. Nothing imported",
    hasErrorsBody:
      "Import is all or nothing: while errors are left, no item is created. Fix the file from the report and choose it again, or go back to the mapping.",
    colRow: "Row",
    colColumn: "Column",
    colValue: "Value",
    colReason: "Reason",
    previewNote: "Showing {{shown}} of {{rows}} rows.",
    rowLabel: "Row {{row}}",
    moreErrors: "and {{count}} more errors in other rows, in the error report.",
    reportNote: "The report gives the row, column and reason for every error.",
    downloadReport: "Download error report (CSV)",
    empty: "(empty)",
    submit_one: "Import {{count}} item",
    submit_other: "Import {{count}} items",
    creating: "Creating items: {{done}} of {{total}}",
    creatingBody:
      "Items show on the board only when the whole file is done. If anything fails, none are kept.",
    creatingNote:
      "The request is sent, so it cannot be cancelled. You can close the dialog; progress stays on the Import button.",
    working: "Importing…",
    queued: "Waiting in line…",
    done: "Imported {{count}} items into {{name}}",
    doneBody:
      "The items sit in the default group. New keys run from {{first}} to {{last}}.",
    doneAsk: "Want to see them now?",
    doneAskBody: "Open the board in Table view with the newest items first.",
    doneOpen: "See imported items",
    finish: "Done",
    failedTitle: "No items were imported",
    failedHasErrors:
      "The server refused at row {{row}}: {{column}} {{value}} {{reason}} No item was created; the board is unchanged.",
    failedRetryNote:
      "Importing the same file again does not create twice: the request uses the same key.",
    chooseFixed: "Choose the fixed file",
    archivedTitle: "{{name}} just archived this board",
    archivedBody:
      "The board is read-only so the import cannot run. No item was created. Unarchive the board and import again.",
    goneTitle: "The board is gone",
    goneBody: "The board was deleted. No item was created.",
    errorBody: "The server failed. No item was created; try again.",
    lostTitle: "The connection dropped",
    lostBody:
      "It is not known how many items were created: check the board before importing again. Importing again uses the same key so nothing is created twice.",
    retry: "Try again",
    denied: "You may not import items into this board.",
    reason_required: "is required but empty.",
    reason_tooLong: "is too long (at most 500 characters).",
    reason_notNumber: "is not a number.",
    reason_notBoolean: "is not yes/no.",
    reason_notDate: "is not a real date.",
    reason_unknownOption: "is not one of the options.",
    reason_invalidLink: "is not an http or https link.",
    reason_invalidValue: "is not valid for this field.",
    reason_noGroup: "the board has no group left to take items.",
    reportName: "error-report-{{file}}.csv",
  },
  export: {
    title: "Export items to a file",
    intro:
      "From “{{name}}”. The file is made on the server and downloads when ready.",
    formatLabel: "Format",
    csv: "CSV",
    csvBody: "Plain text, opens in any tool. UTF-8 encoded.",
    xlsx: "XLSX",
    xlsxBody: "Keeps dates and numbers, opens straight in Excel.",
    scopeLabel: "Items to export",
    scopeView: "Current view: {{view}} · {{count}} items",
    scopeViewBody: "By the filter, sort and columns shown.",
    scopeAll: "All items of the board · {{count}} items",
    scopeAllBody:
      "Ignores the view's filter. Archived and deleted items are not exported.",
    visibleTitle: "The file holds only the fields you may see",
    visibleBody: "Other people may export a different file.",
    hidden:
      "{{count}} field hidden because you may not see it: {{names}}. Other people may export a different file.",
    cancel: "Cancel",
    close: "Close",
    submit_one: "Export {{count}} item",
    submit_other: "Export {{count}} items",
    submitNone: "Export",
    emptyTitle: "The current view has no items to export",
    emptyBody: "Change the filter or choose All items of the board.",
    summaryFailed: "The item count could not be read. Try again.",
    queued: "Waiting in line",
    running: "Making the file",
    runningBody:
      "You can close the dialog: when done, the file is in your inbox and under Export, for 24 hours.",
    progress: "{{done}} of {{total}} items",
    ready: "Ready",
    readyBody:
      "The file is yours alone, downloadable for 24 hours. The board is unchanged; export again for a new file.",
    fileMeta: "{{format}} · {{size}} · expires {{expires}}",
    download: "Download",
    again: "Export again",
    expired: "The file expired",
    expiredBody: "Files are kept 24 hours. Export again for a new file.",
    failedTitle: "The file could not be made",
    failedBody:
      "There is no file to download and the board is unchanged. Trying again does not make two files.",
    failedNoAccess:
      "You can no longer see this board so the file could not be made.",
    retry: "Try again",
    denied: "Guests cannot export. You may not export this board.",
    recent: "Recent files",
    working: "Sending…",
  },
};

export type BoardLifecycleCopy = {
  [K in keyof typeof en]: { [P in keyof (typeof en)[K]]: string };
};

export const vi: BoardLifecycleCopy = {
  move: {
    loading: "Đang tính ai được và mất quyền…",
    failed: "Chưa đọc được tác động. Chưa chuyển gì.",
    retry: "Thử lại",
    losing_one: "{{count}} người mất quyền vào board",
    losing_other: "{{count}} người mất quyền vào board",
    losingEveryone:
      "Mọi người ngoài {{target}} chưa được thêm vào board đều mất quyền vào board",
    losingBody:
      "Họ chỉ vào board qua {{from}}. Sau khi chuyển, board là “Không tìm thấy” với họ.",
    losingBodyRoot:
      "Họ vào được board vì board đang ở cấp workspace. Sau khi chuyển, board là “Không tìm thấy” với họ.",
    strandedTitle_one:
      "{{count}} người mất quyền đang có việc chưa xong trên board",
    strandedTitle_other:
      "{{count}} người mất quyền đang có việc chưa xong trên board",
    strandedBody:
      "{{items}} việc, {{overdue}} quá hạn. Thêm họ vào board trước khi chuyển để việc không bị bỏ dở; không ai bị gỡ khỏi việc.",
    stranded_one: "{{count}} việc chưa xong",
    stranded_other: "{{count}} việc chưa xong",
    strandedOverdue: "{{count}} quá hạn",
    noOwnAccess: "chưa có quyền riêng",
    moreStranded: "và {{count}} người nữa",
    noneStranded: "Không ai đang có việc chưa xong sẽ mất quyền",
    noneStrandedBody: "Có thể chuyển ngay.",
    gaining_one: "{{count}} người được vào board",
    gaining_other: "{{count}} người được vào board",
    gainingEveryone: "Mọi người trong workspace (trừ khách) đều vào được board",
    gainingBody:
      "Người trong {{target}} vào được theo vai trò ở đó: Quản trị là Quản lý, Thành viên là Sửa, Người xem là Xem.",
    kept: "Giữ nguyên quyền riêng",
    keptBody: "{{names}}. Việc của họ vẫn còn nguyên.",
    keptMore: "{{names}} và {{count}} người nữa",
    noChange: "Không ai được hay mất quyền.",
    moveWithout: "Chuyển, không thêm ai",
    moveAdding_one: "Thêm {{count}} người rồi chuyển",
    moveAdding_other: "Thêm {{count}} người rồi chuyển",
    moving: "Đang chuyển…",
    moved: "Đã chuyển {{name}}.",
    movedAdded_one: "Đã chuyển {{name}} và thêm {{count}} người.",
    movedAdded_other: "Đã chuyển {{name}} và thêm {{count}} người.",
  },
  duplicate: {
    title: "Nhân bản board",
    intro: "Sao chép cấu trúc của “{{name}}” sang một board mới.",
    nameLabel: "Tên bản sao",
    defaultName: "{{name}} (bản sao)",
    nameRequired: "Hãy đặt tên cho bản sao.",
    placeLabel: "Đặt trong",
    placeRoot: "Không thuộc phòng ban nào (cấp workspace)",
    placeHint: "Mặc định là nơi board gốc đang đặt.",
    alwaysTitle: "Luôn sao chép",
    alwaysBody:
      "Trường, nhóm việc, chế độ xem, nhãn và thiết lập board. Bản sao là board độc lập, nhận khoá và mã việc riêng.",
    itemsTitle: "Sao chép cả việc",
    itemsCount_one: "{{count}} việc",
    itemsCount_other: "{{count}} việc",
    itemsHint: "Mặc định tắt: bản sao có cấu trúc nhưng chưa có việc.",
    itemsOn:
      "Việc đi kèm nhóm và thứ tự hiện tại; mỗi việc nhận mã mới của bản sao. Không sao chép bình luận, lịch sử và tệp. Tối đa {{max}} việc.",
    tooMany:
      "Board có {{count}} việc, vượt giới hạn {{max}} việc khi sao chép kèm việc. Nhân bản không kèm việc rồi dùng Nhập dữ liệu để đưa việc sang.",
    dropped_one:
      "{{count}} người không có quyền ở board mới sẽ được bỏ khỏi việc đã sao chép.",
    dropped_other:
      "{{count}} người không có quyền ở board mới sẽ được bỏ khỏi việc đã sao chép.",
    membersTitle: "Thành viên",
    membersBody:
      "Không sao chép. Bạn là Quản lý của bản sao; mời người khác ở nút Chia sẻ.",
    automationsTitle: "Tự động hoá",
    automationsBody: "Không sao chép. Bản sao bắt đầu không có quy tắc nào.",
    fixed: "Cố định",
    footer: "Bản sao là board độc lập, nhận khoá và mã việc riêng.",
    cancel: "Huỷ",
    submit: "Nhân bản board",
    working: "Đang sao chép…",
    workingItems:
      "Đang sao chép {{count}} việc… không tạo gì nếu chưa sao chép đủ.",
    failed: "Chưa sao chép được board. Chưa tạo gì, hãy thử lại.",
    done: "Đã tạo {{name}}.",
    doneDropped_one: "Đã tạo {{name}}; bỏ {{count}} người khỏi việc.",
    doneDropped_other: "Đã tạo {{name}}; bỏ {{count}} người khỏi việc.",
    open: "Mở bản sao",
    denied: "Bạn không đặt được board vào đó.",
    previewFailed: "Chưa đọc được cỡ của bản sao.",
  },
  import: {
    title: "Nhập việc từ tệp",
    intro:
      "Mỗi dòng của tệp thành một việc trong “{{name}}”. Tối đa 5.000 dòng; tất cả hoặc không: nếu còn lỗi thì không việc nào được tạo.",
    stepsLabel: "Các bước",
    stepFile: "Tệp",
    stepMap: "Ghép cột",
    stepCheck: "Kiểm tra",
    stepRun: "Nhập",
    dropTitle: "Kéo tệp CSV hoặc XLSX vào đây",
    dropOr: "hoặc",
    choose: "Chọn tệp",
    dropHint: "Dòng đầu là tên cột. Tối đa 5.000 dòng.",
    reading: "Đang đọc tệp",
    readingBody: "Chưa tạo việc nào. Đọc tệp chỉ để ghép cột và kiểm tra.",
    fileMeta: "{{rows}} dòng · {{columns}} cột · {{size}}",
    changeFile: "Đổi tệp",
    chooseOther: "Chọn tệp khác",
    cancel: "Huỷ",
    close: "Đóng",
    next: "Tiếp tục",
    back: "Quay lại",
    reject_unsupportedType_title: "Tệp {{file}} không dùng được",
    reject_unsupportedType:
      "Chỉ nhận tệp CSV hoặc XLSX. Chưa đọc gì từ tệp này.",
    reject_tooLarge_title: "Tệp {{file}} quá lớn",
    reject_tooLarge: "Tệp lớn nhất là 10 MB. Chưa đọc gì từ tệp này.",
    reject_noRows_title: "Tệp không có dòng dữ liệu",
    reject_noRows: "Thêm ít nhất một dòng dưới dòng tên cột rồi chọn lại.",
    reject_tooManyRows_title: "Tệp có {{rows}} dòng, vượt giới hạn 5.000 dòng",
    reject_tooManyRows:
      "Chưa nhập gì. Tách tệp thành nhiều phần dưới 5.000 dòng rồi nhập từng phần.",
    reject_tooManyColumns_title: "Tệp có quá nhiều cột",
    reject_tooManyColumns: "Tối đa 100 cột. Bỏ bớt cột rồi chọn lại.",
    reject_unsafe_title: "Tệp {{file}} chứa nội dung không an toàn",
    reject_unsafe:
      "Tệp XLSX có macro, đối tượng nhúng hoặc liên kết đến sổ khác. Lưu lại thành XLSX hoặc CSV thường rồi chọn lại.",
    reject_malformed_title: "Không đọc được tệp {{file}}",
    reject_malformed:
      "Tệp bị hỏng hoặc có ô quá dài. Kiểm tra tệp rồi chọn lại.",
    mapColumn: "Cột trong tệp",
    mapField: "Trường của board",
    mapCheck: "Kiểm tra",
    mapSkip: "Không nhập cột này",
    mapChoose: "Chọn trường…",
    mapRequired: "Bắt buộc",
    targetTitle: "Tên việc",
    targetGroup: "Nhóm",
    checkOk: "Khớp",
    checkUnknown_one: "{{count}} giá trị lạ",
    checkUnknown_other: "{{count}} giá trị lạ",
    checkIncompatible: "Không ghép được",
    incompatibleBody:
      "Cột có chữ (ví dụ “{{example}}”) nhưng trường {{field}} là {{type}}. Chọn trường khác, hoặc bỏ qua cột này.",
    type_Text: "Văn bản",
    type_LongText: "Văn bản dài",
    type_Number: "Số",
    type_Date: "Ngày",
    type_Select: "Lựa chọn",
    type_Status: "Trạng thái",
    type_MultiSelect: "Nhiều lựa chọn",
    type_Checkbox: "Hộp chọn",
    type_Link: "Liên kết",
    type_Person: "Người",
    locked_person: "Chưa nhập được từ tệp",
    locked_computed: "Tự tính, không nhập được",
    locked_vote: "Do người bình chọn, không nhập được",
    missingTitle: "Thiếu cột cho Tên việc",
    missingTitleBody: "Mỗi việc cần có tên. Chọn cột chứa tên việc.",
    mapProblem_one: "Một cột chưa ghép được",
    mapProblem_other: "{{count}} cột chưa ghép được",
    mapProblemBody:
      "Sửa trước khi sang bước Kiểm tra. Nút Tiếp tục mở khi không còn cột nào báo lỗi.",
    checkingTitle: "Đang kiểm tra tệp",
    allValid: "{{rows}} dòng đều hợp lệ",
    allValidBody:
      "Sẽ tạo {{rows}} việc trong “{{name}}”, vào nhóm mặc định của board. Việc chỉ hiện trên board khi cả tệp xong.",
    hasErrors: "Có {{errors}} lỗi ở {{rows}} dòng. Chưa nhập gì",
    hasErrorsBody:
      "Nhập là tất cả hoặc không: còn lỗi thì không việc nào được tạo. Sửa tệp theo báo cáo rồi chọn lại, hoặc quay lại ghép cột.",
    colRow: "Dòng",
    colColumn: "Cột",
    colValue: "Giá trị",
    colReason: "Lý do",
    previewNote: "Xem trước {{shown}} trong {{rows}} dòng.",
    rowLabel: "Dòng {{row}}",
    moreErrors: "và {{count}} lỗi nữa ở các dòng khác, có trong báo cáo lỗi.",
    reportNote: "Báo cáo nêu số dòng, cột và lý do cho từng lỗi.",
    downloadReport: "Tải báo cáo lỗi (CSV)",
    empty: "(trống)",
    submit_one: "Nhập {{count}} việc",
    submit_other: "Nhập {{count}} việc",
    creating: "Đang tạo việc: {{done}} trên {{total}}",
    creatingBody:
      "Việc chỉ hiện trên board khi toàn bộ tệp xong. Nếu có lỗi, không việc nào được giữ lại.",
    creatingNote:
      "Yêu cầu đã gửi nên không huỷ được. Có thể đóng hộp thoại; tiến độ còn ở nút Nhập việc.",
    working: "Đang nhập…",
    queued: "Đang xếp hàng…",
    done: "Đã nhập {{count}} việc vào {{name}}",
    doneBody:
      "Việc nằm ở nhóm mặc định. Mã việc mới từ {{first}} đến {{last}}.",
    doneAsk: "Muốn xem ngay?",
    doneAskBody: "Mở board ở chế độ Bảng, sắp xếp việc mới nhất lên đầu.",
    doneOpen: "Xem việc đã nhập",
    finish: "Xong",
    failedTitle: "Chưa nhập việc nào",
    failedHasErrors:
      "Máy chủ từ chối ở dòng {{row}}: {{column}} {{value}} {{reason}}. Không việc nào được tạo, board không đổi.",
    failedRetryNote:
      "Nhập lại với cùng tệp cũng không tạo hai lần: yêu cầu dùng cùng khoá.",
    chooseFixed: "Chọn tệp đã sửa",
    archivedTitle: "{{name}} vừa lưu trữ board này",
    archivedBody:
      "Board chỉ đọc nên chưa nhập được. Không việc nào được tạo. Bỏ lưu trữ board rồi nhập lại.",
    goneTitle: "Board không còn",
    goneBody: "Board đã bị xoá. Không việc nào được tạo.",
    errorBody: "Có lỗi ở máy chủ. Không việc nào được tạo; thử lại.",
    lostTitle: "Mất kết nối giữa chừng",
    lostBody:
      "Không biết đã tạo được bao nhiêu việc: kiểm tra lại board trước khi nhập lại. Nhập lại dùng cùng khoá nên không tạo hai lần.",
    retry: "Thử lại",
    denied: "Bạn không có quyền nhập việc vào board này.",
    reason_required: "bắt buộc nhưng để trống.",
    reason_tooLong: "quá dài (tối đa 500 ký tự).",
    reason_notNumber: "không phải số.",
    reason_notBoolean: "không phải có/không.",
    reason_notDate: "không phải ngày có thật.",
    reason_unknownOption: "không có trong lựa chọn.",
    reason_invalidLink: "không phải liên kết http hoặc https.",
    reason_invalidValue: "không hợp lệ cho trường này.",
    reason_noGroup: "board không còn nhóm nào để nhận việc.",
    reportName: "bao-cao-loi-{{file}}.csv",
  },
  export: {
    title: "Xuất việc ra tệp",
    intro: "Từ “{{name}}”. Tệp được tạo ở máy chủ, tải về khi sẵn sàng.",
    formatLabel: "Định dạng",
    csv: "CSV",
    csvBody: "Văn bản thuần, mở được ở mọi công cụ. Mã hoá UTF-8.",
    xlsx: "XLSX",
    xlsxBody: "Giữ kiểu ngày và số, mở thẳng bằng Excel.",
    scopeLabel: "Việc cần xuất",
    scopeView: "Chế độ xem hiện tại: {{view}} · {{count}} việc",
    scopeViewBody: "Theo bộ lọc, sắp xếp và cột đang hiện.",
    scopeAll: "Tất cả việc của board · {{count}} việc",
    scopeAllBody:
      "Bỏ qua bộ lọc của chế độ xem. Việc đã lưu trữ và đã xoá không xuất.",
    visibleTitle: "Tệp chỉ có các trường bạn được xem",
    visibleBody: "Người khác có thể xuất ra tệp khác.",
    hidden:
      "Ẩn {{count}} trường vì bạn chưa có quyền xem: {{names}}. Người khác có thể xuất ra tệp khác.",
    cancel: "Huỷ",
    close: "Đóng",
    submit_one: "Xuất {{count}} việc",
    submit_other: "Xuất {{count}} việc",
    submitNone: "Xuất",
    emptyTitle: "Chế độ xem hiện tại không có việc nào để xuất",
    emptyBody: "Đổi bộ lọc hoặc chọn Tất cả việc của board.",
    summaryFailed: "Chưa đếm được số việc. Thử lại.",
    queued: "Đang xếp hàng",
    running: "Đang tạo tệp",
    runningBody:
      "Có thể đóng hộp thoại: khi xong, tệp có trong hộp thư của bạn và ở nút Xuất việc, trong 24 giờ.",
    progress: "{{done}} trên {{total}} việc",
    ready: "Sẵn sàng",
    readyBody:
      "Tệp chỉ của bạn, tải được trong 24 giờ. Việc trên board không đổi; xuất lại để lấy tệp mới.",
    fileMeta: "{{format}} · {{size}} · hết hạn {{expires}}",
    download: "Tải xuống",
    again: "Xuất lại",
    expired: "Tệp đã hết hạn",
    expiredBody: "Tệp chỉ giữ 24 giờ. Xuất lại để lấy tệp mới.",
    failedTitle: "Chưa tạo được tệp",
    failedBody:
      "Chưa có tệp nào để tải và board không đổi. Thử lại không tạo hai tệp.",
    failedNoAccess: "Bạn không còn quyền xem board này nên không tạo được tệp.",
    retry: "Thử lại",
    denied: "Khách không xuất được. Bạn không có quyền xuất board này.",
    recent: "Tệp gần đây",
    working: "Đang gửi…",
  },
};

export function boardLifecycleCopy(locale: "vi" | "en"): BoardLifecycleCopy {
  return locale === "vi" ? vi : en;
}

export function fillLifecycleCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** The `_one` form for exactly 1, else `_other`, then fills it. */
export function pluralLifecycle(
  copy: Readonly<Record<string, string>>,
  key: string,
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillLifecycleCopy(
    copy[`${key}_${count === 1 ? "one" : "other"}`] ?? "",
    {
      ...values,
      count,
    },
  );
}
