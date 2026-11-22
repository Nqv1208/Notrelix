import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
} from "@notrelix/testing";
import type { LinkAttachResult } from "@notrelix/work-management-state";
import { en as pageCopy } from "../../../item-page/item-page.copy";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { AttachPanel } from "../attach-panel";

vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useFullBoard: () => ({ groups: [] }),
  useLinkablePages: () => ({ pages: [], isError: false, refetch: vi.fn() }),
}));

const T = en.pickers.attach;

function show(
  options: {
    addWebLink?: (url: string, name: string) => Promise<LinkAttachResult>;
    uploads?: readonly Record<string, unknown>[];
    onAddFile?: boolean;
  } = {},
) {
  const onClose = vi.fn();
  const callbacks = {
    onAddFile: options.onAddFile === false ? undefined : vi.fn(),
    onRetryUpload: vi.fn(),
    onDismissUpload: vi.fn(),
  };
  const addWebLink =
    options.addWebLink ?? vi.fn(async () => ({ ok: true as const }));
  const model = {
    mode: "view",
    copy: en,
    card: { id: "item-1", boardId: "b-1", workspaceId: "ws-1" },
    callbacks,
    actions: { addWebLink },
    data: { files: [], filesLoading: false, uploads: options.uploads ?? [] },
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <AttachPanel onClose={onClose} />
      </CardPopupProvider>
    </I18nProvider>,
  );
  return { onClose, callbacks, addWebLink };
}

function typeAddress(value: string) {
  fireEvent.change(screen.getByRole("textbox", { name: T.smartLabel }), {
    target: { value },
  });
}

const attach = () => screen.getByRole("button", { name: T.linkAction });

describe("attach panel: link (UC-COL-B07)", () => {
  it("attaches the address with the typed name and closes", async () => {
    const { addWebLink, onClose } = show();

    typeAddress("https://example.com/spec?v=1");
    fireEvent.change(screen.getByLabelText(T.linkName), {
      target: { value: "Spec" },
    });
    fireEvent.click(attach());

    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(1));
    expect(addWebLink).toHaveBeenCalledWith(
      "https://example.com/spec?v=1",
      "Spec",
    );
  });

  it("names the link after its address when no name is typed", async () => {
    const { addWebLink } = show();

    typeAddress("example.com/docs");
    fireEvent.click(attach());

    await waitFor(() => expect(addWebLink).toHaveBeenCalledTimes(1));
    expect(addWebLink).toHaveBeenCalledWith(
      "https://example.com/docs",
      "example.com/docs",
    );
  });

  it("is busy and cannot be sent twice while the server answers", async () => {
    let answer!: (result: LinkAttachResult) => void;
    const addWebLink = vi.fn(
      () => new Promise<LinkAttachResult>((resolve) => (answer = resolve)),
    );
    const { onClose } = show({ addWebLink });

    typeAddress("https://example.com");
    fireEvent.click(attach());

    expect((attach() as HTMLButtonElement).disabled).toBe(true);
    expect(attach().getAttribute("aria-busy")).toBe("true");
    fireEvent.submit(attach().closest("form")!);
    expect(addWebLink).toHaveBeenCalledTimes(1);

    await act(async () => answer({ ok: true }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("refuses a scheme that is not http or https without asking the server", () => {
    const { addWebLink, onClose } = show();

    typeAddress("ftp://example.com/file");
    fireEvent.click(attach());

    expect(screen.getByRole("alert").textContent).toBe(T.urlInvalid);
    expect(addWebLink).not.toHaveBeenCalled();
    expect(onClose).not.toHaveBeenCalled();
  });

  it.each([
    ["invalid", T.urlInvalid],
    ["rejected", T.linkRejected],
    ["limitReached", T.linkLimit],
    ["forbidden", T.linkForbidden],
    ["notFound", T.linkGone],
    ["offline", T.linkOffline],
    ["failed", T.failed],
  ] as const)(
    "says %s in its own words, keeps the panel open and the button usable",
    async (reason, message) => {
      const addWebLink = vi.fn(async () => ({ ok: false as const, reason }));
      const { onClose } = show({ addWebLink });

      typeAddress("https://example.com");
      fireEvent.click(attach());

      expect((await screen.findByRole("alert")).textContent).toBe(message);
      expect(onClose).not.toHaveBeenCalled();
      expect((attach() as HTMLButtonElement).disabled).toBe(false);
      expect(attach().getAttribute("aria-busy")).toBeNull();
    },
  );

  it("sends the same address again after a refusal, ready to retry", async () => {
    const addWebLink = vi
      .fn<(url: string, name: string) => Promise<LinkAttachResult>>()
      .mockResolvedValueOnce({ ok: false, reason: "offline" })
      .mockResolvedValueOnce({ ok: true });
    const { onClose } = show({ addWebLink });

    typeAddress("https://example.com/a");
    fireEvent.click(attach());
    await screen.findByRole("alert");
    fireEvent.click(attach());

    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(1));
    expect(addWebLink.mock.calls).toEqual([
      ["https://example.com/a", "example.com/a"],
      ["https://example.com/a", "example.com/a"],
    ]);
  });
});

const upload = (extra: Record<string, unknown>) => ({
  id: "up-1",
  name: "so-lieu.xlsx",
  size: 10,
  status: "uploading",
  progress: 40,
  error: null,
  retryable: false,
  ...extra,
});

describe("attach panel: file uploads (UC-COL-B06)", () => {
  it("shows the progress bar and a polite percent status", () => {
    show({ uploads: [upload({})] });

    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe(
      "40",
    );
    expect(screen.getByRole("status").textContent).toBe("40%");
  });

  it("cancels a sending file", () => {
    const { callbacks } = show({ uploads: [upload({})] });

    fireEvent.click(
      screen.getByRole("button", { name: "Cancel the upload of so-lieu.xlsx" }),
    );

    expect(callbacks.onDismissUpload).toHaveBeenCalledWith("up-1");
  });

  it.each([
    ["tooBig", pageCopy.files.tooBig.replace("{{limit}}", "25 MB"), false],
    ["blockedType", pageCopy.files.blockedType, false],
    ["serverTooLarge", pageCopy.files.serverTooLarge, false],
    ["unsupportedType", pageCopy.files.unsupportedType, false],
    ["limitReached", pageCopy.files.limitReached, true],
    ["failed", pageCopy.files.failed, true],
  ] as const)(
    "tells %s as an alert and offers Retry only when it can help",
    (error, message, retryable) => {
      const { callbacks } = show({
        uploads: [
          upload({
            status: "error",
            progress: null,
            error,
            retryable,
            limit: "25 MB",
          }),
        ],
      });

      expect(screen.getByRole("alert").textContent).toBe(message);
      const retry = screen.queryByRole("button", { name: T.retry });
      expect(Boolean(retry)).toBe(retryable);
      if (retry) {
        fireEvent.click(retry);
        expect(callbacks.onRetryUpload).toHaveBeenCalledWith("up-1");
      }
      fireEvent.click(
        screen.getByRole("button", {
          name: "Remove so-lieu.xlsx from the list",
        }),
      );
      expect(callbacks.onDismissUpload).toHaveBeenCalledWith("up-1");
    },
  );

  it("offers no file chooser to someone who cannot add files", () => {
    show({ onAddFile: false });

    expect(screen.queryByRole("button", { name: T.chooseFile })).toBeNull();
  });
});
