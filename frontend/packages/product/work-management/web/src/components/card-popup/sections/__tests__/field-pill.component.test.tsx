import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { FieldControl } from "../field-editors";

const field = {
  id: "f-score",
  name: "Score",
  fieldType: "number",
  options: [],
} as never;

function show(extra: Partial<CardPopupModel>, canEdit = true) {
  const model = {
    mode: "view",
    copy: en,
    readOnly: canEdit ? null : { reason: "viewer" },
    capabilities: { canEditFields: canEdit },
    board: { members: [] },
    data: {},
    fields: {},
    card: { id: "item-1", fieldValues: { "f-score": 13 } },
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
      <CardPopupProvider value={model}>
        <FieldControl field={field} variant="pill" />
      </CardPopupProvider>
    </I18nProvider>,
  );
  return model;
}

describe("a custom field as a pill", () => {
  it("names a version clash at the pill and keeps both choices", () => {
    const keepMine = vi.fn();
    const takeTheirs = vi.fn();
    show({
      fieldConflicts: [
        {
          key: "field:f-score",
          field: "Score",
          theirs: "21",
          mine: "13",
          keepMine,
          takeTheirs,
        },
      ],
    });
    expect(screen.getByText("Someone just changed Score to 21")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Keep my value" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Use the new value (21)" }),
    );
    expect(keepMine).toHaveBeenCalledOnce();
    expect(takeTheirs).toHaveBeenCalledOnce();
  });

  it("shows a failed save with Retry and Discard", () => {
    const retry = vi.fn();
    const model = show({
      saves: { "field:f-score": { status: "error", retry } },
    });
    fireEvent.click(screen.getByRole("button", { name: "Retry" }));
    expect(retry).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "Discard change" }));
    expect(model.discardSave).toHaveBeenCalledWith("field:f-score");
  });
});
