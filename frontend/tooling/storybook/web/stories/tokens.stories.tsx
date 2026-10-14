import type { Meta, StoryObj } from "@storybook/react";
import type { CSSProperties, ReactNode } from "react";
import {
  ACCENT_IDS,
  CHART_SERIES,
  ELEVATION_ROLES,
  IDENTITY_IDS,
  STATUS_IDS,
  Z_INDEX_ORDER,
  durationMs,
  easing,
  fontSizeScale,
  fonts,
  layout,
  radius,
  radiusRoles,
  sizes,
  space,
  textRoles,
  zIndex,
} from "@notrelix/ui-tokens";

/**
 * Token reference rendered through the generated CSS variables
 * (@notrelix/ui-tokens/css) so every swatch reflects the active appearance
 * and accent. The full appearance × accent matrix lives beside the theme
 * owner: packages/ui/web/src/theme/theme-matrix.stories.tsx.
 */

const mono: CSSProperties = { fontFamily: "var(--font-family-mono)" };

/** Literal class names so Tailwind generates every role utility. */
const ROLE_CLASS: Record<keyof typeof textRoles, string> = {
  display: "text-display",
  "page-title": "text-page-title",
  "section-heading": "text-section-heading",
  subheading: "text-subheading",
  body: "text-body",
  "dense-body": "text-dense-body",
  label: "text-label",
  metadata: "text-metadata tabular-nums",
  caption: "text-caption",
  code: "text-code",
  nav: "text-nav",
  compact: "text-compact",
  "rail-label": "text-rail-label",
  key: "text-key",
  "mark-2xs": "text-mark-2xs",
  "mark-xs": "text-mark-xs",
  "mark-sm": "text-mark-sm",
  "mark-md": "text-mark-md",
  "mark-lg": "text-mark-lg",
  "board-title": "text-board-title",
  "sheet-title": "text-sheet-title",
  crumb: "text-crumb",
  micro: "text-micro",
  "mark-3xs": "text-mark-3xs",
  "avatar-sm": "text-avatar-sm",
  "avatar-md": "text-avatar-md",
  "avatar-xs": "text-avatar-xs",
  hint: "text-hint",
  "calendar-title": "text-calendar-title",
  prose: "text-prose",
  "doc-title": "text-doc-title",
  "doc-heading": "text-doc-heading",
  "doc-prose": "text-doc-prose",
  "doc-list": "text-doc-list",
  "doc-check": "text-doc-check",
  "doc-callout": "text-doc-callout",
  "dialog-title": "text-dialog-title",
  "avatar-lg": "text-avatar-lg",
  "item-title": "text-item-title",
  "item-heading": "text-item-heading",
  "item-prose": "text-item-prose",
  "item-text": "text-item-text",
  "item-comment": "text-item-comment",
  "item-ui": "text-item-ui",
  "item-small": "text-item-small",
  "item-note": "text-item-note",
};

function Swatch({ role }: { role: string }) {
  return (
    <div
      style={{ display: "flex", flexDirection: "column", gap: 4, width: 128 }}
    >
      <div
        style={{
          height: 40,
          borderRadius: "var(--corner-control)",
          border: "1px solid var(--border)",
          background: `var(--${role})`,
        }}
      />
      <code className="text-caption text-muted-foreground" style={mono}>
        --{role}
      </code>
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section style={{ marginBottom: 32 }}>
      <h2
        className="text-section-heading text-foreground"
        style={{ margin: "0 0 12px" }}
      >
        {title}
      </h2>
      <div
        style={{
          display: "flex",
          flexWrap: "wrap",
          gap: 16,
          alignItems: "flex-start",
        }}
      >
        {children}
      </div>
    </section>
  );
}

