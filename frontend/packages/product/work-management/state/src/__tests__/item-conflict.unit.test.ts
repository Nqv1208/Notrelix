import { describe, expect, it } from "vitest";
import {
  changedByNameOf,
  conflictDetailOf,
  deletedItemOf,
} from "../queries/use-item-conflict";

const refused = (details: unknown, code?: string) => ({
  kind: "conflict",
  status: 412,
  code,
  details,
});

describe("who changed the item first", () => {
  it("reads the name the server sent on a lost race", () => {
    expect(changedByNameOf(refused({ changedByName: "Ada Lovelace" }))).toBe(
      "Ada Lovelace",
    );
    expect(
      conflictDetailOf(refused({ changedByName: "Ada Lovelace" })),
    ).toEqual({ kind: "stale-version", byName: "Ada Lovelace" });
  });

  it("holds no name when the server named no one", () => {
    expect(changedByNameOf(refused({ changedByName: null }))).toBeNull();
    expect(changedByNameOf(refused(null))).toBeNull();
    expect(changedByNameOf(new Error("x"))).toBeNull();
  });

  it("keeps a deleted field's own detail ahead of the plain lost race", () => {
    expect(
      conflictDetailOf(
        refused(
          {
            errorCode: "work-management.field.deleted",
            fieldName: "Owner",
            changedByName: "Ada",
          },
          "work-management.field.deleted",
        ),
      ),
    ).toEqual({ kind: "field-deleted", fieldName: "Owner", byName: "Ada" });
  });
});

describe("a deleted item", () => {
  const notFound = (code: string | undefined, details: unknown) => ({
    kind: "not_found",
    status: 404,
    code,
    details,
  });

  it("is told apart from an ordinary not-found by its code", () => {
    expect(
      deletedItemOf(
        notFound("work-management.item.deleted", { deletedByName: "Ada" }),
      ),
    ).toEqual({ byName: "Ada" });
    expect(deletedItemOf(notFound("common.resource-not-found", {}))).toBeNull();
    expect(deletedItemOf(null)).toBeNull();
  });

  it("carries no name for a Guest or a person out of reach", () => {
    expect(
      deletedItemOf(
        notFound("work-management.item.deleted", { deletedByName: null }),
      ),
    ).toEqual({ byName: null });
  });
});
