import { vi } from "vitest";
import type { WorkspaceSpaceSurfaceProps } from "../workspace-space-surface";

type Paging = NonNullable<WorkspaceSpaceSurfaceProps["membersPaging"]>;

/** A paging handle with spies; each test says only what differs. */
export function pagingOf(overrides: Partial<Paging> = {}): Paging {
  return {
    hasMore: false,
    loadingMore: false,
    loadMoreFailed: false,
    onLoadMore: vi.fn(),
    onFilterChange: vi.fn(),
    ...overrides,
  };
}

export const DEFAULT_MEMBERS_PAGING = pagingOf();
