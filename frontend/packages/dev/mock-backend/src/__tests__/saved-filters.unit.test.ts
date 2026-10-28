import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const BOARD = mockIds.boards.erp;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  const actAs = (persona: "owner" | "member" | "viewer") => {
    (store as unknown as { config: { persona: string } }).config.persona =
      persona;
  };
  return { client, actAs };
}

type Client = ReturnType<typeof setup>["client"];
interface Row {
  id: string;
  name: string;
  visibility: string;
  isMine: boolean;
  criteria: string | null;
  version: number;
}

const list = (client: Client) =>
  client.api.get<Row[]>(endpoints.savedFilters.forBoard(BOARD));
const create = (client: Client, body: Record<string, unknown>) =>
  client.api.post<string>(endpoints.savedFilters.forBoard(BOARD), body, {
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });

describe("Saved filters in the mock world (WorkManagement.SavedFilters.*)", () => {
  it("keeps a personal filter to its creator and shows a shared one to the board", async () => {
    const { client, actAs } = setup();
    const criteria = JSON.stringify({ filter: "p.high", groupBy: "status" });
    const mine = await create(client, { name: "Của tôi", criteria });
    const shared = await create(client, {
      name: "Chung",
      criteria,
      visibility: 1,
    });

    const own = await list(client);
    expect(own.map((row) => [row.id, row.visibility, row.isMine])).toEqual([
      [mine, "Private", true],
      [shared, "Public", true],
    ]);
    expect(own[0]?.criteria).toBe(criteria);

    actAs("member");
    const seen = await list(client);
    expect(seen.map((row) => [row.id, row.isMine])).toEqual([[shared, false]]);
    // Someone else's personal filter does not exist for the member.
    await expect(
      client.api.patch(endpoints.savedFilters.rename(mine), {
        name: "x",
        expectedVersion: 1,
      }),
    ).rejects.toMatchObject({ status: 404 });
  });

  it("lets an editor rename a shared filter, but only its creator change who sees it", async () => {
    const { client, actAs } = setup();
    const id = await create(client, { name: "Chung", visibility: 1 });

    actAs("member");
    await client.api.patch(endpoints.savedFilters.rename(id), {
      name: "Đổi bởi thành viên",
      expectedVersion: 1,
    });
    await expect(
      client.api.patch(endpoints.savedFilters.visibility(id), {
        visibility: 0,
        expectedVersion: 2,
      }),
    ).rejects.toMatchObject({ status: 403 });

    actAs("owner");
    const [row] = await list(client);
    expect(row).toMatchObject({ name: "Đổi bởi thành viên", version: 2 });
    await client.api.patch(endpoints.savedFilters.visibility(id), {
      visibility: 0,
      expectedVersion: 2,
    });
    expect((await list(client))[0]?.visibility).toBe("Private");
  });

  it("refuses a viewer every write and a stale version", async () => {
    const { client, actAs } = setup();
    const id = await create(client, { name: "Chung", visibility: 1 });

    await expect(
      client.api.patch(endpoints.savedFilters.rename(id), {
        name: "x",
        expectedVersion: 9,
      }),
    ).rejects.toMatchObject({ status: 412 });

    actAs("viewer");
    expect(await list(client)).toHaveLength(1);
    await expect(create(client, { name: "Mới" })).rejects.toMatchObject({
      status: 403,
    });
    await expect(
      client.api.delete(
        `${endpoints.savedFilters.detail(id)}?expectedVersion=1`,
      ),
    ).rejects.toMatchObject({ status: 403 });
  });

  it("deletes softly and restores, and asks for a name", async () => {
    const { client } = setup();
    await expect(create(client, { name: "  " })).rejects.toMatchObject({
      status: 422,
    });
    const id = await create(client, { name: "Xoá rồi khôi phục" });

    await client.api.delete(
      `${endpoints.savedFilters.detail(id)}?expectedVersion=1`,
    );
    expect(await list(client)).toEqual([]);

    await client.api.post(
      endpoints.savedFilters.restore(id),
      {},
      {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      },
    );
    expect((await list(client)).map((row) => row.id)).toEqual([id]);
  });
});
