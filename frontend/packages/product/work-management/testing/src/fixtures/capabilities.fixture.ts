export type WorkManagementUiCapabilities = {
  readonly canCreateCard: boolean;
  readonly canMoveCard: boolean;
  readonly canRenameGroup: boolean;
  readonly canDelete: boolean;
  readonly canEditFields: boolean;
};

export const ownerCapabilities: WorkManagementUiCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: true,
  canDelete: true,
  canEditFields: true,
};

export const editorCapabilities: WorkManagementUiCapabilities = {
  ...ownerCapabilities,
  canDelete: false,
};

export const viewerCapabilities: WorkManagementUiCapabilities = {
  canCreateCard: false,
  canMoveCard: false,
  canRenameGroup: false,
  canDelete: false,
  canEditFields: false,
};
