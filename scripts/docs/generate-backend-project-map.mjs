#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const GENERATED_PATH = "backend/docs/generated/project-map.md";
const SOLUTION_PATH = "backend/backend.slnx";
const GENERATOR_PATH = "scripts/docs/generate-backend-project-map.mjs";
const GENERATOR_COMMAND = `node ${GENERATOR_PATH}`;

const args = new Set(process.argv.slice(2));
const checkMode = args.has("--check");
const stdoutMode = args.has("--stdout");

const unknownArgs = [...args].filter(
  (arg) => !["--check", "--stdout"].includes(arg),
);

if (unknownArgs.length > 0) {
  fail(`Unknown argument(s): ${unknownArgs.join(", ")}`);
}

const repoRoot = findRepoRoot(process.cwd());

function fail(message) {
  console.error(`[backend-project-map] ${message}`);
  process.exit(1);
}

function toPosix(value) {
  return value.replaceAll("\\", "/");
}

function relFromRoot(absolutePath) {
  return toPosix(path.relative(repoRoot, absolutePath));
}

function readRequired(relativePath) {
  const absolutePath = path.join(repoRoot, relativePath);

  if (!fs.existsSync(absolutePath)) {
    fail(`Required source does not exist: ${relativePath}`);
  }

  return fs.readFileSync(absolutePath, "utf8");
}

function findRepoRoot(startDirectory) {
  let current = path.resolve(startDirectory);

  while (true) {
    if (
      fs.existsSync(path.join(current, "backend", "backend.slnx")) &&
      fs.existsSync(path.join(current, "scripts"))
    ) {
      return current;
    }

    const parent = path.dirname(current);

    if (parent === current) {
      console.error(
        "[backend-project-map] Could not locate repository root. " +
          "Expected backend/backend.slnx and scripts/.",
      );

      process.exit(1);
    }

    current = parent;
  }
}

function parseAttributes(attributeText) {
  const attributes = new Map();

  const attributePattern =
    /([A-Za-z_][A-Za-z0-9_.:-]*)\s*=\s*"([^"]*)"/g;

  for (const match of attributeText.matchAll(attributePattern)) {
    attributes.set(match[1], match[2]);
  }

  return attributes;
}

function parseSolution(solutionXml) {
  const lines = solutionXml.split(/\r?\n/);

  const projects = [];

  let sectionHint = null;

  for (const line of lines) {
    const commentMatch = line.match(/<!--\s*(.*?)\s*-->/);

    if (commentMatch) {
      const comment = commentMatch[1].trim().toLowerCase();

      if (comment === "production") {
        sectionHint = "production";
      } else if (comment === "test projects") {
        sectionHint = "test";
      } else if (comment === "testing support libraries") {
        sectionHint = "test-support";
      }

      continue;
    }

    const projectMatch = line.match(/<Project\b([^>]*)\/>/);

    if (!projectMatch) {
      continue;
    }

    const attributes = parseAttributes(projectMatch[1]);

    const projectPath = attributes.get("Path");

    if (!projectPath) {
      fail(
        `Solution contains <Project> without Path: ${line.trim()}`,
      );
    }

    projects.push({
      relativeFromBackend: toPosix(projectPath),
      sectionHint,
    });
  }

  if (projects.length === 0) {
    fail(`${SOLUTION_PATH} contains no project entries.`);
  }

  const duplicatePaths = duplicates(
    projects.map((project) => project.relativeFromBackend),
  );

  if (duplicatePaths.length > 0) {
    fail(
      `Duplicate project path(s) in ${SOLUTION_PATH}: ` +
        duplicatePaths.join(", "),
    );
  }

  return projects;
}

