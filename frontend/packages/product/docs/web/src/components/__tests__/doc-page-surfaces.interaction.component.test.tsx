import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";

import {
  BLOCKS,
  COMMENTS,
  HEADER,
  RELATED_DOCS,
  SHARE,
  TASKS,
  TOC,
  VERSIONS,
  docsCopy,
  docsPageScreenDefaultScenario,
  docsPageScreenConvertScenario,
  docsPageScreenEmptyScenario,
  docsPageScreenFileMenuScenario,
  docsPageScreenLoadingScenario,
  docsPageScreenMentionScenario,
  docsPageScreenNotFoundScenario,
  docsPageScreenPagePickerScenario,
  docsPageScreenReadOnlyScenario,
  docsPageScreenSaveErrorScenario,
  docsPageScreenShareScenario,
  docsPageScreenSlashScenario,
  docsPageScreenTrashScenario,
  docsPageScreenVersionsScenario,
} from "../../verification/docs-ui-fixtures";
import {
  DocArticleSurface,
  DocPageBarSurface,
  DocPageScreenSurface,
  DocShareDialogSurface,
  DocSidePanelSurface,
  DocToolbarSurface,
  DocVersionPanelSurface,
} from "../doc-page-surfaces";

const ARTICLE = {
  copy: docsCopy,
  title: HEADER.title,
  chips: docsPageScreenDefaultScenario().chips,
  why: docsPageScreenDefaultScenario().why,
  blocks: BLOCKS,
  overlay: { kind: "none" } as const,
  mentionTasks: [],
};

