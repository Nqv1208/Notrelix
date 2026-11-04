import { Check, ChevronDown, Layers, Rows3 } from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
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
import { bucketLabel } from "./condition-label";
import { fieldTypeKind } from "./field-type-meta";

export interface FieldRefMenuProps {
  copy: FiltersCopy;
  /** "group": the Table's and Kanban's "Nhóm"; "lane": the Kanban's swimlane "Làn". */
  kind: "group" | "lane";
  /** The fields that can be chosen (the registry's groupable fields). */
  fields: readonly FilterField[];
  value: FieldRef | undefined;
  onChange: (value: FieldRef | undefined) => void;
  /** What shows while the member has not chosen (the view's own default, like Status for a Kanban); not styled as their choice. */
  fallback?: FieldRef | undefined;
  /** A field that cannot be picked here and why ("đang là cột" for the Kanban column field). */
  blockedFieldId?: string | undefined;
  /** The fields this menu cannot offer, named under "Chưa nhóm được" (the group menu only). */
  unavailableFields?: readonly FilterField[] | undefined;
  /** Under the date buckets: which zone the days are judged in. */
  timeZoneNote?: string | undefined;
}

const SECTIONS = ["select", "person", "date", "checkbox"] as const;

const rowClass =
  "min-h-9 gap-2.5 rounded-lg px-2.5 py-1 pr-2.5 text-[13.5px] leading-[1.45] [&>[data-slot=dropdown-menu-radio-item-indicator]]:hidden";
const headingClass =
  "px-2.5 pb-1 pt-2 text-xs font-semibold leading-[1.45] text-muted-foreground";
const hintClass =
  "ml-auto whitespace-nowrap text-xs leading-[1.45] text-muted-foreground";

const pad = (n: number) => String(n).padStart(2, "0");

/** Today in each bucket, as the sample beside the bucket's name ("Thứ Hai 12/10/2026", "12/10 – 18/10/2026"). */
function bucketSample(copy: FiltersCopy, bucket: DateBucket): string {
  const now = new Date();
  const key = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
  if (bucket === "day") {
    const weekday = new Intl.DateTimeFormat(copy.group.locale, {
      weekday: "long",
    }).format(now);
    return `${weekday[0]!.toUpperCase()}${weekday.slice(1)} ${pad(now.getDate())}/${pad(now.getMonth() + 1)}/${now.getFullYear()}`;
  }
  const start = new Date(now);
  start.setDate(now.getDate() - ((now.getDay() + 6) % 7));
  const weekKey = `${start.getFullYear()}-${pad(start.getMonth() + 1)}-${pad(start.getDate())}`;
  const label = bucketLabel(
    copy,
    bucket === "week"
      ? weekKey
      : bucket === "month"
        ? key.slice(0, 7)
        : bucket === "quarter"
          ? `${now.getFullYear()}-Q${Math.floor(now.getMonth() / 3) + 1}`
          : String(now.getFullYear()),
    bucket,
  );
  // The week reads "Tuần 12/10 – 18/10/2026"; the sample drops the word.
  return bucket === "week" ? label.slice(label.indexOf(" ") + 1) : label;
}

/**
 * The "Nhóm" and "Làn" pills: a menu of the fields the registry says can be
 * grouped (Select, Person, Date, Checkbox) under their type, a date field
 * opening its own bucket menu (day, week, month, quarter, year) beside it, and
 * the fields that cannot be grouped named at the end. Choosing writes the
 * member's own view only.
 */