function parseProject(project) {
  const repoRelativePath = toPosix(
    path.posix.join(
      "backend",
      project.relativeFromBackend,
    ),
  );

  const absolutePath = path.join(
    repoRoot,
    repoRelativePath,
  );

  if (!fs.existsSync(absolutePath)) {
    fail(
      `Solution project does not exist: ${repoRelativePath}`,
    );
  }

  const xml = fs.readFileSync(
    absolutePath,
    "utf8",
  );

  const projectTag = xml.match(
    /<Project\b([^>]*)>/,
  );

  if (!projectTag) {
    fail(
      `Could not parse <Project> element: ${repoRelativePath}`,
    );
  }

  const projectAttributes = parseAttributes(
    projectTag[1],
  );

  const sdk =
    projectAttributes.get("Sdk") ?? "unknown";

  const rootNamespace =
    firstElementValue(xml, "RootNamespace") ??
    projectNameFromPath(repoRelativePath);

  const isTestProject = normalizeBoolean(
    firstElementValue(xml, "IsTestProject"),
  );

  const type = classifyProjectType(
    repoRelativePath,
    isTestProject,
  );

  if (
    project.sectionHint &&
    project.sectionHint !== type
  ) {
    fail(
      `${SOLUTION_PATH} section hint classifies ` +
        `${repoRelativePath} as ${project.sectionHint}, ` +
        `but path/IsTestProject classify it as ${type}. ` +
        `Fix the solution grouping or project manifest.`,
    );
  }

  const projectDirectory =
    path.dirname(absolutePath);

  const projectReferences = [
    ...xml.matchAll(
      /<ProjectReference\b([^>]*)\/?>/g,
    ),
  ].map((match) => {
    const attributes = parseAttributes(
      match[1],
    );

    const include =
      attributes.get("Include");

    if (!include) {
      fail(
        `ProjectReference without Include in ` +
          repoRelativePath,
      );
    }

    const absoluteReference =
      path.resolve(
        projectDirectory,
        include.replaceAll(
          "\\",
          path.sep,
        ),
      );

    const referenceRelative =
      relFromRoot(absoluteReference);

    if (!fs.existsSync(absoluteReference)) {
      fail(
        `${repoRelativePath} references missing project: ` +
          referenceRelative,
      );
    }

    return {
      path: referenceRelative,
      name: projectNameFromPath(
        referenceRelative,
      ),
    };
  });

  const packageReferences = [
    ...xml.matchAll(
      /<PackageReference\b([^>]*)/g,
    ),
  ].map((match) => {
    const attributes = parseAttributes(
      match[1],
    );

    const include =
      attributes.get("Include");

    if (!include) {
      fail(
        `PackageReference without Include in ` +
          repoRelativePath,
      );
    }

    return include;
  });

  const internalsVisibleTo = [
    ...xml.matchAll(
      /<InternalsVisibleTo\b([^>]*)\/?>/g,
    ),
  ].map((match) => {
    const attributes =
      parseAttributes(match[1]);

    const include =
      attributes.get("Include");

    if (!include) {
      fail(
        `InternalsVisibleTo without Include in ` +
          repoRelativePath,
      );
    }

    return include;
  });

  return {
    ...project,

    type,

    path: repoRelativePath,

    name: projectNameFromPath(
      repoRelativePath,
    ),

    sdk,

    rootNamespace,

    projectReferences: unique(
      projectReferences,
      (reference) => reference.path,
    ),

    packageReferences: [
      ...new Set(packageReferences),
    ].sort((a, b) => a.localeCompare(b)),

    internalsVisibleTo: [
      ...new Set(internalsVisibleTo),
    ].sort((a, b) => a.localeCompare(b)),
  };
}

function classifyProjectType(
  repoRelativePath,
  isTestProject,
) {
  if (
    repoRelativePath.startsWith(
      "backend/src/",
    )
  ) {
    if (isTestProject === true) {
      fail(
        `Production source project declares ` +
          `IsTestProject=true: ${repoRelativePath}`,
      );
    }

    return "production";
  }

  if (
    repoRelativePath.startsWith(
      "backend/tests/",
    )
  ) {
    return isTestProject === true
      ? "test"
      : "test-support";
  }

  fail(
    `Cannot derive project type for ` +
      `${repoRelativePath}. ` +
      "Expected a project under " +
      "backend/src/ or backend/tests/.",
  );
}

function firstElementValue(
  xml,
  elementName,
) {
  const escaped =
    elementName.replace(
      /[.*+?^${}()|[\]\\]/g,
      "\\$&",
    );

  const pattern = new RegExp(
    `<${escaped}>([^<]*)</${escaped}>`,
  );

  const match = xml.match(pattern);

  return match
    ? match[1].trim()
    : null;
}

function normalizeBoolean(value) {
  if (value == null) {
    return null;
  }

  if (
    value.toLowerCase() === "true"
  ) {
    return true;
  }

  if (
    value.toLowerCase() === "false"
  ) {
    return false;
  }

  return null;
}

