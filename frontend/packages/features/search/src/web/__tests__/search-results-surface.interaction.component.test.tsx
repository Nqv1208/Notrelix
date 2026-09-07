import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  searchResultsDefaultScenario,
  searchResultsEmptyScenario,
} from "../../verification/search-ui-fixtures";
import { SearchResultsSurface } from "../search-results-surface";

describe("search web pure surface", () => {
  it("FUI[search.results:render] renders search results from deterministic fixtures", () => {
    renderPureUi(
      <SearchResultsSurface
        query="plan"
        results={searchResultsDefaultScenario()}
        activeTypes={["page", "board", "task"]}
        onSearchChange={() => undefined}
        onOpenResult={() => undefined}
      />,
    );

    expect(screen.getByText("Operating plan")).toBeTruthy();
    expect(screen.getByText("Migration risks")).toBeTruthy();
  });

  it("FUI[search.results:select] routes opening a result through the injected callback", () => {
    const onOpenResult = vi.fn();

    renderPureUi(
      <SearchResultsSurface
        query="plan"
        results={searchResultsDefaultScenario()}
        activeTypes={["page", "board", "task"]}
        onSearchChange={() => undefined}
        onOpenResult={onOpenResult}
      />,
    );

    fireEvent.click(screen.getByText("Operating plan"));

    expect(onOpenResult).toHaveBeenCalledWith(
      expect.objectContaining({ id: "page-operating-plan" }),
    );
  });

  it("FUI[search.results:filter] toggles result types through the injected callback", () => {
    const onSearchChange = vi.fn();

    renderPureUi(
      <SearchResultsSurface
        query="plan"
        results={searchResultsDefaultScenario()}
        activeTypes={["page", "board", "task"]}
        onSearchChange={onSearchChange}
        onOpenResult={() => undefined}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Pages" }));

    expect(onSearchChange).toHaveBeenCalledWith("plan", ["board", "task"]);
  });

  it("FUI[search.results:empty] renders the empty search state without API effects", () => {
    renderPureUi(
      <SearchResultsSurface
        query=""
        results={searchResultsEmptyScenario()}
        activeTypes={["page"]}
        onSearchChange={() => undefined}
        onOpenResult={() => undefined}
      />,
    );

    expect(
      screen.getByText(
        "Enter a search query to find content across the workspace.",
      ),
    ).toBeTruthy();
  });
});
