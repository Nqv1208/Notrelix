import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import {
  richText,
  type BlockDraft,
  type PageAttachment,
} from "@notrelix/docs-core";
import type { Editor } from "@tiptap/core";
import { AttachmentUploads } from "@notrelix/docs-state";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocEditorSurface } from "../doc-editor-surface";
import { EditorToolbar } from "../editor-toolbar";
import { docToDrafts } from "../block-mapping";
import { insertMediaFiles } from "../media-commands";
import type { DocMediaHost } from "../media-host";
import { matchSlashItems } from "../slash-menu";
import { useDocRichEditor } from "../use-doc-rich-editor";
import { stubLayout } from "./headless-editor";

beforeAll(() => {
  stubLayout();
  URL.createObjectURL = vi.fn(() => "blob:local-preview");
  URL.revokeObjectURL = vi.fn();
});
afterEach(() => vi.clearAllMocks());

const STORED: PageAttachment = {
  id: "att-1",
  fileName: "anh.png",
  sizeBytes: 2048,
  contentType: "image/png",
  createdAt: "2026-10-26T00:00:00Z",
};
const REPORT: PageAttachment = {
  id: "att-2",
  fileName: "bao-cao.pdf",
  sizeBytes: 1536 * 1024,
  contentType: "application/pdf",
  createdAt: "2026-10-26T00:00:00Z",
};

const PARAGRAPH: BlockDraft = {
  id: "a",
  type: "paragraph",
  content: richText([{ text: "Mở đầu", marks: [] }]),
  properties: {},
};

function png(name = "anh.png") {
  return new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], name, {
    type: "image/png",
  });
}

function setup(options: {
  drafts?: BlockDraft[];
  upload?: (file: File) => Promise<PageAttachment>;
  editable?: boolean;
}) {
  let drafts: BlockDraft[] = [];
  const onRefused = vi.fn();
  const download = vi.fn(async () => undefined);
  const retryContent = vi.fn();
  let holder: { completeUpload: (u: never, a: PageAttachment) => void } | null =
    null;
  const uploads = new AttachmentUploads({
    upload: (file) => (options.upload ?? (async () => STORED))(file),
    onUploaded: (upload, attachment) =>
      holder?.completeUpload(upload as never, attachment),
    createPreviewUrl: () => "blob:local-preview",
    revokePreviewUrl: () => undefined,
  });
  const media: DocMediaHost = {
    uploads,
    onRefused,
    download,
    useFiles: () => ({
      files: new Map([STORED, REPORT].map((file) => [file.id, file])),
      loading: false,
    }),
    useContent: (id) => ({
      status: id === "broken" ? "error" : "ready",
      blob: id === "broken" ? null : new Blob(["x"], { type: "image/png" }),
      retry: retryContent,
    }),
  };
  const context = {
    copy: docsCopy,
    readOnly: options.editable === false,
    resolveTask: (id: string) => ({ id, restricted: true as const }),
    viewOf: () => undefined,
    media,
  };
  let editorRef: ReturnType<typeof useDocRichEditor> | null = null;
  function Harness() {
    const rich = useDocRichEditor({
      pageId: "p",
      ready: true,
      initial: options.drafts ?? [PARAGRAPH],
      editable: options.editable ?? true,
      placeholder: "p",
      emptyPlaceholder: "e",
      label: "doc",
      context,
      onDrafts: (next) => {
        drafts = next;
      },
    });
    editorRef = rich;
    holder = rich as never;
    if (!rich.editor) return null;
    return (
      <>
        <EditorToolbar editor={rich.editor} copy={docsCopy} />
        <DocEditorSurface
          editor={rich.editor}
          copy={docsCopy}
          mentionTasks={[]}
        />
      </>
    );
  }
  renderPureUi(<Harness />);
  return {
    media,
    uploads,
    onRefused,
    download,
    retryContent,
    drafts: () => drafts,
    editor: () => editorRef!.editor!,
  };
}

/** Runs the media extension's own paste handler on the editor. */
function pasteInto(editor: Editor, event: ClipboardEvent): boolean {
  const plugin = editor.view.state.plugins.find((candidate) =>
    String((candidate as unknown as { key: string }).key).startsWith(
      "docMediaPaste",
    ),
  );
  return Boolean(
    plugin?.props.handlePaste?.call(plugin, editor.view, event, null as never),
  );
}

const settle = () => act(async () => new Promise((r) => setTimeout(r, 0)));

