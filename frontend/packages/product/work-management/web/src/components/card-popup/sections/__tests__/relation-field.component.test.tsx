import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { RelationLink } from "@notrelix/work-management-core";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { RelationChips, RelationPickerPanel } from "../relation-field";
import { FieldControl } from "../field-editors";

const target = vi.hoisted(() => ({
  state: {} as {
    board?: { title: string };
    groups: { cards: unknown[] }[];
    isLoading: boolean;
    error: unknown;
    refetch: () => void;
  },
}));
vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useFullBoard: () => target.state,
}));

const visible = (id: string, key: string, title: string): RelationLink => ({
  itemId: id,
  itemKey: key,
  title,
  locked: false,
});
const locked = (id: string): RelationLink => ({
  itemId: id,
  itemKey: null,
  title: null,
  locked: true,
});

function items(count: number) {
  return Array.from({ length: count }, (_, i) => ({
    id: `t-${i}`,
    itemKey: `CT-${i}`,
    title: `Target item ${i}`,
  }));
}

function loaded(cards: unknown[]) {
  target.state = {
    board: { title: "Chiến dịch Tết 2027" },
    groups: [{ cards }],
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  };
}

function wrap(node: React.ReactNode, extra: Partial<CardPopupModel> = {}) {
  const model = {
    mode: "view",
    copy: en,
    readOnly: null,
    capabilities: { canEditFields: true },
    board: { workspaceId: "ws-1", members: [] },
    data: {},
    fields: {},
    card: { id: "item-1", fieldValues: {}, relationLinks: {} },
    saves: {},
    fieldConflicts: [],
    track: vi.fn(),
    discardSave: vi.fn(),
    raiseFieldConflict: vi.fn(),
    clearFieldConflict: vi.fn(),
    ...extra,
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>{node}</CardPopupProvider>
    </I18nProvider>,
  );
  return model;
}

const field = {
  id: "f-rel",
  name: "Related work",
  fieldType: "relation",
  relationBoardId: "b-target",
  options: [],
} as never;

function panel(links: RelationLink[], onSave = vi.fn()) {
  wrap(
    <RelationPickerPanel
      targetBoardId="b-target"
      links={links}
      onSave={onSave}
      onCancel={vi.fn()}
    />,
  );
  return onSave;
}

describe("a Relation field in the item sheet", () => {
  it("shows an empty field as not linked", () => {
    wrap(<FieldControl field={field} variant="pill" />);
    expect(screen.getByText("Not linked")).toBeTruthy();
  });

  it("shows the picked items as chips with their keys", () => {
    wrap(<FieldControl field={field} variant="pill" />, {
      card: {
        id: "item-1",
        fieldValues: {},
        relationLinks: {
          "f-rel": [
            visible("a", "CT-21", "Banner"),
            visible("b", "CT-24", "Ảnh"),
          ],
        },
      },
    } as never);
    expect(screen.getByText("CT-21")).toBeTruthy();
    expect(screen.getByText("CT-24")).toBeTruthy();
  });

  it("shows a locked link as No access and never a title, even if one reached the client", () => {
    renderPureUi(
      <I18nProvider catalogs={[]} initialLocale="en">
        <CardPopupProvider value={{ copy: en } as unknown as CardPopupModel}>
          <RelationChips
            withTitle
            links={[
              {
                itemId: "x",
                itemKey: "CT-99",
                title: "Secret plan",
                locked: true,
              },
            ]}
          />
        </CardPopupProvider>
      </I18nProvider>,
    );
    expect(screen.getByText("No access")).toBeTruthy();
    expect(screen.queryByText("Secret plan")).toBeNull();
    expect(screen.queryByText("CT-99")).toBeNull();
  });

  it("shows the field without an editor to a reader who cannot edit", () => {
    wrap(<FieldControl field={field} variant="pill" />, {
      readOnly: { reason: "viewer" },
      capabilities: { canEditFields: false },
    } as never);
    expect(screen.queryByRole("button", { name: /Related work/ })).toBeNull();
  });
});

describe("the item picker of a Relation field", () => {
  it("shows the loading state", () => {
    target.state = {
      groups: [],
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    };
    panel([]);
    expect(screen.getByText("Loading items…")).toBeTruthy();
  });

  it("lists the target items without the item itself, and saves what was picked", () => {
    loaded([...items(2), { id: "item-1", itemKey: "CT-self", title: "Me" }]);
    const onSave = panel([]);
    expect(screen.queryByText("Me")).toBeNull();
    fireEvent.click(screen.getByRole("option", { name: /Target item 1/ }));
    fireEvent.click(screen.getByRole("button", { name: "Save links" }));
    expect(onSave).toHaveBeenCalledWith(["t-1"]);
  });

  it("filters by what is typed and says when nothing matches", () => {
    loaded(items(3));
    panel([]);
    fireEvent.change(screen.getByRole("textbox", { name: "Find an item" }), {
      target: { value: "zzz" },
    });
    expect(screen.getByText("No item matches “zzz”")).toBeTruthy();
  });

  it("says the board has no item", () => {
    loaded([]);
    panel([]);
    expect(screen.getByText("No item on this board.")).toBeTruthy();
  });

  it("deselects a linked item and keeps the locked ones in the saved set", () => {
    loaded(items(3));
    const onSave = panel([
      visible("t-0", "CT-0", "Target item 0"),
      locked("hid-1"),
    ]);
    expect(
      screen.getByText(
        "This field links 2 items: you can see 1, the other 1 show as “No access”.",
      ),
    ).toBeTruthy();
    expect(
      screen.getByText("You do not have access to this item"),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("option", { name: /Target item 0/ }));
    fireEvent.click(screen.getByRole("option", { name: /Target item 2/ }));
    fireEvent.click(screen.getByRole("button", { name: "Save links" }));
    expect(onSave).toHaveBeenCalledWith(["hid-1", "t-2"]);
  });

  it("stops at 100 links, locked ones counted, and says so", () => {
    loaded(items(120));
    panel([
      ...Array.from({ length: 99 }, (_, i) =>
        visible(`t-${i}`, `CT-${i}`, `Target item ${i}`),
      ),
      locked("hid"),
    ]);
    expect(screen.getByText("A field links at most 100 items.")).toBeTruthy();
    const extra = screen.getByRole("option", { name: /Target item 110/ });
    expect((extra as HTMLButtonElement).disabled).toBe(true);
    // A linked one can still be taken off.
    expect(
      (
        screen.getByRole("option", {
          name: /Target item 5$/,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(false);
  });

  it("lists nothing of a target the reader cannot see or that is gone, and lets the visible links be removed", () => {
    target.state = {
      groups: [],
      isLoading: false,
      error: { kind: "not_found" },
      refetch: vi.fn(),
    };
    const onSave = panel([visible("a", "CT-21", "Banner"), locked("hid")]);
    expect(
      screen.getByText(/gone or you cannot see it/, { exact: false }),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("option", { name: /CT-21/ }));
    fireEvent.click(screen.getByRole("button", { name: "Save links" }));
    expect(onSave).toHaveBeenCalledWith(["hid"]);
  });

  it("says a field with no target board cannot list items", () => {
    loaded([]);
    wrap(
      <RelationPickerPanel
        targetBoardId={undefined}
        links={[]}
        onSave={vi.fn()}
        onCancel={vi.fn()}
      />,
    );
    expect(
      screen.getByText("This field has no board to link to yet."),
    ).toBeTruthy();
  });
});
