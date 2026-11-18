import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
import { I18nProvider } from "@notrelix/i18n";
import { chatCatalog } from "../web/components/chat.catalog";
import { CreateChannelDialog } from "../web/components/chat-dialogs";
import { useChatCopy } from "../web/components/use-chat-copy";

const groups = [
  { id: "g1", name: "Projects", position: 0, version: 1 },
  { id: "g2", name: "Company", position: 1, version: 1 },
];

function Harness(props: {
  groups?: typeof groups;
  error?: string | null;
  onSubmit: (values: unknown) => void;
}) {
  const copy = useChatCopy();
  return (
    <CreateChannelDialog
      open
      busy={false}
      error={props.error ?? null}
      groups={props.groups}
      copy={copy}
      onOpenChange={() => undefined}
      onSubmit={props.onSubmit}
    />
  );
}

function renderDialog(
  props: Parameters<typeof Harness>[0],
  locale: "en" | "vi" = "en",
) {
  return render(
    <I18nProvider catalogs={[chatCatalog]} initialLocale={locale}>
      <Harness {...props} />
    </I18nProvider>,
  );
}

describe("CreateChannelDialog to the Chat-Var-Channel-Create board (UC-CHT-A06)", () => {
  it("counts the name against its 80 characters and keeps Create off until there is a name", () => {
    renderDialog({ onSubmit: vi.fn() });
    const create = screen.getByRole("button", { name: "Create channel" });
    expect((create as HTMLButtonElement).disabled).toBe(true);
    fireEvent.change(screen.getByRole("textbox", { name: "Name" }), {
      target: { value: "customer-survey" },
    });
    expect(screen.getByText("15/80")).toBeTruthy();
    expect((create as HTMLButtonElement).disabled).toBe(false);
  });

  it("offers Public and Private as a radio group, public first, and says who sees each", () => {
    renderDialog({ onSubmit: vi.fn() });
    const group = screen.getByRole("radiogroup", {
      name: "Who can see this channel",
    });
    const radios = group.querySelectorAll('[role="radio"]');
    expect(radios).toHaveLength(2);
    expect(radios[0]?.getAttribute("aria-checked")).toBe("true");
    expect(screen.getByText(/except Guests can find and join it/)).toBeTruthy();
    expect(
      screen.getByText(/Workspace Owner or Admin who is not in it/),
    ).toBeTruthy();
  });

  it("submits the name, description, visibility and group", () => {
    const onSubmit = vi.fn();
    renderDialog({ groups, onSubmit });
    fireEvent.change(screen.getByRole("textbox", { name: "Name" }), {
      target: { value: "  secret-room " },
    });
    fireEvent.click(screen.getByRole("radio", { name: /^Private/ }));
    fireEvent.change(screen.getByRole("combobox", { name: "Channel group" }), {
      target: { value: "g2" },
    });
    fireEvent.change(
      screen.getByRole("textbox", { name: "Description (optional)" }),
      { target: { value: "Launch notes" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Create channel" }));
    expect(onSubmit).toHaveBeenCalledWith({
      name: "secret-room",
      topic: "Launch notes",
      isPrivate: true,
      groupId: "g2",
    });
  });

  it("leaves the group field out when the Workspace has no groups, and submits no group", () => {
    const onSubmit = vi.fn();
    renderDialog({ onSubmit });
    expect(
      screen.queryByRole("combobox", { name: "Channel group" }),
    ).toBeNull();
    fireEvent.change(screen.getByRole("textbox", { name: "Name" }), {
      target: { value: "plain" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Create channel" }));
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ isPrivate: false, groupId: null }),
    );
  });

  it("shows a failure from the server as an alert and words the labels in Vietnamese", () => {
    renderDialog({ error: "Đã có kênh với tên này.", onSubmit: vi.fn() }, "vi");
    expect(screen.getByRole("alert").textContent).toBe(
      "Đã có kênh với tên này.",
    );
    expect(
      screen.getByRole("radiogroup", { name: "Ai thấy kênh này" }),
    ).toBeTruthy();
    expect(screen.getByRole("radio", { name: /^Công khai/ })).toBeTruthy();
    expect(screen.getByRole("radio", { name: /^Riêng tư/ })).toBeTruthy();
  });
});
