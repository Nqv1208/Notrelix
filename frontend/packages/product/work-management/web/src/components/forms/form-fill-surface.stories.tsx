import type { Meta, StoryObj } from "@storybook/react";
import {
  BOARD_FIELDS,
  formFixture,
  formsCopyFixture,
} from "../../verification/forms-fixtures";
import {
  FormFillSurface,
  type FormFillSurfaceProps,
} from "./form-fill-surface";

const noop = () => undefined;
const props = (
  overrides: Partial<FormFillSurfaceProps> = {},
): FormFillSurfaceProps => ({
  copy: formsCopyFixture("vi"),
  status: "ready",
  form: formFixture({ status: "published" }),
  fields: BOARD_FIELDS,
  boardTitle: "Triển khai ERP",
  user: { name: "Hà Nguyễn", email: "ha.nguyen@minhan.vn" },
  todayLabel: "Thứ Hai 12/10/2026",
  canSubmit: true,
  onBack: noop,
  onHome: noop,
  onReload: noop,
  onSubmit: async () => ({ ok: true }),
  ...overrides,
});

const meta: Meta = {
  title: "Work Management/Forms/Fill",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** The member's page, nothing filled yet. */
export const Default: Story = {
  render: () => (
    <div className="flex h-[720px]">
      <FormFillSurface {...props()} />
    </div>
  ),
};

/** A member who cannot create items on the board cannot send the form. */
export const ReadOnly: Story = {
  render: () => (
    <div className="flex h-[480px]">
      <FormFillSurface {...props({ canSubmit: false })} />
    </div>
  ),
};

/** A form that closed: the page says so and names no way to send. */
export const EdgeData: Story = {
  render: () => (
    <div className="flex h-[480px]">
      <FormFillSurface
        {...props({ form: formFixture({ status: "closed" }) })}
      />
    </div>
  ),
};
