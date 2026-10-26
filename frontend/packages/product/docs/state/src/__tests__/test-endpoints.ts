import type { PageApiEndpoints } from "../api/page.api";

/** The Documents endpoints the unit tests call, as the contracts name them. */
export const testEndpoints: PageApiEndpoints = {
  pages: {
    list: (workspaceId) => `/workspaces/${workspaceId}/pages`,
    detail: (pageId) => `/pages/${pageId}`,
    breadcrumb: (pageId) => `/pages/${pageId}/breadcrumb`,
    blocks: (pageId) => `/pages/${pageId}/blocks`,
    comments: (pageId) => `/pages/${pageId}/comments`,
    history: (pageId) => `/pages/${pageId}/history`,
    search: (workspaceId) => `/workspaces/${workspaceId}/pages/search`,
    moveToSpace: (pageId) => `/pages/${pageId}/move-to-space`,
    archive: (pageId) => `/pages/${pageId}/archive`,
    restore: (pageId) => `/pages/${pageId}/restore`,
    trash: (workspaceId) => `/workspaces/${workspaceId}/pages/trash`,
    backlinks: (pageId) => `/pages/${pageId}/backlinks`,
    versions: (pageId) => `/pages/${pageId}/versions`,
    version: (pageId, versionId) => `/pages/${pageId}/versions/${versionId}`,
    restoreVersion: (pageId, versionId) =>
      `/pages/${pageId}/versions/${versionId}/restore`,
  },
  pageTemplates: {
    list: (workspaceId) => `/workspaces/${workspaceId}/page-templates`,
    createPage: (workspaceId, templateId) =>
      `/workspaces/${workspaceId}/page-templates/${templateId}/pages`,
  },
  resourcePermissions: {
    list: (kind, id) => `/resources/${kind}/${id}/permissions`,
  },
  blocks: {
    detail: (pageId, blockId) => `/pages/${pageId}/blocks/${blockId}`,
    reorder: "/blocks/reorder",
    batch: (pageId) => `/pages/${pageId}/blocks/batch`,
  },
  pageAttachments: {
    list: (pageId) => `/pages/${pageId}/attachments`,
    files: (pageId) => `/pages/${pageId}/attachments/files`,
    content: (pageId, id) => `/pages/${pageId}/attachments/${id}/content`,
    detail: (pageId, id) => `/pages/${pageId}/attachments/${id}`,
  },
  comments: {
    detail: (commentId) => `/comments/${commentId}`,
    resolve: (commentId) => `/comments/${commentId}/resolve`,
  },
  boardItems: {
    create: (boardId) => `/boards/${boardId}/items`,
    linkPage: (itemId) => `/board-items/${itemId}/link-page`,
  },
};
