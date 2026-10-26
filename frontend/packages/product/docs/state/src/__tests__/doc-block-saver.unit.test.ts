import { describe, expect, it, vi } from "vitest";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import { DocBlockSaver, type BlockWriter } from "../editor/doc-block-saver";
import { decodeBlockBody, encodeBlockBody } from "../model/block-wire";

function draft(id: string, text: string): BlockDraft {
  return {
    id,
    type: "paragraph",
    content: richText(text ? [{ text, marks: [] }] : []),
    properties: {},
  };
}

function writer(overrides: Partial<BlockWriter> = {}) {
  let next = 0;
  const calls: string[] = [];
  const w: BlockWriter = {
    create: vi.fn(async (d, after) => {
      calls.push(`create ${d.id} after ${after}`);
      next += 1;
      return `s${next}`;
    }),
    update: vi.fn(async (id) => void calls.push(`update ${id}`)),
    remove: vi.fn(async (id) => void calls.push(`remove ${id}`)),
    ...overrides,
  };
  return { w, calls };
}

function saverWith(w: BlockWriter, extra = {}) {
  const statuses: string[] = [];
  const saver = new DocBlockSaver(w, {
    onStatus: (s) => statuses.push(s),
    schedule: () => 1,
    cancel: () => undefined,
    ...extra,
  });
  return { saver, statuses };
}

describe("DocBlockSaver", () => {
  it("writes only the changed blocks, in order, and reports saving then saved", async () => {
    const { w, calls } = writer();
    const { saver, statuses } = saverWith(w);
    saver.reset([draft("a", "x"), draft("b", "y")]);
    saver.change([draft("a", "x!"), draft("b", "y")]);
    await saver.flush();
    expect(calls).toEqual(["update a"]);
    expect(statuses).toEqual(["saving", "saved"]);
  });

  it("creates new blocks after their neighbour and adopts the stored id", async () => {
    const assigned: Array<[string, string]> = [];
    const { w, calls } = writer();
    const { saver } = saverWith(w, {
      onIdAssigned: (c: string, s: string) => assigned.push([c, s]),
    });
    saver.reset([draft("a", "x")]);
    saver.change([draft("a", "x"), draft("n1", "1"), draft("n2", "2")]);
    await saver.flush();
    expect(calls).toEqual(["create n1 after a", "create n2 after s1"]);
    expect(assigned).toEqual([
      ["n1", "s1"],
      ["n2", "s2"],
    ]);
    // Nothing is written twice once the ids are adopted.
    saver.change([draft("a", "x"), draft("s1", "1"), draft("s2", "2")]);
    await saver.flush();
    expect(calls).toHaveLength(2);
  });

  it("keeps the text pending after a failure and resumes without duplicating writes", async () => {
    let fail = true;
    const { w, calls } = writer({
      update: vi.fn(async (id: string) => {
        if (id === "b" && fail) throw new Error("offline");
        calls.push(`update ${id}`);
      }),
    });
    const { saver, statuses } = saverWith(w);
    saver.reset([draft("a", "1"), draft("b", "2")]);
    saver.change([draft("a", "1!"), draft("b", "2!")]);
    await saver.flush();
    expect(saver.getStatus()).toBe("error");
    expect(calls).toEqual(["update a"]);
    fail = false;
    await saver.retry();
    expect(calls).toEqual(["update a", "update b"]);
    expect(statuses.at(-1)).toBe("saved");
  });

  it("reports a conflict when the producer answers 412", async () => {
    const onConflict = vi.fn();
    const { w } = writer({
      update: vi.fn(async () => {
        throw Object.assign(new Error("stale"), { status: 412 });
      }),
    });
    const { saver } = saverWith(w, { onConflict });
    saver.reset([draft("a", "1")]);
    saver.change([draft("a", "2")]);
    await saver.flush();
    expect(onConflict).toHaveBeenCalledWith("a", expect.any(Error));
    expect(saver.getStatus()).toBe("error");
  });
});

describe("block wire", () => {
  it("round-trips rich text beside the block properties", () => {
    const d: BlockDraft = {
      id: "t",
      type: "todo",
      content: richText([{ text: "Do", marks: [{ type: "bold" }] }]),
      properties: { checked: true },
    };
    const decoded = decodeBlockBody("todo", { content: encodeBlockBody(d) });
    expect(decoded.content).toEqual(d.content);
    expect(decoded.properties).toEqual({ checked: true, text: "Do" });
  });

  it("upgrades a legacy flat body", () => {
    const decoded = decodeBlockBody("paragraph", {
      content: JSON.stringify({ text: "Hi", bold: true, align: "right" }),
    });
    expect(decoded.content.runs).toEqual([
      { text: "Hi", marks: [{ type: "bold" }] },
    ]);
    expect(decoded.properties).toEqual({ align: "right", text: "Hi" });
  });
});