describe("image block", () => {
  const IMAGE: BlockDraft = {
    id: "img",
    type: "image",
    content: richText(),
    properties: {
      attachmentId: "att-1",
      width: 400,
      alt: "Sơ đồ",
      caption: "Hình 1",
    },
  };

  it("shows the stored image through the content blob, never a stored address", async () => {
    setup({ drafts: [IMAGE] });
    const image = (await screen.findByRole("img", {
      name: "Sơ đồ",
    })) as HTMLImageElement;
    expect(image.getAttribute("src")).toBe("blob:local-preview");
    expect(screen.getByDisplayValue("Hình 1")).toBeTruthy();
  });

  it("saves a caption and an alternative text", async () => {
    const view = setup({ drafts: [IMAGE] });
    const caption = await screen.findByLabelText(docsCopy.media.image.caption);
    await act(async () => {
      fireEvent.change(caption, { target: { value: "Hình 2" } });
      fireEvent.blur(caption);
    });
    expect(view.drafts()[0]?.properties.caption).toBe("Hình 2");

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.media.image.altText }),
      );
    });
    const alt = screen
      .getAllByLabelText(docsCopy.media.image.altText)
      .find((node) => node.tagName === "INPUT")!;
    await act(async () => {
      fireEvent.change(alt, { target: { value: "Mô tả mới" } });
      fireEvent.blur(alt);
    });
    expect(view.drafts()[0]?.properties.alt).toBe("Mô tả mới");
    expect(JSON.stringify(view.drafts())).not.toMatch(/blob:|data:/);
  });

  it("resizes with the keyboard inside the contract's 80 to 2000 range", async () => {
    const view = setup({ drafts: [IMAGE] });
    const handle = await screen.findByRole("slider", {
      name: docsCopy.media.image.resize,
    });
    expect(handle.getAttribute("aria-valuemin")).toBe("80");
    expect(handle.getAttribute("aria-valuemax")).toBe("2000");
    expect(handle.getAttribute("aria-valuenow")).toBe("400");
    await act(async () => {
      fireEvent.keyDown(handle, { key: "ArrowRight" });
    });
    expect(view.drafts()[0]?.properties.width).toBe(410);
    await act(async () => {
      fireEvent.keyDown(screen.getByRole("slider"), { key: "End" });
    });
    expect(view.drafts()[0]?.properties.width).toBe(2000);
    await act(async () => {
      fireEvent.keyDown(screen.getByRole("slider"), { key: "Home" });
    });
    expect(view.drafts()[0]?.properties.width).toBe(80);
  });

  it("offers no editing controls to a viewer", async () => {
    setup({ drafts: [IMAGE], editable: false });
    await screen.findByRole("img", { name: "Sơ đồ" });
    expect(screen.queryByRole("slider")).toBeNull();
    expect(screen.queryByLabelText(docsCopy.media.image.caption)).toBeNull();
    expect(screen.getByText("Hình 1")).toBeTruthy();
  });

  it("says so when an image has no attachment or cannot be read", async () => {
    const view = setup({
      drafts: [
        { ...IMAGE, id: "none", properties: {} },
        { ...IMAGE, id: "bad", properties: { attachmentId: "broken" } },
      ],
    });
    expect(
      await screen.findByText(docsCopy.media.image.unavailable),
    ).toBeTruthy();
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.media.image.retry }),
      );
    });
    expect(view.retryContent).toHaveBeenCalled();
  });
});

describe("file block", () => {
  it("shows the file's name, size and type and downloads it", async () => {
    const view = setup({
      drafts: [
        {
          id: "f",
          type: "file",
          content: richText(),
          properties: { attachmentId: "att-2" },
        },
      ],
    });
    expect(await screen.findByText("bao-cao.pdf")).toBeTruthy();
    expect(screen.getByText(/1,5 MB · PDF/)).toBeTruthy();
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Tải xuống bao-cao.pdf" }),
      );
    });
    expect(view.download).toHaveBeenCalledWith(REPORT);
  });
});

