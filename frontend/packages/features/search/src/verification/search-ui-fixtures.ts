import type { SearchResult, SearchResultType } from "../core/search-model";

export function searchResult(
  overrides: Partial<SearchResult> &
    Pick<SearchResult, "id" | "title" | "type">,
): SearchResult {
  const groupFor = (type: SearchResultType) => {
    switch (type) {
      case "page":
        return "Pages" as const;
      case "block":
        return "Blocks" as const;
      case "task":
        return "Tasks" as const;
      case "board":
        return "Boards" as const;
    }
  };
  return {
    excerpt: overrides.excerpt ?? "Match in workspace content.",
    icon: overrides.icon ?? null,
    score: overrides.score ?? 1,
    group: overrides.group ?? groupFor(overrides.type),
    ...overrides,
  };
}

export function searchResultsDefaultScenario(): SearchResult[] {
  return [
    searchResult({
      id: "page-operating-plan",
      type: "page",
      title: "Operating plan",
      excerpt: "Coordinate launch readiness and evidence capture.",
    }),
    searchResult({
      id: "board-migration",
      type: "board",
      title: "Migration risks",
      excerpt: "Track regional migration risk and owners.",
    }),
    searchResult({
      id: "task-launch-checklist",
      type: "task",
      title: "Prepare launch checklist",
      excerpt: "Publish the customer-facing migration guide.",
    }),
  ];
}

export function searchResultsEmptyScenario(): SearchResult[] {
  return [];
}

export function searchResultsEdgeDataScenario(): SearchResult[] {
  return [
    searchResult({
      id: "page-enterprise-rollout",
      type: "page",
      title:
        "Enterprise rollout readiness checklist with regional localization and audit evidence",
      excerpt:
        "Security, legal, and workspace operations sign-off is required.",
    }),
    searchResult({
      id: "board-enterprise-apac",
      type: "board",
      title: "APAC workspace tenant exceptions",
      excerpt: "Data residency and regional constraints.",
    }),
    searchResult({
      id: "block-sign-off",
      type: "block",
      title: "Sign-off decision log",
      excerpt: "Owner review and audit evidence both pass.",
    }),
  ];
}
