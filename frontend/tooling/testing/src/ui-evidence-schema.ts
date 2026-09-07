export const UI_EVIDENCE_STATES = [
  "Default",
  "Loading",
  "Empty",
  "Error",
  "Success",
  "Unavailable",
  "ReadOnly",
  "PermissionLimited",
  "EdgeData",
  "HighDensity",
] as const;

export const UI_EVIDENCE_CHECKS = [
  "interaction",
  "a11y",
  "visual",
  "purity",
] as const;

export const UI_EVIDENCE_SURFACE_KINDS = [
  "data",
  "form",
  "navigation",
  "shell",
  "composition",
  "feedback",
  "primitive",
] as const;

export const UI_EVIDENCE_VIEWPORTS = [
  "mobile",
  "tablet",
  "desktop",
] as const;

export const UI_EVIDENCE_THEMES = ["light", "dark"] as const;

export const UI_EVIDENCE_EXCLUSION_CATEGORIES = [
  "container",
  "route",
  "provider",
  "application-adapter",
  "nonvisual-composition",
  "platform-specific",
] as const;

/**
 * Per-surface-kind state universe (SPEC §9.3). Enforced by state-coverage
 * accounting (Wave 3); declared here as the single source of the policy.
 */
export const UI_EVIDENCE_SURFACE_KIND_STATE_UNIVERSES: Record<
  (typeof UI_EVIDENCE_SURFACE_KINDS)[number],
  readonly (typeof UI_EVIDENCE_STATES)[number][]
> = {
  data: [
    "Default",
    "Loading",
    "Empty",
    "Error",
    "EdgeData",
    "HighDensity",
    "ReadOnly",
    "PermissionLimited",
  ],
  form: ["Default", "Loading", "Error", "Success", "EdgeData", "ReadOnly"],
  navigation: ["Default", "EdgeData", "ReadOnly"],
  shell: ["Default", "Loading", "Error", "EdgeData"],
  composition: ["Default", "Loading", "Error", "EdgeData"],
  feedback: ["Default"],
  primitive: ["Default", "EdgeData"],
};

export type UiEvidenceState = (typeof UI_EVIDENCE_STATES)[number];
export type UiEvidenceCheck = (typeof UI_EVIDENCE_CHECKS)[number];
export type UiEvidenceSurfaceKind =
  (typeof UI_EVIDENCE_SURFACE_KINDS)[number];
export type UiEvidenceViewport = (typeof UI_EVIDENCE_VIEWPORTS)[number];
export type UiEvidenceTheme = (typeof UI_EVIDENCE_THEMES)[number];
export type UiEvidenceExclusionCategory =
  (typeof UI_EVIDENCE_EXCLUSION_CATEGORIES)[number];

export interface UiEvidenceVisualTarget {
  viewport: UiEvidenceViewport;
  theme: UiEvidenceTheme;
}

export interface UiEvidenceStory {
  id: string;
  state: UiEvidenceState;
  visualTargets: UiEvidenceVisualTarget[];
}

export interface UiEvidenceDelegatedState {
  state: UiEvidenceState;
  surfaceId: string;
  targetState: UiEvidenceState;
}

export interface UiEvidenceNotApplicableState {
  state: UiEvidenceState;
  reason: string;
  authority: string;
}

export interface UiEvidenceStateCoverage {
  required: UiEvidenceState[];
  delegated: UiEvidenceDelegatedState[];
  notApplicable: UiEvidenceNotApplicableState[];
}

export interface UiEvidenceInteractionCase {
  id: string;
  testFile: string;
}

export interface UiEvidenceExcludedSource {
  path: string;
  category: UiEvidenceExclusionCategory;
  reason: string;
}

export interface UiEvidenceSurface {
  surfaceId: string;
  owner: string;
  surfaceKind: UiEvidenceSurfaceKind;
  pureEntry: string;
  coveredSources: string[];
  stories: UiEvidenceStory[];
  stateCoverage: UiEvidenceStateCoverage;
  responsive: boolean;
  themeAware: boolean;
  checks: UiEvidenceCheck[];
  interactionCases: UiEvidenceInteractionCase[];
}

