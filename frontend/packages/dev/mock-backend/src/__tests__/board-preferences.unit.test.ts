import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const BOARD = mockIds.boards.erp;
const VIEW = "mock-view-personal";
const url = endpoints.boardPreferences.forView(BOARD, VIEW);

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

const put = (
  client: ReturnType<typeof setup>["client"],
  body: Record<string, unknown>,
) =>
  client.api.put<void>(url, body, {
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });
const read = (client: ReturnType<typeof setup>["client"]) =>
  client.api.get<{ criteria: string | null }>(url);

describe("Board preferences in the mock world (WorkManagement.BoardPreferences.*)", () => {
  it("keeps the criteria per member, including a viewer's, and clears it when blank", async () => {
    const { client, actAs } = setup();
    actAs("owner");
    expect((await read(client)).criteria).toBeNull();
    await put(client, { criteria: '{"v":2,"filter":"c2:and~a.is_empty"}' });
    expect((await read(client)).criteria).toBe(
      '{"v":2,"filter":"c2:and~a.is_empty"}',
    );

    actAs("viewer");
    expect((await read(client)).criteria).toBeNull();
    await put(client, { criteria: '{"v":2,"sort":"a.asc"}' });
    expect((await read(client)).criteria).toBe('{"v":2,"sort":"a.asc"}');

    actAs("owner");
    await put(client, { criteria: "" });
    expect((await read(client)).criteria).toBeNull();
  });

  it("leaves the criteria alone when omitted and refuses a non-object or oversize body", async () => {
    const { client, actAs } = setup();
    actAs("owner");
    await put(client, { criteria: '{"v":2}' });
    await put(client, {});
    expect((await read(client)).criteria).toBe('{"v":2}');
    await expect(put(client, { criteria: "[1]" })).rejects.toMatchObject({
      status: 422,
    });
    await expect(
      put(client, { criteria: `{"x":"${"a".repeat(8200)}"}` }),
    ).rejects.toMatchObject({ status: 422 });
    expect((await read(client)).criteria).toBe('{"v":2}');
  });
});
