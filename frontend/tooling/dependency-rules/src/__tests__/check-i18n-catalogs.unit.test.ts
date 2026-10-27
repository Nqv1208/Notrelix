import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  checkI18nCatalogs,
  discoverCatalogFiles,
} from "../check-i18n-catalogs";

const frontendRoot = resolve(__dirname, "../../../..");
const fixtureRoot = resolve(__dirname, "fixtures/i18n");

describe("check-i18n-catalogs", () => {
  it("passes on the repository catalogs and discovers at least one", async () => {
    const result = await checkI18nCatalogs(frontendRoot);
    expect(result.issues).toEqual([]);
    expect(result.catalogs).toBeGreaterThan(0);
    expect(result.keys).toBeGreaterThan(0);
  }, 30_000); // Loads every catalog in the repository; slow on a loaded machine.

  it("discovers catalogs only under src/ (never the fixtures of this test)", () => {
    const files = discoverCatalogFiles(frontendRoot);
    expect(
      files.some((file) =>
        file.includes(
          "packages/features/account/src/web/appearance/account-appearance.catalog.ts",
        ),
      ),
    ).toBe(true);
    expect(files.every((file) => !file.includes("/fixtures/"))).toBe(true);
  });

  it("fails on missing keys, missing plural forms and duplicate namespaces", async () => {
    const result = await checkI18nCatalogs(fixtureRoot);
    expect(result.ok).toBe(false);
    expect(result.issues).toEqual(
      expect.arrayContaining([
        expect.stringContaining('en: missing-key "onlyVi"'),
        expect.stringContaining('en: missing-plural-form "items_one"'),
        expect.stringContaining(
          'namespace "broken" is declared by more than one catalog',
        ),
      ]),
    );
  });
});
