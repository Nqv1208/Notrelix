import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocTemplatesDialogSurface } from "../doc-templates-dialog-surface";
import type { DocTemplatesView } from "../doc-page-models";

const d = docsCopy.templates;

const ready: DocTemplatesView = {
  status: "ready",
  templates: [
    {
      id: "t1",
      name: "Biên bản họp tuần",
      description: "Mỗi sáng thứ Hai",
      blockCount: 4,
      canManage: true,
    },
    {
      id: "t2",
      name: "Kế hoạch quý",
      description: null,
      blockCount: 9,
      canManage: false,
    },
  ],
  busyId: null,
  failure: null,
};

describe("DocTemplatesDialogSurface", () => {
  it("says it is loading, shows the failure with a retry, and the empty state", () => {
    const onTemplatesRetry = vi.fn();
    const view = renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={{ ...ready, status: "loading", templates: [] }}
      />,
    );
    expect(screen.getByRole("status").textContent).toBe(d.loading);
    view.unmount();

    const failed = renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={{ ...ready, status: "error", templates: [] }}
        callbacks={{ onTemplatesRetry }}
      />,
    );
    expect(screen.getByRole("alert").textContent).toContain(d.loadFailed);
    fireEvent.click(screen.getByRole("button", { name: d.retry }));
    expect(onTemplatesRetry).toHaveBeenCalledTimes(1);
    failed.unmount();

    renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={{ ...ready, templates: [] }}
      />,
    );
    expect(screen.getByText(d.emptyTitle)).toBeTruthy();
    expect(screen.getByText(d.emptyBody)).toBeTruthy();
  });

  it("offers rename, archive and delete only on a template the caller manages", () => {
    renderPureUi(<DocTemplatesDialogSurface copy={docsCopy} view={ready} />);
    expect(
      screen.getByRole("button", {
        name: d.renameLabel.replace("{{name}}", "Biên bản họp tuần"),
      }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", {
        name: d.renameLabel.replace("{{name}}", "Kế hoạch quý"),
      }),
    ).toBeNull();
    expect(
      screen.queryAllByRole("button", { name: /^Lưu trữ mẫu Kế/ }),
    ).toHaveLength(0);
  });

  it("renames in place with a trimmed name and refuses an empty one", () => {
    const onTemplateRename = vi.fn();
    renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={ready}
        callbacks={{ onTemplateRename }}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: d.renameLabel.replace("{{name}}", "Biên bản họp tuần"),
      }),
    );
    const name = screen.getByLabelText(d.nameLabel);
    expect((name as HTMLInputElement).value).toBe("Biên bản họp tuần");
    fireEvent.change(name, { target: { value: "   " } });
    fireEvent.click(screen.getByRole("button", { name: d.save }));
    expect(onTemplateRename).not.toHaveBeenCalled();
    expect(screen.getByRole("alert").textContent).toBe(d.required);

    fireEvent.change(name, { target: { value: "  Họp nhóm  " } });
    fireEvent.click(screen.getByRole("button", { name: d.save }));
    expect(onTemplateRename).toHaveBeenCalledWith(
      "t1",
      "Họp nhóm",
      "Mỗi sáng thứ Hai",
    );
  });

  it("asks before it deletes or archives, and cancel changes nothing", () => {
    const onTemplateDelete = vi.fn();
    const onTemplateArchive = vi.fn();
    renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={ready}
        callbacks={{ onTemplateDelete, onTemplateArchive }}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: d.deleteLabel.replace("{{name}}", "Biên bản họp tuần"),
      }),
    );
    expect(onTemplateDelete).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: d.cancel }));
    expect(onTemplateDelete).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", {
        name: d.deleteLabel.replace("{{name}}", "Biên bản họp tuần"),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: d.deleteConfirm }));
    expect(onTemplateDelete).toHaveBeenCalledWith("t1");

    fireEvent.click(
      screen.getByRole("button", {
        name: d.archiveLabel.replace("{{name}}", "Biên bản họp tuần"),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: d.archiveConfirm }));
    expect(onTemplateArchive).toHaveBeenCalledWith("t1");
  });

  it("words a refusal on the row it came from", () => {
    const view = renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={{ ...ready, failure: { id: "t1", kind: "conflict" } }}
      />,
    );
    expect(screen.getByRole("alert").textContent).toBe(d.conflict);
    view.unmount();

    renderPureUi(
      <DocTemplatesDialogSurface
        copy={docsCopy}
        view={{ ...ready, failure: { id: "t1", kind: "forbidden" } }}
      />,
    );
    expect(screen.getByRole("alert").textContent).toBe(d.forbidden);
  });
});
