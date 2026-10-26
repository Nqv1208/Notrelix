import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createItemExtrasApi } from "../../../../product/work-management/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

function setup() {
  const store = new MockStore({ ...defaultConfig, persona: "owner" });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, extras: createItemExtrasApi(client) };
}

describe("People an item can name, read from the producers that exist", () => {
  it("lists the workspace members and the teams for a board manager", async () => {
    const { extras } = setup();
    const directory = await extras.getPeopleDirectory(
      mockIds.boards.erp,
      mockIds.workspaces.primary,
    );
    expect(directory.people.length).toBeGreaterThan(0);
    expect(directory.canInvite).toBe(true);
    for (const person of directory.people) {
      expect(person.userId).toBeTruthy();
      expect(person.openCount).toBe(0);
    }
  });

  it("adds a person to the board through the board-member write", async () => {
    const { extras, store } = setup();
    const outsider = store
      .listUsers()
      .find((user) => user.id !== store.getCurrentUser().id)!;
    await expect(
      extras.invitePerson(mockIds.boards.erp, outsider.id),
    ).resolves.toBeUndefined();
  });
});
