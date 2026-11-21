import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import { en, vi as viCopy } from "../web/components/chat.copy";
import { ChatInviteDialog } from "../web/components/chat-channel-members";
import type { PickablePerson } from "../web/components/chat-person-picker";

const people: readonly PickablePerson[] = [
  { id: "u1", name: "Đặng Khánh Linh", detail: "Trưởng nhóm kiểm soát" },
  { id: "u2", name: "Vũ Đức Hiếu", detail: "Kỹ sư tích hợp" },
  { id: "u3", name: "Ngô Thanh Mai" },
  { id: "u4", name: "Bùi Quang Nam", detail: "Đối tác kiểm toán", guest: true },
];

function mount(
  options: {
    candidates?: readonly PickablePerson[];
    isPrivate?: boolean;
    notice?: string | null;
    locale?: "en" | "vi";
  } = {},
) {
  const onSubmit = vi.fn();
  const onOpenChange = vi.fn();
  render(
    <ChatInviteDialog
      open
      busy={false}
      channelName="launch-2026"
      isPrivate={options.isPrivate}
      candidates={options.candidates ?? people}
      error={null}
      notice={options.notice}
      copy={options.locale === "vi" ? viCopy : en}
      onOpenChange={onOpenChange}
      onSubmit={onSubmit}
    />,
  );
  return { onSubmit, onOpenChange, dialog: screen.getByRole("dialog") };
}

describe("ChatInviteDialog against Chat-Var-Invite (UC-CHT-A06)", () => {
  it("names the private channel, groups members and guests, and shows titles under names", () => {
    const { dialog } = mount();
    expect(
      within(dialog).getByText(
        "Private channel #launch-2026: only invited people see the channel and read its messages.",
      ),
    ).toBeTruthy();
    const members = within(dialog).getByRole("region", {
      name: "Workspace members",
    });
    const guests = within(dialog).getByRole("region", { name: "Guests" });
    expect(within(members).getAllByRole("checkbox")).toHaveLength(3);
    expect(within(guests).getAllByRole("checkbox")).toHaveLength(1);
    expect(within(members).getByText("Trưởng nhóm kiểm soát")).toBeTruthy();
    expect(within(guests).getByText("Bùi Quang Nam")).toBeTruthy();
  });

  it("puts each picked person in the token field as a chip, counts them on the button, and removes one from its chip", () => {
    const { dialog, onSubmit } = mount();
    const submit = () =>
      within(dialog).getByRole("button", {
        name: /^Invite/,
      }) as HTMLButtonElement;
    expect(submit().disabled).toBe(true);

    fireEvent.click(within(dialog).getByRole("checkbox", { name: /Linh/ }));
    fireEvent.click(within(dialog).getByRole("checkbox", { name: /Hiếu/ }));

    const field = within(dialog).getByRole("group", { name: "Find people" });
    expect(within(field).getByText("Linh")).toBeTruthy();
    expect(within(field).getByText("Hiếu")).toBeTruthy();
    expect(submit().textContent).toBe("Invite 2 people");

    fireEvent.click(
      within(field).getByRole("button", { name: "Remove Vũ Đức Hiếu" }),
    );
    expect(within(field).queryByText("Hiếu")).toBeNull();
    expect(submit().textContent).toBe("Invite 1 person");

    fireEvent.click(submit());
    expect(onSubmit).toHaveBeenCalledWith(["u1"]);
  });

  it("filters the list from the field without accents and removes the last chip on Backspace", () => {
    const { dialog } = mount();
    fireEvent.click(within(dialog).getByRole("checkbox", { name: /Linh/ }));
    const search = within(dialog).getByRole("textbox", { name: "Find people" });

    fireEvent.change(search, { target: { value: "hieu" } });
    expect(within(dialog).getAllByRole("checkbox")).toHaveLength(1);
    expect(within(dialog).getByRole("checkbox", { name: /Hiếu/ })).toBeTruthy();

    fireEvent.change(search, { target: { value: "" } });
    fireEvent.keyDown(search, { key: "Backspace" });
    expect(
      within(dialog).queryByRole("button", { name: "Remove Đặng Khánh Linh" }),
    ).toBeNull();
  });

  it("warns that a picked guest only sees this channel", () => {
    const { dialog } = mount();
    expect(within(dialog).queryByText(/is a guest/)).toBeNull();
    fireEvent.click(within(dialog).getByRole("checkbox", { name: /Nam/ }));
    expect(
      within(dialog).getByText(
        "Bùi Quang Nam is a guest: they only see this channel, not the directory, and cannot use @channel.",
      ),
    ).toBeTruthy();
  });

  it("says inside the dialog when some people were skipped", () => {
    const { dialog } = mount({ notice: "1 person could not be added." });
    expect(
      within(dialog).getByText("1 person could not be added."),
    ).toBeTruthy();
  });

  it("draws the empty state with a disabled button when nobody is left to invite", () => {
    const { dialog } = mount({ candidates: [] });
    expect(
      within(dialog).getByText("Everyone is already in this channel"),
    ).toBeTruthy();
    expect(
      within(dialog).getByText(
        "No one is left to invite. Add new people to the Workspace in member settings.",
      ),
    ).toBeTruthy();
    expect(
      (
        within(dialog).getByRole("button", {
          name: "Invite",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("uses the plain hint for a public channel and closes from Skip", () => {
    const { dialog, onOpenChange } = mount({ isPrivate: false });
    expect(
      within(dialog).getByText(
        "Only people already in this Workspace can be invited.",
      ),
    ).toBeTruthy();
    fireEvent.click(within(dialog).getByRole("button", { name: "Skip" }));
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("reads like the board in Vietnamese", () => {
    const { dialog } = mount({ locale: "vi" });
    expect(
      within(dialog).getByText(
        "Kênh riêng tư #launch-2026: chỉ người được mời mới thấy kênh và đọc được tin nhắn.",
      ),
    ).toBeTruthy();
    expect(
      within(dialog).getByRole("region", { name: "Thành viên workspace" }),
    ).toBeTruthy();
    expect(within(dialog).getByRole("region", { name: "Khách" })).toBeTruthy();
    fireEvent.click(within(dialog).getByRole("checkbox", { name: /Linh/ }));
    expect(
      within(dialog).getByRole("button", { name: "Mời 1 người" }),
    ).toBeTruthy();
  });

  it("is a full page below 768 px: a back button, the short title, no desktop close or Skip", () => {
    const { dialog } = mount();
    expect(dialog.className).toContain("max-md:inset-0");
    expect(dialog.className).toContain("max-md:rounded-none");
    expect(within(dialog).getByText("Invite to #launch-2026")).toBeTruthy();
    expect(within(dialog).getByText("Private channel")).toBeTruthy();
    const skip = within(dialog).getByRole("button", { name: "Skip" });
    expect(skip.className).toContain("max-md:hidden");
  });
});
