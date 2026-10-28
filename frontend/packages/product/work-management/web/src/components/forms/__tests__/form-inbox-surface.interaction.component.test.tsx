import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  ALL_RESPONSES,
  BOARD_FIELDS,
  PEOPLE,
  formFixture,
  formsCopyFixture,
} from "../../../verification/forms-fixtures";
import { fillFormsCopy } from "../forms.copy";
import {
  FormInboxSurface,
  type FormInboxSurfaceProps,
} from "../form-inbox-surface";

const ok = async () => ({ ok: true }) as const;

function render(
  locale: "vi" | "en",
  overrides: Partial<FormInboxSurfaceProps> = {},
) {
  const props: FormInboxSurfaceProps = {
    copy: formsCopyFixture(locale),
    locale,
    timeZone: "Asia/Ho_Chi_Minh",
    now: new Date("2026-10-12T03:40:00.000Z"),
    status: "ready",
    form: formFixture({ status: "published" }),
    fields: BOARD_FIELDS,
    submissions: ALL_RESPONSES,
    people: PEOPLE,
    deciderName: "Hà Nguyễn",
    tab: "new",
    onTab: vi.fn(),
    selectedId: "s-1",
    onSelect: vi.fn(),
    onCreateItem: vi.fn(),
    onReject: vi.fn(ok),
    onSpam: vi.fn(ok),
    onRemove: vi.fn(ok),
    onOpenItem: vi.fn(),
    onReload: vi.fn(),
    onBack: vi.fn(),
    ...overrides,
  };
  renderPureUi(<FormInboxSurface {...props} />);
  return props;
}

