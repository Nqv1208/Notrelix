import { describe, expect, it } from "vitest";
import { render } from "@testing-library/react";
import { NotrelixLogoMark } from "../components/brand/notrelix-logo";

describe("NotrelixLogoMark", () => {
  it("gives every mark its own gradient ids, each fill pointing at a gradient of its own svg", () => {
    // A mark kept in a hidden panel must not hold the id a visible mark paints with.
    const { container } = render(
      <>
        <NotrelixLogoMark />
        <NotrelixLogoMark />
      </>,
    );
    const marks = Array.from(container.querySelectorAll("svg"));
    expect(marks).toHaveLength(2);
    const ids = marks.map((mark) =>
      Array.from(mark.querySelectorAll("linearGradient")).map((g) => g.id),
    );
    expect(ids[0]).toHaveLength(3);
    expect(new Set(ids.flat()).size).toBe(6);
    for (const [index, mark] of marks.entries()) {
      const fills = Array.from(mark.querySelectorAll("path"))
        .map((path) => path.getAttribute("fill") ?? "")
        .filter((fill) => fill.startsWith("url("))
        .map((fill) => fill.slice(5, -1));
      expect(fills.length).toBeGreaterThan(0);
      for (const id of fills) expect(ids[index]).toContain(id);
    }
  });
});
