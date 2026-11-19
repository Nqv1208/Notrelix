import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  commandCopyFixture,
  commandNoneScenario,
  commandSearchScenario,
} from "../../../verification/workspace-command-fixtures";
import {
  WorkspaceCommandPaletteSurface,
  type WorkspaceCommandPaletteSurfaceProps,
} from "../command-palette-surface";

const copy = commandCopyFixture("vi");

function renderPalette(
  scenario: ReturnType<typeof commandSearchScenario>,
  overrides: Partial<WorkspaceCommandPaletteSurfaceProps> = {},
) {
  const props: WorkspaceCommandPaletteSurfaceProps = {
    ...scenario,
    onOpenChange: vi.fn(),
    onSelect: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceCommandPaletteSurface {...props} />);
  return props;
}

describe("WorkspaceCommandPaletteSurface server search states", () => {
  it("FUI[workspace.command-palette:loading] shows skeleton rows while the search is in flight and does not claim there is no match", () => {
    renderPalette(commandNoneScenario(), {
      search: { status: "loading", onRetry: vi.fn() },
    });
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
    expect(screen.queryByText(/Không tìm thấy/)).toBeNull();
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("FUI[workspace.command-palette:loading-with-local-results] keeps the rows the palette already holds usable while the search loads", () => {
    renderPalette(commandSearchScenario(), {
      search: { status: "loading", onRetry: vi.fn() },
    });
    expect(screen.getAllByRole("option").length).toBeGreaterThan(0);
    expect(screen.queryByRole("status", { name: copy.loading })).toBeNull();
  });

  it("FUI[workspace.command-palette:error] a failed search says so with a retry, and is not worded as no match", () => {
    const onRetry = vi.fn();
    renderPalette(commandNoneScenario(), {
      search: { status: "error", onRetry },
    });
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain(copy.searchError.title);
    expect(alert.textContent).toContain(copy.searchError.text);
    expect(screen.queryByText(/Không tìm thấy/)).toBeNull();
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.searchError.retry }),
    );
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.command-palette:error-with-local-results] a failed search keeps the local rows and still offers the retry", () => {
    renderPalette(commandSearchScenario(), {
      search: { status: "error", onRetry: vi.fn() },
    });
    expect(screen.getByRole("alert")).toBeTruthy();
    expect(screen.getAllByRole("option").length).toBeGreaterThan(0);
  });

  it("FUI[workspace.command-palette:no-match] with no search problem, nothing found still reads as no match", () => {
    renderPalette(commandNoneScenario());
    expect(screen.getByText(/Không tìm thấy/)).toBeTruthy();
    expect(screen.queryByRole("alert")).toBeNull();
    expect(screen.queryByRole("status", { name: copy.loading })).toBeNull();
  });
});

describe("WorkspaceCommandPaletteSurface on a phone (full-screen search sheet)", () => {
  const original = window.matchMedia;
  afterEach(() => {
    window.matchMedia = original;
  });
  function asPhone() {
    window.matchMedia = ((query: string) => ({
      matches: query.includes("max-width: 639px"),
      media: query,
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })) as typeof window.matchMedia;
  }

  it("FUI[workspace.command-palette:phone-results] draws the query field, the type chips and the result count, and Cancel closes it", () => {
    asPhone();
    const props = renderPalette(commandSearchScenario());
    expect(screen.getByRole("combobox")).toBeTruthy();
    expect(screen.getAllByRole("tab").length).toBeGreaterThan(1);
    expect(screen.getAllByRole("option").length).toBeGreaterThan(0);
    expect(screen.getByText(/\d+ kết quả/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.phone.cancel }));
    expect(props.onOpenChange).toHaveBeenCalledWith(false);
  });

  it("FUI[workspace.command-palette:phone-none] says nothing matched when the search answered with nothing", () => {
    asPhone();
    renderPalette(commandNoneScenario());
    expect(screen.getByText(/Không tìm thấy/)).toBeTruthy();
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("FUI[workspace.command-palette:phone-loading] shows the loading rows, not a no-match line", () => {
    asPhone();
    renderPalette(commandNoneScenario(), {
      search: { status: "loading", onRetry: vi.fn() },
    });
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
    expect(screen.queryByText(/Không tìm thấy/)).toBeNull();
  });

  it("FUI[workspace.command-palette:phone-error] shows the failure with a retry, not a no-match line", () => {
    asPhone();
    const onRetry = vi.fn();
    renderPalette(commandNoneScenario(), {
      search: { status: "error", onRetry },
    });
    expect(screen.queryByText(/Không tìm thấy/)).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: copy.searchError.retry }),
    );
    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
