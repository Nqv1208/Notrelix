import type { Meta, StoryObj } from "@storybook/react";
import {
  BOARD_FIELDS,
  Q_OWN_CHOICES,
  Q_PRIORITY,
  formFixture,
  formsCopyFixture,
  question,
} from "../../verification/forms-fixtures";
import {
  FormBuilderSurface,
  type FormBuilderSurfaceProps,
} from "./form-builder-surface";

const ok = async () => ({ ok: true }) as const;
const noop = () => undefined;

const props = (
  overrides: Partial<FormBuilderSurfaceProps> = {},
): FormBuilderSurfaceProps => ({
  copy: formsCopyFixture("vi"),
  status: "ready",
  form: formFixture(),
  fields: BOARD_FIELDS,
  boardTitle: "Triển khai ERP",
  selectedId: "q-priority",
  onSelect: noop,
  savedAt: "09:42",
  preview: false,
  onPreview: noop,
  linkUrl: "https://notrelix.vn/minh-an/f/yeu-cau-ho-tro-it",
  onUpdateDetails: ok,
  onAddQuestion: ok,
  onUpdateQuestion: ok,
  onRemoveQuestion: ok,
  onPublish: ok,
  onClose: ok,
  onCopyLink: async () => true,
  onReload: noop,
  onBack: noop,
  ...overrides,
});

const meta: Meta = {
  title: "Work Management/Forms/Builder",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** A draft with a mapped select open: its options are the board field's, read-only. */
export const Default: Story = {
  render: () => <FormBuilderSurface {...props()} />,
};

/** A closed form: questions and settings cannot be changed. */
export const ReadOnly: Story = {
  render: () => (
    <FormBuilderSurface
      {...props({
        form: formFixture({ status: "closed" }),
        selectedId: "q-title",
      })}
    />
  ),
};

/** Own choices, two questions on one field and a very long label. */
export const EdgeData: Story = {
  render: () => (
    <FormBuilderSurface
      {...props({
        form: formFixture({
          questions: [
            Q_PRIORITY,
            question("q-urgent", "a1", {
              key: "urgent",
              label: "Mức độ khẩn cấp",
              type: "select",
              boardFieldId: Q_PRIORITY.boardFieldId,
            }),
            question("q-long", "a2", {
              key: "long",
              label:
                "Vui lòng mô tả đầy đủ bối cảnh, thiết bị, hệ điều hành và các bước đã thử trước khi gửi yêu cầu",
              type: "longtext",
            }),
            Q_OWN_CHOICES,
          ],
        }),
        selectedId: Q_OWN_CHOICES.id,
      })}
    />
  ),
};
