import { describe, expect, it } from "vitest";
import type { PageDtoApi } from "../dto";
import { mapPage } from "../model/page.mapper";

const dto: PageDtoApi = {
  id: "page-1",
  workspaceId: "ws-1",
  title: "Playbook",
  createdAt: "2026-10-01T00:00:00.000Z",
};

describe("Page canEdit (the server's decision for a write of the page)", () => {
  it("keeps what the producer says, so a Viewer reads false and an Editor true", () => {
    expect(mapPage({ ...dto, canEdit: false }).canEdit).toBe(false);
    expect(mapPage({ ...dto, canEdit: true }).canEdit).toBe(true);
  });

  it("stays absent when the producer says nothing, so the host falls back to the role", () => {
    expect(mapPage(dto).canEdit).toBeUndefined();
    expect(mapPage({ ...dto, canEdit: null }).canEdit).toBeUndefined();
  });
});
