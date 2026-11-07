import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { LabelsPanel } from "../labels-panel";

vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<object>()),
  useFullBoard: () => ({ groups: [] }),
}));

const labels = Array.from({ length: 200 }, (_, i) => ({
  id: `l${i}`,
  name: `Nhãn ${i}`,
  color: "#1666CC",
}));

function show(count: number) {
  const model = {
    mode: "view",
    copy: en,
    readOnly: null,
    capabilities: { canEditFields: true },
    card: { id: "i1", boardId: "b1", workspaceId: "w1", labels: [] },
    data: { boardLabels: labels.slice(0, count) },
    saves: {},
    slots: {},
    actions: {},
    callbacks: {},
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <LabelsPanel onClose={() => undefined} />
      </CardPopupProvider>
    </I18nProvider>,
  );
}

describe("the label picker with many labels", () => {
  it("mounts only the rows in view for 200 labels and follows the scroll", () => {
    show(200);
    const first = screen.getAllByRole("checkbox");
    expect(first.length).toBeLessThan(60);
    expect(screen.getByText("Nhãn 0")).toBeTruthy();
    expect(screen.queryByText("Nhãn 150")).toBeNull();
    const list = first[0]!.parentElement!.parentElement!;
    list.scrollTop = 150 * 38;
    fireEvent.scroll(list);
    expect(screen.getByText("Nhãn 150")).toBeTruthy();
    expect(screen.queryByText("Nhãn 0")).toBeNull();
  });

  it("mounts every row of a short list", () => {
    show(40);
    expect(screen.getAllByRole("checkbox")).toHaveLength(40);
  });
});
