import { vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { CardFile } from "@notrelix/work-management-core";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { AttachmentsSection } from "../attachments-section";

export const T = en.related.attachments;

export function fileOf(overrides: Partial<CardFile> = {}): CardFile {
  return {
    id: "file-1",
    cardId: "item-1",
    name: "bao-cao.pdf",
    size: 2048,
    contentType: "application/pdf",
    url: "",
    source: "upload",
    kind: "file",
    createdBy: { id: "u-1", name: "Ada" },
    createdAt: "2026-11-01T09:00:00Z",
    ...overrides,
  } as unknown as CardFile;
}

export function linkOf(url: string, name = "Spec"): CardFile {
  return fileOf({
    id: `link-${name}`,
    name,
    url,
    kind: "link",
    source: "link",
    contentType: "text/uri-list",
    size: 0,
  });
}

interface Options {
  readonly canEdit?: boolean;
  readonly mode?: "view" | "create";
  readonly data?: Record<string, unknown>;
  readonly callbacks?: Record<string, unknown>;
}

/** Renders the section over a hand-made popup model; `rerender` swaps its data. */
export function showSection(options: Options = {}) {
  const canEdit = options.canEdit ?? true;
  const callbacks = {
    onDeleteFile: vi.fn(async () => true),
    onDownloadFile: vi.fn(),
    onRetryUpload: vi.fn(),
    onDismissUpload: vi.fn(),
    onDismissFileNotice: vi.fn(),
    onRefreshFiles: vi.fn(),
    ...options.callbacks,
  };
  const openPicker = vi.fn();
  const build = (data: Record<string, unknown> | undefined) =>
    ({
      mode: options.mode ?? "view",
      copy: en,
      card: { id: "item-1", workspaceId: "ws-1" },
      readOnly: canEdit ? null : { reason: "viewer" },
      capabilities: { canEditFields: canEdit },
      timeZone: "UTC",
      openPicker,
      callbacks,
      data: {
        files: [],
        filesLoading: false,
        uploads: [],
        fileNotice: null,
        ...data,
      },
    }) as unknown as CardPopupModel;
  const tree = (data: Record<string, unknown> | undefined) => (
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={build(data)}>
        <AttachmentsSection />
      </CardPopupProvider>
    </I18nProvider>
  );
  const view = renderPureUi(tree(options.data));
  return {
    callbacks,
    openPicker,
    rerender: (data: Record<string, unknown>) => view.rerender(tree(data)),
  };
}

/** Opens a row's options menu with the pointer. */
export function openOptions(name: string) {
  const trigger = screen.getByRole("button", { name: `Options for ${name}` });
  const pointerDown = new MouseEvent("pointerdown", {
    bubbles: true,
    button: 0,
  });
  Object.defineProperty(pointerDown, "isPrimary", { value: true });
  act(() => {
    fireEvent(trigger, pointerDown);
  });
}