function Rows({ rows }: { rows: ReadonlyArray<readonly [string, string]> }) {
  return (
    <table
      className="text-dense-body text-foreground"
      style={{ borderCollapse: "collapse", width: "100%", maxWidth: 720 }}
    >
      <tbody>
        {rows.map(([name, value]) => (
          <tr key={name} style={{ borderBottom: "1px solid var(--border)" }}>
            <td style={{ padding: "6px 12px", ...mono }}>{name}</td>
            <td
              style={{ padding: "6px 12px", ...mono }}
              className="text-muted-foreground"
            >
              {value}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

const meta: Meta = {
  title: "Foundation/Tokens",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

export const Colors: Story = {
  render: () => (
    <div style={{ padding: 24 }}>
      <Section title="Surface tiers">
        {[
          "surface-canvas",
          "surface-sidebar",
          "surface-panel",
          "surface-raised",
        ].map((role) => (
          <Swatch key={role} role={role} />
        ))}
      </Section>
      <Section title="Text and lines">
        {["foreground", "muted-foreground", "icon", "border", "input"].map(
          (role) => (
            <Swatch key={role} role={role} />
          ),
        )}
      </Section>
      <Section title="Brand (active accent)">
        {[
          "brand-mark",
          "brand-solid",
          "brand-ink",
          "brand-subtle",
          "focus-ring",
        ].map((role) => (
          <Swatch key={role} role={role} />
        ))}
      </Section>
      <Section title="State">
        {[
          "state-hover",
          "state-selected",
          "state-disabled",
          "state-disabled-foreground",
        ].map((role) => (
          <Swatch key={role} role={role} />
        ))}
      </Section>
      <Section title="Status (stable meaning)">
        {STATUS_IDS.flatMap((id) => [id, `${id}-text`, `${id}-subtle`]).map(
          (role) => (
            <Swatch key={role} role={role} />
          ),
        )}
      </Section>
      <Section title="Identity (marks and charts, never status)">
        {IDENTITY_IDS.map((id) => (
          <Swatch key={id} role={`identity-${id}`} />
        ))}
      </Section>
      <Section title="Chart series">
        {CHART_SERIES.map((_, index) => (
          <Swatch key={index} role={`chart-${index + 1}`} />
        ))}
      </Section>
      <Section title="Accent swatches (picker previews)">
        {ACCENT_IDS.map((id) => (
          <Swatch key={id} role={`accent-swatch-${id}`} />
        ))}
      </Section>
    </div>
  ),
};

export const Typography: Story = {
  render: () => (
    <div
      style={{ padding: 24, display: "flex", flexDirection: "column", gap: 16 }}
    >
      <p className="text-caption text-muted-foreground" style={mono}>
        {fonts.sans}
      </p>
      {Object.entries(textRoles).map(([role, spec]) => (
        <div
          key={role}
          style={{ display: "flex", gap: 24, alignItems: "baseline" }}
        >
          <code
            className="text-caption text-muted-foreground"
            style={{ width: 160, ...mono }}
          >
            text-{role}
          </code>
          <span
            className={`${ROLE_CLASS[role as keyof typeof textRoles]} text-foreground`}
            style={spec.family === "mono" ? mono : undefined}
          >
            Bảng công việc · Board 1,234.50{" "}
            {"tabular" in spec && spec.tabular ? "(tabular)" : ""}
          </span>
        </div>
      ))}
      <Rows
        rows={Object.entries(fontSizeScale).map(([step, rem]) => [
          step,
          `${rem}rem`,
        ])}
      />
      <p className="text-body text-foreground tabular-nums">
        Tabular numerals: 1,111.11 · 8,888.88 · 10/10/2026
      </p>
    </div>
  ),
};

export const SpacingRadiusShadows: Story = {
  render: () => (
    <div style={{ padding: 24 }}>
      <Section title="Spacing roles">
        <Rows rows={Object.entries(space)} />
      </Section>
      <Section title="Sizes and layout">
        <Rows rows={[...Object.entries(sizes), ...Object.entries(layout)]} />
      </Section>
      <Section title="Radius">
        {[...Object.entries(radius), ...Object.entries(radiusRoles)].map(
          ([name, value]) => (
            <div
              key={name}
              style={{ display: "flex", flexDirection: "column", gap: 4 }}
            >
              <div
                style={{
                  width: 72,
                  height: 48,
                  borderRadius: value,
                  background: "var(--muted)",
                  border: "1px solid var(--border)",
                }}
              />
              <code className="text-caption text-muted-foreground" style={mono}>
                {name}
              </code>
            </div>
          ),
        )}
      </Section>
      <Section title="Elevation">
        {ELEVATION_ROLES.map((role) => (
          <div
            key={role}
            className="text-label text-foreground"
            style={{
              width: 160,
              height: 88,
              display: "grid",
              placeItems: "center",
              borderRadius: "var(--corner-surface)",
              background: "var(--surface-raised)",
              boxShadow: `var(--elevation-${role})`,
            }}
          >
            {role}
          </div>
        ))}
      </Section>
    </div>
  ),
};

export const MotionAndSemantics: Story = {
  render: () => (
    <div style={{ padding: 24 }}>
      <Section title="Motion (collapses under prefers-reduced-motion)">
        <Rows
          rows={[
            ...Object.entries(durationMs).map(
              ([name, ms]) => [`duration-${name}`, `${ms}ms`] as const,
            ),
            ...Object.entries(easing).map(
              ([name, curve]) => [`ease-${name}`, curve] as const,
            ),
          ]}
        />
      </Section>
      <Section title="Stacking order">
        <Rows
          rows={Z_INDEX_ORDER.map(
            (role) => [`z-${role}`, String(zIndex[role])] as const,
          )}
        />
      </Section>
    </div>
  ),
};
