import { describe, expect, it } from "vitest";
import {
  allowOffered,
  cellOf,
  cellsOf,
  denyOffered,
  entriesOf,
  isReadOnlyMatrix,
  readOnlyCells,
  sameCells,
  withCell,
} from "../core/model/permission-model";

describe("MFB-CHAT-S3.1: permission matrix model (UC-CHT-H01)", () => {
  it("a missing cell is the default and a set round-trips", () => {
    const cells = cellsOf([
      { subject: "member", action: "post", effect: "deny" },
      { subject: "guest", action: "react", effect: "allow" },
    ]);
    expect(cellOf(cells, "member", "post")).toBe("deny");
    expect(cellOf(cells, "guest", "react")).toBe("allow");
    expect(cellOf(cells, "manager", "pin")).toBe("default");
    expect(entriesOf(cells)).toEqual([
      { subject: "member", action: "post", effect: "deny" },
      { subject: "guest", action: "react", effect: "allow" },
    ]);
  });

  it("sends the entries in the order of the roles, then the actions", () => {
    let cells = cellsOf([]);
    cells = withCell(cells, "guest", "post", "deny");
    cells = withCell(cells, "member", "invite", "deny");
    cells = withCell(cells, "member", "post", "deny");
    expect(entriesOf(cells).map((e) => `${e.subject}:${e.action}`)).toEqual([
      "member:post",
      "member:invite",
      "guest:post",
    ]);
  });

  it("setting a cell back to the default removes its entry", () => {
    const cells = withCell(
      withCell({}, "member", "post", "deny"),
      "member",
      "post",
      "default",
    );
    expect(entriesOf(cells)).toEqual([]);
    expect(sameCells(cells, {})).toBe(true);
  });

  it("does not offer an allow above the role, nor a deny of the manager's invite", () => {
    expect(allowOffered("guest", "pin")).toBe(false);
    expect(allowOffered("member", "broadcast")).toBe(false);
    expect(allowOffered("member", "pin")).toBe(true);
    expect(allowOffered("guest", "post")).toBe(true);
    expect(denyOffered("manager", "invite")).toBe(false);
    expect(denyOffered("member", "invite")).toBe(true);
  });

  it("the read-only preset denies post for members and guests only", () => {
    const cells = readOnlyCells({});
    expect(isReadOnlyMatrix(cells)).toBe(true);
    expect(cellOf(cells, "manager", "post")).toBe("default");
    expect(isReadOnlyMatrix({})).toBe(false);
  });
});