function projectNameFromPath(projectPath) {
  return path.posix.basename(
    toPosix(projectPath),
    ".csproj",
  );
}

function unique(
  values,
  keySelector = (value) => value,
) {
  const seen = new Set();
  const result = [];

  for (const value of values) {
    const key = keySelector(value);

    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    result.push(value);
  }

  return result;
}

function duplicates(values) {
  const counts = new Map();

  for (const value of values) {
    counts.set(
      value,
      (counts.get(value) ?? 0) + 1,
    );
  }

  return [...counts.entries()]
    .filter(
      ([, count]) => count > 1,
    )
    .map(([value]) => value);
}

function validateReferenceClosure(projects) {
  const solutionPaths = new Set(
    projects.map(
      (project) => project.path,
    ),
  );

  const solutionNames = new Set(
    projects.map(
      (project) => project.name,
    ),
  );

  const duplicateNames = duplicates(
    projects.map(
      (project) => project.name,
    ),
  );

  if (duplicateNames.length > 0) {
    fail(
      `Duplicate project name(s) in solution: ` +
        duplicateNames.join(", "),
    );
  }

  for (const project of projects) {
    for (
      const reference
      of project.projectReferences
    ) {
      if (
        !solutionPaths.has(
          reference.path,
        )
      ) {
        fail(
          `${project.path} references ` +
            `${reference.path}, but that project ` +
            `is not listed in ${SOLUTION_PATH}.`,
        );
      }

      if (
        !solutionNames.has(
          reference.name,
        )
      ) {
        fail(
          `Reference name resolution failed for ` +
            reference.path,
        );
      }
    }
  }
}

function testRelationship(
  project,
  projects,
) {
  const tests = projects.filter(
    (candidate) =>
      candidate.type === "test",
  );

  if (project.type === "test") {
    const productionTargets =
      project.projectReferences
        .map((reference) =>
          projects.find(
            (candidate) =>
              candidate.path ===
              reference.path,
          ),
        )
        .filter(
          (candidate) =>
            candidate?.type ===
            "production",
        )
        .map(
          (candidate) =>
            candidate.name,
        );

    return productionTargets.length > 0
      ? `Directly exercises: ${
          productionTargets
            .map(code)
            .join(", ")
        }`
      : "No direct production project reference";
  }

  const incomingTests = tests
    .filter((testProject) =>
      testProject.projectReferences.some(
        (reference) =>
          reference.path === project.path,
      ),
    )
    .map(
      (testProject) =>
        testProject.name,
    );

  if (incomingTests.length === 0) {
    return project.type === "test-support"
      ? "No direct test-project consumer"
      : "No direct test-project reference";
  }

  return `Directly referenced by: ${
    incomingTests
      .map(code)
      .join(", ")
  }`;
}

function formatReferences(project) {
  if (
    project.projectReferences.length === 0
  ) {
    return "—";
  }

  return project.projectReferences
    .map(
      (reference) =>
        code(reference.name),
    )
    .join("<br>");
}

function formatPackages(project) {
  if (
    project.packageReferences.length === 0
  ) {
    return "—";
  }

  return project.packageReferences
    .map(code)
    .join("<br>");
}

function formatInternals(project) {
  if (
    project.internalsVisibleTo.length === 0
  ) {
    return "—";
  }

  return project.internalsVisibleTo
    .map(code)
    .join("<br>");
}

function code(value) {
  return `\`${String(value).replaceAll(
    "`",
    "\\`",
  )}\``;
}

function typeLabel(type) {
  if (type === "production") {
    return "Production";
  }

  if (type === "test") {
    return "Test";
  }

  if (type === "test-support") {
    return "Testing support";
  }

  fail(`Unknown project type: ${type}`);
}

function renderProjectTable(
  projects,
  type,
) {
  const rows = projects.filter(
    (project) =>
      project.type === type,
  );

  const lines = [
    "| Project | SDK | Project references | Package references | Test relationship |",
    "|---|---|---|---|---|",
  ];

  for (const project of rows) {
    lines.push(
      `| ${code(project.name)} | ` +
        `${code(project.sdk)} | ` +
        `${formatReferences(project)} | ` +
        `${formatPackages(project)} | ` +
        `${testRelationship(
          project,
          projects,
        )} |`,
    );
  }

  return lines.join("\n");
}

