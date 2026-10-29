import { ChevronDown, Layers, Rows3 } from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import {
  DATE_BUCKETS,
  type DateBucket,
  type FieldRef,
  type FilterField,
} from "@notrelix/work-management-core";
import { kanbanPillClass } from "../views/kanban/kanban-chrome";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";
import { activePillClass } from "./filter-builder";

export interface FieldRefMenuProps {
  copy: FiltersCopy;
  /** "group": the Table's and Kanban's "Nhóm"; "lane": the Kanban's swimlane "Làn". */
  kind: "group" | "lane";
  /** The fields that can be chosen (the registry's groupable fields). */
  fields: readonly FilterField[];
  value: FieldRef | undefined;
  onChange: (value: FieldRef | undefined) => void;
  /** A field that cannot be picked here and why ("đang là cột" for the Kanban column field). */
  blockedFieldId?: string | undefined;
}

const typeGroup = (
  field: FilterField,
): "select" | "person" | "date" | "checkbox" =>
  field.field.fieldType === "person"
    ? "person"
    : field.field.fieldType === "date"
      ? "date"
      : field.field.fieldType === "checkbox"
        ? "checkbox"
        : "select";

/**
 * The "Nhóm" and "Làn" pills: a radio list of the fields the registry says can
 * be grouped (Select, Person, Date, Checkbox), a date field adding its bucket
 * (day, week, month, quarter, year). Choosing writes the member's own view only.
 */
export function FieldRefMenu({
  copy,
  kind,
  fields,
  value,
  onChange,
  blockedFieldId,
}: FieldRefMenuProps) {
  const t = kind === "group" ? copy.group : copy.lane;
  const current = fields.find((field) => field.id === value?.fieldId);
  const Icon = kind === "group" ? Layers : Rows3;
  const sections = (["select", "person", "date", "checkbox"] as const).flatMap(
    (group) => {
      const inGroup = fields.filter((field) => typeGroup(field) === group);
      return inGroup.length > 0 ? [[group, inGroup] as const] : [];
    },
  );
  const bucket = value?.bucket ?? "week";
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={cn(kanbanPillClass, current && activePillClass)}
        >
          <Icon className="size-4" aria-hidden="true" />
          {t.trigger}
          {current ? (
            <>
              {": "}
              <b className="max-w-40 truncate font-semibold">{current.name}</b>
            </>
          ) : null}
          <ChevronDown className="size-3.5" aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-72">
        <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
          {t.label}
        </DropdownMenuLabel>
        <DropdownMenuRadioGroup
          value={value?.fieldId ?? ""}
          onValueChange={(fieldId) => {
            if (fieldId === "") return onChange(undefined);
            const picked = fields.find((field) => field.id === fieldId);
            if (!picked) return;
            onChange(
              picked.field.fieldType === "date"
                ? { fieldId, bucket }
                : { fieldId },
            );
          }}
        >
          <DropdownMenuRadioItem value="">{t.none}</DropdownMenuRadioItem>
          {sections.map(([group, inGroup]) => (
            <div key={group}>
              <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
                {copy.group[group]}
              </DropdownMenuLabel>
              {inGroup.map((field) => {
                const blocked = field.id === blockedFieldId;
                return (
                  <DropdownMenuRadioItem
                    key={field.id}
                    value={field.id}
                    disabled={blocked}
                    onSelect={(event) => {
                      if (field.field.fieldType === "date")
                        event.preventDefault();
                    }}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {field.name}
                    </span>
                    {blocked && "sameAsColumn" in copy.lane ? (
                      <span className="text-xs text-muted-foreground">
                        {copy.lane.sameAsColumn}
                      </span>
                    ) : null}
                  </DropdownMenuRadioItem>
                );
              })}
            </div>
          ))}
        </DropdownMenuRadioGroup>
        {current?.field.fieldType === "date" ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
              {fillFiltersCopy(copy.group.bucketTitle, { field: current.name })}
            </DropdownMenuLabel>
            <DropdownMenuRadioGroup
              value={bucket}
              onValueChange={(next) =>
                onChange({
                  fieldId: current.id,
                  bucket: DATE_BUCKETS.find((b) => b === next) ?? "week",
                })
              }
            >
              {DATE_BUCKETS.map((option: DateBucket) => (
                <DropdownMenuRadioItem key={option} value={option}>
                  <span className="flex-1">{copy.group.bucket[option]}</span>
                  <span className="text-xs text-muted-foreground">
                    {copy.group.bucketHint[option]}
                  </span>
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
          </>
        ) : null}
        {kind === "lane" ? (
          <p className="px-2 py-1.5 text-xs text-muted-foreground">
            {copy.lane.note}
          </p>
        ) : (
          <p className="px-2 py-1.5 text-xs text-muted-foreground">
            {copy.group.unavailable}: {copy.group.unavailableReason}
          </p>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
