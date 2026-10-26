import React from "react";
import { describe, expect, it, vi } from "vitest";
import type {
  Block,
  BlockProperties,
  BlockType,
  Mark,
} from "@notrelix/docs-core";
import { MobileDocRenderer, buildDocTree, presentRun } from "../index";
import { expand, findAll, textOf } from "./test-tree";

let counter = 0;
function block(
  type: BlockType,
  text: string,
  extra: Partial<Block> & { properties?: BlockProperties; marks?: Mark[] } = {},
): Block {
  counter += 1;
  const { marks, properties, ...rest } = extra;
  return {
    id: `b${counter}`,
    pageId: "p1",
    type,
    content: { v: 1, runs: text ? [{ text, marks: marks ?? [] }] : [] },
    properties: properties ?? {},
    position: counter,
    parentId: null,
    createdById: "u1",
    updatedById: "u1",
    createdAt: "2026-10-26T00:00:00Z",
    updatedAt: "2026-10-26T00:00:00Z",
    ...rest,
  };
}

function render(blocks: Block[], props = {}) {
  return expand(<MobileDocRenderer blocks={blocks} {...props} />);
}

describe("docs mobile reader: structure", () => {
  it("draws paragraph, headings, quote and divider in position order", () => {
    const out = render([
      block("heading_1", "Title"),
      block("paragraph", "Body"),
      block("quote", "Said"),
      block("divider", ""),
      block("heading_3", "Small"),
    ]);
    expect(textOf(out)).toBe("TitleBodySaidSmall");
    expect(
      findAll(out, (n) => n.props.accessibilityRole === "header"),
    ).toHaveLength(2);
  });

  it("numbers consecutive numbered items and restarts after another block", () => {
    const out = render([
      block("numbered_list", "a"),
      block("numbered_list", "b"),
      block("paragraph", "gap"),
      block("numbered_list", "c"),
      block("bulleted_list", "d"),
    ]);
    expect(textOf(out)).toBe("1.a2.bgap1.c•d");
  });

  it("shows a task's state to assistive technology", () => {
    const out = render([
      block("todo", "open"),
      block("todo", "shut", { properties: { checked: true } }),
    ]);
    const boxes = findAll(out, (n) => n.props.accessibilityRole === "checkbox");
    expect(boxes.map((b) => b.props.accessibilityState)).toEqual([
      { checked: false },
      { checked: true },
    ]);
    expect(textOf(out)).toBe("☐open☑shut");
  });

  it("nests children from parentId and from children, and keeps an orphan visible", () => {
    const parent = block("bulleted_list", "parent");
    const child = block("bulleted_list", "child", { parentId: parent.id });
    const nested = block("paragraph", "inline", {
      children: [block("paragraph", "grandchild")],
    });
    const orphan = block("paragraph", "orphan", { parentId: "gone" });
    const tree = buildDocTree([child, nested, orphan, parent]);
    expect(tree.map((n) => n.block.id)).toEqual(
      [nested.id, orphan.id, parent.id].sort((a, b) => {
        const pos = (id: string) =>
          [child, nested, orphan, parent].find((x) => x.id === id)!.position;
        return pos(a) - pos(b);
      }),
    );
    expect(
      tree.find((n) => n.block.id === parent.id)?.children[0]?.block.id,
    ).toBe(child.id);
    expect(tree.find((n) => n.block.id === nested.id)?.children).toHaveLength(
      1,
    );
    expect(textOf(render([parent, child, nested, orphan]))).toContain(
      "grandchild",
    );
  });

  it("upgrades a legacy block that has no runs", () => {
    const legacy: Block = {
      ...block("paragraph", ""),
      content: undefined,
      properties: { text: "old", bold: true },
    };
    const out = render([legacy]);
    expect(textOf(out)).toBe("old");
    const bold = findAll(
      out,
      (n) =>
        Array.isArray(n.props.style) &&
        JSON.stringify(n.props.style).includes("700"),
    );
    expect(bold.length).toBeGreaterThan(0);
  });
});

