import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { type BlockDraft } from "@notrelix/docs-core";
import type { DocMentionPeople } from "../../components/doc-page-models";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocEditorSurface } from "../doc-editor-surface";
import { useDocRichEditor } from "../use-doc-rich-editor";
import { paragraph, stubLayout, typeText } from "./headless-editor";

beforeAll(stubLayout);

const PEOPLE = [
  { id: "u-an", name: "Ân Nguyễn", initials: "ÂN", tone: "gold" as const },
  { id: "u-binh", name: "Bình Trần", initials: "BT", tone: "blue" as const },
];

const context = {
  copy: docsCopy,
  readOnly: false,
  resolveTask: (id: string) => ({ id, restricted: true as const }),
  viewOf: () => undefined,
};

let lastDrafts: BlockDraft[] = [];
const onDrafts = vi.fn((d: BlockDraft[]) => {
  lastDrafts = d;
});

let typed: ((text: string) => void) | null = null;

function Harness({
  mention = { people: PEOPLE, status: "success" },
}: {
  mention?: DocMentionPeople;
}) {
  const { editor } = useDocRichEditor({
    pageId: "p",
    ready: true,
    initial: [paragraph("a", "")],
    editable: true,
    placeholder: "p",
    emptyPlaceholder: "e",
    label: "doc",
    context,
    onDrafts,
  });
  if (!editor) return null;
  typed = (text) => {
    editor.commands.focus("end");
    typeText(editor, text);
  };
  return (
    <DocEditorSurface
      editor={editor}
      copy={docsCopy}
      mentionTasks={[]}
      mentionPeople={mention}
    />
  );
}

describe("@ mention of a person", () => {
  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("lists the members and stores the pick as a mention mark on the label", async () => {
    renderPureUi(<Harness />);
    await screen.findByRole("textbox");
    await act(async () => {
      typed?.("Hi @b");
    });
    const option = await screen.findByRole("option", { name: /Bình Trần/u });
    await act(async () => {
      fireEvent.click(option);
    });
    const runs = lastDrafts[0]?.content.runs ?? [];
    expect(runs).toEqual([
      { text: "Hi ", marks: [] },
      { text: "@Bình Trần", marks: [{ type: "mention", userId: "u-binh" }] },
      { text: " ", marks: [] },
    ]);
    expect(screen.queryByRole("listbox")).toBeNull();
  }, 20_000);

  it("lists members before tasks and picks the first with Enter", async () => {
    renderPureUi(<Harness />);
    await screen.findByRole("textbox");
    await act(async () => {
      typed?.("@");
    });
    const options = await screen.findAllByRole("option");
    expect(options.map((o) => o.textContent)).toEqual([
      "ÂNÂn Nguyễn",
      "BTBình Trần",
    ]);
    await act(async () => {
      fireEvent.keyDown(window, { key: "Enter" });
    });
    expect(lastDrafts[0]?.content.runs[0]).toEqual({
      text: "@Ân Nguyễn",
      marks: [{ type: "mention", userId: "u-an" }],
    });
  });
});

describe("@ mention list states (server read)", () => {
  const m = docsCopy.mention;

  async function open(mention: DocMentionPeople) {
    renderPureUi(<Harness mention={mention} />);
    await screen.findByRole("textbox");
    await act(async () => {
      typed?.("@");
    });
    await screen.findByRole("listbox");
  }

  it("lists only the people the host was given", async () => {
    await open({ people: [PEOPLE[1]!], status: "success" });
    const names = screen.getAllByRole("option").map((o) => o.textContent);
    expect(names).toEqual(["BTBình Trần"]);
    expect(document.body.textContent).not.toContain("Ân Nguyễn");
  });

  it("says it is loading and offers nobody yet", async () => {
    await open({ people: [], status: "loading" });
    expect(screen.getByText(m.peopleLoading)).toBeTruthy();
    expect(screen.queryAllByRole("option")).toHaveLength(0);
  });

  it("says there is nobody to mention", async () => {
    await open({ people: [], status: "success" });
    expect(screen.getByText(m.peopleEmpty)).toBeTruthy();
  });

  it("says the read failed and retries it", async () => {
    const onRetry = vi.fn();
    await open({ people: [], status: "error", onRetry });
    expect(screen.getByRole("alert").textContent).toContain(m.peopleError);
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: m.peopleRetry }));
    });
    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
