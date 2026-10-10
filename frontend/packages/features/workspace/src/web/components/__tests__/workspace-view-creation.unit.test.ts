import { describe, expect, it } from "vitest";
import { getWorkspaceViewCreationState } from "../workspace-view-creation";

describe("getWorkspaceViewCreationState", () => {
  it("disables board-backed views when there is no board", () => {
    const state = getWorkspaceViewCreationState("kanban", {
      boards: [],
      pages: [],
    });
    expect(state.disabled).toBe(true);
    expect(state.target).toEqual({});
  });

  it("requires a selected page for Doc views", () => {
    const resources = {
      boards: [],
      pages: [{ id: "doc-1", title: "Roadmap" }],
    };
    expect(getWorkspaceViewCreationState("doc", resources).disabled).toBe(true);
    expect(getWorkspaceViewCreationState("doc", resources, "doc-1")).toEqual({
      disabled: false,
      target: { pageId: "doc-1" },
    });
  });

  it("maps the first available board into board-backed views", () => {
    expect(
      getWorkspaceViewCreationState("table", {
        boards: [{ id: "board-1" }],
        pages: [],
      }),
    ).toEqual({ disabled: false, target: { boardId: "board-1" } });
  });
});
