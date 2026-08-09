import type { Preview } from '@storybook/react';

// Same global token/styles entry used by the web app.
import '../../../../apps/web/src/styles/globals.css';
// Adds Tailwind v4 source roots for the ui packages + stories.
import './preview.css';

const preview: Preview = {
  decorators: [
    (Story) => (
      <div
        data-storybook-preview
        style={{
          background: 'var(--background, #ffffff)',
          color: 'var(--foreground, #09090b)',
          padding: '24px',
          fontFamily: 'var(--font-sans, ui-sans-serif, system-ui, sans-serif)',
        }}
      >
        <Story />
      </div>
    ),
  ],
  parameters: {
    backgrounds: {
      default: 'light',
      values: [
        { name: 'light', value: '#ffffff' },
        { name: 'dark', value: '#18181b' },
      ],
    },
    a11y: {
      test: 'error',
    },
  },
};

export default preview;
