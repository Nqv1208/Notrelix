import { describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
} from "@notrelix/testing";
import type { CardFile } from "@notrelix/work-management-core";
import {
  WorkManagementTestScope,
  canRetryItemFileUpload,
  useCardFiles,
  useItemFileUploads,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import { ItemPageFiles } from "../item-page-sections";
import { en, vi as viCopy, type ItemPageCopy } from "../item-page.copy";
import {
  editorSheetCapabilities,
  commenterSheetCapabilities,
} from "../../../verification/item-sheet-fixtures";
import { ERP_PAGE_NOW } from "../../../verification/item-page-fixtures";

const ITEM = "item-1";
const WORKSPACE = "ws-1";

const author = {
  id: "cm-1",
  userId: "u-1",
  name: "Trần Minh Quân",
  initials: "TQ",
  color: "#000",
};

function storedFile(over: Partial<CardFile> = {}): CardFile {
  return {
    id: "att-1",
    cardId: ITEM,
    name: "ghi-chu.pdf",
    size: 8,
    contentType: "application/pdf",
    url: "",
    source: "upload",
    kind: "file",
    createdBy: author,
    createdAt: "2026-10-22T03:00:00.000Z",
    ...over,
  };
}

interface Call {
  file: File;
  idempotencyKey: string;
  signal: AbortSignal | undefined;
  progress: (loaded: number, total: number) => void;
  resolve: () => void;
  reject: (error: unknown) => void;
}

/** Fake services: uploads wait for the test; the list reflects what was stored. */
function fakeServices() {
  const calls: Call[] = [];
  const stored: CardFile[] = [];
  const cards = {
    getCardFiles: vi.fn(async () => [...stored]),
    getCardActivity: vi.fn(async () => []),
    uploadCardFile: vi.fn(
      (
        _itemId: string,
        file: File,
        options: {
          idempotencyKey: string;
          signal?: AbortSignal;
          onProgress?: (p: { loaded: number; total: number }) => void;
        },
      ) =>
        new Promise<CardFile>((resolve, reject) => {
          calls.push({
            file,
            idempotencyKey: options.idempotencyKey,
            signal: options.signal,
            progress: (loaded, total) =>
              options.onProgress?.({ loaded, total }),
            resolve: () => {
              const record = storedFile({ name: file.name, size: file.size });
              stored.push(record);
              resolve(record);
            },
            reject,
          });
        }),
    ),
  };
  return {
    calls,
    cards,
    services: { cards } as unknown as WorkManagementServices,
  };
}

/** The item page's file section driven by the real upload hook. */
function Harness({
  copy,
  canEdit = true,
  onDownloadFile,
}: {
  copy: ItemPageCopy;
  canEdit?: boolean;
  onDownloadFile?: (file: CardFile) => void;
}) {
  const uploads = useItemFileUploads(ITEM, WORKSPACE);
  const files = useCardFiles(ITEM, WORKSPACE);
  return (
    <ItemPageFiles
      files={files.data ?? []}
      updates={[]}
      capabilities={
        canEdit ? editorSheetCapabilities : commenterSheetCapabilities
      }
      callbacks={{
        onAddFile: uploads.start,
        ...(onDownloadFile ? { onDownloadFile } : {}),
      }}
      copy={copy}
      locale="en"
      now={ERP_PAGE_NOW}
      uploads={uploads.uploads.map((upload) => ({
        ...upload,
        ...(upload.error === "tooBig" ? { limit: "100 MB" } : {}),
        retryable: canRetryItemFileUpload(upload),
      }))}
      onRetryUpload={uploads.retry}
      onDismissUpload={uploads.dismiss}
    />
  );
}

function setup(
  copy: ItemPageCopy,
  props: Partial<Parameters<typeof Harness>[0]> = {},
) {
  const fake = fakeServices();
  const view = renderPureUi(
    <WorkManagementTestScope services={fake.services}>
      <Harness copy={copy} {...props} />
    </WorkManagementTestScope>,
  );
  const input = () =>
    view.container.querySelector<HTMLInputElement>('input[type="file"]')!;
  const choose = (file: File) =>
    fireEvent.change(input(), { target: { files: [file] } });
  return { ...fake, ...view, choose };
}

function bigFile(name: string, size: number): File {
  const file = new File(["x"], name, { type: "application/zip" });
  Object.defineProperty(file, "size", { value: size });
  return file;
}

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("item files: upload and download (%s)", (_locale, copy) => {
  it("shows a chosen file uploading with progress, then the stored file in the list", async () => {
    const { choose, calls, cards } = setup(copy);
    await waitFor(() => expect(cards.getCardFiles).toHaveBeenCalledTimes(1));
    const file = new File(["noi dung"], "ghi-chu.pdf", {
      type: "application/pdf",
    });
    choose(file);

    const bar = await screen.findByRole("progressbar", { name: "ghi-chu.pdf" });
    expect(bar.getAttribute("aria-valuenow")).toBeNull();
    expect(calls).toHaveLength(1);
    expect(calls[0]!.file).toBe(file);
    expect(calls[0]!.idempotencyKey).toMatch(/^[0-9a-f-]{36}$/);
    expect(calls[0]!.signal?.aborted).toBe(false);

    act(() => calls[0]!.progress(4, 8));
    expect(
      screen
        .getByRole("progressbar", { name: "ghi-chu.pdf" })
        .getAttribute("aria-valuenow"),
    ).toBe("50");

    await act(async () => calls[0]!.resolve());
    expect(document.querySelector("[data-upload-status=done]")).not.toBeNull();
    // The item's files are read again from the server: the stored file shows.
    await waitFor(() => expect(cards.getCardFiles).toHaveBeenCalledTimes(2));
    await waitFor(() =>
      expect(screen.getAllByText("ghi-chu.pdf").length).toBeGreaterThan(0),
    );
    expect(cards.getCardActivity).not.toHaveBeenCalled();
  });

  it("refuses a file over 100 MB before sending, with the limit", async () => {
    const { choose, cards } = setup(copy);
    choose(bigFile("du-lieu.zip", 100 * 1024 * 1024 + 1));
    const row = await waitFor(() => {
      const found = document.querySelector("[data-upload-status=error]");
      expect(found).not.toBeNull();
      return found!;
    });
    expect(row.textContent).toContain(
      copy.files.tooBig.replace("{{limit}}", "100 MB"),
    );
    expect(cards.uploadCardFile).not.toHaveBeenCalled();
    // A refused file cannot be retried, only skipped.
    expect(screen.queryByRole("button", { name: copy.files.retry })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: copy.files.skip }));
    expect(document.querySelector("[data-upload-status]")).toBeNull();
  });

  it("refuses a blocked extension before sending", async () => {
    const { choose, cards } = setup(copy);
    choose(new File(["MZ"], "setup.EXE"));
    await waitFor(() =>
      expect(
        document.querySelector("[data-upload-status=error]")?.textContent,
      ).toContain(copy.files.blockedType),
    );
    expect(cards.uploadCardFile).not.toHaveBeenCalled();
  });

  it.each([
    [
      { status: 413, code: "attachment.payload_too_large" },
      "serverTooLarge",
      false,
    ],
    [
      { status: 415, code: "attachment.unsupported_media_type" },
      "unsupportedType",
      false,
    ],
    [{ status: 409, code: "attachment.limit_exceeded" }, "limitReached", true],
    [{ status: 403 }, "forbidden", false],
    [{ status: 404 }, "notFound", false],
    [{ kind: "network" }, "failed", true],
  ] as const)(
    "says why the server refused %j and offers a retry only when it can help",
    async (failure, message, retryable) => {
      const { choose, calls } = setup(copy);
      choose(new File(["x"], "a.pdf"));
      await waitFor(() => expect(calls).toHaveLength(1));
      await act(async () => calls[0]!.reject(failure));
      await waitFor(() =>
        expect(
          document.querySelector("[data-upload-status=error]")?.textContent,
        ).toContain(copy.files[message]),
      );
      expect(
        screen.queryByRole("button", { name: copy.files.retry }) !== null,
      ).toBe(retryable);
    },
  );

  it("retries the same action with the same idempotency key", async () => {
    const { choose, calls } = setup(copy);
    choose(new File(["x"], "a.pdf"));
    await waitFor(() => expect(calls).toHaveLength(1));
    await act(async () => calls[0]!.reject({ kind: "network" }));
    fireEvent.click(
      await screen.findByRole("button", { name: copy.files.retry }),
    );
    await waitFor(() => expect(calls).toHaveLength(2));
    expect(calls[1]!.idempotencyKey).toBe(calls[0]!.idempotencyKey);
    expect(calls[1]!.file).toBe(calls[0]!.file);
    await act(async () => calls[1]!.resolve());
    expect(document.querySelector("[data-upload-status=done]")).not.toBeNull();
  });

  it("uses a fresh idempotency key for each chosen file", async () => {
    const { choose, calls } = setup(copy);
    choose(new File(["x"], "a.pdf"));
    choose(new File(["y"], "b.pdf"));
    await waitFor(() => expect(calls).toHaveLength(2));
    expect(calls[0]!.idempotencyKey).not.toBe(calls[1]!.idempotencyKey);
  });

  it("cancels an upload in flight by aborting its request", async () => {
    const { choose, calls } = setup(copy);
    choose(new File(["x"], "a.pdf"));
    await waitFor(() => expect(calls).toHaveLength(1));
    fireEvent.click(
      screen.getByRole("button", {
        name: copy.files.cancel.replace("{{name}}", "a.pdf"),
      }),
    );
    expect(calls[0]!.signal?.aborted).toBe(true);
    expect(document.querySelector("[data-upload-status]")).toBeNull();
  });

  it("enables the download for a stored file and calls the download with it", async () => {
    const onDownloadFile = vi.fn();
    const fake = fakeServices();
    fake.cards.getCardFiles.mockResolvedValue([storedFile()]);
    renderPureUi(
      <WorkManagementTestScope services={fake.services}>
        <Harness copy={copy} onDownloadFile={onDownloadFile} />
      </WorkManagementTestScope>,
    );
    const button = await screen.findByRole("button", {
      name: copy.files.download.replace("{{name}}", "ghi-chu.pdf"),
    });
    expect(button.getAttribute("aria-disabled")).toBeNull();
    fireEvent.click(button);
    expect(onDownloadFile).toHaveBeenCalledWith(
      expect.objectContaining({ id: "att-1", kind: "file" }),
    );
  });

  it("keeps the download off for a file with no way to fetch it and opens a link by its address", async () => {
    const fake = fakeServices();
    fake.cards.getCardFiles.mockResolvedValue([
      storedFile({ id: "att-2", name: "mat-tich.pdf", kind: undefined }),
      storedFile({
        id: "att-3",
        name: "tai-lieu",
        kind: "link",
        source: "link",
        url: "https://example.com/doc",
      }),
    ]);
    renderPureUi(
      <WorkManagementTestScope services={fake.services}>
        <Harness copy={copy} />
      </WorkManagementTestScope>,
    );
    const missing = await screen.findByRole("button", {
      name: copy.files.download.replace("{{name}}", "mat-tich.pdf"),
    });
    expect(missing.getAttribute("aria-disabled")).toBe("true");
    const link = screen.getByRole("link", {
      name: copy.files.download.replace("{{name}}", "tai-lieu"),
    });
    expect(link.getAttribute("href")).toBe("https://example.com/doc");
  });

  it("offers no upload to someone who cannot edit", async () => {
    const { container } = setup(copy, { canEdit: false });
    expect(container.querySelector('input[type="file"]')).toBeNull();
  });
});
