import { ArrowDown, ArrowUp, SlidersHorizontal } from "lucide-react";
import {
  Checkbox,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Button,
} from "@notrelix/ui-web";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { faceEligibleFields } from "./face-fields";
import { fillCapabilityCopy } from "./capabilities.copy";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

export interface FaceFieldsSetting {
  cardFieldIds: string[];
  cardHideEmpty: string[];
}

/**
 * "Trường hiển thị trên thẻ": which fields the Kanban card face shows, in which order, and for each
 * whether it hides while empty, with a preview of the face. It edits the Kanban view's own setting, so
 * it needs edit rights on the board (`canEdit`); without them it only shows the current choice.
 * Reordering uses the up and down buttons, which also work from the keyboard.
 */
export function FaceFieldsPopover({
  fields,
  value,
  canEdit,
  onChange,
  trigger,
}: {
  fields: readonly FieldDefinition[];
  value: FaceFieldsSetting;
  canEdit: boolean;
  onChange: (next: FaceFieldsSetting) => void;
  trigger?: React.ReactNode;
}) {
  const copy = useCapabilitiesCopy();
  const eligible = faceEligibleFields(fields);
  const byId = new Map(eligible.map((field) => [field.id, field]));
  const shown = value.cardFieldIds
    .map((id) => byId.get(id))
    .filter((field): field is FieldDefinition => field !== undefined);
  const rest = eligible.filter(
    (field) => !value.cardFieldIds.includes(field.id),
  );

  const set = (ids: string[], hide = value.cardHideEmpty) =>
    onChange({
      cardFieldIds: ids,
      cardHideEmpty: hide.filter((id) => ids.includes(id)),
    });
  const move = (index: number, by: -1 | 1) => {
    const ids = shown.map((field) => field.id);
    const target = index + by;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target]!, ids[index]!];
    set(ids);
  };
  const toggleHide = (id: string, on: boolean) =>
    onChange({
      cardFieldIds: value.cardFieldIds,
      cardHideEmpty: on
        ? [...value.cardHideEmpty, id]
        : value.cardHideEmpty.filter((existing) => existing !== id),
    });

  return (
    <Popover>
      <PopoverTrigger asChild>
        {trigger ?? (
          <Button type="button" variant="ghost" size="sm">
            <SlidersHorizontal aria-hidden="true" />
            {copy.face.open}
          </Button>
        )}
      </PopoverTrigger>
      <PopoverContent align="end" className="flex w-88 flex-col gap-2 p-3">
        <h2 className="text-nav font-semibold">{copy.face.title}</h2>
        <p className="text-micro text-meta-foreground">{copy.face.note}</p>
        {!canEdit ? (
          <p className="text-micro text-meta-foreground">{copy.face.locked}</p>
        ) : null}
        <p className="text-micro font-semibold text-meta-foreground">
          {copy.face.shown}
        </p>
        {shown.length === 0 ? (
          <p className="text-micro text-meta-foreground">{copy.face.empty}</p>
        ) : (
          <ul className="flex flex-col gap-1">
            {shown.map((field, index) => (
              <li
                key={field.id}
                className="flex items-center gap-1.5 rounded-md border border-border px-2 py-1"
              >
                <span className="min-w-0 flex-1 truncate text-nav">
                  {field.name}
                </span>
                <label className="flex items-center gap-1 text-micro text-meta-foreground">
                  <Checkbox
                    disabled={!canEdit}
                    checked={value.cardHideEmpty.includes(field.id)}
                    onCheckedChange={(on) => toggleHide(field.id, on === true)}
                  />
                  {copy.face.hideEmpty}
                </label>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  disabled={!canEdit || index === 0}
                  aria-label={fillCapabilityCopy(copy.face.moveUp, {
                    name: field.name,
                  })}
                  onClick={() => move(index, -1)}
                >
                  <ArrowUp aria-hidden="true" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  disabled={!canEdit || index === shown.length - 1}
                  aria-label={fillCapabilityCopy(copy.face.moveDown, {
                    name: field.name,
                  })}
                  onClick={() => move(index, 1)}
                >
                  <ArrowDown aria-hidden="true" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={!canEdit}
                  aria-label={fillCapabilityCopy(copy.face.remove, {
                    name: field.name,
                  })}
                  onClick={() =>
                    set(value.cardFieldIds.filter((id) => id !== field.id))
                  }
                >
                  ×
                </Button>
              </li>
            ))}
          </ul>
        )}
        {rest.length > 0 ? (
          <>
            <p className="text-micro font-semibold text-meta-foreground">
              {copy.face.hidden}
            </p>
            <ul className="flex flex-col gap-1">
              {rest.map((field) => (
                <li key={field.id}>
                  <button
                    type="button"
                    disabled={!canEdit}
                    aria-label={fillCapabilityCopy(copy.face.show, {
                      name: field.name,
                    })}
                    onClick={() => set([...value.cardFieldIds, field.id])}
                    className="w-full rounded-md px-2 py-1 text-left text-nav text-muted-foreground hover:bg-state-hover disabled:opacity-60"
                  >
                    {field.name}
                  </button>
                </li>
              ))}
            </ul>
          </>
        ) : null}
        <p className="text-micro font-semibold text-meta-foreground">
          {copy.face.preview}
        </p>
        <div className="flex flex-col gap-1.5 rounded-[11px] bg-card p-3 shadow-card">
          <span className="text-nav">{copy.face.previewTitle}</span>
          <span className="flex flex-wrap gap-1">
            {shown.map((field) => (
              <span
                key={field.id}
                className="inline-flex h-5 items-center rounded-[5px] bg-state-hover-strong px-1.5 text-micro text-muted-foreground"
              >
                {field.name}
              </span>
            ))}
          </span>
        </div>
      </PopoverContent>
    </Popover>
  );
}
