import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * UI freeze gate: Foundation Gallery accessibility scan.
 *
 * Gate: 0 critical + 0 serious axe violations across the gallery and token
 * stories. No blanket rule disable is allowed.
 */

const GALLERY_STORIES = [
  'foundation-gallery--primitives',
  'foundation-gallery--overlays',
  'foundation-gallery--feedback-states',
  'tokens--colors',
  'tokens--typography',
  'tokens--spacing-radius-shadows',
  'tokens--motion-and-semantics',
];

for (const storyId of GALLERY_STORIES) {
  test(`a11y: ${storyId} has no critical or serious violations`, async ({ page }) => {
    await page.goto(`/iframe.html?id=${storyId}`);

    const results = await new AxeBuilder({ page }).analyze();

    const blocking = results.violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    );

    expect(
      blocking.map((violation) => ({
        id: violation.id,
        impact: violation.impact,
        nodes: violation.nodes.map((node) => node.target.join(' ')),
      })),
      `critical/serious axe violations in ${storyId}`,
    ).toEqual([]);
  });
}
