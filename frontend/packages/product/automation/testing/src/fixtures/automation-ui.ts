import { buildAutomationRule } from "../builders/rule-builder";

export function automationRulesDefaultScenario() {
  return [
    buildAutomationRule({
      id: "rule-archive-done",
      name: 'When card status goes to "Done"',
      description:
        "Archive the card and remove all member assignments automatically.",
      triggerConfig: { to: "done" },
      actionType: "archive_card",
      isEnabled: true,
    }),
    buildAutomationRule({
      id: "rule-urgent-owner",
      name: "When card has urgent priority",
      description: "Notify the workspace owner instantly via email.",
      triggerType: "card_priority_changed",
      triggerConfig: { priority: "urgent" },
      actionType: "notify_owner",
      isEnabled: false,
    }),
  ];
}

export function automationRulesEmptyScenario() {
  return [];
}

export function automationRulesEdgeDataScenario() {
  return [
    buildAutomationRule({
      id: "rule-long-escalation",
      name: "Escalate blocked enterprise migration work with unusually long rule naming",
      description:
        "Notify governance, assign the workspace owner, and move the item when migration risk remains blocked after two review cycles.",
      triggerType: "field_updated",
      triggerConfig: { field: "risk", value: "blocked" },
      actionType: "move_to_group",
      actionConfig: { groupId: "blocked-risk-review" },
      isEnabled: true,
    }),
    buildAutomationRule({
      id: "rule-disabled-email",
      name: "Send launch digest email",
      description: "Paused while notification templates are being reviewed.",
      triggerType: "card_created",
      actionType: "send_email",
      isEnabled: false,
    }),
  ];
}
