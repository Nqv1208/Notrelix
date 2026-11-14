import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { compareFieldValues, fieldTypeRegistry } from "./field-type-registry";
import type { FieldDefinition, FieldType } from "../types/field-types";

/**
 * The client half of the field-type parity contract (UC-WM-C17, C18): the shared fixture
 * backend/contracts/fixtures/field-type-contract.v1.json lists every type with valid and invalid values and a
 * fixed sort dataset. The server reads the same file (FieldTypeParityFixtureTests in the Domain tests).
 */
interface ParityEntry {
  readonly fieldType: string;
  readonly clientType: FieldType;
  readonly settings: Record<string, unknown>;
  readonly valid: unknown[];
  readonly invalid: unknown[];
  readonly sort?: { readonly values: unknown[]; readonly ascending: unknown[] };
}

const fixturePath = resolve(
  __dirname,
  "../../../../../../../backend/contracts/fixtures/field-type-contract.v1.json",
);
const fixture = JSON.parse(readFileSync(fixturePath, "utf8")) as {
  types: ParityEntry[];
};

const fieldOf = (entry: ParityEntry): FieldDefinition => ({
  id: "f1",
  boardId: "b1",
  name: entry.fieldType,
  fieldType: entry.clientType,
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
});

describe("field type contract fixture (client)", () => {
  it("lists a spec in the client registry for every entry", () => {
    for (const entry of fixture.types) {
      expect(
        fieldTypeRegistry[entry.clientType],
        entry.fieldType,
      ).toBeDefined();
      expect(fieldTypeRegistry[entry.clientType].type).toBe(entry.clientType);
    }
  });

  it("treats each valid value as non-empty and an empty relation as empty", () => {
    for (const entry of fixture.types) {
      const spec = fieldTypeRegistry[entry.clientType];
      for (const value of entry.valid) {
        if (
          entry.clientType === "relation" &&
          Array.isArray(value) &&
          value.length === 0
        ) {
          expect(spec.isEmpty(value)).toBe(true);
        } else {
          expect(
            spec.isEmpty(value),
            `${entry.fieldType} ${JSON.stringify(value)}`,
          ).toBe(false);
        }
      }
    }
  });

  it("sorts the fixed dataset the same way as the server", () => {
    for (const entry of fixture.types) {
      if (!entry.sort) continue;
      const sorted = [...entry.sort.values].sort((a, b) =>
        compareFieldValues(
          { field: fieldOf(entry), memberName: (id) => id },
          a,
          b,
          "asc",
        ),
      );
      expect(sorted, entry.fieldType).toEqual(entry.sort.ascending);
    }
  });

  it("gives a relation no text or group operators, so only the emptiness filters are offered", () => {
    expect(fieldTypeRegistry.relation.operators).toEqual([
      "is_empty",
      "is_not_empty",
    ]);
    expect(fieldTypeRegistry.relation.groupable).toBe(false);
  });
});
