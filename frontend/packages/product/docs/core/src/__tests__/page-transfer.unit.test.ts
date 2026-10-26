import { describe, expect, it } from "vitest";
import {
  exportHtml,
  exportMarkdown,
  parseMarkdown,
  richText,
  sanitizeImport,
  type Block,
  type BlockProperties,
  type BlockType,
  type Run,
} from "../index";

let order = 0;
function block(
  type: BlockType,
  runs: Run[] | string,
  properties: BlockProperties = {},
  extra: Partial<Block> = {},
): Block {
  order += 1;
  return {
    id: `b${order}`,
    pageId: "p",
    type,
    content: richText(
      typeof runs === "string" ? [{ text: runs, marks: [] }] : runs,
    ),
    properties,
    position: order * 1000,
    parentId: null,
    createdById: "u",
    updatedById: "u",
    createdAt: "",
    updatedAt: "",
    ...extra,
  };
}

describe("exportMarkdown", () => {
  it("writes the title, headings, lists, tasks, quotes, code and rules", () => {
    const md = exportMarkdown("Plan", [
      block("heading_1", "Goals"),
      block("numbered_list", "One"),
      block("numbered_list", "Two"),
      block("paragraph", "After the list"),
      block("bulleted_list", "Item"),
      block("todo", "Done", { checked: true }),
      block("todo", "Open", { checked: false }),
      block("quote", "Wise"),
      block("code", "const a = 1;", { language: "ts" }),
      block("divider", []),
    ]);
    expect(md).toBe(
      [
        "# Plan",
        "",
        "# Goals",
        "",
        "1. One",
        "2. Two",
        "",
        "After the list",
        "",
        "- Item",
        "- [x] Done",
        "- [ ] Open",
        "",
        "> Wise",
        "",
        "```ts",
        "const a = 1;",
        "```",
        "",
        "---",
        "",
      ].join("\n"),
    );
  });

  it("writes marks, escapes Markdown syntax in text and keeps only safe links", () => {
    const md = exportMarkdown("T", [
      block("paragraph", [
        { text: "plain *star* ", marks: [] },
        { text: "bold", marks: [{ type: "bold" }] },
        { text: " ", marks: [] },
        {
          text: "site",
          marks: [{ type: "link", href: "https://example.com/a(b)" }],
        },
        { text: " ", marks: [] },
        { text: "code", marks: [{ type: "code" }] },
      ]),
    ]);
    expect(md).toContain(
      "plain \\*star\\* **bold** [site](https://example.com/a%28b%29) `code`",
    );
  });

  it("writes a table as GFM with the pipe escaped", () => {
    const table = block("table", [], {
      table: {
        headerRow: true,
        colWidths: [null, null],
        rows: [
          [
            { runs: [{ text: "A", marks: [] }] },
            { runs: [{ text: "B|C", marks: [] }] },
          ],
          [{ runs: [{ text: "1", marks: [] }] }, { runs: [] }],
        ],
      },
    });
    expect(exportMarkdown("T", [table])).toContain(
      "| A | B\\|C |\n| --- | --- |\n| 1 |  |",
    );
  });

  it("orders children right after their parent", () => {
    const parent = block("paragraph", "parent");
    const later = block("paragraph", "later");
    const child = block(
      "paragraph",
      "child",
      {},
      { parentId: parent.id, position: 9e6 },
    );
    const md = exportMarkdown("T", [later, child, parent]);
    expect(md.indexOf("parent")).toBeLessThan(md.indexOf("child"));
    expect(md.indexOf("child")).toBeLessThan(md.indexOf("later"));
  });
});

describe("exportHtml", () => {
  it("escapes every piece of text and refuses unsafe links and scripts", () => {
    const html = exportHtml("<img src=x onerror=alert(1)>", [
      block("paragraph", [
        { text: "<script>alert(1)</script>", marks: [] },
        {
          text: "bad",
          marks: [{ type: "link", href: "javascript:alert(1)" }],
        },
        {
          text: "good",
          marks: [{ type: "link", href: 'https://e.com/?a="b"' }],
        },
      ]),
      block("code", "</pre><script>x()</script>", { language: 'x" onload="y' }),
    ]);
    expect(html).not.toContain("<script>");
    expect(html).not.toContain("javascript:");
    expect(html).not.toContain("<img");
    expect(html).not.toContain('onload="y');
    expect(html).toContain("&lt;script&gt;alert(1)&lt;/script&gt;");
    expect(html).toContain('href="https://e.com/?a=&quot;b&quot;"');
    expect(html).toContain("<title>&lt;img src=x onerror=alert(1)&gt;</title>");
  });

  it("groups list items and writes tasks, headings and tables", () => {
    const html = exportHtml("T", [
      block("heading_2", "Sub"),
      block("bulleted_list", "a"),
      block("todo", "t", { checked: true }),
      block("numbered_list", "n"),
    ]);
    expect(html).toContain("<h3>Sub</h3>");
    expect(html).toContain("<ul>\n<li>a</li>\n<li>&#9745; t</li>\n</ul>");
    expect(html).toContain("<ol>\n<li>n</li>\n</ol>");
  });
});

