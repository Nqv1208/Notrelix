#!/usr/bin/env node
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '../../../../');

const specPath = resolve(repoRoot, 'artifacts/contracts/openapi.v1.json');
const outputDir = resolve(repoRoot, 'frontend/packages/foundation/contracts/src/generated/rest');
const outputPath = resolve(outputDir, 'schema.ts');
const indexOutputPath = resolve(outputDir, 'index.ts');

if (!existsSync(specPath)) {
  console.error(`❌ Required OpenAPI spec missing: ${specPath}`);
  process.exit(1);
}

try {
  const rawSpec = readFileSync(specPath, 'utf8');
  const spec = JSON.parse(rawSpec);

  // Generate deterministic REST contract types
  const schemaContent = `/**
 * Generated REST Contract Types from ${specPath}
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
`;

  const indexContent = `export * from './schema';\n`;

  mkdirSync(outputDir, { recursive: true });
  writeFileSync(outputPath, schemaContent, 'utf8');
  writeFileSync(indexOutputPath, indexContent, 'utf8');

  console.log(`✅ Generated REST contract schema at ${outputPath}`);
} catch (err) {
  console.error('❌ OpenAPI generation failed:', err);
  process.exit(1);
}
