export const UI_EVIDENCE_STATES = [
  "Default",
  "Empty",
  "EdgeData",
  "HighDensity",
  "ReadOnly",
  "PermissionLimited",
  "Loading",
  "Error",
  "Unavailable",
] as const;

export const UI_EVIDENCE_CHECKS = [
  "interaction",
  "a11y",
  "visual",
  "purity",
] as const;

export type UiEvidenceState = (typeof UI_EVIDENCE_STATES)[number];
export type UiEvidenceCheck = (typeof UI_EVIDENCE_CHECKS)[number];

export interface UiEvidenceStory {
  id: string;
  state: UiEvidenceState;
}

export interface UiEvidenceNotApplicableState {
  state: UiEvidenceState;
  reason: string;
  authority: string;
}

export interface UiEvidenceSurface {
  surfaceId: string;
  owner: string;
  pureEntry: string;
  stories: UiEvidenceStory[];
  requiredStates: UiEvidenceState[];
  checks: UiEvidenceCheck[];
  interactionTests: string[];
  notApplicableStates: UiEvidenceNotApplicableState[];
}

export interface UiEvidenceManifest {
  schemaVersion: 1;
  surfaces: UiEvidenceSurface[];
}

export interface UiEvidenceValidationResult {
  ok: boolean;
  diagnostics: string[];
  manifest?: UiEvidenceManifest;
}

const allowedStates = new Set<string>(UI_EVIDENCE_STATES);
const allowedChecks = new Set<string>(UI_EVIDENCE_CHECKS);

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function requireString(
  diagnostics: string[],
  value: unknown,
  path: string,
): value is string {
  if (typeof value === "string" && value.length > 0) return true;
  diagnostics.push(`${path} must be a non-empty string`);
  return false;
}

function validateState(
  diagnostics: string[],
  value: unknown,
  path: string,
): value is UiEvidenceState {
  if (typeof value === "string" && allowedStates.has(value)) return true;
  diagnostics.push(
    `${path} must be one of ${UI_EVIDENCE_STATES.join(", ")}; received ${String(value)}`,
  );
  return false;
}

function validateCheck(
  diagnostics: string[],
  value: unknown,
  path: string,
): value is UiEvidenceCheck {
  if (typeof value === "string" && allowedChecks.has(value)) return true;
  diagnostics.push(
    `${path} must be one of ${UI_EVIDENCE_CHECKS.join(", ")}; received ${String(value)}`,
  );
  return false;
}

function validateArray<T>(
  diagnostics: string[],
  value: unknown,
  path: string,
  validate: (item: unknown, itemPath: string) => item is T,
): T[] {
  if (!Array.isArray(value)) {
    diagnostics.push(`${path} must be an array`);
    return [];
  }
  return value.filter((item, index): item is T =>
    validate(item, `${path}[${index}]`),
  );
}

function validateSurface(
  value: unknown,
  path: string,
  diagnostics: string[],
): UiEvidenceSurface | undefined {
  if (!isRecord(value)) {
    diagnostics.push(`${path} must be an object`);
    return undefined;
  }

  const surfaceId = requireString(
    diagnostics,
    value.surfaceId,
    `${path}.surfaceId`,
  )
    ? value.surfaceId
    : "";
  const owner = requireString(diagnostics, value.owner, `${path}.owner`)
    ? value.owner
    : "";
  const pureEntry = requireString(
    diagnostics,
    value.pureEntry,
    `${path}.pureEntry`,
  )
    ? value.pureEntry
    : "";
  if (pureEntry.startsWith("/") || pureEntry.includes("..")) {
    diagnostics.push(`${path}.pureEntry must be owner-local and relative`);
  }

  const requiredStates = validateArray(
    diagnostics,
    value.requiredStates,
    `${path}.requiredStates`,
    (item, itemPath): item is UiEvidenceState =>
      validateState(diagnostics, item, itemPath),
  );
  const checks = validateArray(
    diagnostics,
    value.checks,
    `${path}.checks`,
    (item, itemPath): item is UiEvidenceCheck =>
      validateCheck(diagnostics, item, itemPath),
  );
  const stories = validateArray(
    diagnostics,
    value.stories,
    `${path}.stories`,
    (item, itemPath): item is UiEvidenceStory => {
      if (!isRecord(item)) {
        diagnostics.push(`${itemPath} must be an object`);
        return false;
      }
      const idOk = requireString(diagnostics, item.id, `${itemPath}.id`);
      const stateOk = validateState(
        diagnostics,
        item.state,
        `${itemPath}.state`,
      );
      return idOk && stateOk;
    },
  );
  const interactionTests = validateArray(
    diagnostics,
    value.interactionTests,
    `${path}.interactionTests`,
    (item, itemPath): item is string => {
      const ok = requireString(diagnostics, item, itemPath);
      if (ok && !item.endsWith(".component.test.tsx")) {
        diagnostics.push(
          `${itemPath} must be an owner-local *.component.test.tsx path`,
        );
      }
      if (ok && (item.startsWith("/") || item.includes(".."))) {
        diagnostics.push(`${itemPath} must be owner-local and relative`);
      }
      return ok;
    },
  );
  const notApplicableStates = validateArray(
    diagnostics,
    value.notApplicableStates,
    `${path}.notApplicableStates`,
    (item, itemPath): item is UiEvidenceNotApplicableState => {
      if (!isRecord(item)) {
        diagnostics.push(`${itemPath} must be an object`);
        return false;
      }
      const stateOk = validateState(
        diagnostics,
        item.state,
        `${itemPath}.state`,
      );
      const reasonOk = requireString(
        diagnostics,
        item.reason,
        `${itemPath}.reason`,
      );
      const authorityOk = requireString(
        diagnostics,
        item.authority,
        `${itemPath}.authority`,
      );
      return stateOk && reasonOk && authorityOk;
    },
  );

  const hasInteraction = checks.includes("interaction");
  if (hasInteraction && interactionTests.length === 0) {
    diagnostics.push(
      `${path}.interactionTests must not be empty when checks includes interaction`,
    );
  }
  if (!hasInteraction && interactionTests.length > 0) {
    diagnostics.push(
      `${path}.interactionTests must be empty when interaction is not required`,
    );
  }

  return {
    surfaceId,
    owner,
    pureEntry,
    stories,
    requiredStates,
    checks,
    interactionTests,
    notApplicableStates,
  };
}

export function validateUiEvidenceManifest(
  value: unknown,
): UiEvidenceValidationResult {
  const diagnostics: string[] = [];
  if (!isRecord(value)) {
    return { ok: false, diagnostics: ["manifest must be an object"] };
  }
  if (value.schemaVersion !== 1) {
    diagnostics.push("schemaVersion must be 1");
  }

  const surfaces = validateArray(
    diagnostics,
    value.surfaces,
    "surfaces",
    (item, itemPath): item is UiEvidenceSurface =>
      Boolean(validateSurface(item, itemPath, diagnostics)),
  );

  const surfaceIds = new Set<string>();
  const storyIds = new Set<string>();
  for (const surface of surfaces) {
    if (surfaceIds.has(surface.surfaceId))
      diagnostics.push(`duplicate surfaceId ${surface.surfaceId}`);
    surfaceIds.add(surface.surfaceId);
    for (const story of surface.stories) {
      if (storyIds.has(story.id))
        diagnostics.push(`duplicate story id ${story.id}`);
      storyIds.add(story.id);
    }
  }

  return {
    ok: diagnostics.length === 0,
    diagnostics,
    manifest:
      diagnostics.length === 0 ? { schemaVersion: 1, surfaces } : undefined,
  };
}
