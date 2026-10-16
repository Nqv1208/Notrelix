import type { Meta, StoryObj } from "@storybook/react";

import {
  gettingStartedScenario,
  joinScenario,
  onboardingScenario,
  saoMaiDraft,
  SAO_MAI_SUMMARY,
} from "../../verification/workspace-onboarding-fixtures";
import { GettingStartedCard } from "./getting-started-card";
import { OnboardingJoinSurface } from "./onboarding-join-surface";
import { emptyDraft } from "./onboarding-models";
import { OnboardingSurface } from "./onboarding-surface";

/** The onboarding pages at the mockup's 1440 x 900 (Final-Onboarding). */
const meta: Meta = {
  title: "Workspace/Onboarding",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

function Page({ children }: { children: React.ReactNode }) {
  return <div className="h-225 w-360 overflow-hidden">{children}</div>;
}

export const WorkspaceStep: Story = {
  render: () => (
    <Page>
      <OnboardingSurface {...onboardingScenario()} />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Default"],
};

export const WorkspaceStepEmpty: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          draft: emptyDraft(),
          address: { kind: "idle" },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Empty"],
};

export const WorkspaceStepChecking: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({ address: { kind: "checking" } })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Loading"],
};

export const WorkspaceStepTaken: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          address: { kind: "taken", suggestion: "saomai-food" },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Error"],
};

export const WorkspaceStepLocked: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          created: { workspace: true, departments: false },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--ReadOnly"],
};

export const DepartmentsStep: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          step: "departments",
          created: { workspace: true, departments: false },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Default"],
};

export const InviteStep: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          step: "invite",
          created: { workspace: true, departments: true },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Default"],
};

export const InviteStepProblems: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          step: "invite",
          draft: saoMaiDraft({
            invites: [
              {
                id: "row-1",
                email: "khong-phai-email",
                departmentKey: "san-xuat",
                role: "Admin",
              },
              {
                id: "row-2",
                email: "khong-phai-email",
                departmentKey: "ke-toan",
                role: "Member",
              },
            ],
          }),
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Error"],
};

export const BoardStep: Story = {
  render: () => (
    <Page>
      <OnboardingSurface {...onboardingScenario({ step: "board" })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Default"],
};

export const BoardStepSaving: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({ step: "board", pending: true })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Loading"],
};

export const BoardStepUnreachable: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({ step: "board", error: "unreachable" })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Error"],
};

export const Done: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({ step: "done", summary: SAO_MAI_SUMMARY })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--Success"],
};

export const DoneWithoutOptionalSteps: Story = {
  render: () => (
    <Page>
      <OnboardingSurface
        {...onboardingScenario({
          step: "done",
          summary: {
            ...SAO_MAI_SUMMARY,
            departments: [],
            invitesSent: 0,
            board: null,
          },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.setup-flow", "fui-state--EdgeData"],
};

export const JoinDefault: Story = {
  render: () => (
    <Page>
      <OnboardingJoinSurface {...joinScenario()} />
    </Page>
  ),
  tags: ["fui-surface--workspace.onboarding-join", "fui-state--Default"],
};

export const JoinNoDepartment: Story = {
  render: () => (
    <Page>
      <OnboardingJoinSurface {...joinScenario({ spaces: [] })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.onboarding-join", "fui-state--Empty"],
};

export const JoinNameMissing: Story = {
  render: () => (
    <Page>
      <OnboardingJoinSurface
        {...joinScenario({ displayName: "", initials: "?", error: "name" })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.onboarding-join", "fui-state--Error"],
};

export const JoinStarting: Story = {
  render: () => (
    <Page>
      <OnboardingJoinSurface {...joinScenario({ pending: true })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.onboarding-join", "fui-state--Loading"],
};

export const GettingStartedDefault: Story = {
  render: () => (
    <div className="w-180 p-6">
      <GettingStartedCard {...gettingStartedScenario()} />
    </div>
  ),
  tags: ["fui-surface--workspace.getting-started", "fui-state--Default"],
};

export const GettingStartedPartlyDone: Story = {
  render: () => {
    const base = gettingStartedScenario();
    return (
      <div className="w-180 p-6">
        <GettingStartedCard
          {...base}
          items={base.items.map((item, index) =>
            index === 0 ? { ...item, done: true } : item,
          )}
        />
      </div>
    );
  },
  tags: ["fui-surface--workspace.getting-started", "fui-state--Success"],
};
