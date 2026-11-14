import { describe, expect, it } from "vitest";
import { act, renderHook } from "@testing-library/react";

import { useOnline } from "../use-online";

describe("useOnline", () => {
  it("follows the offline and online events of the window", () => {
    const { result } = renderHook(() => useOnline());
    expect(result.current).toBe(true);

    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    expect(result.current).toBe(false);

    act(() => {
      window.dispatchEvent(new Event("online"));
    });
    expect(result.current).toBe(true);
  });
});