function renderTestSupportTable(projects) {
  const rows = projects.filter(
    (project) =>
      project.type === "test-support",
  );

  const lines = [
    "| Project | SDK | Project references | Package references | Used directly by tests |",
    "|---|---|---|---|---|",
  ];

  for (const project of rows) {
    const incomingTests = projects
      .filter(
        (candidate) =>
          candidate.type === "test",
      )
      .filter((candidate) =>
        candidate.projectReferences.some(
          (reference) =>
            reference.path ===
            project.path,
        ),
      )
      .map(
        (candidate) =>
          candidate.name,
      );

    lines.push(
      `| ${code(project.name)} | ` +
        `${code(project.sdk)} | ` +
        `${formatReferences(project)} | ` +
        `${formatPackages(project)} | ` +
        `${
          incomingTests.length > 0
            ? incomingTests
                .map(code)
                .join("<br>")
            : "—"
        } |`,
    );
  }

  return lines.join("\n");
}

function renderDependencyEdges(projects) {
  const edges = [];

  for (const project of projects) {
    for (
      const reference
      of project.projectReferences
    ) {
      edges.push({
        from: project.name,
        to: reference.name,
        fromType: project.type,
      });
    }
  }

  if (edges.length === 0) {
    return "No direct project-reference edges.";
  }

  return [
    "| From | Type | Directly references |",
    "|---|---|---|",

    ...edges.map(
      (edge) =>
        `| ${code(edge.from)} | ` +
        `${typeLabel(edge.fromType)} | ` +
        `${code(edge.to)} |`,
    ),
  ].join("\n");
}

function renderProductionTestMatrix(
  projects,
) {
  const production = projects.filter(
    (project) =>
      project.type === "production",
  );

  const tests = projects.filter(
    (project) =>
      project.type === "test",
  );

  const lines = [
    "| Production project | Direct test-project relationships |",
    "|---|---|",
  ];

  for (
    const productionProject
    of production
  ) {
    const related = tests
      .filter((testProject) =>
        testProject.projectReferences.some(
          (reference) =>
            reference.path ===
            productionProject.path,
        ),
      )
      .map(
        (testProject) =>
          testProject.name,
      );

    lines.push(
      `| ${code(productionProject.name)} | ` +
        `${
          related.length > 0
            ? related
                .map(code)
                .join("<br>")
            : "—"
        } |`,
    );
  }

  return lines.join("\n");
}

function renderInternalsVisibleTo(
  projects,
) {
  const rows = projects.filter(
    (project) =>
      project.internalsVisibleTo.length > 0,
  );

  if (rows.length === 0) {
    return "No `InternalsVisibleTo` declarations were found.";
  }

  return [
    "| Project | `InternalsVisibleTo` |",
    "|---|---|",

    ...rows.map(
      (project) =>
        `| ${code(project.name)} | ` +
        `${formatInternals(project)} |`,
    ),
  ].join("\n");
}

