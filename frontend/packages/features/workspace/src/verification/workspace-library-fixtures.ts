/**
 * Deterministic fixtures for the Boards and Docs directory (WX-DIR-001),
 * including a 320-row workspace for windowing and performance evidence.
 */
import type { LibraryEntry } from "../core/model/library";
import { NAVIGATION_RESOURCE_KIND } from "../core/model/navigation";
import {
  workspaceLibraryCopy,
  type WorkspaceLibraryCopy,
} from "../web/library/workspace-library.copy";
import type { WorkspaceLibrarySurfaceProps } from "../web/library/workspace-library-surface";
import {
  HOME_FIXTURE_NOW,
  homeFormattersFixture,
} from "./workspace-home-fixtures";
import { shellNavigationFixture } from "./workspace-shell-fixtures";
import { spaceTreeFixture } from "./workspace-spaces-fixtures";

const HOUR = 60 * 60 * 1000;

export function libraryCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceLibraryCopy {
  return workspaceLibraryCopy(locale);
}

export function libraryEntry(
  overrides: Partial<LibraryEntry> & Pick<LibraryEntry, "id" | "name">,
): LibraryEntry {
  return {
    kind: NAVIGATION_RESOURCE_KIND.board,
    location: null,
    updatedAt: null,
    pinned: false,
    ...overrides,
  };
}

export function libraryEntriesFixture(): LibraryEntry[] {
  const at = (hours: number) =>
    new Date(HOME_FIXTURE_NOW - hours * HOUR).toISOString();
  return [
    libraryEntry({
      id: "board-roadmap",
      name: "Product Roadmap",
      pinned: true,
      spaceId: "sp-it",
    }),
    libraryEntry({
      id: "board-launch",
      name: "Q4 Launch",
      spaceId: "sp-it-platform",
    }),
    libraryEntry({ id: "board-triage", name: "Bug Triage" }),
    libraryEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      id: "doc-retro",
      name: "Sprint 42 retro",
      location: "Team rituals",
      parentId: "doc-rituals",
      updatedAt: at(3),
      pinned: true,
      spaceId: "sp-accounting",
    }),
    libraryEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      id: "doc-rituals",
      name: "Team rituals",
      updatedAt: at(30),
    }),
    libraryEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      id: "doc-onboarding",
      name: "Onboarding guide",
      updatedAt: at(52),
    }),
    libraryEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      id: "doc-pricing",
      name: "Pricing research notes",
      location: "Onboarding guide",
      parentId: "doc-onboarding",
      updatedAt: at(24 * 9),
    }),
  ];
}

/** `count` entries alternating Boards and Docs (default 320). */
export function libraryManyEntriesFixture(count = 320): LibraryEntry[] {
  return Array.from({ length: count }, (_, index) => {
    const isDoc = index % 3 === 0;
    const number = String(index + 1).padStart(3, "0");
    return libraryEntry({
      kind: isDoc
        ? NAVIGATION_RESOURCE_KIND.doc
        : NAVIGATION_RESOURCE_KIND.board,
      id: `${isDoc ? "doc" : "board"}-${number}`,
      name: `${isDoc ? "Spec" : "Board"} ${number}`,
      location: isDoc ? `Area ${(index % 7) + 1}` : null,
      updatedAt: isDoc
        ? new Date(HOME_FIXTURE_NOW - index * 3 * HOUR).toISOString()
        : null,
      pinned: index % 25 === 0,
    });
  });
}

type LibraryScenario = Omit<
  WorkspaceLibrarySurfaceProps,
  "onCreateBoard" | "onCreateDoc" | "onSetPinned" | "onRetry"
>;

export function libraryDefaultScenario(
  locale: "en" | "vi" = "en",
): LibraryScenario {
  return {
    copy: libraryCopyFixture(locale),
    navigation: shellNavigationFixture(),
    formatters: homeFormattersFixture(locale),
    now: HOME_FIXTURE_NOW,
    workspaceName: "Product",
    state: { status: "ready", data: libraryEntriesFixture() },
    pendingPinKeys: new Set(),
    canCreate: true,
    spaces: spaceTreeFixture(),
    onMoveBoard: () => undefined,
    onMoveDoc: () => undefined,
  };
}

export function libraryLoadingScenario(): LibraryScenario {
  return {
    ...libraryDefaultScenario(),
    workspaceName: null,
    state: { status: "loading" },
  };
}

export function libraryErrorScenario(): LibraryScenario {
  return { ...libraryDefaultScenario(), state: { status: "error" } };
}

export function libraryEmptyScenario(): LibraryScenario {
  return { ...libraryDefaultScenario(), state: { status: "ready", data: [] } };
}

/** A guest: no create actions, nothing shared yet. */
export function libraryReadOnlyScenario(): LibraryScenario {
  return {
    ...libraryDefaultScenario(),
    canCreate: false,
    state: { status: "ready", data: [] },
  };
}

export function libraryPermissionLimitedScenario(): LibraryScenario {
  return { ...libraryDefaultScenario(), canCreate: false };
}

export function libraryHighDensityScenario(): LibraryScenario {
  return {
    ...libraryDefaultScenario(),
    state: { status: "ready", data: libraryManyEntriesFixture() },
    pendingPinKeys: new Set(["work-management.board:board-002"]),
  };
}

export function libraryEdgeDataScenario(): LibraryScenario {
  return {
    ...libraryDefaultScenario("vi"),
    workspaceName: "Phòng Kế hoạch & Vận hành Khu vực Đông Nam Á",
    state: {
      status: "ready",
      data: [
        libraryEntry({
          id: "board-long",
          name: "Lộ trình tích hợp hệ thống thanh toán đa quốc gia cho thị trường Đông Nam Á giai đoạn hai",
          pinned: true,
        }),
        libraryEntry({
          kind: NAVIGATION_RESOURCE_KIND.doc,
          id: "doc-long",
          name: "Biên bản họp: Đánh giá rủi ro vận hành và kế hoạch dự phòng 🚀",
          location: "Tài liệu nội bộ / Vận hành / Quý 4 / Khu vực",
          updatedAt: new Date(HOME_FIXTURE_NOW - 400 * 24 * HOUR).toISOString(),
        }),
        libraryEntry({ id: "board-short", name: "A" }),
      ],
    },
  };
}
