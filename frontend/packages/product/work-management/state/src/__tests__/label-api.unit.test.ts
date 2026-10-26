import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createLabelApi } from "../api/label.api";

function apiWith(methods: { get?: unknown; post?: unknown }) {
  return createLabelApi({ api: methods } as unknown as NotrelixClient);
}

describe("createLabelApi", () => {
  it("reads a board's labels by the label id the server names", async () => {
    const get = vi.fn(async () => [
      { labelId: "l-1", name: "Bug", color: "#E53935" },
      { labelId: "l-2", name: null, color: "#1E88E5" },
    ]);

    const labels = await apiWith({ get }).getBoardLabels("b-1");

    expect(get).toHaveBeenCalledWith("/boards/b-1/labels");
    expect(labels).toEqual([
      { id: "l-1", name: "Bug", color: "#E53935" },
      { id: "l-2", name: "", color: "#1E88E5" },
    ]);
  });

  it("answers an empty list when the board has no labels", async () => {
    const get = vi.fn(async () => null);

    expect(await apiWith({ get }).getBoardLabels("b-1")).toEqual([]);
  });

  it("takes the new label's id from the bare id the server returns", async () => {
    const post = vi.fn(async () => "l-9");

    const label = await apiWith({ post }).createLabel({
      boardId: "b-1",
      color: "#43A047",
      name: "Idea",
    });

    expect(label).toEqual({ id: "l-9", name: "Idea", color: "#43A047" });
  });
});
