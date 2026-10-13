import { describe, expect, it } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { createBoardAccessApi } from "../api/board-access.api";
import type { GeneratedBoardAccessDto } from "../api/board-api-mappers";
import { wmQueryKeys } from "../queries/keys";
import { boardAccessQueryOptions } from "../queries/use-board-access";
import { createWorkManagementServices } from "../services";

const accessDto: GeneratedBoardAccessDto = {
  boardId: "board-erp",
  version: 9,
  myLevel: "manager",
  mySource: { kind: "owner" },
  peopleCount: 32,
  guestCount: 1,
  peopleCountComplete: false,
  sources: [
    { kind: "space", id: "space-cn", name: "Phòng Công nghệ", count: 24 },
  ],
};

function recordingClient(response: unknown) {
  const calls: string[] = [];
  const client = {
    api: {
      get: async <T>(url: string) => {
        calls.push(url);
        return response as T;
      },
    },
  } as unknown as NotrelixClient;
  return { client, calls };
}

describe("board access read", () => {
  it("reads GET /boards/{id}/access and maps the summary", async () => {
    const { client, calls } = recordingClient(accessDto);
    const access =
      await createBoardAccessApi(client).getBoardAccess("board-erp");

    expect(calls).toEqual(["/boards/board-erp/access"]);
    expect(access).toMatchObject({
      boardId: "board-erp",
      myLevel: "manager",
      mySource: { kind: "owner", grantedBy: null },
      peopleCount: 32,
      peopleCountComplete: false,
    });
  });

  it("is reachable from the Work Management services", async () => {
    const { client, calls } = recordingClient(accessDto);
    await createWorkManagementServices(client).access.getBoardAccess("b-1");
    expect(calls).toEqual(["/boards/b-1/access"]);
  });

  it("keys the access summary by workspace and board", () => {
    const key = wmQueryKeys.boardAccess("ws-1", "b-1");
    expect(key.slice(0, 3)).toEqual(["workspace", "ws-1", "work-management"]);
    expect(() => assertNotrelixQueryKey(key)).not.toThrow();
    expect(key).not.toEqual(wmQueryKeys.boardAccess("ws-2", "b-1"));
    expect(key).not.toEqual(wmQueryKeys.boardAccess("ws-1", "b-2"));
    // A board's access is not part of its `/full` read cache entry.
    expect(key).not.toEqual(wmQueryKeys.fullBoard("ws-1", "b-1"));
  });

  it("builds the hook's query under the workspace-scoped key and waits for both ids", async () => {
    const { client } = recordingClient(accessDto);
    const services = createWorkManagementServices(client);

    const ready = boardAccessQueryOptions(services.access, "b-1", "ws-1");
    expect(ready.queryKey).toEqual(wmQueryKeys.boardAccess("ws-1", "b-1"));
    expect(ready.enabled).toBe(true);

    const pending = boardAccessQueryOptions(services.access, "b-1", undefined);
    expect(pending.enabled).toBe(false);
    expect(pending.queryKey[1]).toBe("pending");
  });
});
