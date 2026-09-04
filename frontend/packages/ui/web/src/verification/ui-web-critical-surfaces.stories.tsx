import {
  UiWebBrandLogoSurface,
  UiWebDataDisplayPrimitivesSurface,
  UiWebFeedbackStatesSurface,
  UiWebFormControlsSurface,
  UiWebNavigationPrimitivesSurface,
  UiWebOverlayPrimitivesSurface,
  UiWebSubmitStateSurface,
} from "./ui-web-critical-surfaces";

const meta = {
  title: "UI Web/Critical Surfaces",
  parameters: {
    layout: "fullscreen",
    a11y: { disable: true },
  },
};

export default meta;

export const BrandLogo = {
  tags: ["fui-surface--ui-web.brand.logo", "fui-state--Default"],
  render: () => <UiWebBrandLogoSurface />,
};

export const FeedbackStates = {
  tags: ["fui-surface--ui-web.feedback.states", "fui-state--Default"],
  render: () => <UiWebFeedbackStatesSurface />,
};

export const SubmitState = {
  tags: ["fui-surface--ui-web.forms.submit-state", "fui-state--Default"],
  render: () => <UiWebSubmitStateSurface />,
};

export const FormControls = {
  tags: ["fui-surface--ui-web.primitives.form-controls", "fui-state--Default"],
  render: () => <UiWebFormControlsSurface />,
};

export const NavigationPrimitives = {
  tags: ["fui-surface--ui-web.primitives.navigation", "fui-state--Default"],
  render: () => <UiWebNavigationPrimitivesSurface />,
};

export const OverlayPrimitives = {
  tags: ["fui-surface--ui-web.primitives.overlays", "fui-state--Default"],
  render: () => <UiWebOverlayPrimitivesSurface />,
};

export const DataDisplayPrimitives = {
  tags: ["fui-surface--ui-web.primitives.data-display", "fui-state--Default"],
  render: () => <UiWebDataDisplayPrimitivesSurface />,
};
