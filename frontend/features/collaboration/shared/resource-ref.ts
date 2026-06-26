// ResourceRef standard for cross-resource collaboration.

export type ResourceRef = {
  workspaceId: string
  resourceType: "board" | "item" | "page" | "comment" | "attachment"
  resourceId: string
}
