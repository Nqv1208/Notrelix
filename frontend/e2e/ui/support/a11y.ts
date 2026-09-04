import type { Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

export async function analyzeA11y(page: Page) {
  await page.evaluate(() => {
    Reflect.deleteProperty(window, "axe");
    Reflect.deleteProperty(window, "partialResults");
  });

  return new AxeBuilder({ page }).analyze();
}