const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[wm.forms.inbox:${id}] ` : "";

describe.each(["vi", "en"] as const)("form inbox (%s)", (locale) => {
  const copy = formsCopyFixture(locale);
  const t = copy.inbox;

  it(`${fui("tabs", locale)}counts every tab and lists only the open one, the newest first (${locale})`, () => {
    const props = render(locale);
    const tabs = screen.getByRole("tablist", { name: t.tabsAria });
    expect(
      within(tabs).getByRole("tab", { name: new RegExp(`${t.tabs.new}\\s*3`) }),
    ).toBeTruthy();
    expect(
      within(tabs).getByRole("tab", {
        name: new RegExp(`${t.tabs.rejected}\\s*2`),
      }),
    ).toBeTruthy();
    const options = within(
      screen.getByRole("listbox", { name: t.listAria }),
    ).getAllByRole("option");
    expect(options).toHaveLength(3);
    expect(options[0]!.textContent).toContain(
      "Làm mới tên máy kế toán Đà Nẵng",
    );
    expect(options[0]!.textContent).toContain("Trần Thu Trang");
    fireEvent.click(
      within(tabs).getByRole("tab", { name: new RegExp(t.tabs.spam) }),
    );
    expect(props.onTab).toHaveBeenCalledWith("spam");
  });

  it(`${fui("keyboard", locale)}moves through the list with the arrow keys (${locale})`, () => {
    const props = render(locale);
    const first = screen.getAllByRole("option")[0]!;
    first.focus();
    fireEvent.keyDown(first, { key: "ArrowDown" });
    expect(props.onSelect).toHaveBeenCalledWith("s-2");
    fireEvent.keyDown(first, { key: "ArrowUp" });
    expect(props.onSelect).toHaveBeenLastCalledWith("s-1");
  });

  it(`${fui("detail", locale)}shows each answer with the board field it goes into, and says when it goes nowhere (${locale})`, () => {
    render(locale);
    const detail = screen.getByRole("article", { name: t.detailAria });
    expect(
      within(detail).getByRole("heading", {
        name: "Làm mới tên máy kế toán Đà Nẵng",
      }),
    ).toBeTruthy();
    expect(
      within(detail).getByText(
        fillFormsCopy(t.mapsTo, { field: copy.builder.mapTitle }),
      ),
    ).toBeTruthy();
    expect(
      within(detail).getByText(fillFormsCopy(t.mapsTo, { field: "Ưu tiên" })),
    ).toBeTruthy();
    // The select answer is shown as the field option's name, not its id.
    expect(within(detail).getByText("Cao")).toBeTruthy();
    expect(within(detail).getByText(t.notMapped)).toBeTruthy();
  });

  it(`${fui("create-item", locale)}hands the response to the add-item dialog of the host (${locale})`, () => {
    const props = render(locale);
    fireEvent.click(screen.getByRole("button", { name: t.createItem }));
    expect(props.onCreateItem).toHaveBeenCalledWith(
      expect.objectContaining({ id: "s-1" }),
    );
  });

  it(`${fui("reject", locale)}rejects with no reason as readily as with one (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(screen.getByRole("button", { name: t.reject }));
    const dialog = await screen.findByRole("dialog", { name: t.rejectTitle });
    const confirm = within(dialog).getByRole("button", {
      name: t.rejectConfirm,
    }) as HTMLButtonElement;
    expect(confirm.disabled).toBe(false);
    fireEvent.click(confirm);
    await waitFor(() =>
      expect(props.onReject).toHaveBeenCalledWith(
        expect.objectContaining({ id: "s-1" }),
        "",
      ),
    );
    fireEvent.click(screen.getByRole("button", { name: t.reject }));
    const again = await screen.findByRole("dialog", { name: t.rejectTitle });
    fireEvent.change(
      within(again).getByRole("textbox", { name: t.reasonLabel }),
      { target: { value: "Trùng ERP-168" } },
    );
    fireEvent.click(
      within(again).getByRole("button", { name: t.rejectConfirm }),
    );
    await waitFor(() =>
      expect(props.onReject).toHaveBeenLastCalledWith(
        expect.anything(),
        "Trùng ERP-168",
      ),
    );
  });

  it(`${fui("spam-remove", locale)}marks spam at once and asks before it deletes (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(screen.getByRole("button", { name: t.spamAction }));
    expect(props.onSpam).toHaveBeenCalledWith(
      expect.objectContaining({ id: "s-1" }),
    );
    fireEvent.click(screen.getByRole("button", { name: t.removeAria }));
    const ask = screen.getByRole("alertdialog");
    fireEvent.click(
      within(ask).getByRole("button", { name: copy.common.keep }),
    );
    expect(props.onRemove).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: t.removeAria }));
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: t.removeConfirm,
      }),
    );
    await waitFor(() => expect(props.onRemove).toHaveBeenCalled());
  });

  it(`${fui("history", locale)}keeps the history line of a processed, rejected (with or without a reason) and spam response and offers no decision on them (${locale})`, () => {
    render(locale, { tab: "processed", selectedId: "s-4" });
    expect(screen.getByRole("button", { name: t.openItem })).toBeTruthy();
    expect(screen.queryByRole("button", { name: t.createItem })).toBeNull();
    document.body.innerHTML = "";
    render(locale, { tab: "rejected", selectedId: "s-5" });
    expect(screen.getByText(t.rejectedReason)).toBeTruthy();
    document.body.innerHTML = "";
    render(locale, { tab: "rejected", selectedId: "s-6" });
    expect(screen.getByText(/không ghi lý do|no reason given/)).toBeTruthy();
    document.body.innerHTML = "";
    render(locale, { tab: "spam", selectedId: "s-7" });
    expect(
      screen.getByText(
        new RegExp(
          fillFormsCopy(t.spamBanner, { name: "Hà Nguyễn", when: "" }).slice(
            0,
            12,
          ),
        ),
      ),
    ).toBeTruthy();
  });

  it(`${fui("states", locale)}says what an empty tab holds, loading, a load error with retry, a handled-by-someone-else conflict and a failed decision (${locale})`, () => {
    render(locale, { submissions: [] });
    expect(screen.getByText(t.empty.new)).toBeTruthy();
    document.body.innerHTML = "";
    render(locale, { submissions: [], tab: "rejected" });
    expect(screen.getByText(t.empty.rejected)).toBeTruthy();
    document.body.innerHTML = "";
    render(locale, { status: "loading" });
    expect(screen.getByRole("status", { name: t.loading })).toBeTruthy();
    document.body.innerHTML = "";
    const props = render(locale, { status: "error" });
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    expect(props.onReload).toHaveBeenCalled();
    document.body.innerHTML = "";
    render(locale, { conflict: true });
    expect(screen.getByText(t.conflictTitle)).toBeTruthy();
    document.body.innerHTML = "";
    const retry = vi.fn();
    render(locale, { failed: { retry } });
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    expect(retry).toHaveBeenCalled();
  });
});
