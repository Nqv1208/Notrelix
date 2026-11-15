import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocSidePanelSurface } from "../doc-page-surfaces";

describe("side panel tab list", () => {
  it("holds only tabs, so the close button sits beside it and screen readers get a valid tab list", () => {
    renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="links"
        tasks={[]}
        relatedDocs={[]}
        toc={[]}
        comments={[]}
        callbacks={{ onPanelOpenChange: vi.fn() }}
      />,
    );
    const list = screen.getByRole("tablist");
    expect(list.children.length).toBe(3);
    for (const child of Array.from(list.children)) {
      expect(child.getAttribute("role")).toBe("tab");
    }
    expect(
      screen.getByRole("button", { name: docsCopy.panel.close }),
    ).toBeTruthy();
  });
});