function renderDocument(projects) {
  const productionCount =
    projects.filter(
      (project) =>
        project.type === "production",
    ).length;

  const testCount =
    projects.filter(
      (project) =>
        project.type === "test",
    ).length;

  const supportCount =
    projects.filter(
      (project) =>
        project.type === "test-support",
    ).length;

  const sourceProjectPaths =
    projects.map(
      (project) => project.path,
    );

  return `---
document_id: BE-GEN-PROJECT-MAP
document_type: generated
status: generated
owner: backend-architecture
applies_to:
  - backend-project-inventory
  - backend-project-references
  - backend-test-project-relationships
evidence:
  - ${SOLUTION_PATH}
  - backend/src/**/*.csproj
  - backend/tests/**/*.csproj
review_on:
  - generated
---

# Backend Project Map

> **GENERATED FILE — DO NOT EDIT.**
>
> Source of truth: ${code(
    SOLUTION_PATH,
  )} and the ${code(
    ".csproj",
  )} files listed by that solution.
>
> Regenerate:
>
> ${code(GENERATOR_COMMAND)}
>
> Check drift without writing:
>
> ${code(
    `${GENERATOR_COMMAND} --check`,
  )}

This file is a **source-derived inventory**, not normative architecture.

For project roles, allowed dependency direction, bounded-context placement, and rules for adding a production project, read:

- ${code(
    "backend/docs/architecture/backend-overview.md",
  )}
- ${code(
    "backend/docs/architecture/testing-and-quality-gates.md",
  )}

## Generated summary

| Type | Count |
|---|---:|
| Production | ${productionCount} |
| Test | ${testCount} |
| Testing support | ${supportCount} |
| **Total** | **${projects.length}** |

The generator derives project type from source location plus ${code(
    "<IsTestProject>",
  )} and uses recognized solution comments only as consistency hints. It fails if a project cannot be classified, if a solution hint conflicts with the project manifest, if a referenced project is missing, or if a direct ${code(
    "ProjectReference",
  )} points to a project outside the solution inventory.

## Production projects

${renderProjectTable(
  projects,
  "production",
)}

## Test projects

${renderProjectTable(
  projects,
  "test",
)}

## Testing support projects

${renderTestSupportTable(projects)}

## Production-to-test relationship

This matrix is derived only from **direct** ${code(
    "ProjectReference",
  )} edges from test projects.

It does not claim that every test project proves every behavior in the referenced production assembly.

${renderProductionTestMatrix(projects)}

## Direct project-reference edges

${renderDependencyEdges(projects)}

## Explicit internal test seams

${renderInternalsVisibleTo(projects)}

${rowsForSourceInputs(
  sourceProjectPaths,
)}

## Generation contract

The generator MUST derive this document from the solution and project manifests.

It MUST NOT:

- infer product or layer ownership from project names;
- invent a human-authored "current role" column;
- treat package presence as architecture permission;
- infer transitive references as direct references;
- silently ignore projects outside the recognized backend source/test locations;
- treat solution comments as stronger evidence than project path/${code(
    "<IsTestProject>",
  )};
- leave placeholder rows such as "inspect csproj later".

Architecture semantics remain in canonical authored documents.

Source inventory remains here.

---

Generated by ${code(GENERATOR_PATH)}.
`;
}

function rowsForSourceInputs(
  sourceProjectPaths,
) {
  return `## Source inputs

The current generation read:

${[
  SOLUTION_PATH,
  ...sourceProjectPaths,
]
  .map(
    (sourcePath) =>
      `- ${code(sourcePath)}`,
  )
  .join("\n")}`;
}

const solutionXml =
  readRequired(SOLUTION_PATH);

const classifiedProjects =
  parseSolution(solutionXml);

const projects =
  classifiedProjects.map(parseProject);

validateReferenceClosure(projects);

const generated =
  renderDocument(projects).replace(
    /\r\n/g,
    "\n",
  );

const outputPath = path.join(
  repoRoot,
  GENERATED_PATH,
);

if (stdoutMode) {
  process.stdout.write(generated);
  process.exit(0);
}

if (checkMode) {
  if (!fs.existsSync(outputPath)) {
    fail(
      `${GENERATED_PATH} does not exist. ` +
        `Run ${GENERATOR_COMMAND} and commit ` +
        `the generated file.`,
    );
  }

  const current =
    fs
      .readFileSync(
        outputPath,
        "utf8",
      )
      .replace(
        /\r\n/g,
        "\n",
      );

  if (current !== generated) {
    fail(
      `${GENERATED_PATH} is stale. ` +
        `Run ${GENERATOR_COMMAND}, review the ` +
        `source-derived diff, and commit it.`,
    );
  }

  console.log(
    `[backend-project-map] OK — ` +
      `${GENERATED_PATH} matches ` +
      `${SOLUTION_PATH} and ` +
      `${projects.length} project manifests.`,
  );

  process.exit(0);
}

fs.mkdirSync(
  path.dirname(outputPath),
  {
    recursive: true,
  },
);

fs.writeFileSync(
  outputPath,
  generated,
  "utf8",
);

console.log(
  `[backend-project-map] Generated ` +
    `${GENERATED_PATH} from ` +
    `${projects.length} projects ` +
    `(${
      projects.filter(
        (project) =>
          project.type === "production",
      ).length
    } production, ` +
    `${
      projects.filter(
        (project) =>
          project.type === "test",
      ).length
    } test, ` +
    `${
      projects.filter(
        (project) =>
          project.type === "test-support",
      ).length
    } testing support).`,
);