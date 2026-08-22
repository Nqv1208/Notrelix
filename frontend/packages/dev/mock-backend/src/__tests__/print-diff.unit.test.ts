import { describe, it } from "vitest";
import { buildOperationRegistry } from "../operations/build-registry";

describe("Print Diff", () => {
  it("prints registry operations", () => {
    const reg = buildOperationRegistry();
    console.log(reg.operationIds().join(", "));
  });
});