/** Opens the styled Select behind a label and picks one option by its text. */
function choose(trigger: HTMLElement, option: string | RegExp) {
  fireEvent.keyDown(trigger, { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe("docs web pure surfaces", () => {
  it("FUI[docs.page.screen:render] renders the document bar, tools, sheet and side panel from fixture data", () => {
    const onPanelTab = vi.fn();
    renderPureUi(
      <DocPageScreenSurface
        {...docsPageScreenDefaultScenario()}
        callbacks={{ onPanelTab }}
      />,
    );

    expect(
      screen.getByRole("heading", {
        level: 1,
        name: docsCopy.titlePlaceholder,
      }),
    ).toHaveProperty("textContent", HEADER.title);
    expect(
      screen.getByRole("heading", { name: "Phạm vi áp dụng" }),
    ).toBeTruthy();
    expect(
      screen.getByRole("toolbar", { name: docsCopy.toolbar.label }),
    ).toBeTruthy();
    expect(
      screen.getByRole("table", { name: docsCopy.blocks.liveTable }),
    ).toBeTruthy();
    expect(
      screen.getByText("Công việc được nhắc", { selector: "h3" }),
    ).toBeTruthy();

    fireEvent.click(screen.getByRole("tab", { name: /Mục lục/ }));
    fireEvent.click(screen.getByRole("tab", { name: /Bình luận/ }));
    expect(onPanelTab).toHaveBeenNthCalledWith(1, "toc");
    expect(onPanelTab).toHaveBeenNthCalledWith(2, "comments");
  });

  it("FUI[docs.page.screen:panel] closes and reopens the side panel from its tab row and the document bar", () => {
    const onPanelOpenChange = vi.fn();
    const open = renderPureUi(
      <DocPageScreenSurface
        {...docsPageScreenDefaultScenario()}
        callbacks={{ onPanelOpenChange }}
      />,
    );
    const toggle = screen.getByRole("button", {
      name: docsCopy.header.hidePanel,
    });
    expect(toggle.getAttribute("aria-pressed")).toBe("true");
    fireEvent.click(
      within(screen.getByRole("complementary")).getByRole("button", {
        name: docsCopy.panel.close,
      }),
    );
    fireEvent.click(toggle);
    expect(onPanelOpenChange).toHaveBeenNthCalledWith(1, false);
    expect(onPanelOpenChange).toHaveBeenNthCalledWith(2, false);
    open.unmount();

    renderPureUi(
      <DocPageScreenSurface
        {...docsPageScreenDefaultScenario()}
        panelOpen={false}
        callbacks={{ onPanelOpenChange }}
      />,
    );
    expect(screen.queryByRole("complementary")).toBeNull();
    const reopen = screen.getByRole("button", {
      name: docsCopy.header.showPanel,
    });
    expect(reopen.getAttribute("aria-pressed")).toBe("false");
    fireEvent.click(reopen);
    expect(onPanelOpenChange).toHaveBeenLastCalledWith(true);
  });

  it("FUI[docs.page.screen:states] renders loading, not found, empty, read-only, save error and overlay states", () => {
    const loading = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenLoadingScenario()} />,
    );
    expect(screen.getByRole("status", { name: docsCopy.loading })).toBeTruthy();
    loading.unmount();

    const missing = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenNotFoundScenario()} />,
    );
    expect(screen.getByText(docsCopy.notFound)).toBeTruthy();
    missing.unmount();

    const empty = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenEmptyScenario()} />,
    );
    expect(screen.getByText(docsCopy.blocks.emptyDoc)).toBeTruthy();
    empty.unmount();

    const readOnly = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenReadOnlyScenario()} />,
    );
    expect(
      screen
        .getByRole("heading", { level: 1, name: docsCopy.titlePlaceholder })
        .getAttribute("contenteditable"),
    ).toBe("false");
    expect(
      screen
        .getAllByRole("textbox")
        .every((box) => box.getAttribute("aria-readonly") === "true"),
    ).toBe(true);
    readOnly.unmount();

    const failed = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenSaveErrorScenario()} />,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      docsCopy.header.retry,
    );
    failed.unmount();

    const menu = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenFileMenuScenario()} />,
    );
    expect(
      screen.getByRole("menu", { name: docsCopy.fileMenu.label }),
    ).toBeTruthy();
    expect(
      screen.getByRole("menu", { name: docsCopy.fileMenu.downloadLabel }),
    ).toBeTruthy();
    menu.unmount();

    const slash = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenSlashScenario()} />,
    );
    expect(
      screen.getByRole("listbox", { name: docsCopy.slash.label }),
    ).toBeTruthy();
    slash.unmount();

    const mention = renderPureUi(
      <DocPageScreenSurface {...docsPageScreenMentionScenario()} />,
    );
    expect(screen.getAllByRole("option")).toHaveLength(4);
    mention.unmount();

    renderPureUi(<DocPageScreenSurface {...docsPageScreenShareScenario()} />);
    expect(screen.getByRole("dialog")).toBeTruthy();

    // The page behind the dialog is not reachable.
    expect(
      screen.queryByRole("toolbar", { name: docsCopy.toolbar.label }),
    ).toBeNull();
  });

  it("FUI[docs.page.screen:versions] swaps the tools and side tabs for the compare banner and version list", () => {
    renderPureUi(
      <DocPageScreenSurface {...docsPageScreenVersionsScenario()} />,
    );

    expect(
      screen.getByRole("region", { name: docsCopy.versions.bar }),
    ).toBeTruthy();
    expect(screen.queryByRole("toolbar")).toBeNull();
    expect(
      screen.getByRole("heading", { name: docsCopy.versions.title }),
    ).toBeTruthy();
    expect(document.querySelectorAll("ins").length).toBeGreaterThan(0);
    expect(document.querySelectorAll("del").length).toBeGreaterThan(0);
  });

  it("FUI[docs.page.bar:actions] routes retry and share through callbacks and states the unavailable actions", () => {
    const onSaveRetry = vi.fn();
    const onOpenShare = vi.fn();
    renderPureUi(
      <DocPageBarSurface
        copy={docsCopy}
        header={HEADER}
        saveState="error"
        canShare
        callbacks={{ onSaveRetry, onOpenShare }}
      />,
    );

    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.header.retry }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.header.share }),
    );
    expect(onSaveRetry).toHaveBeenCalledTimes(1);
    expect(onOpenShare).toHaveBeenCalledTimes(1);
    expect(
      screen
        .getByRole("button", { name: docsCopy.header.approve })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it("FUI[docs.page.toolbar:menus] opens the File menu and routes its actions through callbacks", () => {
    const onOpenFileMenu = vi.fn();
    const onNewDoc = vi.fn();
    const onDownload = vi.fn();
    const onOpenVersions = vi.fn();
    const onToggleDownload = vi.fn();
    const closed = renderPureUi(
      <DocToolbarSurface
        copy={docsCopy}
        fileMenuOpen={false}
        callbacks={{ onOpenFileMenu }}
      />,
    );
    // The File button is a menu bar item now (role menuitem inside role menubar).
    fireEvent.click(
      screen.getByRole("menuitem", { name: docsCopy.menus.file }),
    );
    expect(onOpenFileMenu).toHaveBeenCalledTimes(1);
    closed.unmount();

    renderPureUi(
      <DocToolbarSurface
        copy={docsCopy}
        fileMenuOpen
        downloadOpen
        canShare
        callbacks={{ onNewDoc, onDownload, onOpenVersions, onToggleDownload }}
      />,
    );
    fireEvent.click(screen.getByRole("menuitem", { name: /Tài liệu mới/ }));
    fireEvent.click(
      screen.getByRole("menuitem", { name: /^Markdown \(\.md\)/ }),
    );
    fireEvent.click(
      screen.getByRole("menuitem", { name: /Lịch sử phiên bản/ }),
    );
    fireEvent.click(screen.getByRole("menuitem", { name: /Tải xuống/ }));
    expect(onNewDoc).toHaveBeenCalledTimes(1);
    expect(onDownload).toHaveBeenCalledWith("markdown");
    expect(onOpenVersions).toHaveBeenCalledTimes(1);
    expect(onToggleDownload).toHaveBeenCalledTimes(1);
    expect(
      screen
        .getByRole("menuitem", { name: /^HTML \(\.html\)/ })
        .getAttribute("aria-disabled"),
    ).not.toBe("true");
  });

  it("FUI[docs.page.article:editing] commits text on blur, toggles a checklist item and picks a block from the slash menu", () => {
    const onBlockCommit = vi.fn();
    const onTodoToggle = vi.fn();
    const onSlashSelect = vi.fn();
    const onBlockTrigger = vi.fn();
    const scenario = docsPageScreenSlashScenario();
    renderPureUi(
      <DocArticleSurface
        {...ARTICLE}
        blocks={scenario.blocks}
        overlay={scenario.overlay}
        callbacks={{
          onBlockCommit,
          onTodoToggle,
          onSlashSelect,
          onBlockTrigger,
        }}
      />,
    );

    const heading = screen.getByRole("heading", { name: "Phạm vi áp dụng" });
    heading.textContent = "Phạm vi mới";
    fireEvent.input(heading);
    fireEvent.blur(heading);
    expect(onBlockCommit).toHaveBeenCalledWith("b1", "Phạm vi mới");

    fireEvent.click(
      screen.getByRole("checkbox", {
        name: "Đánh dấu hoàn thành: Đào tạo nhóm Công nợ về đối chiếu tự động",
      }),
    );
    expect(onTodoToggle).toHaveBeenCalledWith("b10", true);

    const options = screen.getAllByRole("option");
    expect(options).toHaveLength(3);
    fireEvent.keyDown(window, { key: "ArrowDown" });
    fireEvent.keyDown(window, { key: "Enter" });
    expect(onSlashSelect).toHaveBeenCalledWith("b-slash", "todo");
  });

  it("FUI[docs.page.article:mention] lists matching tasks and routes the pick through the callback", () => {
    const onMentionSelect = vi.fn();
    const scenario = docsPageScreenMentionScenario();
    renderPureUi(
      <DocArticleSurface
        {...ARTICLE}
        blocks={scenario.blocks}
        overlay={scenario.overlay}
        mentionTasks={scenario.mentionTasks}
        callbacks={{ onMentionSelect }}
      />,
    );

    const list = screen.getByRole("listbox", { name: docsCopy.mention.label });
    fireEvent.click(within(list).getByText("ERP-144"));
    expect(onMentionSelect).toHaveBeenCalledWith("b-mention", "task-erp-144");
  });

  it("FUI[docs.page.side-panel:tabs] switches tabs, selects a heading and posts a comment on Enter", () => {
    const onPanelTab = vi.fn();
    const onTocSelect = vi.fn();
    const onCommentCreate = vi.fn();
    const links = renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="links"
        tasks={TASKS}
        relatedDocs={RELATED_DOCS}
        toc={TOC}
        comments={COMMENTS}
        callbacks={{ onPanelTab }}
      />,
    );
    expect(screen.getByText("ERP-144")).toBeTruthy();
    expect(screen.getByText(docsCopy.panel.restrictedTask)).toBeTruthy();
    fireEvent.click(screen.getByRole("tab", { name: /Bình luận/ }));
    expect(onPanelTab).toHaveBeenCalledWith("comments");
    links.unmount();

    const toc = renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="toc"
        tasks={TASKS}
        relatedDocs={[]}
        toc={TOC}
        comments={COMMENTS}
        callbacks={{ onTocSelect }}
      />,
    );
    fireEvent.click(screen.getByRole("link", { name: "Các bước xử lý" }));
    expect(onTocSelect).toHaveBeenCalledWith("b3");
    toc.unmount();

    renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="comments"
        tasks={[]}
        relatedDocs={[]}
        toc={TOC}
        comments={COMMENTS}
        canComment
        callbacks={{ onCommentCreate }}
      />,
    );
    const reply = screen.getByRole("textbox", {
      name: docsCopy.comments.reply,
    });
    fireEvent.change(reply, { target: { value: "Đã rà lại" } });
    fireEvent.keyDown(reply, { key: "Enter" });
    expect(onCommentCreate).toHaveBeenCalledWith("Đã rà lại");
  });

  it("FUI[docs.page.versions:select] selects a version, filters named versions and closes the panel", () => {
    const onSelectVersion = vi.fn();
    const onCloseVersions = vi.fn();
    const onNamedOnlyChange = vi.fn();
    const view = renderPureUi(
      <DocVersionPanelSurface
        copy={docsCopy}
        versions={VERSIONS}
        selectedId="v3"
        callbacks={{ onSelectVersion, onCloseVersions }}
        onNamedOnlyChange={onNamedOnlyChange}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: /15:05/ }));
    expect(onSelectVersion).toHaveBeenCalledWith("v2");
    fireEvent.click(screen.getByRole("checkbox"));
    expect(onNamedOnlyChange).toHaveBeenCalledWith(true);
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.versions.closePanel }),
    );
    expect(onCloseVersions).toHaveBeenCalledTimes(1);
    view.unmount();

    renderPureUi(
      <DocVersionPanelSurface
        copy={docsCopy}
        versions={VERSIONS}
        selectedId="v3"
        namedOnly
      />,
    );
    expect(
      screen.getAllByRole("button", { name: /Bản trình Ban giám đốc/ }),
    ).toHaveLength(1);
  });

  it("FUI[docs.page.share:dialog] closes on Escape and Done, copies the link and keeps unavailable controls disabled", () => {
    const onCloseOverlay = vi.fn();
    const onCopyLink = vi.fn();
    renderPureUi(
      <DocShareDialogSurface
        copy={docsCopy}
        pageTitle={HEADER.title}
        share={SHARE}
        callbacks={{ onCloseOverlay }}
        onCopyLink={onCopyLink}
      />,
    );

    const dialog = screen.getByRole("dialog");
    expect(
      within(dialog).getByRole("heading", {
        name: `Chia sẻ ${HEADER.title}`,
      }),
    ).toBeTruthy();
    fireEvent.click(
      within(dialog).getByRole("button", { name: docsCopy.share.copyLink }),
    );
    expect(onCopyLink).toHaveBeenCalledTimes(1);
    expect(
      within(dialog)
        .getByRole("button", { name: docsCopy.share.invite })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    fireEvent.click(
      within(dialog).getByRole("button", { name: docsCopy.share.done }),
    );
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(onCloseOverlay).toHaveBeenCalledTimes(2);
  });

  it("FUI[docs.page.screen:trash] lists trashed pages with Restore and a two-step Delete permanently", () => {
    const onRestorePage = vi.fn();
    const onDeletePage = vi.fn();
    const onCloseOverlay = vi.fn();
    renderPureUi(
      <DocPageScreenSurface
        {...docsPageScreenTrashScenario()}
        callbacks={{ onRestorePage, onDeletePage, onCloseOverlay }}
      />,
    );
    const dialog = screen.getByRole("dialog", { name: docsCopy.trash.title });
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: "Khôi phục: Quy trình chi tạm ứng (bản cũ)",
      }),
    );
    expect(onRestorePage).toHaveBeenCalledWith("t1");

    fireEvent.click(
      within(dialog).getByRole("button", {
        name: "Xoá vĩnh viễn: Biên bản họp kiểm kê",
      }),
    );
    expect(onDeletePage).not.toHaveBeenCalled();
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: docsCopy.trash.confirmDelete,
      }),
    );
    expect(onDeletePage).toHaveBeenCalledWith("t2");

    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(onCloseOverlay).toHaveBeenCalled();
  });

  it("FUI[docs.page.screen:page-reference] filters the page picker and routes the chosen page", () => {
    const onPickPage = vi.fn();
    renderPureUi(
      <DocPageScreenSurface
        {...docsPageScreenPagePickerScenario()}
        callbacks={{ onPickPage }}
      />,
    );
    const dialog = screen.getByRole("dialog", { name: docsCopy.picker.title });
    fireEvent.change(within(dialog).getByRole("searchbox"), {
      target: { value: "cong no" },
    });
    expect(within(dialog).getAllByRole("option")).toHaveLength(1);
    fireEvent.click(
      within(dialog).getByRole("option", {
        name: /Quy trình đối chiếu công nợ/,
      }),
    );
    expect(onPickPage).toHaveBeenCalledWith("ref-1", "p2");
  });

  it("FUI[docs.page.screen:convert-task] needs a board and a group before it creates the task", () => {
    const onConvertConfirm = vi.fn();
    const onConvertBoard = vi.fn();
    renderPureUi(
      <DocPageScreenSurface
        {...docsPageScreenConvertScenario()}
        callbacks={{ onConvertConfirm, onConvertBoard }}
      />,
    );
    const dialog = screen.getByRole("dialog", { name: docsCopy.convert.title });
    const create = within(dialog).getByRole("button", {
      name: docsCopy.convert.confirm,
    });
    expect((create as HTMLButtonElement).disabled).toBe(true);
    choose(
      within(dialog).getByLabelText(docsCopy.convert.group),
      "Đào tạo và vận hành",
    );
    expect((create as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(create);
    expect(onConvertConfirm).toHaveBeenCalledWith("b1", {
      boardId: "board-1",
      groupId: "g2",
    });
    choose(
      within(dialog).getByLabelText(docsCopy.convert.board),
      "Kế toán hằng tháng",
    );
    expect(onConvertBoard).toHaveBeenCalledWith("board-2");
  });

  describe("on a phone", () => {
    const desktopWidth = window.innerWidth;
    beforeEach(() => {
      window.innerWidth = 320;
    });
    afterEach(() => {
      window.innerWidth = desktopWidth;
    });

    it("FUI[docs.page.screen:sheet] opens from Comments, closes on Escape and returns focus to the button", async () => {
      function Host() {
        const [open, setOpen] = useState(false);
        return (
          <DocPageScreenSurface
            {...docsPageScreenDefaultScenario()}
            panelOpen={open}
            panelTab="comments"
            callbacks={{
              onPanelOpenChange: setOpen,
              onOpenComments: () => setOpen(true),
            }}
          />
        );
      }
      renderPureUi(<Host />);
      // Closed by default: neither the column nor the sheet shows.
      expect(screen.queryByRole("complementary")).toBeNull();
      expect(screen.queryByRole("dialog")).toBeNull();
      const comments = screen.getByRole("button", {
        name: /Bình luận, \d+ chuỗi đang mở/,
      });
      expect(comments.getAttribute("aria-haspopup")).toBe("dialog");
      comments.focus();
      fireEvent.click(comments);
      const dialog = screen.getByRole("dialog", { name: docsCopy.panel.label });
      expect(document.activeElement).toBe(dialog);
      fireEvent.keyDown(dialog, { key: "Escape" });
      expect(screen.queryByRole("dialog")).toBeNull();
      await waitFor(() => expect(document.activeElement).toBe(comments));
    });

    it("FUI[docs.page.screen:sheet-open] shows aria-selected tabs and a 44 px Close that ask the host to close", () => {
      const onPanelOpenChange = vi.fn();
      const onPanelTab = vi.fn();
      renderPureUi(
        <DocPageScreenSurface
          {...docsPageScreenDefaultScenario()}
          panelOpen
          panelTab="comments"
          callbacks={{ onPanelOpenChange, onPanelTab }}
        />,
      );
      const dialog = screen.getByRole("dialog", { name: docsCopy.panel.label });
      expect(dialog.getAttribute("aria-modal")).toBe("true");
      const tabs = within(dialog).getAllByRole("tab");
      expect(tabs.map((tab) => tab.getAttribute("aria-selected"))).toEqual([
        "false",
        "false",
        "true",
      ]);
      fireEvent.click(tabs[1] as HTMLElement);
      expect(onPanelTab).toHaveBeenCalledWith("toc");
      const close = within(dialog).getByRole("button", {
        name: docsCopy.panel.close,
      });
      expect(close.className).toContain("size-11");
      fireEvent.click(close);
      fireEvent.keyDown(dialog, { key: "Escape" });
      expect(onPanelOpenChange).toHaveBeenCalledTimes(2);
      expect(onPanelOpenChange).toHaveBeenCalledWith(false);
    });

    it("FUI[docs.page.screen:back] keeps a named Back on loading, not-found and the ready bar", () => {
      const onBack = vi.fn();
      const backLabel = "Quay lại Phòng Kế toán";
      for (const scenario of [
        docsPageScreenLoadingScenario(),
        docsPageScreenNotFoundScenario(),
        docsPageScreenDefaultScenario(),
      ]) {
        const view = renderPureUi(
          <DocPageScreenSurface
            {...scenario}
            panelOpen={false}
            backLabel={backLabel}
            callbacks={{ onBack }}
          />,
        );
        fireEvent.click(screen.getByRole("button", { name: backLabel }));
        view.unmount();
      }
      expect(onBack).toHaveBeenCalledTimes(3);
    });

    it("FUI[docs.page.screen:more-sheet] moves Share, Approve, Contents and History behind More", () => {
      const onOpenShare = vi.fn();
      const onPanelTab = vi.fn();
      const onPanelOpenChange = vi.fn();
      renderPureUi(
        <DocPageScreenSurface
          {...docsPageScreenDefaultScenario()}
          panelOpen={false}
          callbacks={{ onOpenShare, onPanelTab, onPanelOpenChange }}
        />,
      );
      expect(
        screen.queryByRole("button", { name: docsCopy.header.share }),
      ).toBeNull();
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.header.moreSheet }),
      );
      const dialog = screen.getByRole("dialog", {
        name: docsCopy.header.moreTitle,
      });
      for (const label of [
        docsCopy.header.share,
        docsCopy.header.approve,
        docsCopy.panel.toc,
        docsCopy.fileMenu.versionHistory,
      ]) {
        expect(
          within(dialog).getByRole("button", { name: label }),
        ).toBeTruthy();
      }
      fireEvent.click(
        within(dialog).getByRole("button", { name: docsCopy.panel.toc }),
      );
      expect(onPanelTab).toHaveBeenCalledWith("toc");
      expect(onPanelOpenChange).toHaveBeenCalledWith(true);
      expect(screen.queryByRole("dialog")).toBeNull();
    });
  });
});
