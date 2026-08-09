import type { Meta, StoryObj } from '@storybook/react';
import {
  primitive,
  brand,
  semantic,
  surface,
  gradients,
  badge,
  badgeDark,
  fonts,
  weights,
  typeScale,
  baseUnit,
  spacing,
  layout,
  grid,
  radius,
  shadows,
  duration,
  easing,
  surfaces,
  tableSurface,
  focusRing,
  lightTheme,
  darkTheme,
} from '@notrelix/ui-tokens';

function Swatch({ name, value }: { name: string; value: string }) {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '4px',
        width: '112px',
      }}
    >
      <div
        style={{
          height: '48px',
          borderRadius: '8px',
          border: '1px solid rgba(0,0,0,0.12)',
          background: value,
        }}
      />
      <span style={{ fontSize: '12px', fontWeight: 600 }}>{name}</span>
      <code style={{ fontSize: '11px', color: '#71717a' }}>{value}</code>
    </div>
  );
}

function Section({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section style={{ marginBottom: '40px' }}>
      <h2 style={{ margin: '0 0 16px', fontSize: '18px' }}>{title}</h2>
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: '16px',
          alignItems: 'flex-start',
        }}
      >
        {children}
      </div>
    </section>
  );
}

function TokenTable({
  rows,
}: {
  rows: { name: string; value: string }[];
}) {
  return (
    <table
      style={{
        width: '100%',
        maxWidth: '640px',
        borderCollapse: 'collapse',
        fontSize: '13px',
      }}
    >
      <thead>
        <tr>
          <th style={thStyle}>Token</th>
          <th style={thStyle}>Value</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row.name}>
            <td style={tdStyle}>
              <code>{row.name}</code>
            </td>
            <td style={tdStyle}>
              <code>{row.value}</code>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '6px 12px',
  borderBottom: '1px solid rgba(0,0,0,0.12)',
  fontWeight: 600,
};
const tdStyle: React.CSSProperties = {
  padding: '6px 12px',
  borderBottom: '1px solid rgba(0,0,0,0.08)',
  verticalAlign: 'top',
};

