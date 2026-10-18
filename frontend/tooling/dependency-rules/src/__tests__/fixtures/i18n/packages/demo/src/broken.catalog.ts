// Fixture: intentionally incomplete catalog for check-i18n-catalogs tests.
export const brokenCatalog = {
  namespace: "broken",
  messages: {
    vi: {
      title: "Tiêu đề",
      onlyVi: "Chỉ tiếng Việt",
      items_other: "{{count}} mục",
    },
    en: { title: "Title", items_other: "{{count}} items" },
  },
};

export const duplicateCatalog = {
  namespace: "broken",
  messages: { vi: {}, en: {} },
};
