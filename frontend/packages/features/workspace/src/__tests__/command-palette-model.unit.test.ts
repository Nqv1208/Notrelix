import { describe, expect, it } from "vitest";
import {
  highlightSegments,
  matchesTerm,
  moveSelection,
  nextPaletteFilter,
  paletteCounts,
  parsePaletteQuery,
} from "../core/model/command-palette";

describe("parsePaletteQuery", () => {
  it("reads a plain text as a search", () => {
    expect(parsePaletteQuery("  đối chiếu ")).toEqual({
      scope: "search",
      prefix: "",
      term: "đối chiếu",
    });
  });

  it("reads > as commands, @ as people and # as Boards", () => {
    expect(parsePaletteQuery(">chuyển")).toEqual({
      scope: "command",
      prefix: ">",
      term: "chuyển",
    });
    expect(parsePaletteQuery("@ Hà")).toMatchObject({
      scope: "person",
      term: "Hà",
    });
    expect(parsePaletteQuery("#erp")).toMatchObject({
      scope: "board",
      term: "erp",
    });
  });

  it("keeps a lone prefix as an empty term", () => {
    expect(parsePaletteQuery(">")).toMatchObject({
      scope: "command",
      term: "",
    });
  });
});

describe("matchesTerm and highlightSegments", () => {
  it("ignores case and diacritics, including đ", () => {
    expect(matchesTerm("Đối chiếu sao kê", "doi chieu")).toBe(true);
    expect(matchesTerm("Đối chiếu sao kê", "ĐỐI CHIẾU")).toBe(true);
    expect(matchesTerm("Báo cáo", "bao cao")).toBe(true);
    expect(matchesTerm("Báo cáo", "xyz")).toBe(false);
    expect(matchesTerm("anything", "")).toBe(true);
  });

  it("splits every occurrence into hit segments", () => {
    expect(
      highlightSegments("Đối chiếu và đối chiếu lại", "doi chieu"),
    ).toEqual([
      { text: "Đối chiếu", hit: true },
      { text: " và ", hit: false },
      { text: "đối chiếu", hit: true },
      { text: " lại", hit: false },
    ]);
  });

  it("returns the whole text for an empty term or no match", () => {
    expect(highlightSegments("Báo cáo", "")).toEqual([
      { text: "Báo cáo", hit: false },
    ]);
    expect(highlightSegments("Báo cáo", "zzz")).toEqual([
      { text: "Báo cáo", hit: false },
    ]);
  });

  it("matches text typed with combining marks against precomposed labels", () => {
    const decomposed = "do\u0302i".normalize("NFD");
    expect(decomposed.length).toBe(4);
    expect(matchesTerm("Đôi bạn", decomposed)).toBe(true);
    expect(matchesTerm("Đối chiếu", decomposed)).toBe(true);
    expect(highlightSegments("Đối chiếu", decomposed)[0]).toEqual({
      text: "Đối",
      hit: true,
    });
  });
});

describe("filters and selection", () => {
  it("counts per kind with a total and no zero entries", () => {
    expect(paletteCounts(["work", "work", "doc", "command"])).toEqual({
      work: 2,
      doc: 1,
      command: 1,
      all: 4,
    });
    expect(paletteCounts([])).toEqual({});
  });

  it("cycles the filter tabs both ways", () => {
    expect(nextPaletteFilter("all", 1)).toBe("work");
    expect(nextPaletteFilter("command", 1)).toBe("all");
    expect(nextPaletteFilter("all", -1)).toBe("command");
  });

  it("wraps the selection and survives an empty list", () => {
    expect(moveSelection(0, 3, -1)).toBe(2);
    expect(moveSelection(2, 3, 1)).toBe(0);
    expect(moveSelection(-1, 3, 1)).toBe(0);
    expect(moveSelection(-1, 3, -1)).toBe(2);
    expect(moveSelection(0, 0, 1)).toBe(-1);
  });
});
