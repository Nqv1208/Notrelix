import { describe, it, expect } from "vitest";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";
import { createNotrelixClient } from "@notrelix/contracts";
import type { MockPersona } from "../config/mock-config";

function clientFor(persona: MockPersona) {
  const store = new MockStore({
    seed: 1001,
    persona,
    state: "default",
    density: "normal",
    overlays: [],
    faultProfile: {},
    latency: "instant",
  });
  return createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
}

const base = `/workspaces/${mockIds.workspaces.primary}/members`;

describe("ViewMembers is denied to a Guest (WG-DEBT-002), the own row stays readable", () => {
  it("answers 403 to a Guest on the member list", async () => {
    const client = clientFor("viewer");
    await expect(client.api.get(base)).rejects.toMatchObject({ status: 403 });
  });

  it("returns a Guest only their own row from members/me", async () => {
    const client = clientFor("viewer");
    const me = await client.api.get<{
      userId: string;
      role: string;
      status: string;
    }>(`${base}/me`);
    expect(me).toMatchObject({
      userId: mockIds.users.viewer,
      role: "Guest",
      status: "Active",
    });
  });

  it("still lists members for a Member", async () => {
    const client = clientFor("member");
    const list = await client.api.get<unknown[]>(base);
    expect(list.length).toBeGreaterThan(1);
  });
});
