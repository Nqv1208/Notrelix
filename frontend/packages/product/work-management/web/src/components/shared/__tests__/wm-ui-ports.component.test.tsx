import { describe, expect, it, vi } from "vitest";
import { renderPureUi } from "@notrelix/testing";
import { CardCover } from "../../card-popup/capabilities/card-cover";
import { WmUiPortsProvider, type WmUiPorts } from "../wm-ui-ports";

const cover = { style: "strip", color: null, attachmentId: "att-1" } as never;

describe("Work Management UI ports", () => {
  it("draws no image when no host fills the ports", () => {
    const { container } = renderPureUi(
      <CardCover cover={cover} cardId="c-1" />,
    );
    expect(container.querySelector("img")).toBeNull();
  });

  it("draws the image the host read for the cover attachment", () => {
    const useCardImageUrl = vi.fn(() => "blob:cover-1");
    const ports: WmUiPorts = {
      useCardImageUrl,
      useRelationTarget: () => ({
        groups: [],
        isLoading: false,
        error: null,
        refetch: () => undefined,
      }),
      useMentionSuggestions: () => ({
        people: [],
        status: "success",
        retry: () => undefined,
      }),
    };
    const { container } = renderPureUi(
      <WmUiPortsProvider ports={ports}>
        <CardCover cover={cover} cardId="c-1" />
      </WmUiPortsProvider>,
    );
    expect(container.querySelector("img")?.getAttribute("src")).toBe(
      "blob:cover-1",
    );
    expect(useCardImageUrl).toHaveBeenCalledWith("c-1", "att-1");
  });
});
