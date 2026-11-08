import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";
import {
  evaluateFilter,
  type CompoundFilter,
  type ConditionOperator,
} from "../filters/compound-filter";
import type { FieldDefinition, FieldType } from "../types/field-types";
import {
  compareFieldValues,
  fieldTypeRegistry,
  type FieldContext,
} from "./field-type-registry";

/**
 * UC-WM-C17: the client's sort and filter on the fixed dataset that the server's paged read is held to as well
 * (`PagedReadFieldTypeParityIntegrationTests`). Both read `backend/contracts/fixtures/field-type-parity.v1.json`;
 * a change on one host that the other does not follow fails one of the two.
 */
interface Fixture {
  fields: {
    key: string;
    type: string;
    name: string;
    options?: string[];
  }[];
  items: { key: string; values: Record<string, unknown> }[];
  sorts: { name: string; sort: [string, "asc" | "desc"][]; expected: string[] }[];
  filters: {
    name: string;
    join: "and" | "or";
    conditions: string[][];
    expected: string[];
  }[];
  serverRefuses: { name: string }[];
}

const fixture = JSON.parse(
  readFileSync(
    new URL(
      "../../../../../../../backend/contracts/fixtures/field-type-parity.v1.json",
      import.meta.url,
    ),
    "utf8",
  ),
) as Fixture;

// The web client has no separate Status type: a Status field is a select whose options are the statuses.
const clientType = (type: string): FieldType =>
  (type === "status" ? "select" : type) as FieldType;

const definitions = new Map<string, FieldDefinition>(
  fixture.fields.map((f, position) => [
    f.key,
    {
      id: f.key,
      boardId: "parity",
      name: f.name,
      fieldType: clientType(f.type),
      options: (f.options ?? []).map((id) => ({
        id,
        label: id,
        color: "#808080",
      })),
      position,
      isHidden: false,
      isSystemField: false,
    },
  ]),
);

const contextOf = (key: string): FieldContext => ({
  field: definitions.get(key)!,
  memberName: (id) => id,
});

const valueOf = (item: Fixture["items"][number], key: string): unknown =>
  item.values[key] ?? null;

function clientSort(levels: [string, "asc" | "desc"][]): string[] {
  return fixture.items
    .map((item, index) => ({ item, index }))
    .sort((a, b) => {
      for (const [key, direction] of levels) {
        const order = compareFieldValues(
          contextOf(key),
          valueOf(a.item, key),
          valueOf(b.item, key),
          direction,
        );
        if (order !== 0) return order;
      }
      return a.index - b.index;
    })
    .map(({ item }) => item.key);
}

function clientFilter(join: "and" | "or", conditions: string[][]): string[] {
  const filter: CompoundFilter = {
    join,
    conditions: conditions.map(([fieldId, operator, ...values]) => ({
      fieldId: fieldId!,
      operator: operator as ConditionOperator,
      values,
    })),
  };
  return fixture.items
    .filter(
      (item) =>
        evaluateFilter(filter, {
          resolve: (key) => ({
            context: contextOf(key),
            value: valueOf(item, key),
          }),
        }).matches,
    )
    .map((item) => item.key);
}

describe("field type parity, client side (UC-WM-C17)", () => {
  it("covers the twelve field types of the contract, each known to the registry", () => {
    expect(fixture.fields).toHaveLength(12);
    for (const f of fixture.fields)
      expect(fieldTypeRegistry[clientType(f.type)]).toBeDefined();
  });

  for (const { name, sort, expected } of fixture.sorts)
    it(`sorts: ${name}`, () => {
      expect(clientSort(sort)).toEqual(expected);
    });

  for (const { name, join, conditions, expected } of fixture.filters)
    it(`filters: ${name}`, () => {
      expect(clientFilter(join, conditions)).toEqual(expected);
    });
});
