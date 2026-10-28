import { describe, expect, it, vi } from "vitest";

vi.mock("mammoth/mammoth.browser.min.js", () => ({
  default: {
    convertToHtml: vi.fn(async () => ({
      value:
        '<h1>Báo cáo</h1><p>Đoạn <strong>đậm</strong> và <a href="https://example.com">liên kết</a> <a href="javascript:alert(1)">xấu</a><img src=""></p>' +
        "<ul><li>một<ul><li>hai</li></ul></li></ul><ol><li>đầu</li></ol>" +
        "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>" +
        "<script>alert(1)</script>",
      messages: [],
    })),
    images: { imgElement: () => ({}) },
  },
}));

import { htmlToImport } from "../html-to-blocks";
import { IMPORT_MAX_BYTES, readImportFile } from "../read-import-file";

function file(name: string, content: string | ArrayBuffer): File {
  return new File([content], name);
}

describe("htmlToImport", () => {
  it("reads structure and marks, flattens nested lists and drops images and scripts", () => {
    const page = htmlToImport(
      '<h2>Title</h2><p>a <em>b</em> <img src="x.png"></p><ul><li>one<ul><li>two</li></ul></li></ul><pre>x = 1</pre><hr><blockquote><p>q</p></blockquote><script>alert(1)</script>',
    );
    expect(page.blocks.map((entry) => entry.type)).toEqual([
      "heading_2",
      "paragraph",
      "bulleted_list",
      "bulleted_list",
      "code",
      "divider",
      "quote",
    ]);
    expect(page.imagesDropped).toBe(1);
    expect(
      page.blocks[1]?.content.runs.map((run) => run.marks.map((m) => m.type)),
    ).toEqual([[], ["italic"]]);
    expect(JSON.stringify(page)).not.toContain("alert");
  });

  it("keeps only safe links and reads a header row", () => {
    const page = htmlToImport(
      '<p><a href="javascript:alert(1)">bad</a><a href="https://ok.example/">ok</a></p><table><tr><th>H</th></tr><tr><td>c</td></tr></table>',
    );
    const links = page.blocks[0]?.content.runs.flatMap((run) =>
      run.marks.filter((mark) => mark.type === "link"),
    );
    expect(links).toEqual([{ type: "link", href: "https://ok.example/" }]);
    expect(page.blocks[1]?.properties.table?.headerRow).toBe(true);
  });
});

describe("readImportFile", () => {
  it("reads a Markdown file into a titled page of sanitised blocks", async () => {
    const result = await readImportFile(
      file("notes.md", "# Họp tuần\n\nNội dung **chính**\n"),
    );
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.title).toBe("Họp tuần");
    expect(result.page.blocks.map((entry) => entry.type)).toEqual([
      "paragraph",
    ]);
  });

  it("names the page after the file when it has no top heading", async () => {
    const result = await readImportFile(file("plan.markdown", "just text"));
    expect(result.ok && result.title).toBe("plan");
  });

  it("refuses unsupported, oversized and empty files", async () => {
    expect(await readImportFile(file("a.pdf", "x"))).toEqual({
      ok: false,
      error: "unsupported",
    });
    expect(
      await readImportFile(file("big.md", "x".repeat(IMPORT_MAX_BYTES + 1))),
    ).toEqual({ ok: false, error: "tooLarge" });
    expect(await readImportFile(file("empty.md", "  \n\n"))).toEqual({
      ok: false,
      error: "empty",
    });
  });

  it("converts a Word file through the lazily loaded reader and counts dropped images", async () => {
    const result = await readImportFile(
      file("report.docx", new ArrayBuffer(8)),
    );
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.title).toBe("report");
    expect(result.page.imagesDropped).toBe(1);
    expect(result.page.blocks.map((entry) => entry.type)).toEqual([
      "heading_1",
      "paragraph",
      "bulleted_list",
      "bulleted_list",
      "numbered_list",
      "table",
    ]);
  });
});
