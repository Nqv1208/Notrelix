import { cleanup, configure } from "@testing-library/react";
import { afterEach, vi } from "vitest";
import "@testing-library/jest-dom/vitest";

// findBy*/waitFor poll 1 s by default, which a loaded runner can exceed.
configure({ asyncUtilTimeout: 5000 });

// Automatically cleanup React Testing Library and restore mocks after each test
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

// Stub window.matchMedia for JSDOM
if (typeof window !== "undefined") {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }),
  });

  // Stub ResizeObserver for JSDOM
  if (!window.ResizeObserver) {
    window.ResizeObserver = class ResizeObserver {
      observe() {}
      unobserve() {}
      disconnect() {}
    };
  }

  // jsdom lacks the layout and pointer-capture APIs the Radix listbox calls
  // when it opens; stub them so the shared Select can be driven in tests.
  if (typeof Element !== "undefined") {
    Element.prototype.scrollIntoView ??= () => undefined;
    Element.prototype.hasPointerCapture ??= () => false;
    Element.prototype.releasePointerCapture ??= () => undefined;
  }
}
