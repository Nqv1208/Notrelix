#!/usr/bin/env node
// Canonical mock E2E scenario manifest (delivery plan v2 §10.8).
//
// GitHub YAML matrix definitions are not the scenario authority. Shard
// assignment and completeness verification must import this module so the
// logical scenario space stays exactly 4 personas x 5 states = 20 scenarios
// with stable `<persona>__<state>` identifiers.

export const PERSONAS = ["owner", "admin", "member", "viewer"];

export const STATES = [
  "default",
  "new-user",
  "empty-workspace",
  "permission-limited",
  "expired-session",
];

export const SCENARIOS = PERSONAS.flatMap((persona) =>
  STATES.map((state) => ({ id: `${persona}__${state}`, persona, state })),
);

export function validateManifest() {
  const errors = [];
  const expected = PERSONAS.length * STATES.length;
  if (SCENARIOS.length !== expected) {
    errors.push(`scenario count ${SCENARIOS.length} != ${PERSONAS.length}x${STATES.length}`);
  }
  const ids = SCENARIOS.map((scenario) => scenario.id);
  if (new Set(ids).size !== ids.length) {
    errors.push("duplicate scenario ids");
  }
  for (const persona of PERSONAS) {
    for (const state of STATES) {
      if (!SCENARIOS.some((s) => s.persona === persona && s.state === state)) {
        errors.push(`missing scenario ${persona}__${state}`);
      }
    }
  }
  return errors;
}

if (process.argv[1] && process.argv[1].endsWith("mock-scenarios.mjs")) {
  const errors = validateManifest();
  if (errors.length > 0) {
    for (const error of errors) console.error(`[mock-scenarios] ${error}`);
    process.exit(1);
  }
  console.log(
    JSON.stringify(
      {
        personas: PERSONAS,
        states: STATES,
        count: SCENARIOS.length,
        scenarios: SCENARIOS.map((scenario) => scenario.id),
      },
      null,
      2,
    ),
  );
}
