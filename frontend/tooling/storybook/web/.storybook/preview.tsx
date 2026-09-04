import type { Preview } from "@storybook/react";
import type { ReactNode } from "react";
import { useEffect, useMemo } from "react";
import { installPureUiNetworkGuard } from "../../../testing/src/pure-ui-network-guard";

// Same global token/styles entry used by the web app.
import "../../../../apps/web/src/styles/globals.css";
// Adds Tailwind v4 source roots for the ui packages + stories.
import "./preview.css";

function PurePreviewShell({ Story }: { Story: () => ReactNode }) {
  const guard = useMemo(() => installPureUiNetworkGuard(), []);

  useEffect(() => () => guard.restore(), [guard]);

  return (
    <div
      data-storybook-preview
      style={{
        background: "var(--background, #ffffff)",
        color: "var(--foreground, #09090b)",
        padding: "24px",
        fontFamily: "var(--font-sans, ui-sans-serif, system-ui, sans-serif)",
      }}
    >
      <Story />
    </div>
  );
}

const preview: Preview = {
  decorators: [(Story) => <PurePreviewShell Story={Story} />],
  parameters: {
    backgrounds: {
      default: "light",
      values: [
        { name: "light", value: "#ffffff" },
        { name: "dark", value: "#18181b" },
      ],
    },
    a11y: {
      disable: true,
      test: "off",
    },
  },
};

export default preview;
