import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import {
  automationRulesDefaultScenario,
  automationRulesEmptyScenario,
} from "@notrelix/automation-testing";

import { AutomationsTab } from "../automations-tab";

describe("AutomationsTab interactions", () => {
  it("renders automation rules from reusable testing fixtures", () => {
    renderPureUi(<AutomationsTab rules={automationRulesDefaultScenario()} />);

    expect(screen.getByText('When card status goes to "Done"')).toBeTruthy();
    expect(screen.getByText("When card has urgent priority")).toBeTruthy();
  });

  it("routes create and toggle interactions through injected callbacks", () => {
    const onCreateRule = vi.fn();
    const onToggleRule = vi.fn();

    renderPureUi(
      <AutomationsTab
        rules={automationRulesDefaultScenario()}
        onCreateRule={onCreateRule}
        onToggleRule={onToggleRule}
      />,
    );

    fireEvent.click(
      screen.getByRole("switch", {
        name: 'Toggle automation rule When card status goes to "Done"',
      }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Create custom automation rule" }),
    );

    expect(onToggleRule).toHaveBeenCalledWith("rule-archive-done", false);
    expect(onCreateRule).toHaveBeenCalledTimes(1);
  });

  it("renders the empty state without state/query providers", () => {
    renderPureUi(<AutomationsTab rules={automationRulesEmptyScenario()} />);

    expect(
      screen.getByRole("button", { name: "Create custom automation rule" }),
    ).toBeTruthy();
  });
});
