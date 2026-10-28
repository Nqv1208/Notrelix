import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { formsCopyFixture } from "../../../verification/forms-fixtures";
import { fillFormsCopy } from "../forms.copy";
import {
  FormsListPanel,
  type BoardFormsPanelProps,
  type FormListRow,
} from "../forms-list-panel";

const ok = async () => ({ ok: true }) as const;
const refused = async () => ({ ok: false, reason: "failed" }) as const;

const ROWS: readonly FormListRow[] = [
  {
    id: "f-1",
    boardId: "b",
    title: "Yêu cầu hỗ trợ IT",
    status: "published",
    createdAt: "2026-10-05T02:00:00Z",
    questionCount: 5,
    newCount: 3,
    totalCount: 18,
  },
  {
    id: "f-2",
    boardId: "b",
    title:
      "Đề xuất mua sắm thiết bị và phần mềm cho chi nhánh Đà Nẵng và Hải Phòng quý 4",
    status: "draft",
    createdAt: "2026-10-09T02:00:00Z",
    questionCount: 4,
    newCount: 0,
    totalCount: 0,
  },
  {
    id: "f-3",
    boardId: "b",
    title: "Báo lỗi ERP",
    status: "closed",
    createdAt: "2026-09-20T02:00:00Z",
    questionCount: 6,
    newCount: 0,
    totalCount: 24,
  },
];

function render(
  locale: "vi" | "en",
  overrides: Partial<BoardFormsPanelProps> = {},
) {
  const props: BoardFormsPanelProps = {
    copy: formsCopyFixture(locale),
    status: "ready",
    forms: ROWS,
    onReload: vi.fn(),
    onCreate: vi.fn(ok),
    onPublish: vi.fn(ok),
    onClose: vi.fn(ok),
    onDelete: vi.fn(ok),
    onOpenBuilder: vi.fn(),
    onOpenResponses: vi.fn(),
    onCopyLink: vi.fn(async () => true),
    ...overrides,
  };
  renderPureUi(<FormsListPanel {...props} />);
  return props;
}

const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[wm.forms.list:${id}] ` : "";

describe.each(["vi", "en"] as const)("forms list (%s)", (locale) => {
  const copy = formsCopyFixture(locale);

  it(`${fui("states", locale)}shows the forms with status chips and counts, and loading, empty and error states (${locale})`, () => {
    render(locale);
    expect(screen.getByText(copy.common.status.published)).toBeTruthy();
    expect(screen.getByText(copy.common.status.draft)).toBeTruthy();
    expect(screen.getByText(copy.common.status.closed)).toBeTruthy();
    expect(
      screen.getByText(fillFormsCopy(copy.list.newCount, { count: 3 })),
    ).toBeTruthy();
    expect(screen.getByText(ROWS[1]!.title).getAttribute("title")).toBe(
      ROWS[1]!.title,
    );
    cleanup();
    render(locale, { status: "loading", forms: [] });
    expect(
      screen.getByRole("status", { name: copy.common.loading }),
    ).toBeTruthy();
    cleanup();
    render(locale, { forms: [] });
    expect(screen.getByText(copy.list.empty)).toBeTruthy();
    cleanup();
    const onReload = vi.fn();
    render(locale, { status: "error", forms: [], onReload });
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    expect(onReload).toHaveBeenCalled();
  });

  it(`${fui("create", locale)}names a form in place, asks for a name, and opens the builder through the host (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(
      screen.getAllByRole("button", { name: copy.list.create })[0]!,
    );
    const input = screen.getByRole("textbox", { name: copy.list.nameLabel });
    fireEvent.click(
      screen.getByRole("button", { name: copy.list.submitCreate }),
    );
    expect(screen.getByText(copy.list.nameRequired)).toBeTruthy();
    expect(props.onCreate).not.toHaveBeenCalled();
    fireEvent.change(input, { target: { value: "Yêu cầu cấp quyền" } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.list.submitCreate }),
    );
    await waitFor(() =>
      expect(props.onCreate).toHaveBeenCalledWith("Yêu cầu cấp quyền"),
    );
  });

  it(`${fui("row-actions", locale)}publishes a draft, closes a published form and copies its member link (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(screen.getByRole("button", { name: copy.list.publish }));
    expect(props.onPublish).toHaveBeenCalledWith("f-2");
    fireEvent.click(screen.getByRole("button", { name: copy.list.closeForm }));
    expect(props.onClose).toHaveBeenCalledWith("f-1");
    fireEvent.click(screen.getByRole("button", { name: copy.list.copyLink }));
    await waitFor(() => expect(props.onCopyLink).toHaveBeenCalledWith("f-1"));
    await screen.findByRole("button", { name: copy.list.copied });
    // A closed form has no reopen: the producer cannot publish a closed form again.
    expect(screen.queryByRole("button", { name: /Mở lại|Reopen/ })).toBeNull();
  });

  it(`${fui("failed", locale)}tells a close that did not go through and keeps the status, with a retry (${locale})`, async () => {
    const onClose = vi.fn(refused);
    render(locale, { onClose });
    fireEvent.click(screen.getByRole("button", { name: copy.list.closeForm }));
    await screen.findByText(copy.list.closeFailed);
    expect(screen.getByText(copy.common.status.published)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(2));
  });

  it(`${fui("remove", locale)}asks before it deletes and keeps the form when kept (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(
      screen.getByRole("button", {
        name: fillFormsCopy(copy.list.removeAria, { name: "Báo lỗi ERP" }),
      }),
    );
    const ask = screen.getByRole("alertdialog");
    fireEvent.click(
      within(ask).getByRole("button", { name: copy.common.keep }),
    );
    expect(screen.queryByRole("alertdialog")).toBeNull();
    expect(props.onDelete).not.toHaveBeenCalled();
    fireEvent.click(
      screen.getByRole("button", {
        name: fillFormsCopy(copy.list.removeAria, { name: "Báo lỗi ERP" }),
      }),
    );
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: copy.list.remove,
      }),
    );
    await waitFor(() =>
      expect(props.onDelete).toHaveBeenCalledWith(
        expect.objectContaining({ id: "f-3" }),
      ),
    );
  });
});

function cleanup() {
  document.body.innerHTML = "";
}
