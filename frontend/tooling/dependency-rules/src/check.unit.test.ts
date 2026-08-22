import { afterEach, expect, test } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { checkArchitecture } from "./check-frontend-dependencies";
import { checkPackageManifests } from "./check-package-manifests";
import { checkFolderBoundaries } from "./check-folder-boundaries";

let currentRoot: string | null = null;

afterEach(() => {
  if (currentRoot) {
    rmSync(currentRoot, { recursive: true, force: true });
    currentRoot = null;
  }
});

function createFixtureRoot() {
  currentRoot = mkdtempSync(join(tmpdir(), "notrelix-boundary-"));
  return currentRoot;
}

function writePackage(
  root: string,
  packagePath: string,
  packageName: string,
  source: string,
) {
  const dir = join(root, packagePath);
  mkdirSync(join(dir, "src"), { recursive: true });
  writeFileSync(
    join(dir, "package.json"),
    JSON.stringify(
      { name: packageName, version: "0.0.0", type: "module" },
      null,
      2,
    ),
  );
  writeFileSync(join(dir, "src", "index.ts"), source);
}

function runChecker(root: string) {
  const violations = [
    ...checkPackageManifests(root).violations,
    ...checkArchitecture(root).violations,
    ...checkFolderBoundaries(root).violations,
  ];

  return {
    status: violations.length > 0 ? 1 : 0,
    stderr: violations.join("\n"),
  };
}

test("rejects Next.js imports inside shared packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/state",
    "@notrelix/work-management-state",
    'import { useRouter } from "next/navigation";\nexport const useBadRouter = useRouter;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("EXTERNAL_FORBIDDEN");
  expect(result.stderr).toContain("next/navigation");
});

test("rejects direct environment reads inside shared packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/foundation/contracts",
    "@notrelix/contracts",
    "export const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? '/api/v1';\n",
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("DIRECT_ENV_READ");
  expect(result.stderr).toContain("NEXT_PUBLIC_API_URL");
});

test("rejects raw fetch in governed feature source", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/features/search",
    "@notrelix/features-search",
    "export const search = () => fetch('/api/v1/search');\n",
  );

  const result = runChecker(root);
  expect(result.status).toBe(1);
  expect(result.stderr).toContain("FORBIDDEN_BACKEND_FETCH");
});

test("checks apps package boundaries, not only packages directories", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "apps/marketing",
    "@notrelix/app-marketing",
    'import { boardApi } from "@notrelix/work-management-state";\nexport const leaked = boardApi;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("FORBIDDEN");
  expect(result.stderr).toContain("@notrelix/work-management-state");
});

test("rejects React Query imports in package-core layout", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/docs/core",
    "@notrelix/docs-core",
    'import { useQuery } from "@tanstack/react-query";\nexport const leaked = useQuery;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("CORE_IMPURE_IMPORT");
  expect(result.stderr).toContain("@tanstack/react-query");
});

test("rejects browser globals in core source", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/docs/core",
    "@notrelix/docs-core",
    "export const href = window.location.href;\n",
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("CORE_BROWSER_GLOBAL");
  expect(result.stderr).toContain("window");
});

test("rejects data layer toast imports", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/state",
    "@notrelix/work-management-state",
    'import { toast } from "sonner";\nexport const notify = toast;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("DATA_UI_SIDE_EFFECT");
  expect(result.stderr).toContain("sonner");
});

test("rejects data layer manifest declarations of toast libraries", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/state",
    "@notrelix/work-management-state",
    "export const empty = true;\n",
  );
  const pkgDir = join(root, "packages/product/work-management/state");
  writeFileSync(
    join(pkgDir, "package.json"),
    JSON.stringify(
      {
        name: "@notrelix/work-management-state",
        version: "0.0.0",
        type: "module",
        dependencies: { sonner: "^1.0.0" },
      },
      null,
      2,
    ),
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("DECLARED_FORBIDDEN_DEPENDENCY");
  expect(result.stderr).toContain("sonner");
});

test("rejects exported API instances from production source", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/state",
    "@notrelix/work-management-state",
    "function createBoardApi() { return {}; }\nexport const boardApi = createBoardApi();\n",
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("EXPORTED_API_INSTANCE");
  expect(result.stderr).toContain("boardApi");
});

test("rejects react-dom and @notrelix/ui-web imports in mobile packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/mobile",
    "@notrelix/work-management-mobile",
    'import React from "react";\nimport { render } from "react-dom";\nimport { Button } from "@notrelix/ui-web";\nexport const x = 1;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("FORBIDDEN");
  expect(result.stderr).toContain("react-dom");
  expect(result.stderr).toContain("@notrelix/ui-web");
});

test("rejects runtime-web and react-dom subpath imports in mobile packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/mobile",
    "@notrelix/work-management-mobile",
    'import { createRoot } from "react-dom/client";\nimport { createWebRuntime } from "@notrelix/runtime-web";\nexport const leaked = [createRoot, createWebRuntime];\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("react-dom/client");
  expect(result.stderr).toContain("@notrelix/runtime-web");
});

test("rejects intrinsic DOM JSX elements inside mobile package TSX", () => {
  const root = createFixtureRoot();
  const dir = join(root, "packages/product/work-management/mobile");
  mkdirSync(join(dir, "src"), { recursive: true });
  writeFileSync(
    join(dir, "package.json"),
    JSON.stringify(
      {
        name: "@notrelix/work-management-mobile",
        version: "0.0.0",
        type: "module",
      },
      null,
      2,
    ),
  );
  writeFileSync(
    join(dir, "src", "screen.tsx"),
    'import React from "react";\nexport function Screen() { return <div><button>Click</button></div>; }\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("FORBIDDEN_MOBILE_DOM_ELEMENT");
  expect(result.stderr).toContain("div");
  expect(result.stderr).toContain("button");
});

test("passes React Native components and imports in mobile package TSX", () => {
  const root = createFixtureRoot();
  const dir = join(root, "packages/product/work-management/mobile");
  mkdirSync(join(dir, "src"), { recursive: true });
  writeFileSync(
    join(dir, "package.json"),
    JSON.stringify(
      {
        name: "@notrelix/work-management-mobile",
        version: "0.0.0",
        type: "module",
      },
      null,
      2,
    ),
  );
  writeFileSync(
    join(dir, "src", "screen.tsx"),
    'import React from "react";\nimport { View, Text } from "react-native";\nexport function Screen() { return <View><Text>Hello</Text></View>; }\n',
  );

  const result = runChecker(root);

  expect(result.stderr).not.toContain("FORBIDDEN_MOBILE_DOM_ELEMENT");
  expect(result.stderr).not.toContain("react-native");
});

test("applies mobile platform rules to future manifest-registered mobile packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/future/mobile",
    "@notrelix/future-mobile",
    'import { Button } from "@notrelix/ui-web";\nexport const leaked = Button;\n',
  );

  const result = checkArchitecture(root, [
    {
      packageName: "@notrelix/future-mobile",
      relativePath: "packages/product/future/mobile",
      layer: "product-adapter",
      freezeScope: "core-production",
      allowedInternalImports: [],
    },
  ]);

  expect(result.ok).toBe(false);
  expect(result.violations.join("\n")).toContain("@notrelix/ui-web");
});
