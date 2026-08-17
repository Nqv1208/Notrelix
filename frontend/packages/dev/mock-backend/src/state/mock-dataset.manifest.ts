/**
 * Typed Mock Dataset Manifest
 *
 * Conforms to:
 * - 00-MOCKDATA-SPEC.md §6
 * - 01-MOCKDATA-EXECUTION-PLAN.md §MFD-03
 */

import type { MockDensity } from "../config/mock-config";

export interface MockDensityCardinalities {
  readonly users: number;
  readonly primaryWorkspaceMemberships: number;
  readonly workspacesVisibleToOwner: number;
  readonly boardsInPrimaryWorkspace: number;
  readonly listsPerPrimaryBoard: number;
  readonly cardsPerList: number;
  readonly pagesInPrimaryWorkspace: number;
  readonly blocksPerRepresentativePage: number;
  readonly notificationsPerCurrentActor: number;
}

export const MOCK_DATASET_CARDINALITIES: Record<
  MockDensity,
  MockDensityCardinalities
> = {
  tiny: {
    users: 4,
    primaryWorkspaceMemberships: 4,
    workspacesVisibleToOwner: 1,
    boardsInPrimaryWorkspace: 1,
    listsPerPrimaryBoard: 2,
    cardsPerList: 3,
    pagesInPrimaryWorkspace: 1,
    blocksPerRepresentativePage: 3,
    notificationsPerCurrentActor: 2,
  },
  normal: {
    users: 4,
    primaryWorkspaceMemberships: 4,
    workspacesVisibleToOwner: 2,
    boardsInPrimaryWorkspace: 2,
    listsPerPrimaryBoard: 4,
    cardsPerList: 6,
    pagesInPrimaryWorkspace: 4,
    blocksPerRepresentativePage: 12,
    notificationsPerCurrentActor: 12,
  },
  large: {
    users: 4,
    primaryWorkspaceMemberships: 4,
    workspacesVisibleToOwner: 3,
    boardsInPrimaryWorkspace: 8,
    listsPerPrimaryBoard: 12,
    cardsPerList: 40,
    pagesInPrimaryWorkspace: 25,
    blocksPerRepresentativePage: 80,
    notificationsPerCurrentActor: 100,
  },
  stress: {
    users: 4,
    primaryWorkspaceMemberships: 4,
    workspacesVisibleToOwner: 5,
    boardsInPrimaryWorkspace: 20,
    listsPerPrimaryBoard: 30,
    cardsPerList: 100,
    pagesInPrimaryWorkspace: 100,
    blocksPerRepresentativePage: 250,
    notificationsPerCurrentActor: 500,
  },
};
