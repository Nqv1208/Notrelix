import { useEffect, useId, useState } from "react";
import { Minus, Plus } from "lucide-react";
import { Button, Checkbox, Input, Label, Switch } from "@notrelix/ui-web";
import {
  MAX_KANBAN_LANE_CARD_LIMIT,
  MIN_KANBAN_LANE_CARD_LIMIT,
  normalizeStatusManageRoles,
  type KanbanStatusManageRole,
  type KanbanViewSettings,
} from "@notrelix/work-management-core";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

export interface KanbanSettingsPanelProps {
  copy: KanbanLaneCopy;
  settings: KanbanViewSettings;
  /** The change to store on the view; the rest of the config is untouched. */
  onChange: (patch: Partial<KanbanViewSettings>) => void;
}

/**
 * The Kanban section of the view settings: items per lane (1..20), which
 * board levels see the add / edit status controls (Manager is always on) and
 * the keyboard-move switch. Each change is stored at once.
 */
export function KanbanSettingsPanel({
  copy,
  settings,
  onChange,
}: KanbanSettingsPanelProps) {
  const id = useId();
  const [draft, setDraft] = useState(String(settings.laneCardLimit));
  useEffect(() => {
    setDraft(String(settings.laneCardLimit));
  }, [settings.laneCardLimit]);

  const setLimit = (value: number) => {
    const next = Math.min(
      MAX_KANBAN_LANE_CARD_LIMIT,
      Math.max(MIN_KANBAN_LANE_CARD_LIMIT, value),
    );
    setDraft(String(next));
    if (next !== settings.laneCardLimit) onChange({ laneCardLimit: next });
  };
  const setRoles = (role: KanbanStatusManageRole, on: boolean) => {
    const roles = new Set(settings.statusManageRoles);
    if (on) roles.add(role);
    else roles.delete(role);
    onChange({ statusManageRoles: normalizeStatusManageRoles([...roles]) });
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor={`${id}-limit`}>{copy.laneLimitLabel}</Label>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            aria-label={copy.laneLimitDecrease}
            disabled={settings.laneCardLimit <= MIN_KANBAN_LANE_CARD_LIMIT}
            onClick={() => setLimit(settings.laneCardLimit - 1)}
          >
            <Minus aria-hidden="true" />
          </Button>
          <Input
            id={`${id}-limit`}
            type="number"
            inputMode="numeric"
            min={MIN_KANBAN_LANE_CARD_LIMIT}
            max={MAX_KANBAN_LANE_CARD_LIMIT}
            value={draft}
            aria-describedby={`${id}-limit-hint`}
            className="w-16 text-center tabular-nums"
            onChange={(event) => {
              setDraft(event.target.value);
              const value = Number(event.target.value);
              if (
                event.target.value !== "" &&
                Number.isInteger(value) &&
                value >= MIN_KANBAN_LANE_CARD_LIMIT &&
                value <= MAX_KANBAN_LANE_CARD_LIMIT
              ) {
                onChange({ laneCardLimit: value });
              }
            }}
            onBlur={() => setDraft(String(settings.laneCardLimit))}
          />
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            aria-label={copy.laneLimitIncrease}
            disabled={settings.laneCardLimit >= MAX_KANBAN_LANE_CARD_LIMIT}
            onClick={() => setLimit(settings.laneCardLimit + 1)}
          >
            <Plus aria-hidden="true" />
          </Button>
        </div>
        <p
          id={`${id}-limit-hint`}
          className="text-metadata text-meta-foreground"
        >
          {copy.laneLimitHint}
        </p>
      </div>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-1 text-sm font-medium text-foreground">
          {copy.statusRolesLabel}
        </legend>
        <div className="flex items-center gap-2">
          <Checkbox
            id={`${id}-manager`}
            checked
            disabled
            aria-describedby={`${id}-roles-hint`}
          />
          <Label htmlFor={`${id}-manager`}>{copy.roleManager}</Label>
        </div>
        <div className="flex items-center gap-2">
          <Checkbox
            id={`${id}-editor`}
            checked={settings.statusManageRoles.includes("editor")}
            aria-describedby={`${id}-roles-hint`}
            onCheckedChange={(checked) => setRoles("editor", checked === true)}
          />
          <Label htmlFor={`${id}-editor`}>{copy.roleEditor}</Label>
        </div>
        <p
          id={`${id}-roles-hint`}
          className="text-metadata text-meta-foreground"
        >
          {copy.statusRolesHint}
        </p>
      </fieldset>

      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor={`${id}-keys`}>{copy.keyboardMoveLabel}</Label>
          <p
            id={`${id}-keys-hint`}
            className="text-metadata text-meta-foreground"
          >
            {copy.keyboardMoveHint}
          </p>
        </div>
        <Switch
          id={`${id}-keys`}
          checked={settings.keyboardMove}
          aria-describedby={`${id}-keys-hint`}
          onCheckedChange={(checked) => onChange({ keyboardMove: checked })}
        />
      </div>
    </div>
  );
}
