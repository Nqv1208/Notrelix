import { useEffect, useId, useState } from "react";
import { SquareKanban, TriangleAlert } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { DisabledControl, MARK_TONE_CLASS } from "../item-page/item-page-parts";
import type { BoardSettingsSpace } from "./board-settings-types";
import { useSettings } from "./board-settings-context";
import { fillCopy } from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  HINT_CLASS,
  INPUT_CLASS,
  SectionHeader,
  TEXTAREA_CLASS,
} from "./board-settings-parts";

const COLORS = [
  "#1E6FD0",
  "#5B5BD6",
  "#4E8574",
  "#C2410C",
  "#9A6A12",
  "#475569",
] as const;
const ZONES = ["Asia/Ho_Chi_Minh", "Asia/Singapore", "Asia/Tokyo"] as const;
const ROW_CLASS =
  "grid grid-cols-[190px_minmax(0,1fr)] items-start gap-4 border-t border-line px-4 py-2.5 first:border-t-0";
const LABEL_CLASS = "pt-[7px] text-item-small font-semibold";

/** The "Chung" section: identity of the board, defaults and the danger zone. */
export function GeneralSection({
  onLeave,
  space,
}: {
  onLeave?: (reason: "archived" | "deleted") => void;
  space?: BoardSettingsSpace;
}) {
  const { copy, settings, actions, run } = useSettings();
  const t = copy.general;
  const general = settings.general;
  const ids = {
    name: useId(),
    desc: useId(),
    icon: useId(),
    key: useId(),
    keyHint: useId(),
    zone: useId(),
    week: useId(),
    del: useId(),
  };
  const [title, setTitle] = useState(general.title);
  const [description, setDescription] = useState(general.description);
  const [color, setColor] = useState(general.color);
  const [prefix, setPrefix] = useState(general.itemKeyPrefix);
  const [timeZone, setTimeZone] = useState(general.timeZone);
  const [weekStart, setWeekStart] = useState(general.weekStart);
  const [saving, setSaving] = useState(false);
  const [confirmArchive, setConfirmArchive] = useState(false);
  const [confirmName, setConfirmName] = useState("");

  useEffect(() => {
    setTitle(general.title);
    setDescription(general.description);
    setColor(general.color);
    setPrefix(general.itemKeyPrefix);
    setTimeZone(general.timeZone);
    setWeekStart(general.weekStart);
  }, [general]);

  const trimmed = title.trim();
  const prefixOk = /^[A-Za-z]{1,6}$/.test(prefix.trim());
  const dirty =
    trimmed !== general.title ||
    description !== general.description ||
    color.toLowerCase() !== general.color.toLowerCase() ||
    prefix.trim().toUpperCase() !== general.itemKeyPrefix ||
    timeZone !== general.timeZone ||
    weekStart !== general.weekStart;

  function reset() {
    setTitle(general.title);
    setDescription(general.description);
    setColor(general.color);
    setPrefix(general.itemKeyPrefix);
    setTimeZone(general.timeZone);
    setWeekStart(general.weekStart);
  }

  async function save() {
    if (!dirty || trimmed === "" || !prefixOk) return;
    setSaving(true);
    await run(
      actions.updateGeneral({
        ...(trimmed !== general.title ? { title: trimmed } : {}),
        ...(description !== general.description ? { description } : {}),
        ...(color.toLowerCase() !== general.color.toLowerCase()
          ? { color }
          : {}),
        ...(prefix.trim().toUpperCase() !== general.itemKeyPrefix
          ? { itemKeyPrefix: prefix.trim().toUpperCase() }
          : {}),
        ...(timeZone !== general.timeZone ? { timeZone } : {}),
        ...(weekStart !== general.weekStart ? { weekStart } : {}),
      }),
    );
    setSaving(false);
  }

  async function archive() {
    const result = await run(actions.archiveBoard());
    if (result.ok) onLeave?.("archived");
  }

  async function remove() {
    const result = await run(actions.deleteBoard(confirmName));
    if (result.ok) onLeave?.("deleted");
  }

  return (
    <section
      aria-labelledby="bs-general-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="max-w-[780px]">
        <SectionHeader
          id="bs-general-h"
          title={t.title}
          intro={t.intro}
          actions={
            <>
              <button
                type="button"
                onClick={reset}
                className={BUTTON_CLASS.outline}
              >
                {copy.common.cancel}
              </button>
              <button
                type="button"
                disabled={trimmed === "" || !prefixOk || saving}
                onClick={() => void save()}
                className={BUTTON_CLASS.primary}
              >
                {saving ? copy.common.saving : copy.common.save}
              </button>
            </>
          }
        />

        <div className="rounded-[10px] border border-line bg-popover">
          <div className={ROW_CLASS}>
            <label htmlFor={ids.name} className={LABEL_CLASS}>
              {t.name}
            </label>
            <input
              id={ids.name}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              aria-invalid={trimmed === "" ? true : undefined}
              className={INPUT_CLASS}
            />
          </div>
          <div className={ROW_CLASS}>
            <label htmlFor={ids.desc} className={LABEL_CLASS}>
              {t.description}
              <small className="block text-metadata font-normal text-meta-foreground">
                {t.descriptionHint}
              </small>
            </label>
            <textarea
              id={ids.desc}
              rows={2}
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              className={TEXTAREA_CLASS}
            />
          </div>
          <div className={ROW_CLASS}>
            <span id={ids.icon} className={LABEL_CLASS}>
              {t.icon}
            </span>
            <div
              role="group"
              aria-labelledby={ids.icon}
              className="flex flex-wrap items-center gap-2"
            >
              <DisabledControl reason={t.iconUnavailable}>
                <button
                  type="button"
                  aria-disabled="true"
                  aria-label={t.changeIcon}
                  className="grid size-[30px] cursor-default place-items-center rounded-lg border-0 text-white"
                  style={{ backgroundColor: color }}
                >
                  <SquareKanban aria-hidden="true" className="size-3.5" />
                </button>
              </DisabledControl>
              <span className={cn(HINT_CLASS, "mr-1.5")}>{t.colour}</span>
              {COLORS.map((option) => (
                <button
                  key={option}
                  type="button"
                  aria-pressed={option.toLowerCase() === color.toLowerCase()}
                  aria-label={t.colors[option]}
                  onClick={() => setColor(option)}
                  className="size-[22px] shrink-0 cursor-pointer rounded-[6px] border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-pressed:shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--foreground)]"
                  style={{ backgroundColor: option }}
                />
              ))}
            </div>
          </div>
          <div className={ROW_CLASS}>
            <span className={LABEL_CLASS}>{t.department}</span>
            <div className="flex flex-col gap-1.5">
              <div className="flex flex-wrap items-center gap-2">
                <span
                  className={cn(
                    INPUT_CLASS,
                    "min-w-0 flex-1 bg-background text-foreground",
                  )}
                >
                  {space ? (
                    <span
                      aria-hidden="true"
                      className={cn(
                        "grid size-5 shrink-0 place-items-center rounded-[6px] text-mark-xs font-bold",
                        MARK_TONE_CLASS[space.mark.tone],
                      )}
                    >
                      {space.mark.initials}
                    </span>
                  ) : null}
                  {general.department
                    ? fillCopy(t.departmentName, {
                        name: general.department.name,
                      })
                    : t.departmentNone}
                </span>
                <DisabledControl reason={t.moveUnavailable}>
                  <button
                    type="button"
                    aria-disabled="true"
                    aria-haspopup="dialog"
                    className={BUTTON_CLASS.outline}
                  >
                    {t.move}
                  </button>
                </DisabledControl>
              </div>
              <span className={HINT_CLASS}>{t.moveHint}</span>
            </div>
          </div>
          <div className={ROW_CLASS}>
            <label htmlFor={ids.key} className={LABEL_CLASS}>
              {t.key}
              <small className="block text-metadata font-normal text-meta-foreground">
                {t.keyHint}
              </small>
            </label>
            <div className="flex flex-col gap-1.5">
              <input
                id={ids.key}
                value={prefix}
                maxLength={6}
                onChange={(event) =>
                  setPrefix(event.target.value.toUpperCase())
                }
                aria-describedby={ids.keyHint}
                aria-invalid={prefixOk ? undefined : true}
                className={cn(INPUT_CLASS, "w-[120px] font-mono text-[13px]")}
              />
              <span id={ids.keyHint} className={HINT_CLASS}>
                {fillCopy(t.keyNote, {
                  example:
                    general.sampleItemKey ?? `${general.itemKeyPrefix}-1`,
                })}
              </span>
            </div>
          </div>
          <div className={ROW_CLASS}>
            <label htmlFor={ids.zone} className={LABEL_CLASS}>
              {t.timeZone}
              <small className="block text-metadata font-normal text-meta-foreground">
                {t.timeZoneHint}
              </small>
            </label>
            <select
              id={ids.zone}
              value={timeZone}
              onChange={(event) => setTimeZone(event.target.value)}
              className={INPUT_CLASS}
            >
              {ZONES.map((zone) => (
                <option key={zone} value={zone}>
                  {t.timeZones[zone]}
                </option>
              ))}
            </select>
          </div>
          <div className={ROW_CLASS}>
            <label htmlFor={ids.week} className={LABEL_CLASS}>
              {t.weekStart}
            </label>
            <select
              id={ids.week}
              value={weekStart}
              onChange={(event) =>
                setWeekStart(event.target.value as "monday" | "sunday")
              }
              className={cn(INPUT_CLASS, "w-[200px]")}
            >
              <option value="monday">{t.weekDays.monday}</option>
              <option value="sunday">{t.weekDays.sunday}</option>
            </select>
          </div>
        </div>

        <section
          aria-labelledby="bs-danger-h"
          className="mt-4 rounded-[10px] border border-danger bg-[color-mix(in_srgb,var(--danger)_5%,var(--popover))]"
        >
          <h3
            id="bs-danger-h"
            className="m-0 px-4 pt-3 pb-0.5 text-item-ui font-[650] text-danger-text"
          >
            {t.danger}
          </h3>
          <div className="flex items-center gap-4 px-4 py-3">
            <span className="min-w-0 flex-1">
              <b className="block text-item-ui font-semibold">{t.archive}</b>
              <small className="block text-crumb leading-normal text-muted-foreground">
                {t.archiveBody}
              </small>
            </span>
            <button
              type="button"
              onClick={() => setConfirmArchive(true)}
              className={BUTTON_CLASS.dangerOutline}
            >
              {t.archive}
            </button>
          </div>
          {confirmArchive ? (
            <div className="px-4 pb-3">
              <Callout icon={TriangleAlert} tone="warning" role="alert">
                <span className="flex flex-1 flex-col gap-2">
                  <span>
                    {fillCopy(t.archiveConfirm, { name: general.title })}
                  </span>
                  <span className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => setConfirmArchive(false)}
                      className={cn(BUTTON_CLASS.outline, "h-7")}
                    >
                      {copy.common.cancel}
                    </button>
                    <button
                      type="button"
                      onClick={() => void archive()}
                      className={cn(BUTTON_CLASS.danger, "h-7")}
                    >
                      {t.archiveAction}
                    </button>
                  </span>
                </span>
              </Callout>
            </div>
          ) : null}
          <div className="flex items-center gap-4 border-t border-[color-mix(in_srgb,var(--danger)_30%,transparent)] px-4 py-3">
            <span className="min-w-0 flex-1">
              <b className="block text-item-ui font-semibold">{t.delete}</b>
              <small className="block text-crumb leading-normal text-muted-foreground">
                {t.deleteBody}
              </small>
            </span>
          </div>
          <div className="flex flex-col gap-1.5 px-4 pb-3.5">
            <label
              htmlFor={ids.del}
              className="text-crumb font-semibold text-muted-foreground"
            >
              {t.deleteConfirm}{" "}
              <b className="text-foreground">{general.title}</b>
            </label>
            <div className="flex items-center gap-2">
              <input
                id={ids.del}
                value={confirmName}
                autoComplete="off"
                placeholder={general.title}
                onChange={(event) => setConfirmName(event.target.value)}
                className={cn(INPUT_CLASS, "min-w-0 flex-1")}
              />
              <button
                type="button"
                disabled={confirmName !== general.title}
                onClick={() => void remove()}
                className={BUTTON_CLASS.danger}
              >
                {t.deleteAction}
              </button>
            </div>
          </div>
        </section>
      </div>
    </section>
  );
}
