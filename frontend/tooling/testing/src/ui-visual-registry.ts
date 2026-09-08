import {
  UI_EVIDENCE_THEMES,
  UI_EVIDENCE_VIEWPORTS,
} from "./ui-evidence-schema";

export interface UiEvidenceViewportSize {
  readonly width: number;
  readonly height: number;
}

/**
 * Canonical viewport registry (SPEC §14, FUIR-TST-090).
 * Runners consume these IDs — no ad-hoc viewport numbers in manifest evidence.
 */
export const UI_EVIDENCE_VIEWPORT_SIZES: Record<
  (typeof UI_EVIDENCE_VIEWPORTS)[number],
  UiEvidenceViewportSize
> = {
  mobile: { width: 390, height: 844 },
  tablet: { width: 768, height: 1024 },
  desktop: { width: 1440, height: 900 },
};

export const UI_EVIDENCE_THEME_IDS: readonly string[] = UI_EVIDENCE_THEMES;

export function isUiEvidenceViewportId(
  value: string,
): value is (typeof UI_EVIDENCE_VIEWPORTS)[number] {
  return value in UI_EVIDENCE_VIEWPORT_SIZES;
}

export function isUiEvidenceThemeId(value: string): boolean {
  return (UI_EVIDENCE_THEME_IDS as readonly string[]).includes(value);
}

export function requireViewportSize(viewport: string): UiEvidenceViewportSize {
  if (!isUiEvidenceViewportId(viewport)) {
    throw new Error(`Unknown UI evidence viewport id: ${viewport}`);
  }
  return UI_EVIDENCE_VIEWPORT_SIZES[viewport];
}
