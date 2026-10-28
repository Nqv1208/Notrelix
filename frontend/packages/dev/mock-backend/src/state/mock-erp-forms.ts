/**
 * The forms of the ERP board in the mock world (the 2026-10 Forms mockups):
 * "Yêu cầu hỗ trợ IT" is published with responses in every inbox tab, "Đề xuất
 * mua sắm ..." is a draft and "Báo lỗi ERP" is closed.
 */
import { ERP_PRIORITY_COLUMN_ID } from "./mock-erp-board";
import { mockIds } from "./mock-ids";
import type {
  MockForm,
  MockFormQuestion,
  MockFormSubmission,
} from "./mock-board-forms";
import type { MockStore } from "./mock-store";

function question(
  id: string,
  position: string,
  input: Partial<MockFormQuestion> &
    Pick<MockFormQuestion, "questionKey" | "label" | "questionType">,
): MockFormQuestion {
  return {
    id,
    position,
    isRequired: false,
    boardFieldId: null,
    placeholder: null,
    helpText: null,
    maxLength: null,
    minValue: null,
    maxValue: null,
    options: null,
    ...input,
  };
}

export const ERP_FORM_IDS = {
  support: "mock-form-it-support",
  purchase: "mock-form-purchase",
  bug: "mock-form-erp-bug",
} as const;

export function seedErpForms(store: MockStore): void {
  const boardId = mockIds.boards.erp;
  const support: MockForm = {
    id: ERP_FORM_IDS.support,
    boardId,
    name: "Yêu cầu hỗ trợ IT",
    slug: "yeu-cau-ho-tro-it",
    status: "Published",
    description:
      "Gửi yêu cầu cho nhóm IT: thiết bị, tài khoản, phần mềm. Việc khẩn vui lòng gọi số nội bộ 1234.",
    version: 4,
    createdAt: "2026-10-05T02:00:00.000Z",
    deleted: false,
    questions: [
      question("mock-fq-title", "a0", {
        questionKey: "title",
        label: "Bạn cần gì?",
        questionType: "ShortText",
        isRequired: true,
        placeholder: "Ví dụ: cấp lại mật khẩu email",
        maxLength: 120,
      }),
      question("mock-fq-description", "a1", {
        questionKey: "description",
        label: "Mô tả chi tiết",
        questionType: "LongText",
        placeholder: "Thiết bị, hệ điều hành, các bước đã thử",
      }),
      question("mock-fq-priority", "a2", {
        questionKey: "priority",
        label: "Mức độ ưu tiên",
        questionType: "Select",
        isRequired: true,
        boardFieldId: ERP_PRIORITY_COLUMN_ID,
      }),
      question("mock-fq-due", "a3", {
        questionKey: "due",
        label: "Hạn mong muốn",
        questionType: "Date",
      }),
      question("mock-fq-email", "a4", {
        questionKey: "email",
        label: "Email liên hệ",
        questionType: "Email",
        placeholder: "ten@minhan.vn",
      }),
    ],
  };
  const purchase: MockForm = {
    id: ERP_FORM_IDS.purchase,
    boardId,
    name: "Đề xuất mua sắm thiết bị và phần mềm cho chi nhánh Đà Nẵng và Hải Phòng quý 4",
    slug: "de-xuat-mua-sam",
    status: "Draft",
    description: "",
    version: 1,
    createdAt: "2026-10-09T02:00:00.000Z",
    deleted: false,
    questions: [
      question("mock-fq-p1", "a0", {
        questionKey: "title",
        label: "Cần mua gì?",
        questionType: "ShortText",
        isRequired: true,
      }),
    ],
  };
  const bug: MockForm = {
    id: ERP_FORM_IDS.bug,
    boardId,
    name: "Báo lỗi ERP",
    slug: "bao-loi-erp",
    status: "Closed",
    description: "Báo lỗi phân hệ ERP cho nhóm triển khai.",
    version: 3,
    createdAt: "2026-09-20T02:00:00.000Z",
    deleted: false,
    questions: [
      question("mock-fq-b1", "a0", {
        questionKey: "title",
        label: "Lỗi gì?",
        questionType: "ShortText",
        isRequired: true,
      }),
    ],
  };
  for (const form of [support, purchase, bug]) store.forms.put(form);

  const answer = (title: string, extra: Record<string, unknown> = {}) =>
    JSON.stringify({ title, ...extra });
  const submissions: MockFormSubmission[] = [
    {
      id: "mock-fsub-1",
      status: "Accepted",
      submitterUserId: mockIds.users.commenter,
      submitterEmail: null,
      submittedAt: "2026-10-12T02:12:00.000Z",
      payloadJson: answer("Làm mới tên máy kế toán Đà Nẵng", {
        description:
          "Máy mới cấp nhưng tên cũ còn nằm trong domain, cần đổi để đồng bộ với danh mục tài sản.",
        priority: "mock-erp-priority-high",
        due: "2026-10-16",
        email: "trang.tt@minhan.vn",
      }),
    },
    {
      id: "mock-fsub-2",
      status: "Accepted",
      submitterUserId: "mock-user-erp-phuc",
      submitterEmail: null,
      submittedAt: "2026-10-11T07:00:00.000Z",
      payloadJson: answer("Cấp thêm 2 màn hình cho phòng thiết kế", {
        priority: "mock-erp-priority-medium",
      }),
    },
    {
      id: "mock-fsub-3",
      status: "Accepted",
      submitterUserId: "mock-user-erp-quan",
      submitterEmail: null,
      submittedAt: "2026-10-09T03:00:00.000Z",
      payloadJson: answer(
        "Không in được hoá đơn từ ERP trên máy in tầng 2 sau khi đổi mật khẩu đăng nhập và cập nhật Windows",
        { priority: "mock-erp-priority-high" },
      ),
    },
    {
      id: "mock-fsub-4",
      status: "Accepted",
      submitterUserId: "mock-user-erp-ngoc-anh",
      submitterEmail: null,
      submittedAt: "2026-10-10T03:00:00.000Z",
      processedAt: "2026-10-10T04:00:00.000Z",
      createdItemId: "mock-erp-item-151",
      payloadJson: answer("Cấp quyền ERP cho nhân viên mới"),
    },
    {
      id: "mock-fsub-5",
      status: "Rejected",
      submitterUserId: "mock-user-erp-thanh-tam",
      submitterEmail: null,
      submittedAt: "2026-10-09T01:00:00.000Z",
      processedAt: "2026-10-09T02:00:00.000Z",
      reason: "Trùng với yêu cầu ERP-168, nhóm IT đang xử lý.",
      payloadJson: answer("Yêu cầu trùng ERP-168"),
    },
    {
      id: "mock-fsub-6",
      status: "Spam",
      submitterUserId: "mock-user-erp-hang",
      submitterEmail: null,
      submittedAt: "2026-10-07T01:00:00.000Z",
      processedAt: "2026-10-07T02:00:00.000Z",
      payloadJson: answer("Nội dung không liên quan"),
    },
  ].map((row) => ({
    formId: ERP_FORM_IDS.support,
    boardId,
    createdItemId: null,
    processedAt: null,
    reason: null,
    key: null,
    ...row,
  })) as MockFormSubmission[];
  for (const submission of submissions) store.forms.putSubmission(submission);
}
