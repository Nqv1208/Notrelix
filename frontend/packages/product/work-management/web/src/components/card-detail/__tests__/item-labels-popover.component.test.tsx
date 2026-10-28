import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { ItemLabelsPopover, labelCreate } from "../item-field-editors";
import { en } from "../item-sheet.copy";

const labels = [
  { id: "l-bug", name: "Bug", color: "#D6453D" },
  { id: "l-doc", name: "Docs", color: "#8A8F98" },
];

function renderPicker(onCreate: (name: string) => Promise<boolean>) {
  const onToggle = vi.fn();
  renderPureUi(
    <ItemLabelsPopover
      labels={labels}
      selectedIds={new Set<string>()}
      label="Labels"
      emptyText="none"
      readOnly={false}
      onToggle={onToggle}
      create={labelCreate(en.properties, onCreate)}
    >
      <button type="button">Add label</button>
    </ItemLabelsPopover>,
  );
  fireEvent.click(screen.getByRole("button", { name: "Add label" }));
  return { onToggle };
}

const search = () =>
  screen.findByRole("textbox", { name: en.properties.labelSearch });

describe("the item label picker", () => {
  it("offers Create label for text that no label is named, and clears after creating", async () => {
    const onCreate = vi.fn().mockResolvedValue(true);
    renderPicker(onCreate);
    const input = await search();
    fireEvent.change(input, { target: { value: "Urgent" } });
    fireEvent.click(
      screen.getByRole("button", { name: /Create label “Urgent”/ }),
    );
    await waitFor(() => expect(onCreate).toHaveBeenCalledWith("Urgent"));
    await waitFor(() => expect((input as HTMLInputElement).value).toBe(""));
  });

  it("offers no create row when a label already has that name, ignoring case", async () => {
    renderPicker(vi.fn());
    fireEvent.change(await search(), { target: { value: "bug" } });
    expect(screen.queryByRole("button", { name: /Create label/ })).toBeNull();
    expect(screen.getByRole("button", { name: "Bug" })).toBeTruthy();
  });

  it("keeps the typed name and offers Retry when creating fails", async () => {
    const onCreate = vi
      .fn()
      .mockResolvedValueOnce(false)
      .mockResolvedValueOnce(true);
    renderPicker(onCreate);
    const input = await search();
    fireEvent.change(input, { target: { value: "Urgent" } });
    fireEvent.click(
      screen.getByRole("button", { name: /Create label “Urgent”/ }),
    );
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(en.properties.createLabelFailed);
    expect((input as HTMLInputElement).value).toBe("Urgent");
    expect(input.getAttribute("aria-describedby")).toBe(alert.id);
    fireEvent.click(
      screen.getByRole("button", { name: en.properties.createLabelRetry }),
    );
    await waitFor(() => expect(onCreate).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
  });
});
