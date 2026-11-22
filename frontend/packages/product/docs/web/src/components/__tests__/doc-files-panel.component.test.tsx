import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { fillDocCopy } from "../doc-page.copy";
import type { DocFileEntry, DocFilesPanel } from "../doc-page-models";
import { DocSidePanelSurface } from "../doc-page-surfaces";

const FILE: DocFileEntry = {
  id: "a1",
  name: "bao-cao.pdf",
  detail: "2 KB · PDF · Lan · 10/11",
  kind: "file",
  href: null,
};
const LINK: DocFileEntry = {
  id: "a2",
  name: "Hướng dẫn",
  detail: "example.com · 11/11",
  kind: "link",
  href: "https://example.com/guide",
};

function files(over: Partial<DocFilesPanel> = {}): DocFilesPanel {
  return {
    status: "ready",
    entries: [LINK, FILE],
    canEdit: true,
    onRetry: vi.fn(),
    onDownload: vi.fn(async () => undefined),
    onDelete: vi.fn(async () => undefined),
    onAddLink: vi.fn(async () => null),
    ...over,
  };
}

function show(panel: DocFilesPanel) {
  return renderPureUi(
    <DocSidePanelSurface
      copy={docsCopy}
      tab="files"
      tasks={[]}
      relatedDocs={[]}
      toc={[]}
      comments={[]}
      files={panel}
    />,
  );
}

const f = docsCopy.files;
const urlField = () => screen.getByLabelText(f.addLink.url);
const submit = (label = f.addLink.submit) =>
  fireEvent.click(screen.getByRole("button", { name: label }));