describe("uploading", () => {
  it("puts a placeholder in the page, then an image block that points at the attachment", async () => {
    let finish!: (file: PageAttachment) => void;
    const view = setup({
      upload: () =>
        new Promise<PageAttachment>((resolve) => (finish = resolve)),
    });
    await screen.findByText("Mở đầu");
    await act(async () => {
      insertMediaFiles(view.editor(), [png()]);
    });
    expect(
      await screen.findByRole("progressbar", { name: "Đang tải lên anh.png" }),
    ).toBeTruthy();
    // A placeholder is never a stored block, and no preview address is stored.
    expect(view.drafts().map((draft) => draft.type)).not.toContain("image");
    expect(JSON.stringify(view.drafts())).not.toMatch(/blob:|data:/);

    await act(async () => finish(STORED));
    await settle();
    expect(screen.queryByRole("progressbar")).toBeNull();
    const image = docToDrafts(view.editor().getJSON()).find(
      (draft) => draft.type === "image",
    );
    expect(image?.properties).toEqual({ attachmentId: "att-1" });
    expect(JSON.stringify(view.drafts())).not.toMatch(/blob:|data:/);
  });

  it("makes a file block of anything that is not an image", async () => {
    const view = setup({ upload: async () => REPORT });
    await screen.findByText("Mở đầu");
    await act(async () => {
      insertMediaFiles(view.editor(), [
        new File(["x"], "bao-cao.pdf", { type: "application/pdf" }),
      ]);
    });
    await settle();
    const file = view.drafts().find((draft) => draft.type === "file");
    expect(file?.properties).toEqual({ attachmentId: "att-2" });
  });

  it("keeps a failed upload with Retry and Remove, and Retry stores it", async () => {
    const upload = vi
      .fn<(file: File) => Promise<PageAttachment>>()
      .mockRejectedValueOnce({ kind: "network" })
      .mockResolvedValueOnce(STORED);
    const view = setup({ upload });
    await screen.findByText("Mở đầu");
    await act(async () => {
      insertMediaFiles(view.editor(), [png()]);
    });
    await settle();
    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(view.drafts().some((draft) => draft.type === "image")).toBe(false);

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.media.upload.retry }),
      );
    });
    await settle();
    expect(upload).toHaveBeenCalledTimes(2);
    expect(view.drafts().some((draft) => draft.type === "image")).toBe(true);
  });

  it("removes a failed upload without leaving a block", async () => {
    const view = setup({ upload: async () => Promise.reject({ status: 415 }) });
    await screen.findByText("Mở đầu");
    await act(async () => {
      insertMediaFiles(view.editor(), [png()]);
    });
    await settle();
    // A refused type cannot be retried: only Remove is offered.
    expect(
      screen.queryByRole("button", { name: docsCopy.media.upload.retry }),
    ).toBeNull();
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.media.upload.remove }),
      );
    });
    expect(
      view
        .editor()
        .getJSON()
        .content?.map((n) => n.type)
        .filter((type) => type !== "paragraph"),
    ).toEqual([]);
    expect(view.uploads.snapshot()).toEqual([]);
  });

  it("refuses a blocked or oversized file before sending and starts nothing", async () => {
    const upload = vi.fn(async () => STORED);
    const view = setup({ upload });
    await screen.findByText("Mở đầu");
    await act(async () => {
      insertMediaFiles(view.editor(), [new File(["x"], "run.exe")]);
    });
    expect(view.onRefused).toHaveBeenCalledWith(
      expect.objectContaining({ name: "run.exe" }),
      "blockedType",
    );
    expect(upload).not.toHaveBeenCalled();
    expect(view.editor().getJSON().content).toHaveLength(1);
  });

  it("uploads an image pasted without text", async () => {
    const view = setup({ upload: () => new Promise(() => undefined) });
    await screen.findByText("Mở đầu");
    const event = new Event("paste", { cancelable: true }) as ClipboardEvent;
    Object.defineProperty(event, "clipboardData", {
      value: { files: [png("dan.png")], getData: () => "" },
    });
    const handled = pasteInto(view.editor(), event);
    expect(handled).toBe(true);
    expect(view.uploads.snapshot().map((u) => u.fileName)).toEqual(["dan.png"]);
  });

  it("keeps the normal paste when text comes with the file", async () => {
    const view = setup({ upload: () => new Promise(() => undefined) });
    await screen.findByText("Mở đầu");
    const event = new Event("paste", { cancelable: true }) as ClipboardEvent;
    Object.defineProperty(event, "clipboardData", {
      value: { files: [png()], getData: () => "dòng chữ" },
    });
    const handled = pasteInto(view.editor(), event);
    expect(handled).toBe(false);
    expect(view.uploads.snapshot()).toEqual([]);
  });

  it("offers image and file in the slash menu and the toolbar", async () => {
    setup({});
    expect(matchSlashItems(docsCopy, "anh").map((item) => item.type)).toContain(
      "image",
    );
    expect(matchSlashItems(docsCopy, "tep").map((item) => item.type)).toContain(
      "file",
    );
    expect(
      await screen.findByRole("button", { name: docsCopy.toolbar.image }),
    ).toBeTruthy();
    expect(
      screen.getByRole("button", { name: docsCopy.toolbar.file }),
    ).toBeTruthy();
  });
});
