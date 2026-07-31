/**
 * Generated from artifacts/contracts/openapi.v1.json
 * DO NOT EDIT.
 */

export interface paths {
  "/workspaces/{workspaceId}/boards/{boardId}": {
    get: {
      parameters: {
              path: {
                "boardId": string;
                "workspaceId": string;
              };
            };
      responses: {
        200: {
          content: {
            "application/json": {
              "description"?: string;
              "id": string;
              "name": string;
              "workspaceId": string;
            };
          };
        };
      };
    };
  };
  "/workspaces/{workspaceId}/items": {
    post: {
      parameters: {
              path: {
                "workspaceId": string;
              };
            };
      requestBody: {
        content: {
          "application/json": {
            "boardId": string;
            "groupId"?: string;
            "title": string;
          };
        };
      };
      responses: {
        201: {
          content: {
            "application/json": {
              "boardId": string;
              "id": string;
              "sequence"?: number;
              "title": string;
            };
          };
        };
      };
    };
  };
}

export interface operations {
  "createBoardItem": paths["/workspaces/{workspaceId}/items"]["post"];
  "getBoardDetail": paths["/workspaces/{workspaceId}/boards/{boardId}"]["get"];
}

export type OperationRequestBody<TOperation extends keyof operations> =
  operations[TOperation] extends { requestBody: { content: { "application/json": infer TBody } } } ? TBody : never;

export type OperationResponse<TOperation extends keyof operations, TStatus extends keyof operations[TOperation]["responses"] = 200 & keyof operations[TOperation]["responses"]> =
  operations[TOperation]["responses"][TStatus] extends { content: { "application/json": infer TResponse } } ? TResponse : never;

export type OperationPathParams<TOperation extends keyof operations> =
  operations[TOperation] extends { parameters: { path: infer TPath } } ? TPath : never;
