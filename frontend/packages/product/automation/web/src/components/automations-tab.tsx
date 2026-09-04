import { Plus } from "lucide-react";
import { Button, Switch } from "@notrelix/ui-web";
import type { AutomationRule } from "@notrelix/automation-core";

export interface AutomationsTabProps {
  rules?: AutomationRule[];
  onCreateRule?: () => void;
  onToggleRule?: (ruleId: string, enabled: boolean) => void;
}

const defaultRules: AutomationRule[] = [
  {
    id: "demo-done-archive",
    workspaceId: "demo",
    boardId: "demo",
    name: 'When card status goes to "Done"',
    description:
      "Archive the card and remove all member assignments automatically.",
    triggerType: "card_status_changed",
    triggerConfig: { to: "done" },
    actionType: "archive_card",
    actionConfig: {},
    isEnabled: true,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-01T00:00:00.000Z",
  },
  {
    id: "demo-urgent-owner",
    workspaceId: "demo",
    boardId: "demo",
    name: "When card has urgent priority",
    description: "Notify the workspace owner instantly via email.",
    triggerType: "card_priority_changed",
    triggerConfig: { priority: "urgent" },
    actionType: "notify_owner",
    actionConfig: {},
    isEnabled: false,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-01T00:00:00.000Z",
  },
];

export function AutomationsTab({
  rules = defaultRules,
  onCreateRule,
  onToggleRule,
}: AutomationsTabProps) {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">
          Automation Rules
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Define trigger-action workflows to reduce repetitive tasks.
        </p>
      </div>

      <div className="space-y-3">
        {rules.map((rule) => (
          <div
            key={rule.id}
            className="flex items-center justify-between p-4 rounded-xl border border-border/60 bg-card"
          >
            <div className="pr-4">
              <p className="font-semibold text-sm text-foreground">
                {rule.name}
              </p>
              <p className="text-xs text-muted-foreground mt-0.5">
                {rule.description}
              </p>
            </div>
            <Switch
              aria-label={`Toggle automation rule ${rule.name}`}
              checked={rule.isEnabled}
              onCheckedChange={(enabled) => onToggleRule?.(rule.id, enabled)}
              className="data-[state=checked]:bg-primary"
            />
          </div>
        ))}

        <Button
          variant="outline"
          className="w-full border-dashed rounded-xl py-5 hover:bg-muted/40"
          onClick={onCreateRule}
        >
          <Plus className="size-4 mr-2" />
          Create custom automation rule
        </Button>
      </div>
    </div>
  );
}
