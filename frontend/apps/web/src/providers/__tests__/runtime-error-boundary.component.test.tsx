import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RuntimeErrorBoundary } from "../runtime-error-boundary";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const state = vi.hoisted(() => ({
  broken: true,
  runtime: null as unknown,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => state.runtime,
}));

function Flaky() {
  if (state.broken) {
    throw new Error("render failed");
  }
  return <p>Page content</p>;
}

describe("RuntimeErrorBoundary", () => {
  let container: HTMLDivElement;
  let root: Root;
  let reportError: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    state.broken = true;
    reportError = vi.fn();
    state.runtime = {
      telemetry: { reportError },
      env: { isProduction: true },
    };
    container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
    vi.spyOn(console, "error").mockImplementation(() => {});
  });

  afterEach(() => {
    act(() => root.unmount());
    container.remove();
    vi.restoreAllMocks();
  });

  it("renders the children when nothing throws", () => {
    state.broken = false;

    act(() => {
      root.render(
        <RuntimeErrorBoundary>
          <Flaky />
        </RuntimeErrorBoundary>,
      );
    });

    expect(container.textContent).toContain("Page content");
    expect(container.querySelector("button")).toBeNull();
  });

  it("shows a safe error state with a retry instead of a blank screen, and reports the error", () => {
    act(() => {
      root.render(
        <RuntimeErrorBoundary>
          <Flaky />
        </RuntimeErrorBoundary>,
      );
    });

    expect(container.textContent).toContain("Something went wrong");
    expect(container.textContent).not.toContain("render failed");
    expect(container.querySelector("button")?.textContent).toBe("Try again");
    expect(reportError).toHaveBeenCalledWith(
      expect.objectContaining({ message: "render failed" }),
      expect.objectContaining({ componentStack: expect.any(String) }),
    );
  });

  it("renders the children again when the user retries after the cause is fixed", () => {
    act(() => {
      root.render(
        <RuntimeErrorBoundary>
          <Flaky />
        </RuntimeErrorBoundary>,
      );
    });

    state.broken = false;
    act(() => {
      container.querySelector("button")?.click();
    });

    expect(container.textContent).toContain("Page content");
    expect(container.textContent).not.toContain("Something went wrong");
  });
});
