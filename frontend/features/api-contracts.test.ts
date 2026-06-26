import { describe, expect, test } from "bun:test"
import { readdirSync, readFileSync, statSync } from "node:fs"
import { join } from "node:path"

const frontendRoot = new URL("..", import.meta.url).pathname

function sourceFiles(root: string): string[] {
  return readdirSync(root).flatMap((entry) => {
    const path = join(root, entry)
    const stat = statSync(path)
    if (stat.isDirectory()) return sourceFiles(path)
    return /\.(ts|tsx)$/.test(entry) ? [path] : []
  })
}

describe("frontend API contracts", () => {
  test("board runtime services and hooks do not reference mock board mode", () => {
    const files = sourceFiles(join(frontendRoot, "features", "work-management"))
      .filter((file) => !file.includes(`${join("features", "work-management", "mock")}${"/"}`))

    const offenders = files
      .map((file) => ({
        file,
        source: readFileSync(file, "utf8"),
      }))
      .filter(({ source }) =>
        source.includes("NEXT_PUBLIC_USE_MOCK_BOARDS") ||
        source.includes("mockBoardService") ||
        source.includes("mockCardDetailService")
      )
      .map(({ file }) => file.replace(frontendRoot, "frontend/"))

    expect(offenders).toEqual([])
  })

  test("feature hooks import auth-style service modules rather than legacy api files", () => {
    const files = [
      ...sourceFiles(join(frontendRoot, "features", "work-management", "hooks")),
      ...sourceFiles(join(frontendRoot, "features", "workspace", "hooks")),
      ...sourceFiles(join(frontendRoot, "features", "docs", "hooks")),
    ]

    const offenders = files
      .map((file) => ({
        file,
        source: readFileSync(file, "utf8"),
      }))
      .filter(({ source }) =>
        source.includes("../api/boards-api") ||
        source.includes("../api/workspaces-api") ||
        source.includes("../api/pages-api") ||
        source.includes("../api/blocks-api") ||
        source.includes("../mock/")
      )
      .map(({ file }) => file.replace(frontendRoot, "frontend/"))

    expect(offenders).toEqual([])
  })

  test("workspace API modules do not import other feature domains", () => {
    const files = sourceFiles(join(frontendRoot, "features", "workspace", "api"))
      .filter((file) => !file.endsWith(".test.ts"))

    const offenders = files
      .map((file) => ({
        file,
        source: readFileSync(file, "utf8"),
      }))
      .filter(({ source }) =>
        source.includes("@/features/work-management") ||
        source.includes("@/features/docs")
      )
      .map(({ file }) => file.replace(frontendRoot, "frontend/"))

    expect(offenders).toEqual([])
  })

  test("workspace API modules do not hard-code the API version prefix", () => {
    const files = sourceFiles(join(frontendRoot, "features", "workspace", "api"))
      .filter((file) => !file.endsWith(".test.ts"))

    const offenders = files
      .map((file) => ({
        file,
        source: readFileSync(file, "utf8"),
      }))
      .filter(({ source }) =>
        source.includes("\"/api/v1") ||
        source.includes("'/api/v1")
      )
      .map(({ file }) => file.replace(frontendRoot, "frontend/"))

    expect(offenders).toEqual([])
  })

  test("board table runtime uses split api modules and workspaceId route naming", () => {
    const requiredApiFiles = [
      join("boards", "api", "board.api.ts"),
      join("groups", "api", "group.api.ts"),
      join("items", "api", "item.api.ts"),
      join("fields", "api", "field.api.ts"),
      join("items", "api", "item-comments.api.ts"),
    ]

    for (const relPath of requiredApiFiles) {
      expect(() => statSync(join(frontendRoot, "features", "work-management", relPath))).not.toThrow()
    }

    const boardFeatureFiles = [
      ...sourceFiles(join(frontendRoot, "features", "work-management", "boards")),
      ...sourceFiles(join(frontendRoot, "features", "work-management", "items")),
      ...sourceFiles(join(frontendRoot, "features", "work-management", "fields")),
      ...sourceFiles(join(frontendRoot, "features", "work-management", "groups")),
    ]

    const legacyBoardImports = boardFeatureFiles
      .map((file) => ({
        file,
        source: readFileSync(file, "utf8"),
      }))
      .filter(({ source }) => source.includes("board.service") || source.includes("boards-api"))
      .map(({ file }) => file.replace(frontendRoot, "frontend/"))

    expect(legacyBoardImports).toEqual([])

    const workspaceRouteFiles = sourceFiles(join(frontendRoot, "app", "(workspace)", "[workspaceId]"))
    const workspaceSlugOffenders = workspaceRouteFiles
      .map((file) => ({
        file,
        source: readFileSync(file, "utf8"),
      }))
      .filter(({ source }) => source.includes("workspaceSlug"))
      .map(({ file }) => file.replace(frontendRoot, "frontend/"))

    expect(workspaceSlugOffenders).toEqual([])
  })

  test("main table declares TanStack Table as a runtime dependency", () => {
    const pkg = JSON.parse(readFileSync(join(frontendRoot, "package.json"), "utf8")) as {
      dependencies?: Record<string, string>
    }

    expect(pkg.dependencies?.["@tanstack/react-table"]).toBeTruthy()
  })
})
