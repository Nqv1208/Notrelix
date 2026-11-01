import { useId, useState, type ReactNode } from "react";
import { Check, Plus, RotateCcw, TriangleAlert, X } from "lucide-react";
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
  BoardSettingsViewDto,
} from "@notrelix/work-management-core";
import { DisabledControl } from "../item-page/item-page-parts";
import { useSettings } from "./board-settings-context";
import { fieldLabel } from "./board-settings-format";
import { fillCopy, shortDay } from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  FIELD_LABEL_CLASS,
  FormGroup,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  TypeMark,
  VIEW_ICON,
  fieldIcon,
} from "./board-settings-parts";

const COND_CLASS =
  "flex flex-wrap items-center gap-1.5 rounded-lg border border-line px-2.5 py-2 text-crumb";

/**
 * The panel that edits one saved view (mockup `.ed`). A view whose date field
 * was archived opens as the "needs a fix" panel: it says why the view is empty
 * and offers the two ways out.
 */
export function ViewPanel({
  view,
  onClose,
  kanbanSettings,
}: {
  view: BoardSettingsViewDto;
  onClose: () => void;
  kanbanSettings?: ReactNode;
}) {
  const { copy, settings, actions, run, timeZone } = useSettings();
  const t = copy.viewPanel;
  const nameId = useId();
  const [name, setName] = useState(view.name);
  const [fieldId, setFieldId] = useState<string | null>(view.fieldId);
  const [cardFieldIds, setCardFieldIds] = useState<readonly string[]>(
    view.cardFieldIds,
  );
  const [visibility, setVisibility] = useState(view.visibility);
  const [confirmDelete, setConfirmDelete] = useState(false);
  // The default view leaves only with a next one: a shared view that is not it.
  const nextChoices = settings.views.filter(
    (entry) => entry.id !== view.id && entry.visibility === "board",
  );
  const [nextDefaultId, setNextDefaultId] = useState(nextChoices[0]?.id ?? "");
  const [saving, setSaving] = useState(false);

  const Icon = VIEW_ICON[view.kind];
  const live = settings.fields;
  const archivedField = settings.archivedFields.find(
    (f) => f.id === view.fieldId,
  );
  const broken = view.needsFix && archivedField !== undefined;
  const trimmed = name.trim();
  const dirty =
    trimmed !== view.name ||
    fieldId !== view.fieldId ||
    visibility !== view.visibility ||
    cardFieldIds.join("|") !== view.cardFieldIds.join("|");
  const canSave =
    trimmed !== "" && !saving && !(broken && fieldId === view.fieldId);

  const splitChoices = live.filter((field) =>
    ["status", "select", "person"].includes(field.type),
  );
  const dateChoices = live
    .filter((field) => field.type === "date")
    .sort((a, b) => b.usedIn.length - a.usedIn.length);
  const cardChoices = view.cardFieldChoices
    .map((id) => live.find((field) => field.id === id))
    .filter((field): field is BoardSettingsFieldDto => field !== undefined);

  const who = view.visibility === "board" ? t.shared : t.mine;
  const metaText = view.lastEditedBy
    ? view.createdBy && broken
      ? fillCopy(t.metaCreated, { who, name: view.createdBy })
      : fillCopy(t.meta, { who, name: view.lastEditedBy })
    : who;

  async function save() {
    if (!canSave || !dirty) return;
    setSaving(true);
    await run(
      actions.updateView(view.id, {
        ...(trimmed !== view.name ? { name: trimmed } : {}),
        ...(fieldId !== view.fieldId ? { fieldId } : {}),
        ...(visibility !== view.visibility ? { visibility } : {}),
        ...(cardFieldIds.join("|") !== view.cardFieldIds.join("|")
          ? { cardFieldIds }
          : {}),
      }),
    );
    setSaving(false);
  }

  function reset() {
    setName(view.name);
    setFieldId(view.fieldId);
    setCardFieldIds(view.cardFieldIds);
    setVisibility(view.visibility);
  }

  async function remove() {
    const result = await run(() =>
      actions.deleteView(view.id, view.isDefault ? nextDefaultId : undefined),
    );
    if (result.ok) onClose();
  }

  async function duplicate() {
    await run(() => actions.duplicateView(view.id));
  }

  return (
    <aside
      aria-label={fillCopy(t.label, { name: view.name })}
      className="flex min-h-0 w-[400px] shrink-0 flex-col border-l border-line bg-popover"
    >
      <div className="flex items-center gap-2.5 border-b border-line px-[18px] py-3.5">
        <TypeMark
          icon={Icon}
          size="md"
          className={cn(broken && "text-warning-text")}
        />
        <div className="min-w-0">
          <b className="block truncate text-[15px] leading-[1.45] font-[650]">
            {view.name}
          </b>
          <small className="block text-metadata leading-[1.45] text-meta-foreground">
            {metaText}
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
        {broken && archivedField ? (
          <>
            <Callout icon={TriangleAlert} tone="warning" role="alert">
              <span>
                <b className="font-semibold">{t.brokenTitle}</b>
                <br />
                {fillCopy(t.brokenBody, {
                  field: archivedField.name,
                  date: shortDay(archivedField.archivedAt, timeZone),
                  name: archivedField.archivedByName,
                  view: view.name,
                })}
              </span>
            </Callout>
            <div className="flex flex-col gap-1.5">
              <span className={FIELD_LABEL_CLASS}>{t.fixTitle}</span>
              <div className="flex flex-col gap-1">
                <FixBox
                  icon={VIEW_ICON.calendar}
                  title={t.fixChange}
                  hint={dateChoices
                    .map((field) => field.name)
                    .join(` ${t.or} `)}
                  action={
                    <DatePicker
                      choices={dateChoices}
                      onPick={(id) => setFieldId(id)}
                    />
                  }
                />
                <FixBox
                  icon={RotateCcw}
                  title={fillCopy(t.fixRestore, { field: archivedField.name })}
                  hint={fillCopy(t.fixRestoreHint, {
                    count: archivedField.valueCount,
                  })}
                  action={
                    <button
                      type="button"
                      onClick={() =>
                        void run(() => actions.restoreField(archivedField.id))
                      }
                      className={cn(BUTTON_CLASS.outline, "h-7")}
                    >
                      {t.fixRestoreAction}
                    </button>
                  }
                />
              </div>
            </div>
          </>
        ) : (
          <FormGroup label={copy.viewPanel.name} htmlFor={nameId}>
            <input
              id={nameId}
              value={name}
              onChange={(event) => setName(event.target.value)}
              aria-invalid={trimmed === "" ? true : undefined}
              className={INPUT_CLASS}
            />
          </FormGroup>
        )}

        {view.kind === "kanban" &&
        !broken &&
        !settings.unavailable?.viewSplit ? (
          <FormGroup label={t.splitBy} hint={t.splitHint}>
            <FieldSelect
              label={t.splitBy}
              value={fieldId}
              choices={splitChoices}
              onChange={setFieldId}
            />
          </FormGroup>
        ) : null}

        {view.kind === "calendar" && !settings.unavailable?.viewSplit ? (
          <FormGroup label={t.placeBy} hint={t.placeHint}>
            <FieldSelect
              label={t.placeBy}
              value={fieldId}
              choices={dateChoices}
              archived={broken ? archivedField : undefined}
              onChange={setFieldId}
            />
          </FormGroup>
        ) : null}

        {view.filters.length > 0 || view.kind === "kanban" ? (
          <div className="flex flex-col gap-1.5">
            <span className={FIELD_LABEL_CLASS}>{t.filter}</span>
            {view.filters.map((filter) => (
              <div
                key={`${filter.fieldName}:${filter.values}`}
                className={COND_CLASS}
              >
                <b className="font-semibold">{filter.fieldName}</b>
                <span className="text-meta-foreground">{filter.operator}</span>
                <b className="font-semibold">{filter.values}</b>
              </div>
            ))}
            {view.kind === "kanban" ? (
              <DisabledControl reason={t.addConditionUnavailable}>
                <button
                  type="button"
                  aria-disabled="true"
                  className="flex cursor-default items-center gap-1 self-start border-0 bg-transparent p-0 text-metadata font-medium text-brand-subtle-foreground"
                >
                  <Plus aria-hidden="true" className="size-3" />
                  {t.addCondition}
                </button>
              </DisabledControl>
            ) : null}
          </div>
        ) : null}

        {view.sort ? (
          <div className="flex flex-col gap-1.5">
            <span className={FIELD_LABEL_CLASS}>{t.sort}</span>
            <div className={COND_CLASS}>
              <b className="font-semibold">{view.sort.fieldName}</b>
              <span className="text-meta-foreground">{view.sort.text}</span>
            </div>
          </div>
        ) : null}

        {view.kind === "kanban" && cardChoices.length > 0 ? (
          <div className="flex flex-col gap-1.5">
            <span className={FIELD_LABEL_CLASS}>{t.cardFields}</span>
            <div>
              {cardChoices.map((field) => {
                const checked = cardFieldIds.includes(field.id);
                return (
                  <label
                    key={field.id}
                    className="flex h-8 cursor-pointer items-center gap-[9px] text-item-small"
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() =>
                        setCardFieldIds((ids) =>
                          checked
                            ? ids.filter((id) => id !== field.id)
                            : [...ids, field.id],
                        )
                      }
                      className="peer sr-only"
                    />
                    <span
                      aria-hidden="true"
                      className={cn(
                        "grid size-4 shrink-0 place-items-center rounded-[4px] border-[1.5px] border-line-strong peer-focus-visible:ring-2 peer-focus-visible:ring-focus-ring",
                        checked &&
                          "border-primary bg-primary text-primary-foreground",
                      )}
                    >
                      {checked ? (
                        <Check className="size-[11px]" strokeWidth={3} />
                      ) : null}
                    </span>
                    {fieldLabel(field)}
                  </label>
                );
              })}
            </div>
          </div>
        ) : null}

        {broken ? null : (
          <FormGroup label={t.who} hint={t.whoHint}>
            <div
              role="radiogroup"
              aria-label={t.who}
              className="flex gap-0.5 rounded-lg border border-line-strong bg-background p-0.5"
            >
              {(["board", "me"] as const).map((option) => (
                <button
                  key={option}
                  type="button"
                  role="radio"
                  aria-checked={visibility === option}
                  onClick={() => setVisibility(option)}
                  className={cn(
                    "flex h-[30px] flex-1 cursor-pointer items-center justify-center gap-1.5 rounded-[6px] border-0 bg-transparent text-crumb font-medium text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                    visibility === option &&
                      "bg-popover font-semibold text-foreground shadow-[0_1px_2px_rgb(15_23_42/0.12)]",
                  )}
                >
                  {option === "board" ? t.whoBoard : t.whoMe}
                </button>
              ))}
            </div>
          </FormGroup>
        )}

        {view.kind === "kanban" && kanbanSettings ? (
          <div className="flex flex-col gap-1.5">
            <span className={FIELD_LABEL_CLASS}>{t.kanbanSettings}</span>
            {kanbanSettings}
          </div>
        ) : null}

        {confirmDelete ? (
          <Callout icon={TriangleAlert} tone="warning" role="alert">
            <span className="flex flex-1 flex-col gap-2">
              <span>{fillCopy(t.deleteConfirm, { name: view.name })}</span>
              {view.isDefault ? (
                <span className="flex flex-wrap items-center gap-2">
                  <label
                    htmlFor={`${nameId}-next`}
                    className="text-item-small font-medium"
                  >
                    {t.nextDefault}
                  </label>
                  <Select
                    value={nextDefaultId}
                    onValueChange={setNextDefaultId}
                  >
                    <SelectTrigger id={`${nameId}-next`} className="w-[220px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {nextChoices.map((entry) => (
                        <SelectItem key={entry.id} value={entry.id}>
                          {entry.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </span>
              ) : null}
              <span className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setConfirmDelete(false)}
                  className={cn(BUTTON_CLASS.outline, "h-7")}
                >
                  {copy.common.cancel}
                </button>
                <button
                  type="button"
                  onClick={() => void remove()}
                  disabled={view.isDefault && nextDefaultId === ""}
                  className={cn(BUTTON_CLASS.danger, "h-7")}
                >
                  {broken ? t.deleteView : t.delete}
                </button>
              </span>
            </span>
          </Callout>
        ) : null}
      </div>

      <div className="flex items-center gap-2 border-t border-line px-[18px] py-3">
        {view.isDefault && nextChoices.length === 0 ? (
          <DisabledControl reason={t.deleteDefault}>
            <button
              type="button"
              aria-disabled="true"
              className={BUTTON_CLASS.ghostDanger}
            >
              {broken ? t.deleteView : t.delete}
            </button>
          </DisabledControl>
        ) : (
          <button
            type="button"
            onClick={() => setConfirmDelete(true)}
            className={BUTTON_CLASS.ghostDanger}
          >
            {broken ? t.deleteView : t.delete}
          </button>
        )}
        {broken || view.isDefault || view.visibility === "me" ? null : (
          <button
            type="button"
            onClick={() => void run(() => actions.setDefaultView(view.id))}
            className={BUTTON_CLASS.ghost}
          >
            {t.makeDefault}
          </button>
        )}
        {broken ? null : (
          <button
            type="button"
            onClick={() => void duplicate()}
            className={BUTTON_CLASS.ghost}
          >
            {t.duplicate}
          </button>
        )}
        <span className="flex-1" />
        <button type="button" onClick={reset} className={BUTTON_CLASS.outline}>
          {copy.common.cancel}
        </button>
        <button
          type="button"
          disabled={!canSave}
          onClick={() => void save()}
          className={BUTTON_CLASS.primary}
        >
          {saving ? copy.common.saving : t.save}
        </button>
      </div>
    </aside>
  );
}

