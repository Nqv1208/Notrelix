import { useMemo } from "react";
import {
  buildSpaceIndex,
  markToneFor,
  spaceAncestors,
} from "@notrelix/features-workspace/core";
import type { MyWorkDepartment } from "@notrelix/features-workspace/web";
import { useBoardPlacements } from "./board-placement-bridge";
import { useSpaceHooks } from "./use-workspace-spaces";

/**
 * The department each Board belongs to: the top-level Space of the Space the
 * Board is placed in (Boards at the workspace level have none). The reads are
 * the shell's own (placements and the Space tree), so this shares their cache.
 */
export function useBoardDepartments(
  workspaceId: string,
): ReadonlyMap<string, MyWorkDepartment> {
  const { useSpaceTree } = useSpaceHooks();
  const placements = useBoardPlacements(workspaceId);
  const tree = useSpaceTree(workspaceId);
  return useMemo(() => {
    const index = buildSpaceIndex(tree.data ?? []);
    const out = new Map<string, MyWorkDepartment>();
    for (const placement of placements.data ?? []) {
      if (!placement.spaceId) continue;
      const space = index.byId.get(placement.spaceId);
      if (!space) continue;
      const root = spaceAncestors(index, space.id)[0] ?? space;
      out.set(placement.id, {
        name: root.name,
        markTone: markToneFor(root.id),
        destination: { section: "space", spaceId: root.id },
      });
    }
    return out;
  }, [placements.data, tree.data]);
}
