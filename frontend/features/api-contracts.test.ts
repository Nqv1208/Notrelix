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
    const files = sourceFiles(join(frontendRoot, "features", "boards"))
      .filter((file) => !file.includes(`${join("features", "boards", "mock")}${"/"}`))

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
      ...sourceFiles(join(frontendRoot, "features", "boards", "hooks")),
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

  test("board table runtime uses split api modules and workspaceId route naming", () => {
    const requiredApiFiles = [
      "board.api.ts",
      "group.api.ts",
      "card.api.ts",
      "column.api.ts",
      "comment.api.ts",
    ]

    for (const file of requiredApiFiles) {
      expect(() => statSync(join(frontendRoot, "features", "boards", "api", file))).not.toThrow()
    }

    const boardFeatureFiles = [
      ...sourceFiles(join(frontendRoot, "features", "boards", "hooks")),
      ...sourceFiles(join(frontendRoot, "features", "boards", "api")),
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
