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
  comments: { detail: (commentId) => `/comments/${commentId}` },
  boardItems: {
    create: (boardId) => `/boards/${boardId}/items`,
    linkPage: (itemId) => `/board-items/${itemId}/link-page`,
  },
};
