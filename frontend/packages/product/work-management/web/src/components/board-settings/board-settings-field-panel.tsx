import { useEffect, useId, useRef, useState } from "react";
import {
  Columns3,
  Info,
  Lock,
  Plus,
  SquareKanban,
  Table2,
  X,
  Zap,
} from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import type {
  BoardSettingsFieldDto,
  BoardSettingsOptionDto,
  BoardSettingsUsageDto,
} from "@notrelix/work-management-core";
import { DisabledControl } from "../item-page/item-page-parts";
import { useSettings } from "./board-settings-context";
import {
  SettingsConflictSlot,
  show,
  useConflictFor,
  useSavedBy,
  type ConflictPart,
} from "./board-settings-conflict";
import {
  CHANGED_INPUT_CLASS,
  ConflictFieldNote,
} from "./board-settings-conflict-parts";
import { WriteError, useWriteErrorProps } from "./board-settings-write-error";
import {
  OPTION_COLORS,
  fillCopy,
  plural,
  typeLongName,
  typeName,
} from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  ColorSquare,
  FIELD_LABEL_CLASS,
  FormGroup,
  HINT_CLASS,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  SwitchControl,
  TEXTAREA_CLASS,
  TypeMark,
  fieldIcon,
} from "./board-settings-parts";
import { SortableList, SortableRow } from "./board-settings-sortable";

const NO_DEFAULT = "__none__";

/**
 * The panel that edits one field (mockup `.ed`). The name, the default, the
 * "required" switch and the guidance are a draft that "Lưu thay đổi" writes;
 * adding, renaming, colouring, ordering and deleting an option write at once,
 * since each is its own change of the field.
 */
