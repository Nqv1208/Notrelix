import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { DescriptionSection } from "../description-section";

const lostRace = (by: string | null) =>
  Object.assign(new Error("stale"), {
    status: 412,
    kind: "conflict",
    details: by ? { changedByName: by } : {},
  });

function show(onUpdateItem: (id: string, patch: unknown) => Promise<unknown>) {
  const model = {
    mode: "view",
    copy: en,
    readOnly: null,
    capabilities: { canEditFields: true },
    card: { id: "item-1", workspaceId: "ws-1", descriptionMd: "" },
    saves: {},
    track: (_key: string, run: () => Promise<unknown>) =>
      void run().catch(() => undefined),
    callbacks: { onUpdateItem },
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <DescriptionSection />
      </CardPopupProvider>
    </I18nProvider>,
  );
}

async function typeAndSave(text: string) {
  fireEvent.click(screen.getByRole("button", { name: "Add a description…" }));
  fireEvent.change(
    screen.getByRole("textbox", { name: en.sections.description.editorLabel }),
    {
      target: { value: text },
    },
  );
  fireEvent.click(screen.getByRole("button", { name: "Save" }));
  await screen.findByRole("alert");
}

describe("description save that lost the race", () => {
  it("names who saved first, keeps my text in the editor and sends nothing more", async () => {
    const onUpdateItem = vi.fn().mockRejectedValue(lostRace("Ada Lovelace"));
    show(onUpdateItem);
    await typeAndSave("my text");
    expect(screen.getByRole("alert").textContent).toContain(
      "Ada Lovelace saved the description first",
    );
    expect(
      (
        screen.getByRole("textbox", {
          name: en.sections.description.editorLabel,
        }) as HTMLTextAreaElement
      ).value,
    ).toBe("my text");
    expect(onUpdateItem).toHaveBeenCalledTimes(1);
  });

  it("Keep mine saves my text again over theirs", async () => {
    const onUpdateItem = vi
      .fn()
      .mockRejectedValueOnce(lostRace(null))
      .mockResolvedValue(undefined);
    show(onUpdateItem);
    await typeAndSave("my text");
    expect(screen.getByRole("alert").textContent).toContain("Someone else");
    fireEvent.click(screen.getByRole("button", { name: "Keep mine" }));
    await waitFor(() => expect(onUpdateItem).toHaveBeenCalledTimes(2));
    expect(onUpdateItem).toHaveBeenLastCalledWith("item-1", {
      descriptionMd: "my text",
    });
    await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
  });

  it("View the new version closes the editor without a second save", async () => {
    const onUpdateItem = vi.fn().mockRejectedValue(lostRace("Ada Lovelace"));
    show(onUpdateItem);
    await typeAndSave("my text");
    fireEvent.click(
      screen.getByRole("button", { name: "View the new version" }),
    );
    expect(screen.queryByRole("textbox")).toBeNull();
    expect(onUpdateItem).toHaveBeenCalledTimes(1);
  });

  it("another refusal is not a clash: the save error stays with Retry", async () => {
    const onUpdateItem = vi.fn().mockRejectedValue(new Error("boom"));
    show(onUpdateItem);
    fireEvent.click(screen.getByRole("button", { name: "Add a description…" }));
    fireEvent.change(
      screen.getByRole("textbox", {
        name: en.sections.description.editorLabel,
      }),
      { target: { value: "x" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(onUpdateItem).toHaveBeenCalledTimes(1));
    expect(screen.queryByText(/saved the description first/)).toBeNull();
  });
});
