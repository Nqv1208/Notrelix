import { createContext, useContext, type ReactNode } from "react";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import type {
  ItemSheetGroup,
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "../card-detail/task-detail-types";
import type { findItemFields } from "../card-detail/item-field-editors";
import type { CardPopupCopy } from "./card-popup.copy";

export type CardPopupMode = "view" | "create";

/** Why every write is off: a viewer without the right, or an archived board. */
export type CardPopupReadOnly =
  | "permission"
  | "archived"
  /** A create is on its way: every field is read-only until it answers. */
  | "creating"
  | null;

/** The state of one field's save: the chip row and the sections show it in place. */
export type FieldSave =
  | { readonly status: "saving" }
  | { readonly status: "error"; readonly retry: () => void };

/** The pickers the quick row and the "+ Thêm" menu open. */
export type CardPopupPicker =
  "labels"
  | "dates"
  | "checklist"
  | "assignees"
  | "attach"
  | "link"
  | "subitem"
  | "fields"
  | "add";

/**
 * Mount slots for capabilities another part of the product fills in: each is
 * rendered where the artboard puts it and is absent until a capability plugs in.
 */
export interface CardPopupSlots {
  /** The cover control of the header (the picture button and its panel). */
  readonly cover?: ReactNode;
  /** The banner image above the title row. */
  readonly coverBanner?: ReactNode;
  /** "Lặp lại" row of the dates panel. */
  readonly repeat?: ReactNode;
  /** "Nhắc trước hạn" row of the dates panel. */
  readonly reminder?: ReactNode;
  /** Extra chips of the chip row (vote, viewers). */
  readonly chips?: ReactNode;
  /** Colour-blind marks of the label picker. */
  readonly labelMarks?: ReactNode;
  /** Extra entries of the checklist menu ("Chép mục từ checklist khác"). */
  readonly checklistExtras?: ReactNode;
  /** Extra entries of the links section ("Gỡ tất cả liên kết"). */
  readonly linksExtras?: ReactNode;
}

/** Writes the popup needs beyond the sheet's callbacks; absent: the control is off. */
export interface CardPopupActions {
  /** Moves the card to another column of the board (MoveBoardItem). */
  readonly moveToGroup?: (groupId: string) => void;
  /** Renames or recolours a board label (UpdateLabel); resolves false when refused. */
  readonly updateLabel?: (
    labelId: string,
    patch: { readonly name?: string; readonly color?: string },
  ) => Promise<boolean>;
  /** Deletes a board label (it leaves every card); resolves false when refused. */
  readonly removeLabel?: (labelId: string) => Promise<boolean>;
  /** Attaches a web link (CreateBoardItemAttachment with a URL). */
  readonly addWebLink?: (url: string, name: string) => Promise<boolean>;
  /** Links a Documents page to the card (LinkPageToBoardItem). */
  readonly linkPage?: (pageId: string) => Promise<boolean>;
}

export interface CardPopupModel {
  readonly mode: CardPopupMode;
  readonly board: Board;
  /** The card; in create mode the draft shaped as a card. */
  readonly card: CardDetail;
  readonly capabilities: TaskDetailCapabilities;
  readonly readOnly: CardPopupReadOnly;
  readonly data: TaskDetailData;
  readonly callbacks: TaskDetailCallbacks;
  readonly actions: CardPopupActions;
  readonly groups: readonly ItemSheetGroup[];
  readonly fields: ReturnType<typeof findItemFields>;
  readonly copy: CardPopupCopy;
  readonly now: Date;
  readonly timeZone?: string;
  readonly slots: CardPopupSlots;
  /** Per-field save state by field key ("title", "description", a field id). */
  readonly saves: Readonly<Record<string, FieldSave | undefined>>;
  /** Runs a write and tracks it under `key`; a failure keeps `retry`. */
  readonly track: (key: string, run: () => Promise<unknown>) => void;
  /** A write was refused because someone else saved first. */
  readonly conflict: boolean;
  readonly clearConflict: () => void;
  /** The open picker; null: none. */
  readonly picker: CardPopupPicker | null;
  /** The control that opened the picker; the panel opens under it. Null: the quick row's own button. */
  readonly pickerAnchor: HTMLElement | null;
  /**
   * Opens a picker (null closes it). `anchor` is the control the panel opens under: pass the element that was
   * clicked, `null` for the quick row's own buttons; left out, the earlier anchor stays (a menu entry).
   */
  readonly openPicker: (
    picker: CardPopupPicker | null,
    anchor?: HTMLElement | null,
  ) => void;
  /** Board fields the "+ Thêm" menu added to this card's chip row, in the order added. */
  readonly addedFieldIds: readonly string[];
  /** Adds the field's chip to the chip row and asks its editor to open. */
  readonly addField: (fieldId: string) => void;
  /** The field whose editor is asked to open; its control clears it once open. */
  readonly openFieldId: string | null;
  readonly clearOpenField: () => void;
  /** The name typed in the menu for a field that does not exist yet; the new-field form starts with it. */
  readonly newFieldName: string | null;
  /** Opens the new-field form of the fields section, named `name`; null forgets the name. */
  readonly startNewField: (name: string | null) => void;
}

const CardPopupContext = createContext<CardPopupModel | null>(null);

export const CardPopupProvider = CardPopupContext.Provider;

/** The popup model; every part of the popup reads its card, words and writes here. */
export function useCardPopup(): CardPopupModel {
  const model = useContext(CardPopupContext);
  if (!model) throw new Error("useCardPopup needs a CardPopupProvider");
  return model;
}

/** Whether a property may be edited right now. */
export function useCanEdit(): boolean {
  const { readOnly, capabilities } = useCardPopup();
  return readOnly === null && capabilities.canEditFields;
}