describe("docs mobile reader: blocks with their own content", () => {
  it("draws a callout with its icon and a toggle with its content shown", () => {
    const callout = block("callout", "Careful", {
      properties: { icon: "!", tone: "warning" },
    });
    const toggle = block("toggle", "More", { properties: { collapsed: true } });
    const inner = block("paragraph", "hidden?", { parentId: toggle.id });
    const out = render([callout, toggle, inner]);
    expect(textOf(out)).toBe("!Careful▾Morehidden?");
  });

  it("draws code as one literal text", () => {
    const out = render([block("code", "const a = 1;\nreturn a;")]);
    expect(textOf(out)).toBe("const a = 1;\nreturn a;");
  });

  it("draws a table as a grid of cells, header first", () => {
    const cell = (text: string) => ({ runs: [{ text, marks: [] }] });
    const out = render([
      block("table", "", {
        properties: {
          table: {
            headerRow: true,
            colWidths: [null, null],
            rows: [
              [cell("A"), cell("B")],
              [cell("1"), { runs: [{ text: "2", marks: [] }], bg: "danger" }],
            ],
          },
        },
      }),
    ]);
    expect(textOf(out)).toBe("AB12");
    const rows = findAll(out, (n) =>
      JSON.stringify(n.props.style ?? "").includes("row"),
    );
    expect(rows.length).toBeGreaterThanOrEqual(2);
    expect(JSON.stringify(out)).toContain("#fee2e2");
  });

  it("shows an image or file as a placeholder with its caption, and lets the host draw it", () => {
    const image = block("image", "", {
      properties: {
        attachmentId: "a1",
        alt: "Chart",
        captionRuns: [{ text: "Q3", marks: [{ type: "bold" }] }],
      },
    });
    const file = block("file", "", { properties: { attachmentId: "a2" } });
    expect(textOf(render([image, file]))).toBe("Hình ảnhQ3Tệp đính kèm");
    const hosted = render([image], {
      renderAttachment: (b: Block) => (
        <>{`hosted:${b.properties.attachmentId}`}</>
      ),
    });
    expect(textOf(hosted)).toBe("hosted:a1");
  });

  it("reports a page reference press and draws a board reference by its title", () => {
    const onOpenPage = vi.fn();
    const out = render(
      [
        block("page_reference", "", { properties: { linkedPageId: "p9" } }),
        block("board_reference", "", { properties: { title: "Sprint" } }),
      ],
      { onOpenPage },
    );
    const press = findAll(
      out,
      (n) => typeof n.props.onPress === "function",
    )[0]!;
    (press.props.onPress as () => void)();
    expect(onOpenPage).toHaveBeenCalledWith("p9");
    expect(textOf(out)).toContain("Sprint");
  });
});

describe("docs mobile reader: marks", () => {
  it("maps every mark to a presentation", () => {
    const view = presentRun([
      { type: "bold" },
      { type: "italic" },
      { type: "underline" },
      { type: "strike" },
      { type: "code" },
      { type: "color", token: "danger" },
      { type: "highlight", token: "warning" },
    ]);
    expect(view).toMatchObject({
      bold: true,
      italic: true,
      underline: true,
      strike: true,
      code: true,
      color: "danger",
      highlight: "warning",
    });
    expect(
      presentRun([
        { type: "color", token: "default" },
        { type: "highlight", token: "none" },
      ]),
    ).toEqual({
      bold: false,
      italic: false,
      underline: false,
      strike: false,
      code: false,
    });
  });

  it("keeps only a safe link", () => {
    expect(
      presentRun([{ type: "link", href: "https://example.com/a" }]).href,
    ).toBe("https://example.com/a");
    expect(
      presentRun([{ type: "link", href: "javascript:alert(1)" }]).href,
    ).toBeUndefined();
    expect(presentRun([{ type: "link", href: "mailto:a@b.co" }]).href).toBe(
      "mailto:a@b.co",
    );
  });

  it("reports link, mention and page reference presses to the host", () => {
    const onOpenLink = vi.fn();
    const onOpenMember = vi.fn();
    const onOpenPage = vi.fn();
    const para: Block = {
      ...block("paragraph", ""),
      content: {
        v: 1,
        runs: [
          { text: "see ", marks: [] },
          {
            text: "site",
            marks: [{ type: "link", href: "https://example.com" }],
          },
          { text: "@Lan", marks: [{ type: "mention", userId: "u7" }] },
          { text: "Spec", marks: [{ type: "pageRef", pageId: "p3" }] },
        ],
      },
    };
    const out = render([para], { onOpenLink, onOpenMember, onOpenPage });
    const pressable = findAll(
      out,
      (n) => typeof n.props.onPress === "function",
    );
    expect(pressable).toHaveLength(3);
    for (const node of pressable) (node.props.onPress as () => void)();
    expect(onOpenLink).toHaveBeenCalledWith("https://example.com");
    expect(onOpenMember).toHaveBeenCalledWith("u7");
    expect(onOpenPage).toHaveBeenCalledWith("p3");
    expect(
      findAll(out, (n) => n.props.accessibilityRole === "link"),
    ).toHaveLength(1);
  });

  it("does not offer a press for a link that is not safe", () => {
    const para: Block = {
      ...block("paragraph", ""),
      content: {
        v: 1,
        runs: [
          { text: "x", marks: [{ type: "link", href: "javascript:alert(1)" }] },
        ],
      },
    };
    const out = render([para]);
    expect(
      findAll(out, (n) => typeof n.props.onPress === "function"),
    ).toHaveLength(0);
  });
});
