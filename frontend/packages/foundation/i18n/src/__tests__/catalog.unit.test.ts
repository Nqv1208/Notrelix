import { describe, expect, it } from "vitest";
import {
  defineCatalog,
  findCatalogIssues,
  findDuplicateNamespaces,
  flattenMessages,
} from "../catalog";

describe("catalog completeness", () => {
  it("accepts a complete catalog with locale-specific plural forms", () => {
    const catalog = defineCatalog("demo", {
      vi: { title: "Tiêu đề", items_other: "{{count}} mục" },
      en: {
        title: "Title",
        items_one: "{{count}} item",
        items_other: "{{count}} items",
      },
    });
    expect(findCatalogIssues(catalog)).toEqual([]);
  });

  it("reports keys missing in either locale", () => {
    const catalog = defineCatalog("demo", {
      vi: { a: "A", nested: { b: "B" } },
      en: { a: "A", extra: "X" },
    });
    expect(findCatalogIssues(catalog)).toEqual([
      { namespace: "demo", locale: "vi", key: "extra", kind: "missing-key" },
      { namespace: "demo", locale: "en", key: "nested.b", kind: "missing-key" },
    ]);
  });

  it("requires every CLDR plural category per locale", () => {
    const catalog = defineCatalog("demo", {
      vi: { items_other: "{{count}} mục" },
      en: { items_other: "{{count}} items" },
    });
    expect(findCatalogIssues(catalog)).toEqual([
      {
        namespace: "demo",
        locale: "en",
        key: "items_one",
        kind: "missing-plural-form",
      },
    ]);
  });

  it("reports empty messages", () => {
    const catalog = defineCatalog("demo", {
      vi: { a: " " },
      en: { a: "A" },
    });
    expect(findCatalogIssues(catalog)).toEqual([
      { namespace: "demo", locale: "vi", key: "a", kind: "empty-message" },
    ]);
  });

  it("flattens nested messages and finds duplicate namespaces", () => {
    expect(flattenMessages({ a: { b: { c: "x" } }, d: "y" })).toEqual({
      "a.b.c": "x",
      d: "y",
    });
    const a = defineCatalog("x", { vi: {}, en: {} });
    const b = defineCatalog("x", { vi: {}, en: {} });
    expect(findDuplicateNamespaces([a, b])).toEqual(["x"]);
  });
});
