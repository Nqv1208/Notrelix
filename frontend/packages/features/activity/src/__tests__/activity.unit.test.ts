import { describe, it, expect } from "vitest";
import { activityQueryKeys } from "../core/query/keys";

describe("activityQueryKeys", () => {
  it("should generate activity root query key", () => {
    expect(activityQueryKeys.all).toEqual(["activity"]);
  });

  it("should generate activity query key with workspaceId", () => {
    const workspaceId = "ws-123";
    expect(activityQueryKeys.workspace(workspaceId)).toEqual([
      "activity",
      "workspace",
      "ws-123",
    ]);
  });
});