export function FieldPanel({
  field,
  onClose,
  onArchive,
  onDelete,
  onEditComputed,
}: {
  field: BoardSettingsFieldDto;
  /** Present for a Formula or Rollup: opens its definition dialog. */
  onEditComputed?: () => void;
  onClose: () => void;
  /** Absent when the product has no archive of a field yet. */
  onArchive?: () => void;
  /** Absent when the member may not delete fields; opens the confirmation. */
  onDelete?: () => void;
}) {
  const { copy, settings, actions, run } = useSettings();
  const rules = settings.unavailable?.fieldRules !== true;
  const t = copy.panel;
  const nameId = useId();
  const guidanceId = useId();
  const [name, setName] = useState(field.name);
  const [defaultId, setDefaultId] = useState<string | null>(
    field.defaultOptionId,
  );
  const [required, setRequired] = useState(field.required);
  const [guidance, setGuidance] = useState(field.guidance);
  const [nameTaken, setNameTaken] = useState(false);
  const [saving, setSaving] = useState(false);

  const saveScope = `field:${field.id}`;
  const conflict = useConflictFor(
    (scope) => scope === saveScope || scope === optionsScope(field.id),
  );
  // The server's value wins after a refetch of the same field, except while a
  // stale save holds what the person typed until they choose.
  useEffect(() => {
    if (conflict) return;
    setName(field.name);
    setDefaultId(field.defaultOptionId);
    setRequired(field.required);
    setGuidance(field.guidance);
  }, [
    field.name,
    field.defaultOptionId,
    field.required,
    field.guidance,
    conflict,
  ]);
  const savedBy = useSavedBy(conflict);
  const baseField = conflict?.base.fields.find((f) => f.id === field.id);
  const edited = baseField ?? field;
  const latestField = conflict?.latest?.fields.find((f) => f.id === field.id);
  const theirs = <
    K extends "name" | "required" | "guidance" | "defaultOptionId",
  >(
    key: K,
  ) =>
    baseField && latestField && latestField[key] !== baseField[key]
      ? latestField[key]
      : undefined;
  const optionNames = (
    list: readonly { id: string; name: string }[] | undefined,
  ) => new Map((list ?? []).map((option) => [option.id, option.name]));
  const optionChanges = (() => {
    if (!baseField || !latestField) return { removed: [], added: [] };
    const before = optionNames(baseField.options);
    const after = optionNames(latestField.options);
    return {
      removed: [...before].filter(([id]) => !after.has(id)).map(([, n]) => n),
      added: [...after].filter(([id]) => !before.has(id)).map(([, n]) => n),
    };
  })();
  const yesNo = (value: boolean) =>
    value ? copy.conflict.yes : copy.conflict.no;
  const optionName = (id: string | null) =>
    (latestField ?? field).options.find((o) => o.id === id)?.name ?? "";
  const conflictParts: ConflictPart[] = [
    {
      id: "name",
      label: copy.conflict.parts.fieldName,
      mine: name.trim() !== edited.name ? show(name.trim()) : null,
      latest: theirs("name") !== undefined ? show(latestField!.name) : null,
    },
    {
      id: "options",
      label: copy.conflict.parts.options,
      mine: null,
      latest:
        optionChanges.removed.length + optionChanges.added.length > 0
          ? [
              ...optionChanges.removed.map((n) =>
                fillCopy(copy.conflict.optionRemoved, { by: savedBy, name: n }),
              ),
              ...optionChanges.added.map((n) =>
                fillCopy(copy.conflict.optionAdded, { by: savedBy, name: n }),
              ),
            ].join(" ")
          : null,
    },
    {
      id: "required",
      label: copy.conflict.parts.required,
      mine: required !== edited.required ? yesNo(required) : null,
      latest:
        theirs("required") !== undefined ? yesNo(latestField!.required) : null,
    },
    {
      id: "guidance",
      label: copy.conflict.parts.guidance,
      mine: guidance !== edited.guidance ? show(guidance) : null,
      latest:
        theirs("guidance") !== undefined ? show(latestField!.guidance) : null,
    },
    {
      id: "defaultOption",
      label: copy.conflict.parts.defaultOption,
      mine:
        defaultId !== edited.defaultOptionId
          ? show(optionName(defaultId))
          : null,
      latest:
        theirs("defaultOptionId") !== undefined
          ? show(optionName(latestField!.defaultOptionId))
          : null,
    },
  ];
  const saveErrorId = `${nameId}-save-error`;
  const saveFailedProps = useWriteErrorProps(saveScope, saveErrorId);
  const trimmed = name.trim();
  const dirty =
    trimmed !== field.name ||
    defaultId !== field.defaultOptionId ||
    required !== field.required ||
    guidance !== field.guidance;

  function reset() {
    setName(field.name);
    setDefaultId(field.defaultOptionId);
    setRequired(field.required);
    setGuidance(field.guidance);
    setNameTaken(false);
  }

  async function save() {
    if (trimmed === "") return;
    if (!dirty) return;
    setSaving(true);
    const result = await run(
      () =>
        actions.updateField(field.id, {
          ...(trimmed !== field.name ? { name: trimmed } : {}),
          ...(defaultId !== field.defaultOptionId
            ? { defaultOptionId: defaultId }
            : {}),
          ...(required !== field.required ? { required } : {}),
          ...(guidance !== field.guidance ? { guidance } : {}),
        }),
      saveScope,
    );
    setSaving(false);
    setNameTaken(!result.ok && result.field === "name");
  }

  const Icon = fieldIcon(field.type, field.key);
  const hasOptions = field.hasColumn && field.options.length > 0;
  const canArchive = field.type !== "status";

  return (
    <aside
      aria-label={fillCopy(t.label, { name: field.name })}
      className="flex min-h-0 w-[400px] shrink-0 flex-col border-l border-line bg-popover max-xl:absolute max-xl:inset-y-0 max-xl:right-0 max-xl:z-10 max-xl:shadow-pop max-md:inset-0 max-md:w-full max-md:border-l-0 max-md:shadow-none"
    >
      <div className="flex items-center gap-2.5 border-b border-line px-[18px] py-3.5">
        <TypeMark icon={Icon} size="md" className="rounded-[8px]" />
        <div className="min-w-0">
          <b className="block truncate text-[15px] leading-[1.45] font-[650]">
            {field.name}
          </b>
          <small className="block text-metadata leading-[1.45] text-meta-foreground">
            {field.valueCount > 0
              ? fillCopy(t.sub, {
                  type: typeName(copy, field.type),
                  count: field.valueCount,
                })
              : fillCopy(t.subNone, {
                  type: typeName(copy, field.type),
                })}
          </small>
        </div>
        <span className="flex-1" />
        <button
          type="button"
          aria-label={copy.common.close}
          onClick={onClose}
          className={ICON_BUTTON_CLASS}
        >
          <X aria-hidden="true" className="!size-4" />
        </button>
      </div>

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-auto px-[18px] pt-3.5 pb-[18px]">
        <SettingsConflictSlot
          match={(scope) =>
            scope === saveScope || scope === optionsScope(field.id)
          }
          kind="field"
          parts={conflictParts}
        />
        <FormGroup
          label={t.name}
          htmlFor={nameId}
          hint={nameTaken ? copy.feedback.nameTaken : undefined}
        >
          <input
            id={nameId}
            value={name}
            onChange={(event) => {
              setName(event.target.value);
              setNameTaken(false);
            }}
            {...saveFailedProps}
            aria-invalid={
              nameTaken || trimmed === ""
                ? true
                : saveFailedProps["aria-invalid"]
            }
            className={cn(
              INPUT_CLASS,
              theirs("name") !== undefined && CHANGED_INPUT_CLASS,
            )}
          />
          {theirs("name") !== undefined ? (
            <ConflictFieldNote>
              {fillCopy(copy.conflict.changedTo, {
                by: savedBy,
                value: latestField!.name,
              })}
            </ConflictFieldNote>
          ) : null}
        </FormGroup>

        {onEditComputed ? (
          <FormGroup
            label={
              field.type === "formula"
                ? copy.computedDialog.expression
                : copy.computedDialog.rollupFunction
            }
          >
            <div className={cn(INPUT_CLASS, "font-mono text-[12.5px]")}>
              {field.type === "formula"
                ? (field.computed?.expression ?? "—")
                : `${copy.computedDialog.functionNames[field.computed?.rollupFunction ?? "count"]}`}
            </div>
            <button
              type="button"
              onClick={onEditComputed}
              className={cn(BUTTON_CLASS.outline, "mt-1.5")}
            >
              {field.type === "formula"
                ? copy.computedDialog.editFormula
                : copy.computedDialog.editRollup}
            </button>
          </FormGroup>
        ) : null}

        <FormGroup label={t.key} hint={t.keyHint}>
          <div
            className={cn(
              INPUT_CLASS,
              "bg-background font-mono text-[12.5px] text-meta-foreground",
            )}
          >
            {field.key}
            <Lock aria-hidden="true" className="ml-auto size-3.5" />
          </div>
        </FormGroup>

        <FormGroup
          label={t.type}
          hint={fillCopy(t.typeHint, { count: field.valueCount })}
        >
          <div className={INPUT_CLASS}>
            {typeLongName(copy, field.type)}
            <span className="ml-auto text-crumb font-medium text-brand-subtle-foreground">
              <DisabledControl reason={t.changeTypeUnavailable}>
                <button
                  type="button"
                  aria-disabled="true"
                  className="cursor-default border-0 bg-transparent p-0 font-medium text-brand-subtle-foreground max-md:min-h-11 max-md:min-w-11"
                >
                  {t.changeType}
                </button>
              </DisabledControl>
            </span>
          </div>
        </FormGroup>

        {hasOptions ? (
          <>
            <OptionsEditor field={field} />
            {rules ? (
              <FormGroup label={t.default}>
                <DefaultSelect
                  options={field.options}
                  value={defaultId}
                  onChange={setDefaultId}
                />
                <Callout icon={Info}>
                  <span>
                    {fillCopy(t.defaultHint, { count: field.valueCount })}
                  </span>
                </Callout>
              </FormGroup>
            ) : null}
          </>
        ) : null}

        {rules ? (
          <>
            <div className="flex items-center gap-2.5 text-item-small">
              <span className="flex-1">
                <span id={`${nameId}-req`}>{t.required}</span>
                <small className="block text-metadata text-meta-foreground">
                  {t.requiredHint}
                </small>
              </span>
              <SwitchControl
                checked={required}
                onChange={setRequired}
                label={t.required}
              />
            </div>

            <FormGroup label={t.guidance} htmlFor={guidanceId}>
              <textarea
                id={guidanceId}
                value={guidance}
                rows={2}
                placeholder={t.guidancePlaceholder}
                onChange={(event) => setGuidance(event.target.value)}
                className={TEXTAREA_CLASS}
              />
            </FormGroup>
          </>
        ) : null}

        {rules ? (
          <div className="flex flex-col gap-1.5">
            <span className={FIELD_LABEL_CLASS}>{t.usedIn}</span>
            {field.usedIn.length === 0 ? (
              <span className={HINT_CLASS}>{t.usedNone}</span>
            ) : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0">
                {field.usedIn.map((usage) => (
                  <UsageRow key={`${usage.kind}:${usage.name}`} usage={usage} />
                ))}
              </ul>
            )}
          </div>
        ) : null}
      </div>

      <div className="border-t border-line px-[18px] py-3">
        <WriteError scope={saveScope} id={saveErrorId} />
        <div className="flex items-center gap-2">
          {!onArchive ? null : canArchive ? (
            <button
              type="button"
              onClick={onArchive}
              className={BUTTON_CLASS.ghostDanger}
            >
              {t.archive}
            </button>
          ) : (
            <DisabledControl reason={t.archiveUnavailable}>
              <button
                type="button"
                aria-disabled="true"
                className={BUTTON_CLASS.ghostDanger}
              >
                {t.archive}
              </button>
            </DisabledControl>
          )}
          {!onDelete ? null : canArchive ? (
            <button
              type="button"
              onClick={onDelete}
              className={BUTTON_CLASS.ghostDanger}
            >
              {t.delete}
            </button>
          ) : (
            <DisabledControl reason={t.deleteUnavailable}>
              <button
                type="button"
                aria-disabled="true"
                className={BUTTON_CLASS.ghostDanger}
              >
                {t.delete}
              </button>
            </DisabledControl>
          )}
          <span className="flex-1" />
          <button
            type="button"
            onClick={reset}
            className={BUTTON_CLASS.outline}
          >
            {copy.common.cancel}
          </button>
          <button
            type="button"
            disabled={trimmed === "" || saving || conflict !== null}
            onClick={() => void save()}
            className={BUTTON_CLASS.primary}
          >
            {saving ? copy.common.saving : copy.common.save}
          </button>
        </div>
      </div>
    </aside>
  );
}

