import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { formsCopyFixture } from "../../../verification/forms-fixtures";
import {
  ResponseItemDialog,
  type ResponseItemDialogProps,
} from "../response-item-dialog";

function render(
  locale: "vi" | "en",
  over: Partial<ResponseItemDialogProps> = {},
) {
  const props: ResponseItemDialogProps = {
    copy: formsCopyFixture(locale),
    boardTitle: "Triển khai ERP",
    notice: "Điền sẵn từ phản hồi của Trần Thu Trang",
    initial: {
      title: "Làm mới tên máy",
      description: "",
      groupId: "g2",
      priorityOptionId: "p-high",
      dueDay: "2026-10-16",
    },
    from: { title: "Bạn cần gì?", priority: "Mức độ ưu tiên" },
    groups: [
      { id: "g1", title: "Cần làm" },
      { id: "g2", title: "Đang làm" },
    ],
    priorities: [{ id: "p-high", label: "Cao" }],
    left: ["Email: trang@minhan.vn"],
    pending: false,
    failed: false,
    onSubmit: vi.fn(),
    onClose: vi.fn(),
    ...over,
  };
  renderPureUi(<ResponseItemDialog {...props} />);
  return props;
}

describe.each(["vi", "en"] as const)(
  "create item from response (%s)",
  (locale) => {
    it("shows the prefill with its source chips, the group picker and the unmapped note, and sends the values", () => {
      const props = render(locale);
      const t = formsCopyFixture(locale).createFromResponse;
      expect(screen.getByRole("dialog")).toBeTruthy();
      expect(
        (screen.getByLabelText(new RegExp(t.nameLabel)) as HTMLInputElement)
          .value,
      ).toBe("Làm mới tên máy");
      expect(screen.getAllByText(/Bạn cần gì\?|Mức độ ưu tiên/).length).toBe(2);
      expect(
        (screen.getByLabelText(new RegExp(t.groupLabel)) as HTMLSelectElement)
          .value,
      ).toBe("g2");
      expect(screen.getByRole("note").textContent).toContain("trang@minhan.vn");
      fireEvent.click(screen.getByRole("button", { name: t.submit }));
      expect(props.onSubmit).toHaveBeenCalledWith({
        title: "Làm mới tên máy",
        description: "",
        groupId: "g2",
        priorityOptionId: "p-high",
        dueDay: "2026-10-16",
      });
    });

    it("refuses an empty name before sending", () => {
      const props = render(locale, {
        initial: {
          title: "",
          description: "",
          groupId: "g1",
          priorityOptionId: null,
          dueDay: "",
        },
      });
      const t = formsCopyFixture(locale).createFromResponse;
      fireEvent.click(screen.getByRole("button", { name: t.submit }));
      expect(props.onSubmit).not.toHaveBeenCalled();
      expect(screen.getByRole("alert").textContent).toBe(t.nameRequired);
    });

    it("locks both buttons while the item is being made", () => {
      render(locale, { pending: true });
      const t = formsCopyFixture(locale).createFromResponse;
      expect(
        (screen.getByRole("button", { name: t.submit }) as HTMLButtonElement)
          .disabled,
      ).toBe(true);
      expect(
        (screen.getByRole("button", { name: t.cancel }) as HTMLButtonElement)
          .disabled,
      ).toBe(true);
    });
  },
);