export interface UiEvidenceManifest {
  schemaVersion: 2;
  owner: string;
  inventory: {
    excludedSources: UiEvidenceExcludedSource[];
  };
  surfaces: UiEvidenceSurface[];
}

export interface UiEvidenceValidationResult {
  ok: boolean;
  diagnostics: string[];
  manifest?: UiEvidenceManifest;
}

const allowedStates = new Set<string>(UI_EVIDENCE_STATES);
const allowedChecks = new Set<string>(UI_EVIDENCE_CHECKS);
const allowedKinds = new Set<string>(UI_EVIDENCE_SURFACE_KINDS);
const allowedViewports = new Set<string>(UI_EVIDENCE_VIEWPORTS);
const allowedThemes = new Set<string>(UI_EVIDENCE_THEMES);
const allowedCategories = new Set<string>(UI_EVIDENCE_EXCLUSION_CATEGORIES);

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

const GENERIC_EXCLUSION_REASONS = new Set([
  "not critical",
  "not needed",
  "covered elsewhere",
  "no",
  "n/a",
  "na",
]);

function isGenericExclusionReason(reason: string): boolean {
  const normalized = reason.trim().toLowerCase().replace(/\s+/g, " ");
  if (GENERIC_EXCLUSION_REASONS.has(normalized)) return true;
  if (normalized.startsWith("covered elsewhere")) return true;
  return false;
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

function rejectUnknownFields(
  diagnostics: string[],
  value: Record<string, unknown>,
  path: string,
  allowed: readonly string[],
): void {
  const allowedSet = new Set<string>(allowed);
  for (const key of Object.keys(value)) {
    if (!allowedSet.has(key)) {
      diagnostics.push(`${path} has unknown field "${key}"`);
    }
  }
}

function validateEnum<T extends string>(
  diagnostics: string[],
  value: unknown,
  path: string,
  allowed: Set<string>,
): value is T {
  if (typeof value === "string" && allowed.has(value)) return true;
  diagnostics.push(`${path} must be one of ${[...allowed].join(", ")}`);
  return false;
}

function validateOwnerLocalPath(
  diagnostics: string[],
  value: unknown,
  path: string,
): value is string {
  if (!requireString(diagnostics, value, path)) return false;
  const text = value;
  if (text.startsWith("/")) {
    diagnostics.push(`${path} must be owner-local and relative`);
    return false;
  }
  if (text.split("/").includes("..")) {
    diagnostics.push(`${path} must not traverse outside the owner root`);
    return false;
  }
  return true;
}

function validateVisualTarget(
  value: unknown,
  path: string,
  diagnostics: string[],
): UiEvidenceVisualTarget | undefined {
  if (!isRecord(value)) {
    diagnostics.push(`${path} must be an object`);
    return undefined;
  }
  rejectUnknownFields(diagnostics, value, path, ["viewport", "theme"]);
  const viewportOk = validateEnum<UiEvidenceViewport>(
    diagnostics,
    value.viewport,
    `${path}.viewport`,
    allowedViewports,
  );
  const themeOk = validateEnum<UiEvidenceTheme>(
    diagnostics,
    value.theme,
    `${path}.theme`,
    allowedThemes,
  );
  if (!viewportOk || !themeOk) return undefined;
  return {
    viewport: value.viewport as UiEvidenceViewport,
    theme: value.theme as UiEvidenceTheme,
  };
}

function validateStateCoverage(
  value: unknown,
  path: string,
  diagnostics: string[],
): UiEvidenceStateCoverage | undefined {
  if (!isRecord(value)) {
    diagnostics.push(`${path} must be an object`);
    return undefined;
  }
  rejectUnknownFields(diagnostics, value, path, [
    "required",
    "delegated",
    "notApplicable",
  ]);

  const required: UiEvidenceState[] = [];
  const delegated: UiEvidenceDelegatedState[] = [];
  const notApplicable: UiEvidenceNotApplicableState[] = [];

  if (!Array.isArray(value.required)) {
    diagnostics.push(`${path}.required must be an array`);
  } else {
    value.required.forEach((item, index) => {
      if (validateEnum<UiEvidenceState>(
        diagnostics,
        item,
        `${path}.required[${index}]`,
        allowedStates,
      )) {
        required.push(item);
      }
    });
  }

  if (!Array.isArray(value.delegated)) {
    diagnostics.push(`${path}.delegated must be an array`);
  } else {
    value.delegated.forEach((item, index) => {
      const itemPath = `${path}.delegated[${index}]`;
      if (!isRecord(item)) {
        diagnostics.push(`${itemPath} must be an object`);
        return;
      }
      rejectUnknownFields(diagnostics, item, itemPath, [
        "state",
        "surfaceId",
        "targetState",
      ]);
      const stateOk = validateEnum<UiEvidenceState>(
        diagnostics,
        item.state,
        `${itemPath}.state`,
        allowedStates,
      );
      const surfaceOk = requireString(
        diagnostics,
        item.surfaceId,
        `${itemPath}.surfaceId`,
      );
      const targetOk = validateEnum<UiEvidenceState>(
        diagnostics,
        item.targetState,
        `${itemPath}.targetState`,
        allowedStates,
      );
      if (stateOk && surfaceOk && targetOk) {
        delegated.push({
          state: item.state as UiEvidenceState,
          surfaceId: item.surfaceId as string,
          targetState: item.targetState as UiEvidenceState,
        });
      }
    });
  }

  if (!Array.isArray(value.notApplicable)) {
    diagnostics.push(`${path}.notApplicable must be an array`);
  } else {
    value.notApplicable.forEach((item, index) => {
      const itemPath = `${path}.notApplicable[${index}]`;
      if (!isRecord(item)) {
        diagnostics.push(`${itemPath} must be an object`);
        return;
      }
      rejectUnknownFields(diagnostics, item, itemPath, [
        "state",
        "reason",
        "authority",
      ]);
      const stateOk = validateEnum<UiEvidenceState>(
        diagnostics,
        item.state,
        `${itemPath}.state`,
        allowedStates,
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
      if (stateOk && reasonOk && authorityOk) {
        notApplicable.push({
          state: item.state as UiEvidenceState,
          reason: item.reason as string,
          authority: item.authority as string,
        });
      }
    });
  }

  return { required, delegated, notApplicable };
}

function validateVisualPolicy(
  surface: UiEvidenceSurface,
  diagnostics: string[],
  path: string,
): void {
  const isVisual = surface.checks.includes("visual");
  for (const story of surface.stories) {
    if (!isVisual) {
      if (story.visualTargets.length > 0) {
        diagnostics.push(
          `${path} story ${story.id} declares visualTargets but surface is not visual-checked`,
        );
      }
      continue;
    }
    if (story.visualTargets.length === 0) {
      diagnostics.push(
        `${path} visual story ${story.id} must declare visualTargets`,
      );
      continue;
    }
    const has = (viewport: UiEvidenceViewport, theme: UiEvidenceTheme) =>
      story.visualTargets.some(
        (t) => t.viewport === viewport && t.theme === theme,
      );
    if (!has("desktop", "light")) {
      diagnostics.push(
        `${path} visual story ${story.id} must include desktop/light target`,
      );
    }
    if (story.state === "Default") {
      if (surface.responsive) {
        if (!has("mobile", "light")) {
          diagnostics.push(
            `${path} responsive surface Default story ${story.id} must include mobile/light target`,
          );
        }
        if (!has("tablet", "light")) {
          diagnostics.push(
            `${path} responsive surface Default story ${story.id} must include tablet/light target`,
          );
        }
      }
      if (surface.themeAware) {
        if (!has("desktop", "dark")) {
          diagnostics.push(
            `${path} theme-aware surface Default story ${story.id} must include desktop/dark target`,
          );
        }
      }
    }
  }
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
  rejectUnknownFields(diagnostics, value, path, [
    "surfaceId",
    "owner",
    "surfaceKind",
    "pureEntry",
    "coveredSources",
    "stories",
    "stateCoverage",
    "responsive",
    "themeAware",
    "checks",
    "interactionCases",
  ]);

  const surfaceId = requireString(
    diagnostics,
    value.surfaceId,
    `${path}.surfaceId`,
  )
    ? (value.surfaceId as string)
    : "";
  const owner = requireString(diagnostics, value.owner, `${path}.owner`)
    ? (value.owner as string)
    : "";
  const surfaceKind = validateEnum<UiEvidenceSurfaceKind>(
    diagnostics,
    value.surfaceKind,
    `${path}.surfaceKind`,
    allowedKinds,
  )
    ? (value.surfaceKind as UiEvidenceSurfaceKind)
    : undefined;
  const pureEntryOk = validateOwnerLocalPath(
    diagnostics,
    value.pureEntry,
    `${path}.pureEntry`,
  );
  const pureEntry = pureEntryOk ? (value.pureEntry as string) : "";

  const coveredSources: string[] = [];
  if (!Array.isArray(value.coveredSources)) {
    diagnostics.push(`${path}.coveredSources must be an array`);
  } else {
    value.coveredSources.forEach((item, index) => {
      if (
        validateOwnerLocalPath(
          diagnostics,
          item,
          `${path}.coveredSources[${index}]`,
        )
      ) {
        coveredSources.push(item as string);
      }
    });
  }

  const checks: UiEvidenceCheck[] = [];
  if (!Array.isArray(value.checks)) {
    diagnostics.push(`${path}.checks must be an array`);
  } else {
    value.checks.forEach((item, index) => {
      if (
        validateEnum<UiEvidenceCheck>(
          diagnostics,
          item,
          `${path}.checks[${index}]`,
          allowedChecks,
        )
      ) {
        checks.push(item);
      }
    });
  }

  const stories: UiEvidenceStory[] = [];
  if (!Array.isArray(value.stories)) {
    diagnostics.push(`${path}.stories must be an array`);
  } else {
    value.stories.forEach((item, index) => {
      const itemPath = `${path}.stories[${index}]`;
      if (!isRecord(item)) {
        diagnostics.push(`${itemPath} must be an object`);
        return;
      }
      rejectUnknownFields(diagnostics, item, itemPath, [
        "id",
        "state",
        "visualTargets",
      ]);
      const idOk = requireString(diagnostics, item.id, `${itemPath}.id`);
      const stateOk = validateEnum<UiEvidenceState>(
        diagnostics,
        item.state,
        `${itemPath}.state`,
        allowedStates,
      );
      const visualTargets: UiEvidenceVisualTarget[] = [];
      if (!Array.isArray(item.visualTargets)) {
        diagnostics.push(`${itemPath}.visualTargets must be an array`);
      } else {
        item.visualTargets.forEach((target, targetIndex) => {
          const targetValue = validateVisualTarget(
            target,
            `${itemPath}.visualTargets[${targetIndex}]`,
            diagnostics,
          );
          if (targetValue) visualTargets.push(targetValue);
        });
      }
      if (idOk && stateOk) {
        stories.push({
          id: item.id as string,
          state: item.state as UiEvidenceState,
          visualTargets,
        });
      }
    });
  }

  const stateCoverage = validateStateCoverage(
    value.stateCoverage,
    `${path}.stateCoverage`,
    diagnostics,
  );

  const interactionCases: UiEvidenceInteractionCase[] = [];
  const seenCaseIds = new Set<string>();
  if (!Array.isArray(value.interactionCases)) {
    diagnostics.push(`${path}.interactionCases must be an array`);
  } else {
    value.interactionCases.forEach((item, index) => {
      const itemPath = `${path}.interactionCases[${index}]`;
      if (!isRecord(item)) {
        diagnostics.push(`${itemPath} must be an object`);
        return;
      }
      rejectUnknownFields(diagnostics, item, itemPath, ["id", "testFile"]);
      const idOk = requireString(diagnostics, item.id, `${itemPath}.id`);
      if (idOk) {
        const id = item.id as string;
        if (seenCaseIds.has(id)) {
          diagnostics.push(`${itemPath} duplicate interaction case id ${id}`);
        }
        seenCaseIds.add(id);
        if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(id)) {
          diagnostics.push(
            `${itemPath} case id ${id} must be kebab-case user-visible behavior`,
          );
        }
      }
      const testFileOk = validateOwnerLocalPath(
        diagnostics,
        item.testFile,
        `${itemPath}.testFile`,
      );
      const suffixOk =
        typeof item.testFile === "string" &&
        item.testFile.endsWith(".component.test.tsx");
      if (testFileOk && !suffixOk) {
        diagnostics.push(
          `${itemPath}.testFile must be an owner-local *.component.test.tsx path`,
        );
      }
      if (idOk && testFileOk && suffixOk) {
        interactionCases.push({
          id: item.id as string,
          testFile: item.testFile as string,
        });
      }
    });
  }

  const responsive = value.responsive === true;
  const themeAware = value.themeAware === true;
  if (value.responsive !== undefined && typeof value.responsive !== "boolean") {
    diagnostics.push(`${path}.responsive must be a boolean`);
  }
  if (value.themeAware !== undefined && typeof value.themeAware !== "boolean") {
    diagnostics.push(`${path}.themeAware must be a boolean`);
  }

  if (!surfaceId || !owner || !surfaceKind || !pureEntry) return undefined;
  if (!stateCoverage) return undefined;

  const surface: UiEvidenceSurface = {
    surfaceId,
    owner,
    surfaceKind,
    pureEntry,
    coveredSources,
    stories,
    stateCoverage,
    responsive,
    themeAware,
    checks,
    interactionCases,
  };

  const requiredStateSet = new Set<string>(stateCoverage.required);
  for (const story of surface.stories) {
    if (!requiredStateSet.has(story.state)) {
      diagnostics.push(
        `${path} story ${story.id} state ${story.state} is not in stateCoverage.required`,
      );
    }
  }
  for (const delegatedState of stateCoverage.delegated) {
    if (requiredStateSet.has(delegatedState.state)) {
      diagnostics.push(
        `${path} state ${delegatedState.state} is both required and delegated`,
      );
    }
  }
  const notApplicableStates = new Set<string>(
    stateCoverage.notApplicable.map((entry) => entry.state),
  );
  for (const state of notApplicableStates) {
    if (requiredStateSet.has(state)) {
      diagnostics.push(
        `${path} state ${state} is both required and notApplicable`,
      );
    }
  }
  for (const entry of stateCoverage.notApplicable) {
    if (isGenericExclusionReason(entry.reason)) {
      diagnostics.push(
        `${path} notApplicable ${entry.state} reason must be concrete, not a generic placeholder (SPEC §9.1)`,
      );
    }
  }

  const universe =
    UI_EVIDENCE_SURFACE_KIND_STATE_UNIVERSES[surface.surfaceKind] ?? [];
  const accounted = new Set<string>([
    ...requiredStateSet,
    ...stateCoverage.delegated.map((entry) => entry.state),
    ...notApplicableStates,
  ]);
  for (const state of universe) {
    if (!accounted.has(state)) {
      diagnostics.push(
        `${path} surfaceKind ${surface.surfaceKind} state ${state} must be required, delegated, or notApplicable (SPEC §9.3)`,
      );
    }
  }

  const hasInteraction = surface.checks.includes("interaction");
  if (hasInteraction && surface.interactionCases.length === 0) {
    diagnostics.push(
      `${path}.interactionCases must not be empty when checks includes interaction`,
    );
  }
  if (!hasInteraction && surface.interactionCases.length > 0) {
    diagnostics.push(
      `${path}.interactionCases must be empty when interaction is not required`,
    );
  }

  validateVisualPolicy(surface, diagnostics, path);

  return surface;
}

export function validateUiEvidenceManifest(
  value: unknown,
): UiEvidenceValidationResult {
  const diagnostics: string[] = [];
  if (!isRecord(value)) {
    return { ok: false, diagnostics: ["manifest must be an object"] };
  }
  if (value.schemaVersion !== 2) {
    diagnostics.push("schemaVersion must be 2");
  } else {
    rejectUnknownFields(diagnostics, value, "manifest", [
      "schemaVersion",
      "owner",
      "inventory",
      "surfaces",
    ]);
  }

  const owner = requireString(diagnostics, value.owner, "owner")
    ? value.owner
    : "";
  if (typeof value.owner === "string" && value.owner.length === 0) {
    diagnostics.push("owner must not be empty");
  }

  let inventory: UiEvidenceManifest["inventory"] = { excludedSources: [] };
  if (!isRecord(value.inventory)) {
    diagnostics.push("inventory must be an object");
  } else {
    rejectUnknownFields(diagnostics, value.inventory, "inventory", [
      "excludedSources",
    ]);
    if (!Array.isArray(value.inventory.excludedSources)) {
      diagnostics.push("inventory.excludedSources must be an array");
    } else {
      inventory = { excludedSources: [] };
      value.inventory.excludedSources.forEach((item, index) => {
        const itemPath = `inventory.excludedSources[${index}]`;
        if (!isRecord(item)) {
          diagnostics.push(`${itemPath} must be an object`);
          return;
        }
        rejectUnknownFields(diagnostics, item, itemPath, [
          "path",
          "category",
          "reason",
        ]);
        const pathOk = validateOwnerLocalPath(
          diagnostics,
          item.path,
          `${itemPath}.path`,
        );
        const categoryOk = validateEnum<UiEvidenceExclusionCategory>(
          diagnostics,
          item.category,
          `${itemPath}.category`,
          allowedCategories,
        );
        const reasonOk = requireString(
          diagnostics,
          item.reason,
          `${itemPath}.reason`,
        );
        const concreteOk =
          reasonOk &&
          !isGenericExclusionReason(item.reason as string);
        if (reasonOk && !concreteOk) {
          diagnostics.push(
            `${itemPath}.reason must be concrete, not a generic placeholder (SPEC §9.1)`,
          );
        }
        if (pathOk && categoryOk && reasonOk && concreteOk) {
          inventory.excludedSources.push({
            path: item.path as string,
            category: item.category as UiEvidenceExclusionCategory,
            reason: item.reason as string,
          });
        }
      });
    }
  }

  const surfaces: UiEvidenceSurface[] = [];
  if (!Array.isArray(value.surfaces)) {
    diagnostics.push("surfaces must be an array");
  } else {
    value.surfaces.forEach((item, index) => {
      const surface = validateSurface(
        item,
        `surfaces[${index}]`,
        diagnostics,
      );
      if (surface) surfaces.push(surface);
    });
  }

  const surfaceIds = new Set<string>();
  const storyIds = new Set<string>();
  const surfaceByOwnerAndId = new Map<string, UiEvidenceSurface>();
  for (const surface of surfaces) {
    if (surfaceIds.has(surface.surfaceId)) {
      diagnostics.push(`duplicate surfaceId ${surface.surfaceId}`);
    }
    surfaceIds.add(surface.surfaceId);
    surfaceByOwnerAndId.set(`${surface.owner}::${surface.surfaceId}`, surface);
    for (const story of surface.stories) {
      if (storyIds.has(story.id)) {
        diagnostics.push(`duplicate story id ${story.id}`);
      }
      storyIds.add(story.id);
    }
  }

  for (const surface of surfaces) {
    for (const delegated of surface.stateCoverage.delegated) {
      const targetSurface = surfaceByOwnerAndId.get(
        `${surface.owner}::${delegated.surfaceId}`,
      );
      if (!targetSurface) {
        diagnostics.push(
          `surfaces[${surface.surfaceId}] delegates ${delegated.state} to missing or cross-owner surface ${delegated.surfaceId}`,
        );
        continue;
      }
      if (!targetSurface.stateCoverage.required.includes(delegated.targetState)) {
        diagnostics.push(
          `surfaces[${surface.surfaceId}] delegates ${delegated.state} to surface ${delegated.surfaceId} state ${delegated.targetState} which is not required by the target`,
        );
      }
    }
  }

  return {
    ok: diagnostics.length === 0,
    diagnostics,
    manifest:
      diagnostics.length === 0
        ? { schemaVersion: 2, owner, inventory, surfaces }
        : undefined,
  };
}