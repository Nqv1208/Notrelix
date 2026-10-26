import React from "react";
import { describe, expect, it, vi } from "vitest";
import { MobileDocumentScreen, blocksFromWire } from "../index";
import { expand, findAll, textOf } from "./test-tree";

vi.mock("react-native", async () => {
  const actual = await vi.importActual<Record<string, unknown>>("react-native");
  return {
    ...actual,
    ScrollView: (props: Record<string, unknown>) =>
      React.createElement("div", props),
  };
});

const screen = (props: Record<string, unknown> = {}) =>
  expand(<MobileDocumentScreen workspaceId="w1" documentId="p1" {...props} />);

describe("docs mobile screen states", () => {
  it("names a page that is loading", () => {
    const out = screen({ status: { kind: "loading" } });
    expect(textOf(out)).toContain("Đang tải");
  });

  it("shows one message for a page that cannot be read", () => {
    const out = screen({ status: { kind: "forbidden" } });
    expect(textOf(out)).toContain("không có quyền");
    expect(
      findAll(out, (n) => n.props.accessibilityRole === "alert"),
    ).toHaveLength(1);
  });

  it("offers a retry on a failed read", () => {
    const onRetry = vi.fn();
    const out = screen({ status: { kind: "error", onRetry } });
    const [button] = findAll(
      out,
      (n) => n.props.accessibilityRole === "button",
    );
    (button?.props.onPress as () => void)();
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("lets a status win over blocks", () => {
    const out = screen({ blocks: [], title: "T", status: { kind: "loading" } });
    expect(textOf(out)).not.toContain("T");
  });

  it("draws the title and the decoded blocks read-only", () => {
    const blocks = blocksFromWire([
      {
        id: "b1",
        pageId: "p1",
        type: "Text",
        version: 1,
        createdAt: "2026-10-26T00:00:00Z",
        content: { v: 1, runs: [{ text: "Hello", marks: [] }] },
        properties: {},
      },
    ]);
    const out = screen({ blocks, title: "Plan" });
    expect(textOf(out)).toContain("Plan");
    expect(textOf(out)).toContain("Hello");
  });
});
