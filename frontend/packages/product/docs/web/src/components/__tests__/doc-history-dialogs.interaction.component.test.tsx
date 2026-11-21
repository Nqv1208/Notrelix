import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { VERSIONS, docsCopy } from "../../verification/docs-ui-fixtures";
import {
  DocImportDialogSurface,
  DocRestoreVersionDialogSurface,
  DocSaveTemplateDialogSurface,
  DocSaveVersionDialogSurface,
} from "../doc-history-dialogs-surface";

const idle = {
  canRestore: true,
  saving: false,
  saveFailed: false,
  restoring: false,
  restoreFailed: null,
  conflictCount: 2,
} as const;

describe("docs history dialogs", () => {
  it("asks for a name before saving a version and then submits the trimmed name", () => {
    const onSaveVersion = vi.fn();
    renderPureUi(
      <DocSaveVersionDialogSurface
        copy={docsCopy}
        action={idle}
        callbacks={{ onSaveVersion }}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.versions.saveDialog.save }),
    );
    expect(onSaveVersion).not.toHaveBeenCalled();
    expect(screen.getByRole("alert").textContent).toBe(
      docsCopy.versions.saveDialog.required,
    );
    fireEvent.change(
      screen.getByLabelText(docsCopy.versions.saveDialog.label),
      { target: { value: "  Bản duyệt  " } },
    );
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.versions.saveDialog.save }),
    );
    expect(onSaveVersion).toHaveBeenCalledWith("Bản duyệt");
  });

  it("confirms a restore, and cannot repeat it after a conflict", () => {
    const onRestoreVersion = vi.fn();
    const view = renderPureUi(
      <DocRestoreVersionDialogSurface
        copy={docsCopy}
        version={VERSIONS[2]!}
        action={idle}
        callbacks={{ onRestoreVersion }}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: docsCopy.versions.restoreDialog.confirm,
      }),
    );
    expect(onRestoreVersion).toHaveBeenCalledTimes(1);
    view.unmount();

    renderPureUi(
      <DocRestoreVersionDialogSurface
        copy={docsCopy}
        version={VERSIONS[2]!}
        action={{ ...idle, restoreFailed: "conflict" }}
        callbacks={{ onRestoreVersion }}
      />,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      docsCopy.versions.restoreDialog.conflictTitle,
    );
    expect(
      screen.queryByRole("button", {
        name: docsCopy.versions.restoreDialog.confirm,
      }),
    ).toBeNull();
    expect(
      screen.getByRole("button", {
        name: docsCopy.versions.restoreDialog.review,
      }),
    ).toBeTruthy();
  });

  it("tells a viewer who can restore and keeps Restore off", () => {
    renderPureUi(
      <DocRestoreVersionDialogSurface
        copy={docsCopy}
        version={VERSIONS[2]!}
        action={{ ...idle, canRestore: false }}
      />,
    );
    expect(
      screen.getByText(docsCopy.versions.restoreDialog.denied),
    ).toBeTruthy();
    expect(
      screen
        .getByRole("button", { name: docsCopy.versions.restoreDialog.confirm })
        .hasAttribute("disabled"),
    ).toBe(true);
  });

  it("saves a template under a required name with an optional description", () => {
    const onSaveTemplate = vi.fn();
    renderPureUi(
      <DocSaveTemplateDialogSurface
        copy={docsCopy}
        action={{ saving: false, failed: false }}
        callbacks={{ onSaveTemplate }}
      />,
    );
    fireEvent.change(screen.getByLabelText(docsCopy.template.nameLabel), {
      target: { value: "Biên bản họp" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.template.save }),
    );
    expect(onSaveTemplate).toHaveBeenCalledWith("Biên bản họp", "");
  });

  it("names why an imported file was refused", () => {
    renderPureUi(
      <DocImportDialogSurface
        copy={docsCopy}
        state={{
          busy: false,
          error: "unsupported",
          fileName: "a.pdf",
          imagesDropped: 0,
        }}
      />,
    );
    expect(screen.getByRole("alert").textContent).toBe(
      docsCopy.importDialog.unsupported,
    );
  });
});