describe("the Files tab of the side panel", () => {
  it("adds a Files tab with the count, only when the page hands it files", () => {
    show(files());
    expect(
      screen.getByRole("tab", { name: `${docsCopy.panel.files} 2` }),
    ).toBeTruthy();
  });

  it("shows loading, empty, denied, not found and failed-with-Retry", () => {
    const loading = show(files({ status: "loading", entries: [] }));
    expect(screen.getByRole("status", { name: f.loading })).toBeTruthy();
    loading.unmount();

    const empty = show(files({ entries: [] }));
    expect(screen.getByText(f.empty)).toBeTruthy();
    empty.unmount();

    const denied = show(files({ status: "denied", entries: [] }));
    expect(screen.getByRole("alert").textContent).toBe(f.denied);
    expect(screen.queryByLabelText(f.addLink.url)).toBeNull();
    denied.unmount();

    const missing = show(files({ status: "notFound", entries: [] }));
    expect(screen.getByRole("alert").textContent).toBe(f.notFound);
    missing.unmount();

    const onRetry = vi.fn();
    show(files({ status: "error", entries: [], onRetry }));
    expect(screen.getByRole("alert").textContent).toContain(f.error);
    fireEvent.click(screen.getByRole("button", { name: f.retry }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("opens a link in a new tab without leaking the opener, and downloads a stored file", async () => {
    const onDownload = vi.fn(async () => undefined);
    show(files({ onDownload }));
    const link = screen.getAllByRole("link", {
      name: fillDocCopy(f.openLink, { name: LINK.name }),
    })[0] as HTMLAnchorElement;
    expect(link.getAttribute("href")).toBe("https://example.com/guide");
    expect(link.getAttribute("target")).toBe("_blank");
    expect(link.getAttribute("rel")).toBe("noopener noreferrer");
    const download = screen.getByRole("button", {
      name: fillDocCopy(f.download, { name: FILE.name }),
    }) as HTMLButtonElement;
    fireEvent.click(download);
    expect(onDownload).toHaveBeenCalledWith("a1");
    await waitFor(() => expect(download.disabled).toBe(false));
  });

  it("never makes an anchor of an address that is not http or https", () => {
    show(
      files({
        entries: [
          { ...LINK, href: "javascript:alert(1)" },
          { ...LINK, id: "x", href: null },
        ],
      }),
    );
    expect(screen.queryAllByRole("link")).toHaveLength(0);
  });

  it("asks before deleting and deletes only on confirm", async () => {
    const onDelete = vi.fn(async () => undefined);
    show(files({ onDelete }));
    fireEvent.click(
      screen.getByRole("button", {
        name: fillDocCopy(f.deleteAction, { name: FILE.name }),
      }),
    );
    expect(onDelete).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: f.deleteKeep }));
    expect(onDelete).not.toHaveBeenCalled();
    fireEvent.click(
      screen.getByRole("button", {
        name: fillDocCopy(f.deleteAction, { name: FILE.name }),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: f.delete }));
    await waitFor(() => expect(onDelete).toHaveBeenCalledWith("a1"));
  });

  it("gives a viewer the list and the open actions, with no delete and no link form", () => {
    show(files({ canEdit: false }));
    expect(screen.getByText(f.readOnly)).toBeTruthy();
    expect(screen.queryByLabelText(f.addLink.url)).toBeNull();
    expect(
      screen.queryByRole("button", {
        name: fillDocCopy(f.deleteAction, { name: FILE.name }),
      }),
    ).toBeNull();
    expect(
      screen.getByRole("button", {
        name: fillDocCopy(f.download, { name: FILE.name }),
      }),
    ).toBeTruthy();
  });
});

describe("adding a link on the Files tab (UC-COL-B09)", () => {
  it("refuses an address that is not http or https before sending anything", () => {
    const onAddLink = vi.fn(async () => null);
    show(files({ onAddLink }));
    for (const bad of [
      "javascript:alert(1)",
      "ftp://example.com/a",
      "example.com",
    ]) {
      fireEvent.change(urlField(), { target: { value: bad } });
      submit();
      expect(screen.getByRole("alert").textContent).toBe(
        f.addLink.errors.invalid,
      );
    }
    expect(onAddLink).not.toHaveBeenCalled();
  });

  it("sends a valid link once, shows pending, then clears the form and says it was added", async () => {
    let finish: (value: null) => void = () => undefined;
    const onAddLink = vi.fn(
      () => new Promise<null>((resolve) => (finish = resolve)),
    );
    show(files({ onAddLink }));
    fireEvent.change(urlField(), {
      target: { value: " https://example.com/spec " },
    });
    fireEvent.change(screen.getByLabelText(f.addLink.name), {
      target: { value: " Đặc tả " },
    });
    submit();
    expect(onAddLink).toHaveBeenCalledWith({
      url: "https://example.com/spec",
      name: "Đặc tả",
    });
    const pending = screen.getByRole("button", {
      name: f.addLink.pending,
    }) as HTMLButtonElement;
    expect(pending.disabled).toBe(true);
    finish(null);
    await waitFor(() =>
      expect(screen.getByRole("status").textContent).toBe(f.addLink.added),
    );
    expect((urlField() as HTMLInputElement).value).toBe("");
  });

  it("keeps the form and offers Try again after each refusal", async () => {
    const reasons = [
      "limitReached",
      "forbidden",
      "notFound",
      "network",
      "failed",
      "invalid",
    ] as const;
    for (const reason of reasons) {
      const onAddLink = vi.fn(async () => reason);
      const view = show(files({ onAddLink }));
      fireEvent.change(urlField(), {
        target: { value: "https://example.com/a" },
      });
      submit();
      await waitFor(() =>
        expect(screen.getByRole("alert").textContent).toBe(
          f.addLink.errors[reason],
        ),
      );
      expect((urlField() as HTMLInputElement).value).toBe(
        "https://example.com/a",
      );
      const retry = reason === "network" || reason === "failed";
      expect(!!screen.queryByRole("button", { name: f.addLink.retry })).toBe(
        retry,
      );
      view.unmount();
    }
  });

  it("sends the same link again from Try again after a lost connection", async () => {
    const onAddLink = vi
      .fn<DocFilesPanel["onAddLink"]>()
      .mockResolvedValueOnce("network")
      .mockResolvedValueOnce(null);
    show(files({ onAddLink }));
    fireEvent.change(urlField(), {
      target: { value: "https://example.com/a" },
    });
    submit();
    await waitFor(() => screen.getByRole("button", { name: f.addLink.retry }));
    submit(f.addLink.retry);
    await waitFor(() =>
      expect(screen.getByRole("status").textContent).toBe(f.addLink.added),
    );
    expect(onAddLink).toHaveBeenCalledTimes(2);
    expect(onAddLink.mock.calls[1]).toEqual(onAddLink.mock.calls[0]);
  });
});
