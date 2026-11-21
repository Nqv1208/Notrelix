import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocVersionPanelSurface } from "../doc-versions-surface";

const base = { copy: docsCopy, versions: [], selectedId: null } as const;

describe("document version history panel states", () => {
  it("says it is loading and shows no empty text meanwhile", () => {
    renderPureUi(<DocVersionPanelSurface {...base} status="loading" />);
    expect(screen.getByRole("status").textContent).toBe(
      docsCopy.versions.loading,
    );
    expect(screen.queryByText(docsCopy.versions.emptyTitle)).toBeNull();
  });

  it("shows the error with a retry that rereads the list", () => {
    const onRetry = vi.fn();
    renderPureUi(
      <DocVersionPanelSurface {...base} status="error" onRetry={onRetry} />,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      docsCopy.versions.errorTitle,
    );
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.versions.retry }),
    );
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("shows the empty text only for a loaded empty list", () => {
    renderPureUi(<DocVersionPanelSurface {...base} />);
    expect(screen.getByText(docsCopy.versions.emptyTitle)).toBeTruthy();
  });
});
