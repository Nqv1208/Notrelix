import type { Meta, StoryObj } from "@storybook/react";
import { HomeSidebarSurface } from "./home-sidebar-surface";
import { HomeSurface } from "./home-surface";
import {
  homeDefaultScenario,
  homeEdgeDataScenario,
  homeEmptyScenario,
  homeSidebarEdgeDataScenario,
  homeSidebarScenario,
  homeUnavailableScenario,
} from "./verification/home-ui-fixtures";
import type { HomeSidebarNavigationRenderer } from "./home-sidebar-surface";

const meta: Meta = {
  title: "Home/Home UI Surfaces",
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-background p-6 text-foreground">
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

const renderNavigation: HomeSidebarNavigationRenderer = (
  _target,
  children,
  props,
) => (
  <a
    href="#"
    className={props.className}
    title={props.title}
    aria-label={props.ariaLabel}
    aria-current={props.ariaCurrent}
  >
    {children}
  </a>
);

export const Default: Story = {
  render: () => <HomeSurface data={homeDefaultScenario()} />,
  tags: ["fui-surface--home.surface", "fui-state--Default"],
};

export const Empty: Story = {
  render: () => <HomeSurface data={homeEmptyScenario()} />,
  tags: ["fui-surface--home.surface", "fui-state--Empty"],
};

export const Unavailable: Story = {
  render: () => <HomeSurface data={homeUnavailableScenario()} />,
  tags: ["fui-surface--home.surface", "fui-state--Unavailable"],
};

export const EdgeData: Story = {
  render: () => <HomeSurface data={homeEdgeDataScenario()} />,
  tags: ["fui-surface--home.surface", "fui-state--EdgeData"],
};

export const SidebarDefault: Story = {
  render: () => (
    <HomeSidebarSurface
      data={homeSidebarScenario()}
      renderNavigation={renderNavigation}
    />
  ),
  tags: ["fui-surface--home.sidebar", "fui-state--Default"],
};

export const SidebarCollapsed: Story = {
  render: () => (
    <HomeSidebarSurface
      data={homeSidebarScenario()}
      collapsed
      renderNavigation={renderNavigation}
    />
  ),
  tags: ["fui-surface--home.sidebar", "fui-state--Default"],
};

export const SidebarEdgeData: Story = {
  render: () => (
    <HomeSidebarSurface
      data={homeSidebarEdgeDataScenario()}
      collapsed
      renderNavigation={renderNavigation}
    />
  ),
  tags: ["fui-surface--home.sidebar", "fui-state--EdgeData"],
};