function FixBox({
  icon: Icon,
  title,
  hint,
  action,
}: {
  icon: typeof Check;
  title: string;
  hint: string;
  action: React.ReactNode;
}) {
  return (
    <div className="flex min-h-8 items-center gap-[9px] rounded-[7px] border border-line px-3 py-2.5 text-item-small">
      <Icon aria-hidden="true" className="size-3.5 shrink-0" />
      <span className="min-w-0">
        <b className="block font-semibold">{title}</b>
        <small className="block text-metadata text-meta-foreground">
          {hint}
        </small>
      </span>
      <span className="ml-auto shrink-0">{action}</span>
    </div>
  );
}

function DatePicker({
  choices,
  onPick,
}: {
  choices: readonly BoardSettingsFieldDto[];
  onPick: (fieldId: string) => void;
}) {
  const { copy } = useSettings();
  const [open, setOpen] = useState(false);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button type="button" className={cn(BUTTON_CLASS.outline, "h-7")}>
          {copy.viewPanel.fixChoose}
        </button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-52 p-1">
        {choices.map((field) => (
          <button
            key={field.id}
            type="button"
            onClick={() => {
              onPick(field.id);
              setOpen(false);
            }}
            className="flex h-8 w-full cursor-pointer items-center rounded-[6px] border-0 bg-transparent px-2 text-left text-item-small outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {field.name}
          </button>
        ))}
      </PopoverContent>
    </Popover>
  );
}

