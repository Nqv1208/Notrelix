import type { Meta, StoryObj } from "@storybook/react";
import {
  ALL_RESPONSES,
  BOARD_FIELDS,
  PEOPLE,
  formFixture,
  formsCopyFixture,
  response,
} from "../../verification/forms-fixtures";
import {
  FormInboxSurface,
  type FormInboxSurfaceProps,
} from "./form-inbox-surface";

const ok = async () => ({ ok: true }) as const;
const noop = () => undefined;
const props = (
  overrides: Partial<FormInboxSurfaceProps> = {},
): FormInboxSurfaceProps => ({
  copy: formsCopyFixture("vi"),
  locale: "vi",
  timeZone: "Asia/Ho_Chi_Minh",
  now: new Date("2026-10-12T03:40:00.000Z"),
  status: "ready",
  form: formFixture({ status: "published" }),
  fields: BOARD_FIELDS,
  submissions: ALL_RESPONSES,
  people: PEOPLE,
  deciderName: "Hà Nguyễn",
  tab: "new",
  onTab: noop,
  selectedId: "s-1",
  onSelect: noop,
  onCreateItem: noop,
  onReject: ok,
  onSpam: ok,
  onRemove: ok,
  onOpenItem: noop,
  onReload: noop,
  onBack: noop,
  ...overrides,
});

const meta: Meta = {
  title: "Work Management/Forms/Inbox",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** The New tab with a response open. */
export const Default: Story = {
  render: () => (
    <div className="flex h-[720px]">
      <FormInboxSurface {...props()} />
    </div>
  ),
};

/** A long answer wraps in the detail and is cut with an ellipsis in the list. */
export const EdgeData: Story = {
  render: () => (
    <div className="flex h-[720px]">
      <FormInboxSurface
        {...props({
          submissions: [
            response("s-long", {
              answers: {
                title:
                  "Không in được hoá đơn từ ERP trên máy in tầng 2 sau khi đổi mật khẩu đăng nhập và cập nhật Windows",
                description:
                  "Máy tính phòng kế toán tầng 3 chi nhánh Đà Nẵng không vào được phần mềm ERP từ sáng thứ Sáu sau khi cập nhật Windows, báo lỗi 0x80070005 và không đăng nhập được dù đã đổi mật khẩu hai lần.",
              },
            }),
          ],
          selectedId: "s-long",
        })}
      />
    </div>
  ),
};
