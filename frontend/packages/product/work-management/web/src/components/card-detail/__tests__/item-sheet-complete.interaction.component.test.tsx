import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import type { CardFile } from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "../task-detail-panel-surface";
import { en, vi as viCopy, type ItemSheetCopy } from "../item-sheet.copy";
import { itemPageCopy } from "../../item-page/item-page.copy";
import {
  ERP_SHEET_NOW,
  ERP_SHEET_TIME_ZONE,
  commenterSheetCapabilities,
  editorSheetCapabilities,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetItem,
} from "../../../verification/item-sheet-fixtures";

type SurfaceProps = Parameters<typeof TaskDetailPanelSurface>[0];

const FILE = {
  id: "file-1",
  cardId: "card",
  name: "Bang-doi-chieu.xlsx",
  size: 84_000,
  contentType: "application/vnd.ms-excel",
  kind: "file",
  url: null,
  createdAt: "2026-10-28T08:00:00Z",
  createdBy: { id: "u1", userId: "u1", name: "Lan Anh", initials: "LA" },
} as unknown as CardFile;

function renderSheet(
  copy: ItemSheetCopy,
  locale: "vi" | "en",
  callbacks: Partial<SurfaceProps["callbacks"]> = {},
  data: Partial<NonNullable<SurfaceProps["detailData"]>> = {},
  capabilities: SurfaceProps["capabilities"] = editorSheetCapabilities,
) {
  const item = erpSheetItem();
  const props: SurfaceProps = {
    status: "ready",
    board: erpSheetBoard(),
    card: item,
    capabilities,
    detailData: erpSheetDetailData(item, {
      linksCopy: itemPageCopy(locale),
      ...data,
    }),
    callbacks: {
      onClose: vi.fn(),
      onRenameTitle: vi.fn(),
      onToggleWatch: vi.fn(),
      onDuplicate: vi.fn(),
      onArchive: vi.fn(),
      onUpdateFieldValue: vi.fn(),
      onRefreshActivity: vi.fn(),
      onCreateUpdate: vi.fn((_input, options) => {
        options?.onSuccess?.();
        options?.onSettled?.();
      }),
      onUpdateUpdate: vi.fn(),
      onDeleteUpdate: vi.fn(),
      onSelectTab: vi.fn(),
      ...callbacks,
    },
    copy,
    now: ERP_SHEET_NOW,
    timeZone: ERP_SHEET_TIME_ZONE,
  };
  renderPureUi(<TaskDetailPanelSurface {...props} />);
  return props;
}

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)(
  "the item side sheet does every everyday edit (%s)",
  (locale, copy) => {
    const page = itemPageCopy(locale);

    it(`folds an empty Files section into one add row and attaches picked files (${locale})`, () => {
      const onAddFile = vi.fn();
      renderSheet(copy, locale, { onAddFile }, { files: [] });
      expect(screen.queryByText(page.files.empty)).toBeNull();
      expect(
        screen.queryByRole("heading", { name: page.files.heading }),
      ).toBeNull();
      fireEvent.click(screen.getByRole("button", { name: page.files.add }));
      const input = screen.getByLabelText(page.files.choose, {
        selector: "input",
      });
      const file = new File(["x"], "a.txt", { type: "text/plain" });
      fireEvent.change(input, { target: { files: [file] } });
      expect(onAddFile).toHaveBeenCalledWith(file);
    });

    it(`attaches a file dropped on the add row and one pasted into the section (${locale})`, () => {
      const onAddFile = vi.fn();
      renderSheet(copy, locale, { onAddFile }, { files: [] });
      const file = new File(["x"], "drop.txt", { type: "text/plain" });
      const row = screen.getByRole("button", { name: page.files.add });
      fireEvent.drop(row, { dataTransfer: { files: [file] } });
      expect(onAddFile).toHaveBeenCalledWith(file);
      const pasted = new File(["y"], "paste.png", { type: "image/png" });
      fireEvent.paste(row, { clipboardData: { files: [pasted] } });
      expect(onAddFile).toHaveBeenCalledWith(pasted);
    });

    it(`deletes a file only after asking, and keeps the row with a retry when it fails (${locale})`, async () => {
      const onDeleteFile = vi
        .fn<(file: unknown) => Promise<boolean>>()
        .mockResolvedValueOnce(false)
        .mockResolvedValueOnce(true);
      renderSheet(
        copy,
        locale,
        { onAddFile: vi.fn(), onDeleteFile },
        { files: [FILE] },
      );
      fireEvent.click(
        screen.getByRole("button", {
          name: page.files.delete.replace("{{name}}", FILE.name),
        }),
      );
      expect(onDeleteFile).not.toHaveBeenCalled();
      fireEvent.click(
        screen.getByRole("button", { name: page.files.deleteConfirm }),
      );
      const alert = await screen.findByRole("alert");
      expect(alert.textContent).toContain(page.files.deleteFailed);
      fireEvent.click(screen.getByRole("button", { name: page.files.retry }));
      await waitFor(() => expect(onDeleteFile).toHaveBeenCalledTimes(2));
      await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
    });

    it(`lets a reader open and download files but not add or delete them (${locale})`, () => {
      renderSheet(
        copy,
        locale,
        { onAddFile: vi.fn(), onDownloadFile: vi.fn(), onDeleteFile: vi.fn() },
        { files: [FILE] },
        commenterSheetCapabilities,
      );
      expect(
        screen.getByRole("button", {
          name: page.files.download.replace("{{name}}", FILE.name),
        }),
      ).toBeTruthy();
      expect(
        screen.queryByRole("button", {
          name: page.files.delete.replace("{{name}}", FILE.name),
        }),
      ).toBeNull();
      expect(screen.queryByRole("button", { name: page.files.add })).toBeNull();
    });

    it(`mentions the people who can view the board from the composer (${locale})`, async () => {
      const onCreateUpdate = vi.fn((_input, options) => {
        options?.onSuccess?.();
        options?.onSettled?.();
      });
      renderSheet(
        copy,
        locale,
        { onCreateUpdate },
        {
          people: {
            people: [
              {
                userId: "u-lan",
                name: "Lan Anh",
                department: null,
                openCount: null,
                onBoard: true,
              },
              {
                userId: "u-out",
                name: "Lan Ngoài Board",
                department: null,
                openCount: null,
                onBoard: false,
              },
            ],
            canInvite: false,
          },
        },
      );
      const box = screen.getByRole("textbox", { name: copy.composer.label });
      fireEvent.change(box, { target: { value: "@Lan", selectionStart: 4 } });
      const list = await screen.findByRole("listbox");
      // Someone who cannot view the board is never suggested (decision 42).
      const options = within(list).getAllByRole("option");
      expect(options).toHaveLength(1);
      expect(options[0]?.textContent).toContain("Lan Anh");
      fireEvent.click(options[0]!);
      fireEvent.click(
        screen.getByRole("button", { name: copy.composer.submit }),
      );
      expect(onCreateUpdate).toHaveBeenCalledWith(
        expect.objectContaining({
          body: "@Lan Anh",
          mentionUserIds: ["u-lan"],
        }),
        expect.anything(),
      );
    });

    it(`creates a label with a name and a colour from an empty board (${locale})`, async () => {
      const onCreateLabel = vi.fn().mockResolvedValue(true);
      renderSheet(
        copy,
        locale,
        { onCreateLabel, onToggleLabel: vi.fn() },
        { boardLabels: [] },
      );
      fireEvent.click(
        screen.getByRole("button", {
          name: new RegExp(copy.properties.labels),
        }),
      );
      fireEvent.click(
        await screen.findByRole("button", { name: copy.properties.newLabel }),
      );
      fireEvent.change(
        screen.getByRole("textbox", { name: copy.properties.labelName }),
        { target: { value: "Hạ tầng" } },
      );
      fireEvent.click(
        screen.getByRole("radio", { name: copy.properties.labelColors.red }),
      );
      fireEvent.click(
        screen.getByRole("button", { name: copy.properties.createLabelAction }),
      );
      await waitFor(() =>
        expect(onCreateLabel).toHaveBeenCalledWith("Hạ tầng", "#D6453D"),
      );
    });

    it(`links the label picker to Board settings > Labels (${locale})`, async () => {
      renderSheet(
        copy,
        locale,
        { onCreateLabel: vi.fn(), onToggleLabel: vi.fn() },
        { boardLabels: [] },
      );
      fireEvent.click(
        screen.getByRole("button", {
          name: new RegExp(copy.properties.labels),
        }),
      );
      const link = await screen.findByRole("link", {
        name: copy.properties.manageLabels,
      });
      expect(link.getAttribute("href")).toMatch(
        /\/boards\/[^/]+\/settings\?section=labels$/,
      );
    });

    it(`creates a custom property in place and opens its value control (${locale})`, async () => {
      const onCreateField = vi.fn().mockResolvedValue("field-new");
      renderSheet(copy, locale, { onCreateField });
      fireEvent.click(
        screen.getByRole("button", { name: copy.properties.addProperty }),
      );
      fireEvent.click(
        await screen.findByRole("button", {
          name: copy.properties.newProperty,
        }),
      );
      fireEvent.change(
        screen.getByRole("textbox", { name: copy.properties.propertyName }),
        { target: { value: "Nhà cung cấp" } },
      );
      fireEvent.change(
        screen.getByRole("combobox", { name: copy.properties.propertyType }),
        { target: { value: "select" } },
      );
      const first = screen.getByRole("textbox", {
        name: copy.properties.propertyOption.replace("{{index}}", "1"),
      });
      fireEvent.change(first, { target: { value: "Bên A" } });
      fireEvent.click(
        screen.getByRole("button", { name: copy.properties.createProperty }),
      );
      await waitFor(() =>
        expect(onCreateField).toHaveBeenCalledWith({
          name: "Nhà cung cấp",
          fieldType: "select",
          options: [{ name: "Bên A", color: expect.any(String) }],
        }),
      );
    });

    it(`shows no "add property" to a reader (${locale})`, () => {
      renderSheet(
        copy,
        locale,
        { onCreateField: vi.fn() },
        {},
        commenterSheetCapabilities,
      );
      expect(
        screen.queryByRole("button", { name: copy.properties.addProperty }),
      ).toBeNull();
    });
  },
);