function UsageRow({ usage }: { usage: BoardSettingsUsageDto }) {
  const Icon =
    usage.kind === "rule"
      ? Zap
      : usage.name === "Kanban"
        ? SquareKanban
        : usage.kind === "card"
          ? Columns3
          : Table2;
  return (
    <li className="flex min-h-8 items-center gap-[9px] rounded-[7px] px-2 text-item-small hover:bg-state-hover">
      <Icon
        aria-hidden="true"
        className="size-3.5 shrink-0 text-muted-foreground"
      />
      <span className="min-w-0 truncate">{usage.name}</span>
      <span className="ml-auto shrink-0 text-metadata text-meta-foreground">
        {usage.how}
      </span>
    </li>
  );
}

/** The "default value" select with the option's colour square. */
function DefaultSelect({
  options,
  value,
  onChange,
}: {
  options: readonly BoardSettingsOptionDto[];
  value: string | null;
  onChange: (next: string | null) => void;
}) {
  const { copy } = useSettings();
  const selected = options.find((option) => option.id === value);
  return (
    <Select
      value={value ?? NO_DEFAULT}
      onValueChange={(next) => onChange(next === NO_DEFAULT ? null : next)}
    >
      <SelectTrigger
        aria-label={copy.panel.default}
        className={cn(INPUT_CLASS, "justify-between shadow-none")}
      >
        <SelectValue>
          {selected ? (
            <>
              <ColorSquare color={selected.color} />
              {selected.name}
            </>
          ) : (
            <span className="text-meta-foreground">
              {copy.panel.defaultNone}
            </span>
          )}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={NO_DEFAULT}>{copy.panel.defaultNone}</SelectItem>
        {options.map((option) => (
          <SelectItem key={option.id} value={option.id}>
            <ColorSquare color={option.color} />
            {option.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

const optionsScope = (fieldId: string) => `options:${fieldId}`;
const optionsErrorId = (fieldId: string) => `bs-options-error-${fieldId}`;

/** The option list of a select or status field: drag, rename, recolour, delete, add. */
function OptionsEditor({ field }: { field: BoardSettingsFieldDto }) {
  const { copy, actions, run } = useSettings();
  const t = copy.panel;
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState("");
  const [taken, setTaken] = useState(false);
  const scope = optionsScope(field.id);
  const errorId = optionsErrorId(field.id);
  const failedProps = useWriteErrorProps(scope, errorId);

  // A second Enter while the add is out would send it again with the version it just outdated, and
  // the server would refuse the copy as someone else's change.
  const addingNow = useRef(false);
  async function add() {
    const name = newName.trim();
    if (!name) {
      setAdding(false);
      return;
    }
    if (addingNow.current) return;
    addingNow.current = true;
    const color = OPTION_COLORS[field.options.length % OPTION_COLORS.length]!;
    const result = await run(
      () => actions.addOption(field.id, { name, color }),
      scope,
    ).finally(() => {
      addingNow.current = false;
    });
    if (result.ok) {
      setAdding(false);
      setNewName("");
      setTaken(false);
    } else if (result.field === "name") setTaken(true);
  }

  return (
    <FormGroup
      label={t.options}
      hint={taken ? copy.feedback.nameTaken : t.optionsHint}
    >
      <div className="flex flex-col overflow-hidden rounded-[8px] border border-line">
        <SortableList
          ids={field.options.map((option) => option.id)}
          onReorder={(ids) =>
            run(() => actions.reorderOptions(field.id, ids), scope)
          }
          announce={(id, position, total) =>
            fillCopy(copy.common.moved, {
              name: field.options.find((entry) => entry.id === id)?.name ?? "",
              position,
              total,
            })
          }
        >
          {(order) =>
            order.map((id) => {
              const option = field.options.find((entry) => entry.id === id);
              if (!option) return null;
              return (
                <OptionRow
                  key={option.id}
                  field={field}
                  option={option}
                  canDelete={
                    field.options.length > 1 && field.type !== "status"
                  }
                />
              );
            })
          }
        </SortableList>
        {adding ? (
          <div className="flex h-9 items-center gap-[9px] border-t border-line px-2.5">
            <input
              autoFocus
              value={newName}
              aria-label={t.optionName}
              placeholder={t.optionsNew}
              {...failedProps}
              onChange={(event) => {
                setNewName(event.target.value);
                setTaken(false);
              }}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void add();
                } else if (event.key === "Escape") {
                  event.stopPropagation();
                  setAdding(false);
                  setNewName("");
                }
              }}
              className="min-w-0 flex-1 border-0 bg-transparent text-item-small outline-none placeholder:text-meta-foreground"
            />
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setAdding(true)}
            className="flex h-[35px] max-md:h-11 cursor-pointer items-center gap-[7px] border-0 border-t border-line bg-transparent px-2.5 text-left text-item-small font-medium text-brand-subtle-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <Plus aria-hidden="true" className="size-3.5" />
            {t.addOption}
          </button>
        )}
      </div>
      <WriteError scope={scope} id={errorId} />
    </FormGroup>
  );
}

function OptionRow({
  field,
  option,
  canDelete,
}: {
  field: BoardSettingsFieldDto;
  option: BoardSettingsOptionDto;
  canDelete: boolean;
}) {
  const { copy, actions, run } = useSettings();
  const t = copy.panel;
  const [name, setName] = useState(option.name);
  useEffect(() => setName(option.name), [option.name]);
  const scope = optionsScope(field.id);
  const failedProps = useWriteErrorProps(scope, optionsErrorId(field.id));

  async function commit() {
    const next = name.trim();
    if (next === option.name) return;
    if (!next) {
      setName(option.name);
      return;
    }
    // A failed rename keeps the typed name; the error under the list offers Retry.
    await run(
      () => actions.updateOption(field.id, option.id, { name: next }),
      scope,
    );
  }

  return (
    <SortableRow
      id={option.id}
      handleLabel={`${copy.common.dragHandle}: ${option.name}`}
      className="group relative flex h-[37px] items-center gap-[9px] border-t border-line bg-popover pr-2.5 pl-1.5 text-item-small first:h-9 first:border-t-0 max-md:h-11 max-md:first:h-11"
    >
      {(handle) => (
        <>
          {/* Final-Settings draws a 14 px grip; the 28 px handle keeps its target around it. */}
          <span className="flex md:-mx-[7px]">{handle}</span>
          <Popover>
            <PopoverTrigger asChild>
              <button
                type="button"
                aria-label={fillCopy(t.optionColor, { name: option.name })}
                className="grid size-5 shrink-0 cursor-pointer place-items-center rounded-[5px] border-0 bg-transparent p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring max-md:size-11 md:-mx-[3px]"
              >
                <ColorSquare color={option.color} />
              </button>
            </PopoverTrigger>
            <PopoverContent align="start" className="flex w-auto gap-1.5 p-2">
              {OPTION_COLORS.map((color) => (
                <button
                  key={color}
                  type="button"
                  aria-label={color}
                  aria-pressed={
                    color.toLowerCase() === option.color.toLowerCase()
                  }
                  onClick={() =>
                    void run(
                      () =>
                        actions.updateOption(field.id, option.id, { color }),
                      scope,
                    )
                  }
                  className="size-5.5 max-md:size-11 cursor-pointer rounded-[6px] border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-pressed:shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--foreground)]"
                  style={{ backgroundColor: color }}
                />
              ))}
            </PopoverContent>
          </Popover>
          <input
            value={name}
            aria-label={t.optionName}
            {...(name !== option.name ? failedProps : {})}
            onChange={(event) => setName(event.target.value)}
            onBlur={() => void commit()}
            onKeyDown={(event) => {
              if (event.key === "Enter") event.currentTarget.blur();
              if (event.key === "Escape") {
                event.stopPropagation();
                setName(option.name);
              }
            }}
            className="min-w-0 flex-1 border-0 bg-transparent p-0 text-item-small outline-none focus-visible:ring-2 focus-visible:ring-focus-ring max-md:h-11"
          />
          <span className="ml-auto shrink-0 text-metadata text-meta-foreground md:group-focus-within:invisible md:group-hover:invisible">
            {plural(t.optionCount_one, t.optionCount_other, option.itemCount)}
          </span>
          {canDelete ? (
            <button
              type="button"
              aria-label={fillCopy(t.removeOption, { name: option.name })}
              onClick={() =>
                void run(() => actions.deleteOption(field.id, option.id), scope)
              }
              className="grid size-5 shrink-0 cursor-pointer place-items-center rounded-[5px] border-0 bg-transparent p-0 text-meta-foreground opacity-0 outline-none group-hover:opacity-100 hover:text-danger-text focus-visible:opacity-100 focus-visible:ring-2 focus-visible:ring-focus-ring max-md:size-11 max-md:opacity-100 md:absolute md:top-1/2 md:right-2.5 md:-translate-y-1/2"
            >
              <X aria-hidden="true" className="size-3" />
            </button>
          ) : null}
        </>
      )}
    </SortableRow>
  );
}
