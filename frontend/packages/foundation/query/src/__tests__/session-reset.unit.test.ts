import { describe, expect, it } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { onSessionReset, resetCacheForAccountTransition } from "../index";

describe("onSessionReset", () => {
  it("runs on an account transition and stops after unregistering", () => {
    const unsent = new Map([["card", "text"]]);
    const off = onSessionReset(() => unsent.clear());
    resetCacheForAccountTransition(new QueryClient());
    expect(unsent.size).toBe(0);
    unsent.set("card", "text");
    off();
    resetCacheForAccountTransition(new QueryClient());
    expect(unsent.size).toBe(1);
  });
});
