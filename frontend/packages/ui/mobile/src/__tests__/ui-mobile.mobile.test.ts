import { describe, it, expect } from "vitest";
import { MobileButton } from "../components/mobile-button";
import { MobileCard } from "../components/mobile-card";
import { MobileInput } from "../components/mobile-input";

describe("ui-mobile primitives", () => {
  it("exports MobileButton, MobileCard, MobileInput functions", () => {
    expect(typeof MobileButton).toBe("function");
    expect(typeof MobileCard).toBe("function");
    expect(typeof MobileInput).toBe("function");
  });
});