export function FieldRefMenu({
  copy,
  kind,
  fields,
  value,
  fallback,
  onChange,
  blockedFieldId,
  unavailableFields,
  timeZoneNote,
}: FieldRefMenuProps) {
  const t = kind === "group" ? copy.group : copy.lane;
  const shown = value ?? fallback;
  const current = fields.find((field) => field.id === shown?.fieldId);
  const Icon = kind === "group" ? Layers : Rows3;
  const sections = SECTIONS.flatMap((group) => {
    const inGroup = fields.filter((field) => fieldTypeKind(field) === group);
    return inGroup.length > 0 ? [[group, inGroup] as const] : [];
  });
  const bucket = shown?.bucket ?? "week";
  const tick = (on: boolean) => (
    <span className="grid w-4 shrink-0 place-items-center text-state-selected-foreground">
      {on ? <Check className="size-4" aria-hidden="true" /> : null}
    </span>
  );
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={cn(kanbanPillClass, value && current && activePillClass)}
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
      <DropdownMenuContent
        align="start"
        aria-label={t.label}
        className="flex w-[min(19.375rem,calc(100vw-1.5rem))] flex-col gap-0.5 rounded-[12px] border border-line p-1.5 ring-0"
      >
        <DropdownMenuRadioGroup
          className="flex flex-col gap-0.5"
          value={shown?.fieldId ?? ""}
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
          <DropdownMenuRadioItem value="" className={rowClass}>
            {tick(!shown)}
            <span>{t.none}</span>
          </DropdownMenuRadioItem>
          {sections.map(([group, inGroup]) => (
            <div key={group} className="flex flex-col gap-0.5">
              <DropdownMenuLabel className={headingClass}>
                {copy.group[group]}
              </DropdownMenuLabel>
              {inGroup.map((field) => {
                const blocked = field.id === blockedFieldId;
                const on = field.id === shown?.fieldId;
                if (group === "date" && !blocked) {
                  return (
                    <DropdownMenuSub key={field.id}>
                      <DropdownMenuSubTrigger
                        className={cn(
                          rowClass,
                          "[&>svg:last-child]:hidden",
                          on && "bg-state-selected",
                        )}
                      >
                        {tick(on)}
                        <span className="min-w-0 flex-1 truncate">
                          {field.name}
                        </span>
                        <span className={hintClass}>
                          {on
                            ? `${copy.group.date} · ${copy.group.bucket[bucket].toLowerCase()}`
                            : copy.group.date}
                        </span>
                      </DropdownMenuSubTrigger>
                      <DropdownMenuSubContent
                        aria-label={fillFiltersCopy(copy.group.bucketTitle, {
                          field: field.name,
                        })}
                        className="flex w-[15.625rem] flex-col gap-0.5 rounded-[12px] border border-line p-1.5 ring-0"
                      >
                        <DropdownMenuLabel className={headingClass}>
                          {fillFiltersCopy(copy.group.bucketTitle, {
                            field: field.name,
                          })}
                        </DropdownMenuLabel>
                        <DropdownMenuRadioGroup
                          className="flex flex-col gap-0.5"
                          value={on ? bucket : ""}
                          onValueChange={(next) =>
                            onChange({
                              fieldId: field.id,
                              bucket:
                                DATE_BUCKETS.find((b) => b === next) ?? "week",
                            })
                          }
                        >
                          {DATE_BUCKETS.map((option: DateBucket) => (
                            <DropdownMenuRadioItem
                              key={option}
                              value={option}
                              className={cn(
                                rowClass,
                                on && option === bucket && "bg-state-selected",
                              )}
                            >
                              {tick(on && option === bucket)}
                              <span>{copy.group.bucket[option]}</span>
                              <span className={hintClass}>
                                {bucketSample(copy, option)}
                              </span>
                            </DropdownMenuRadioItem>
                          ))}
                        </DropdownMenuRadioGroup>
                        <DropdownMenuSeparator className="mx-0 my-1 bg-line" />
                        <p className="px-2.5 py-1 text-xs leading-[1.45] text-muted-foreground">
                          {copy.group.noDueNote}
                        </p>
                        {timeZoneNote ? (
                          <p
                            data-f="tz-note"
                            className="px-2.5 py-1 text-xs leading-[1.45] text-muted-foreground"
                          >
                            {timeZoneNote}
                          </p>
                        ) : null}
                      </DropdownMenuSubContent>
                    </DropdownMenuSub>
                  );
                }
                return (
                  <DropdownMenuRadioItem
                    key={field.id}
                    value={field.id}
                    disabled={blocked}
                    className={cn(rowClass, on && "bg-state-selected")}
                  >
                    {tick(on)}
                    <span className="min-w-0 flex-1 truncate">
                      {field.name}
                    </span>
                    <span className={hintClass}>
                      {blocked && "sameAsColumn" in copy.lane
                        ? copy.lane.sameAsColumn
                        : copy.group[group]}
                    </span>
                  </DropdownMenuRadioItem>
                );
              })}
            </div>
          ))}
        </DropdownMenuRadioGroup>
        {kind === "lane" ? (
          <p className="px-2.5 py-1.5 text-xs leading-[1.45] text-muted-foreground">
            {copy.lane.note}
          </p>
        ) : unavailableFields && unavailableFields.length > 0 ? (
          <>
            <DropdownMenuSeparator className="mx-0 my-1 bg-line" />
            <DropdownMenuLabel className={headingClass}>
              {copy.group.unavailable}
            </DropdownMenuLabel>
            <div
              aria-disabled="true"
              className="flex min-h-9 items-center gap-2.5 rounded-lg px-2.5 py-1 text-[13.5px] leading-[1.45] text-muted-foreground"
            >
              <span className="w-4 shrink-0" />
              <span>{unavailableFields.map((f) => f.name).join(", ")}</span>
              <span className={hintClass}>{copy.group.unavailableReason}</span>
            </div>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