function FieldSelect({
  label,
  value,
  choices,
  archived,
  onChange,
}: {
  label: string;
  value: string | null;
  choices: readonly BoardSettingsFieldDto[];
  archived?: {
    id: string;
    name: string;
    type: BoardSettingsFieldDto["type"];
    key: string;
  };
  onChange: (fieldId: string | null) => void;
}) {
  const { copy } = useSettings();
  const current = choices.find((field) => field.id === value);
  const showsArchived = archived !== undefined && value === archived.id;
  return (
    <Select
      value={value ?? ""}
      onValueChange={(next) => onChange(next || null)}
    >
      <SelectTrigger
        aria-label={label}
        className={cn(
          INPUT_CLASS,
          "justify-between gap-2 shadow-none",
          showsArchived && "border-warning-text text-meta-foreground",
        )}
      >
        <SelectValue>
          {current ? (
            <>
              {(() => {
                const FieldIcon = fieldIcon(current.type, current.key);
                return <FieldIcon aria-hidden="true" className="size-3.5" />;
              })()}
              {current.name}
            </>
          ) : showsArchived ? (
            `${archived.name} ${copy.viewPanel.archivedSuffix}`
          ) : (
            <span className="text-meta-foreground">
              {copy.viewPanel.noField}
            </span>
          )}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        {showsArchived ? (
          <SelectItem value={archived.id} disabled>
            {archived.name} {copy.viewPanel.archivedSuffix}
          </SelectItem>
        ) : null}
        {choices.map((field) => (
          <SelectItem key={field.id} value={field.id}>
            {field.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
