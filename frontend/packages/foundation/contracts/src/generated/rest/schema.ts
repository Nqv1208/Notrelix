/**
 * Generated REST Contract Types from /Users/nqvinh/Documents/projects/Notrelix/artifacts/contracts/openapi.v1.json
 * DO NOT EDIT MANUALLY.
 */

export interface paths {
  "/workspaces/{workspaceId}/boards/{boardId}": {
    get: {
      parameters: {
        path: { workspaceId: string; boardId: string };
      };
      responses: {
        200: {
          content: {
            "application/json": {
              id: string;
              workspaceId: string;
              name: string;
              description?: string;
            };
          };
        };
      };
    };
  };
  "/workspaces/{workspaceId}/items": {
    post: {
      parameters: {
        path: { workspaceId: string };
      };
      requestBody: {
        content: {
          "application/json": {
            boardId: string;
            title: string;
            groupId?: string;
          };
        };
      };
      responses: {
        201: {
          content: {
            "application/json": {
              id: string;
              boardId: string;
              title: string;
              sequence?: number;
            };
          };
        };
      };
    };
  };
}

export type operations = {
  getBoardDetail: paths["/workspaces/{workspaceId}/boards/{boardId}"]["get"];
  createBoardItem: paths["/workspaces/{workspaceId}/items"]["post"];
};
