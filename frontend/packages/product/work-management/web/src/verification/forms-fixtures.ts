/**
 * Deterministic fixtures for the forms surfaces: "Yêu cầu hỗ trợ IT" on the
 * ERP board of "Minh An Group" with the five questions of the mockups, the
 * board fields they map to, and a response in every inbox tab. Names and
 * answers are board data, so they stay Vietnamese in both locales.
 */
import type {
  FormDetail,
  FormQuestion,
  FormSubmission,
} from "@notrelix/work-management-core";
import { formsCopy, type FormsCopy } from "../components/forms/forms.copy";
import type { FormBoardField } from "../components/forms/form-model";

export function formsCopyFixture(locale: "vi" | "en" = "vi"): FormsCopy {
  return formsCopy(locale);
}

export const PRIORITY_FIELD: FormBoardField = {
  id: "field-priority",
  name: "Ưu tiên",
  type: "select",
  options: [
    { id: "p-low", name: "Thấp" },
    { id: "p-medium", name: "Trung bình" },
    { id: "p-high", name: "Cao" },
    { id: "p-urgent", name: "Khẩn" },
  ],
};

export const BOARD_FIELDS: readonly FormBoardField[] = [
  PRIORITY_FIELD,
  { id: "field-module", name: "Phân hệ ERP", type: "select", options: [] },
  { id: "field-note", name: "Ghi chú", type: "text", options: [] },
];

export function question(
  id: string,
  position: string,
  overrides: Partial<FormQuestion> &
    Pick<FormQuestion, "key" | "label" | "type">,
): FormQuestion {
  return {
    id,
    position,
    required: false,
    boardFieldId: null,
    placeholder: null,
    helpText: null,
    maxLength: null,
    minValue: null,
    maxValue: null,
    options: [],
    ...overrides,
  };
}

export const Q_TITLE = question("q-title", "a0", {
  key: "title",
  label: "Bạn cần gì?",
  type: "text",
  required: true,
  placeholder: "Ví dụ: cấp lại mật khẩu email",
  maxLength: 120,
});
export const Q_DESCRIPTION = question("q-description", "a1", {
  key: "description",
  label: "Mô tả chi tiết",
  type: "longtext",
});
export const Q_PRIORITY = question("q-priority", "a2", {
  key: "priority",
  label: "Mức độ ưu tiên",
  type: "select",
  required: true,
  boardFieldId: PRIORITY_FIELD.id,
});
export const Q_DUE = question("q-due", "a3", {
  key: "due",
  label: "Hạn mong muốn",
  type: "date",
});
export const Q_EMAIL = question("q-email", "a4", {
  key: "email",
  label: "Email liên hệ",
  type: "email",
  placeholder: "ten@minhan.vn",
});
/** A select that is not mapped: it carries its own choices. */
export const Q_OWN_CHOICES = question("q-software", "a5", {
  key: "q_software",
  label: "Bạn dùng phần mềm nào?",
  type: "multiselect",
  options: [
    { key: "o_1", label: "ERP" },
    { key: "o_2", label: "Email" },
  ],
});

export function formFixture(overrides: Partial<FormDetail> = {}): FormDetail {
  return {
    id: "form-it",
    boardId: "board-erp",
    title: "Yêu cầu hỗ trợ IT",
    status: "draft",
    createdAt: "2026-10-05T02:00:00.000Z",
    description:
      "Gửi yêu cầu cho nhóm IT: thiết bị, tài khoản, phần mềm. Việc khẩn vui lòng gọi số nội bộ 1234.",
    version: 4,
    questions: [Q_TITLE, Q_DESCRIPTION, Q_PRIORITY, Q_DUE, Q_EMAIL],
    ...overrides,
  };
}

export const PEOPLE = {
  "u-trang": "Trần Thu Trang",
  "u-phuc": "Lê Hoàng Phúc",
  "u-minh": "Mai Quốc Minh",
} as const;

export function response(
  id: string,
  overrides: Partial<FormSubmission> = {},
): FormSubmission {
  return {
    id,
    formId: "form-it",
    status: "new",
    submitterUserId: "u-trang",
    submitterEmail: null,
    submittedAt: "2026-10-12T02:12:00.000Z",
    processedAt: null,
    createdItemId: null,
    reason: null,
    answers: {
      title: "Làm mới tên máy kế toán Đà Nẵng",
      description:
        "Máy mới cấp nhưng tên cũ còn nằm trong domain, cần đổi để đồng bộ với danh mục tài sản.",
      priority: "p-high",
      due: "2026-10-16",
      email: "trang.tt@minhan.vn",
    },
    ...overrides,
  };
}

export const NEW_RESPONSES: readonly FormSubmission[] = [
  response("s-1"),
  response("s-2", {
    submitterUserId: "u-phuc",
    submittedAt: "2026-10-11T07:00:00.000Z",
    answers: {
      title: "Cấp thêm 2 màn hình cho phòng thiết kế",
      priority: "p-medium",
    },
  }),
  response("s-3", {
    submitterUserId: "u-minh",
    submittedAt: "2026-10-09T03:00:00.000Z",
    answers: {
      title:
        "Không in được hoá đơn từ ERP trên máy in tầng 2 sau khi đổi mật khẩu đăng nhập và cập nhật Windows",
      priority: "p-high",
    },
  }),
];

export const ALL_RESPONSES: readonly FormSubmission[] = [
  ...NEW_RESPONSES,
  response("s-4", {
    status: "processed",
    createdItemId: "item-171",
    processedAt: "2026-10-12T02:30:00.000Z",
    answers: { title: "Cấp quyền ERP cho nhân viên mới" },
  }),
  response("s-5", {
    status: "rejected",
    reason: "Trùng với yêu cầu ERP-168, nhóm IT đang xử lý.",
    processedAt: "2026-10-12T02:25:00.000Z",
    answers: { title: "Yêu cầu trùng ERP-168" },
  }),
  response("s-6", {
    status: "rejected",
    processedAt: "2026-10-12T02:25:00.000Z",
    answers: { title: "Xin cấp quyền admin" },
  }),
  response("s-7", {
    status: "spam",
    processedAt: "2026-10-12T02:26:00.000Z",
    answers: { title: "Nội dung không liên quan" },
  }),
];