const meta: Meta = {
  title: 'Foundation/Tokens',
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj;

export const Colors: Story = {
  render: () => (
    <div style={{ padding: '24px' }}>
      <Section title="Primitive">
        {Object.entries(primitive).map(([name, value]) => (
          <Swatch key={name} name={name} value={value} />
        ))}
      </Section>
      <Section title="Brand">
        {Object.entries(brand).map(([name, value]) => (
          <Swatch key={name} name={name} value={value} />
        ))}
      </Section>
      <Section title="Semantic">
        {Object.entries(semantic).map(([name, value]) => (
          <Swatch key={name} name={name} value={value} />
        ))}
      </Section>
      <Section title="Surface">
        {Object.entries(surface).map(([name, value]) => (
          <Swatch key={name} name={name} value={value} />
        ))}
      </Section>
      <Section title="Gradients">
        {Object.entries(gradients).map(([name, value]) => (
          <Swatch key={name} name={name} value={value} />
        ))}
      </Section>
      <Section title="Badge (light)">
        {Object.entries(badge).map(([name, value]) => (
          <Swatch key={name} name={name} value={value.bg} />
        ))}
      </Section>
      <Section title="Badge (dark)">
        {Object.entries(badgeDark).map(([name, value]) => (
          <Swatch key={name} name={name} value={value.bg} />
        ))}
      </Section>
    </div>
  ),
};

export const Typography: Story = {
  render: () => (
    <div style={{ padding: '24px' }}>
      <Section title="Fonts & Weights">
        {Object.entries(fonts).map(([name, family]) => (
          <div key={name} style={{ marginBottom: '8px' }}>
            <code>{name}</code> — <span style={{ fontFamily: family }}>{family}</span>
          </div>
        ))}
        {Object.entries(weights).map(([name, weight]) => (
          <div key={name} style={{ marginBottom: '8px' }}>
            <code>{name}</code> ({weight}) —{' '}
            <span style={{ fontWeight: weight }}>Aa 0123456789</span>
          </div>
        ))}
      </Section>
      <Section title="Type Scale">
        {Object.entries(typeScale).map(([name, token]) => (
          <div
            key={name}
            style={{
              marginBottom: '12px',
              fontFamily: token.fontFamily,
              fontSize: token.size,
              lineHeight: token.lineHeight,
              letterSpacing: token.letterSpacing,
              fontWeight: token.fontWeight,
            }}
          >
            <span style={{ color: '#71717a' }}>{name} / </span>
            The quick brown fox jumps over the lazy dog
          </div>
        ))}
      </Section>
    </div>
  ),
};

export const SpacingRadiusShadows: Story = {
  render: () => (
    <div style={{ padding: '24px' }}>
      <Section title={`Spacing (base unit ${baseUnit}px)`}>
        {Object.entries(spacing).map(([name, value]) => (
          <div key={name} style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <code style={{ width: '48px' }}>{name}</code>
            <div
              style={{
                width: '96px',
                height: '16px',
                background: brand.violet,
                opacity: 0.8,
              }}
            />
            <span style={{ fontSize: '12px', color: '#71717a' }}>{value}</span>
          </div>
        ))}
      </Section>
      <Section title="Radius">
        {Object.entries(radius).map(([name, value]) => (
          <div
            key={name}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
              fontSize: '13px',
            }}
          >
            <code style={{ width: '96px' }}>{name}</code>
            <div
              style={{
                width: '48px',
                height: '48px',
                border: '1px solid rgba(0,0,0,0.2)',
                borderRadius: value,
              }}
            />
            <span>{value}</span>
          </div>
        ))}
      </Section>
      <Section title="Shadows">
        {Object.entries(shadows).map(([name, value]) => (
          <div
            key={name}
            style={{
              width: '120px',
              height: '64px',
              borderRadius: '8px',
              background: '#ffffff',
              boxShadow: value,
              display: 'flex',
              alignItems: 'flex-end',
              padding: '6px',
              fontSize: '11px',
            }}
          >
            <code>{name}</code>
          </div>
        ))}
      </Section>
    </div>
  ),
};

export const MotionAndSemantics: Story = {
  render: () => (
    <div style={{ padding: '24px' }}>
      <Section title="Duration">
        {Object.entries(duration).map(([name, value]) => (
          <div key={name} style={{ fontSize: '13px' }}>
            <code>{name}</code> — {value}
          </div>
        ))}
      </Section>
      <Section title="Easing">
        {Object.entries(easing).map(([name, value]) => (
          <div key={name} style={{ fontSize: '13px' }}>
            <code>{name}</code> — {value}
          </div>
        ))}
      </Section>
      <Section title="Surfaces">
        <TokenTable
          rows={Object.entries(surfaces).map(([name, value]) => ({
            name,
            value: `light: ${value.light} / dark: ${value.dark}`,
          }))}
        />
      </Section>
      <Section title="Table Surface">
        <TokenTable rows={Object.entries(tableSurface).map(([name, value]) => ({ name, value }))} />
      </Section>
      <Section title="Focus Ring">
        <TokenTable
          rows={Object.entries(focusRing).map(([name, value]) => ({ name, value: String(value) }))}
        />
      </Section>
      <Section title="Layout & Grid">
        <TokenTable
          rows={[
            ...Object.entries(layout).map(([name, value]) => ({ name, value: String(value) })),
            ...Object.entries(grid).map(([name, value]) => ({
              name: `grid.${name}`,
              value: JSON.stringify(value),
            })),
          ]}
        />
      </Section>
      <Section title="Themes">
        <TokenTable
          rows={[
            { name: 'lightTheme', value: JSON.stringify(lightTheme) },
            { name: 'darkTheme', value: JSON.stringify(darkTheme) },
          ]}
        />
      </Section>
    </div>
  ),
};
