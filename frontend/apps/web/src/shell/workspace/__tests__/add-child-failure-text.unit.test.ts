import { describe, expect, it } from "vitest";
import { describeWorkspaceFailure } from "@notrelix/features-workspace/core";
import { workspaceShellCopy } from "@notrelix/features-workspace/web";
import { addChildFailureText } from "../use-add-child-page";

const sidebar = workspaceShellCopy("en").sidebar;

function refused(status: number, ruleCode?: string) {
  return describeWorkspaceFailure({
    status,
    details: ruleCode ? { ruleCode } : {},
  });
}

describe("why a child page was not added", () => {
  it.each([
    [403, undefined, sidebar.docsAddChildDenied],
    [404, undefined, sidebar.docsAddChildGone],
    [400, "Documents_PageTree_MaxDepthExceeded", sidebar.docsAddChildTooDeep],
    [500, undefined, sidebar.docsAddChildFailed],
  ])(
    "answers %s %s with its own sentence naming the parent",
    (status, rule, text) => {
      expect(
        addChildFailureText(refused(status, rule), sidebar, "Quy trình"),
      ).toBe(text.replace("{{name}}", "Quy trình"));
    },
  );
});