describe("parseMarkdown", () => {
  it("reads the first heading as the title and the rest as blocks", () => {
    const page = parseMarkdown(
      [
        "# My page",
        "",
        "## Section",
        "",
        "Some **bold** and *italic* and ~~gone~~ and `code` and [a link](https://example.com).",
        "",
        "- one",
        "- [x] done",
        "1. first",
        "",
        "> quoted",
        "",
        "```ts",
        "let a = 1;",
        "```",
        "",
        "---",
      ].join("\n"),
    );
    expect(page.title).toBe("My page");
    expect(page.blocks.map((entry) => entry.type)).toEqual([
      "heading_2",
      "paragraph",
      "bulleted_list",
      "todo",
      "numbered_list",
      "quote",
      "code",
      "divider",
    ]);
    const paragraph = page.blocks[1];
    expect(
      paragraph?.content.runs.map((run) => run.marks.map((m) => m.type)),
    ).toEqual([
      [],
      ["bold"],
      [],
      ["italic"],
      [],
      ["strike"],
      [],
      ["code"],
      [],
      ["link"],
      [],
    ]);
    expect(page.blocks[3]?.properties.checked).toBe(true);
    expect(page.blocks[6]?.properties.language).toBe("ts");
  });

  it("keeps later top-level headings as blocks and reads setext headings", () => {
    const page = parseMarkdown("Title line\n===\n\n# Another\n");
    expect(page.title).toBeNull();
    expect(page.blocks.map((entry) => entry.type)).toEqual([
      "heading_1",
      "heading_1",
    ]);
  });

  it("reads a GFM table with a header row", () => {
    const page = parseMarkdown("| A | B |\n| --- | --- |\n| 1 | 2 |\n| 3 |\n");
    const table = page.blocks[0]?.properties.table;
    expect(table?.headerRow).toBe(true);
    expect(table?.rows).toHaveLength(3);
    expect(table?.rows[2]?.[1]?.runs).toEqual([]);
  });

  it("drops images (counting them) and unsafe link targets, and never interprets HTML", () => {
    const page = parseMarkdown(
      "![pic](a.png) text [x](javascript:alert(1)) <b>raw</b> <https://ok.example>",
    );
    expect(page.imagesDropped).toBe(1);
    const runs = page.blocks[0]?.content.runs ?? [];
    expect(
      runs.some((run) =>
        run.marks.some(
          (m) => m.type === "link" && m.href.startsWith("javascript"),
        ),
      ),
    ).toBe(false);
    expect(runs.map((run) => run.text).join("")).toContain("<b>raw</b>");
    expect(
      runs.some((run) =>
        run.marks.some(
          (m) => m.type === "link" && m.href === "https://ok.example",
        ),
      ),
    ).toBe(true);
  });

  it("round trips what the export writes", () => {
    const source = [
      block("heading_1", "Goals"),
      block("paragraph", [
        { text: "a ", marks: [] },
        { text: "bold", marks: [{ type: "bold" }] },
        { text: " *literal* [x]", marks: [] },
      ]),
      block("todo", "task", { checked: true }),
      block("numbered_list", "one"),
      block("code", "x = 1", { language: "py" }),
    ];
    const page = parseMarkdown(exportMarkdown("Doc", source));
    expect(page.title).toBe("Doc");
    expect(page.blocks.map((entry) => entry.type)).toEqual(
      source.map((entry) => entry.type),
    );
    expect(page.blocks[1]?.content.runs.map((run) => run.text).join("")).toBe(
      "a bold *literal* [x]",
    );
  });
});

describe("sanitizeImport", () => {
  it("drops empty text blocks and unsafe marks and splits text that is too long", () => {
    const long = "x".repeat(20_001);
    const result = sanitizeImport({
      title: null,
      imagesDropped: 0,
      blocks: [
        { type: "paragraph", content: richText(), properties: {} },
        {
          type: "paragraph",
          content: {
            v: 1,
            runs: [
              {
                text: "link",
                marks: [{ type: "link", href: "javascript:alert(1)" }],
              },
            ],
          },
          properties: {},
        },
        {
          type: "heading_1",
          content: { v: 1, runs: [{ text: long, marks: [] }] },
          properties: {},
        },
        { type: "image", content: richText(), properties: {} },
      ],
    });
    expect(result?.blocks.map((entry) => entry.type)).toEqual([
      "paragraph",
      "heading_1",
      "paragraph",
    ]);
    expect(result?.blocks[0]?.content.runs[0]?.marks).toEqual([]);
    expect(result?.blocks[2]?.content.runs[0]?.text).toHaveLength(1);
  });

  it("refuses a file with more blocks than a page takes", () => {
    const blocks = Array.from({ length: 5001 }, () => ({
      type: "paragraph" as const,
      content: richText([{ text: "a", marks: [] }]),
      properties: {},
    }));
    expect(
      sanitizeImport({ title: null, imagesDropped: 0, blocks }),
    ).toBeNull();
  });
});
