import { useEffect, useRef } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  BOARD_FIELDS,
  Q_DESCRIPTION,
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
  tags: ["fui-surface--wm.forms.fill", "fui-state--Default"],
  render: () => (
    <div className="flex h-[720px]">
      <FormFillSurface {...props()} />
    </div>
  ),
};

/** The form is on its way. */
export const Loading: Story = {
  tags: ["fui-surface--wm.forms.fill", "fui-state--Loading"],
  render: () => (
    <div className="flex h-[480px]">
      <FormFillSurface {...props({ status: "loading", form: null })} />
    </div>
  ),
};

/** The form could not load: the message offers Retry. */
export const Error: Story = {
  tags: ["fui-surface--wm.forms.fill", "fui-state--Error"],
  render: () => (
    <div className="flex h-[480px]">
      <FormFillSurface {...props({ status: "error", form: null })} />
    </div>
  ),
};

/** Sends a form with no required question once mounted, to show the thank-you page. */
function SentPage() {
  const host = useRef<HTMLDivElement>(null);
  useEffect(() => {
    host.current
      ?.querySelector<HTMLButtonElement>("button[type=submit]")
      ?.click();
  }, []);
  return (
    <div ref={host} className="flex h-[480px]">
      <FormFillSurface
        {...props({
          form: formFixture({
            status: "published",
            questions: [Q_DESCRIPTION],
          }),
        })}
      />
    </div>
  );
}

/** The answers were accepted: the page thanks the member. */
export const Success: Story = {
  tags: ["fui-surface--wm.forms.fill", "fui-state--Success"],
  render: () => <SentPage />,
};

/** A member who cannot create items on the board cannot send the form. */
export const ReadOnly: Story = {
  tags: ["fui-surface--wm.forms.fill", "fui-state--ReadOnly"],
  render: () => (
    <div className="flex h-[480px]">
      <FormFillSurface {...props({ canSubmit: false })} />
    </div>
  ),
};

/** A form that closed: the page says so and names no way to send. */
export const EdgeData: Story = {
  tags: ["fui-surface--wm.forms.fill", "fui-state--EdgeData"],
  render: () => (
    <div className="flex h-[480px]">
      <FormFillSurface
        {...props({ form: formFixture({ status: "closed" }) })}
      />
    </div>
  ),
};
