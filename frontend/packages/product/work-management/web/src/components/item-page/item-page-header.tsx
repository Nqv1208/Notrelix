import { AlertTriangle, Lock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { CardDetail } from "@notrelix/work-management-core";
import {
  ITEM_LABEL_TONE_CLASS,
  formatItemDate,
  itemLabelTone,
} from "../shared/item-display";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import { TaskDetailTitle } from "../card-detail/task-detail-header";
import type { ItemPageCopy } from "./item-page.copy";
import { formatEditedAt } from "./item-page-format";
import { BUTTON_SM_VARIANT_CLASS, DisabledControl } from "./item-page-parts";
import type { ItemPageAccess, ItemPageConflict } from "./item-page-types";
import type { ItemSheetCopy } from "../card-detail/item-sheet.copy";
import type { TaskDetailCallbacks } from "../card-detail/task-detail-types";

/** Label, key, who created it and when it was last edited, above the title. */
export function ItemPageTitleBlock({
  card,
  creatorName,
  editedAt,
  editable,
  titleId,
  copy,
  sheetCopy,
  now,
  timeZone,
  onRenameTitle,
  muted,
}: {
  /** An archived item: the title is drawn in the muted ink. */
  muted?: boolean;
  card: CardDetail;
  creatorName?: string | null;
  /** The last change, history included; the item's own update time without it. */
  editedAt?: string | null;
  editable: boolean;
  titleId: string;
  copy: ItemPageCopy;
  sheetCopy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  onRenameTitle: TaskDetailCallbacks["onRenameTitle"];
}) {
  const label = card.labels[0];
  const created = formatItemDate(card.createdAt, now, timeZone);
  const edited = formatEditedAt(
    editedAt ?? card.updatedAt,
    now,
    copy,
    timeZone,
  );
  const createdText = created
    ? creatorName
      ? fillSheetCopy(copy.meta.createdBy, { name: creatorName, date: created })
      : fillSheetCopy(copy.meta.created, { date: created })
    : null;
  const parts = [
    createdText,
    edited ? fillSheetCopy(copy.meta.edited, { when: edited }) : null,
  ].filter((part): part is string => Boolean(part));

  return (
    <header className="flex flex-col gap-2.5">
      <div className="flex flex-wrap items-center gap-2 text-crumb text-meta-foreground">
        {label ? (
          <span
            className={cn(
              "inline-flex h-5.5 items-center rounded-[6px] px-2 text-micro font-semibold",
              ITEM_LABEL_TONE_CLASS[itemLabelTone(label)],
            )}
          >
            {label.name}
          </span>
        ) : null}
        {card.itemKey ? (
          <span className="font-mono font-medium text-muted-foreground">
            {card.itemKey}
          </span>
        ) : null}
        {parts.map((part, index) => (
          <span key={part} className="contents">
            {index > 0 || label || card.itemKey ? (
              <span aria-hidden="true">·</span>
            ) : null}
            {part}
          </span>
        ))}
      </div>
      <TaskDetailTitle
        key={card.id}
        as="h1"
        className={cn("text-item-title", muted && "text-muted-foreground")}
        card={card}
        titleId={titleId}
        editable={editable}
        copy={sheetCopy}
        onRenameTitle={onRenameTitle}
      />
    </header>
  );
}

/**
 * A write the server refused because the stored value moved on: both values
 * are kept until the viewer picks one. Nothing was overwritten.
 */
export function ItemPageConflictNotice({
  conflict,
  copy,
  onResolve,
  onViewChanges,
}: {
  conflict: ItemPageConflict;
  copy: ItemPageCopy;
  onResolve?: (choice: "theirs" | "mine") => void;
  onViewChanges?: () => void;
}) {
  const t = copy.conflict;
  return (
    <div
      role="alert"
      className="flex items-start gap-3 rounded-[10px] border border-item-conflict-line bg-warning-subtle px-3.5 py-3 text-item-ui leading-normal text-foreground"
    >
      <AlertTriangle
        aria-hidden="true"
        className="mt-0.5 size-4 shrink-0 text-warning-text"
      />
      <div>
        <b className="font-semibold">
          {fillSheetCopy(t.title, { field: conflict.fieldName })}
        </b>{" "}
        {fillSheetCopy(t.body, {
          mine: conflict.mine,
          theirs: conflict.theirs,
        })}
        <div className="mt-2.5 flex flex-wrap gap-1.5">
          <button
            type="button"
            className={BUTTON_SM_VARIANT_CLASS.primary}
            onClick={() => onResolve?.("theirs")}
          >
            {fillSheetCopy(t.takeTheirs, { value: conflict.theirs })}
          </button>
          <button
            type="button"
            className={BUTTON_SM_VARIANT_CLASS.outline}
            onClick={() => onResolve?.("mine")}
          >
            {fillSheetCopy(t.keepMine, { value: conflict.mine })}
          </button>
          {onViewChanges ? (
            <button
              type="button"
              className={BUTTON_SM_VARIANT_CLASS.ghost}
              onClick={onViewChanges}
            >
              {t.viewChanges}
            </button>
          ) : null}
        </div>
      </div>
    </div>
  );
}

/** What a viewer without edit access can and cannot do on the item. */
export function ItemPageReadOnlyNotice({
  access,
  copy,
}: {
  access?: ItemPageAccess | null;
  copy: ItemPageCopy;
}) {
  const t = copy.readOnly;
  const level = access ? t.levels[access.level] : t.levels.viewer;
  const source = access?.grantedBy
    ? fillSheetCopy(t.source, { name: access.grantedBy })
    : "";
  return (
    <div
      role="status"
      className="flex items-start gap-3 rounded-[10px] border border-line-strong bg-popover px-3.5 py-3 text-item-ui leading-normal text-foreground"
    >
      <Lock
        aria-hidden="true"
        className="mt-0.5 size-4 shrink-0 text-meta-foreground"
      />
      <div>
        <b className="font-semibold">
          {fillSheetCopy(t.body, { level, source })}
        </b>
        {t.detail}{" "}
        <DisabledControl reason={t.requestEditUnavailable}>
          <button
            type="button"
            aria-disabled="true"
            className="border-0 bg-transparent p-0 font-medium text-brand-subtle-foreground"
          >
            {t.requestEdit}
          </button>
        </DisabledControl>
      </div>
    </div>
  );
}
