import React from "react";
import { describe, expect, it } from "vitest";
import type { Block, BlockProperties, Run } from "@notrelix/docs-core";
import { MobileDocRenderer, presentRun } from "../index";
import { blockLayoutOf, scriptCharacters } from "../render/doc-layout";
import { expand, findAll, textOf } from "./test-tree";

let counter = 0;
function block(
  type: Block["type"],
  runs: Run[],
  properties: BlockProperties = {},
): Block {
  counter += 1;
  return {
    id: `l${counter}`,
    pageId: "p1",
    type,
    content: { v: 1, runs },
    properties,
    position: counter,
    parentId: null,
    createdById: "u1",
    updatedById: "u1",
    createdAt: "2026-10-27T00:00:00Z",
    updatedAt: "2026-10-27T00:00:00Z",
  };
}

const plain = (text: string): Run[] => [{ text, marks: [] }];

function render(blocks: Block[]) {
  return expand(<MobileDocRenderer blocks={blocks} />);
}

function styleOf(node: { props: Record<string, unknown> }) {
  return Object.assign(
    {},
    ...(([] as unknown[]).concat(node.props.style) as object[]).filter(Boolean),
  ) as Record<string, unknown>;
}

describe("docs mobile reader: indent, spacing, scripts and merged cells", () => {
  it("insets an indented block and applies its alignment and line spacing", () => {
    const out = render([
      block("paragraph", plain("plain")),
      block("paragraph", plain("inset"), {
        indent: 2,
        align: "center",
        lineSpacing: "loose",
      }),
    ]);
    const views = findAll(out, () => true);
    expect(views.some((v) => styleOf(v).marginLeft === 40)).toBe(true);
    const inset = findAll(out, (n) => textOf(n.children) === "inset").find(
      (t) => styleOf(t).textAlign === "center",
    )!;
    expect(inset).toBeDefined();
    expect(styleOf(inset).lineHeight).toBe(Math.round(24 * 1.35));
    const normal = findAll(out, (n) => textOf(n.children) === "plain")[0]!;
    expect(styleOf(normal).textAlign).toBeUndefined();
  });

  it("leaves a block without the properties as it was", () => {
    const out = render([block("paragraph", plain("plain"))]);
    expect(
      findAll(out, () => true).some((v) => styleOf(v).marginLeft !== undefined),
    ).toBe(false);
    expect(blockLayoutOf(block("paragraph", plain("x")))).toBeNull();
    expect(
      blockLayoutOf(block("paragraph", plain("x"), { indent: 99 })),
    ).toEqual({ inset: 160 });
  });

  it("draws digits raised or lowered and other scripted text smaller", () => {
    const out = render([
      block("paragraph", [
        { text: "x", marks: [] },
        { text: "2", marks: [{ type: "superscript" }] },
        { text: "H", marks: [] },
        { text: "2", marks: [{ type: "subscript" }] },
        { text: "a", marks: [{ type: "superscript" }] },
      ]),
    ]);
    expect(textOf(out)).toBe("x²H₂a");
    expect(findAll(out, (n) => styleOf(n).fontSize === 12)).toHaveLength(1);
    expect(presentRun([{ type: "superscript" }]).script).toBe("superscript");
    expect(scriptCharacters("12", "superscript")).toBe("¹²");
    expect(scriptCharacters("a", "subscript")).toBeNull();
  });

  it("gives a merged cell its column span and keeps columns aligned under a row span", () => {
    const cell = (text: string, extra: object = {}) => ({
      runs: plain(text),
      ...extra,
    });
    const out = render([
      block("table", [], {
        table: {
          headerRow: false,
          colWidths: [null, null, null],
          rows: [
            [cell("wide", { colSpan: 2 }), cell("tall", { rowSpan: 2 })],
            [cell("a"), cell("b")],
          ],
        },
      }),
    ]);
    const cells = findAll(out, (n) => styleOf(n).borderWidth === 0.5);
    // Row one: wide and tall; row two: a, b and the slot under "tall".
    expect(cells).toHaveLength(5);
    const wide = cells.find((c) => textOf(c.children) === "wide")!;
    expect(styleOf(wide).flex).toBe(2);
    expect(cells.filter((c) => textOf(c.children) === "")).toHaveLength(1);
    expect(textOf(out)).toBe("widetallab");
  });
});
