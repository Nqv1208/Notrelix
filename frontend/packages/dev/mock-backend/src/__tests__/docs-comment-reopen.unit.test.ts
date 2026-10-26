import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { createCommentApi } from "../../../../product/docs/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { createMockFetch } from "../transport/create-mock-fetch";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return {
    store,
    comments: createCommentApi(client.api as never, endpoints as never),
  };
}

describe("Collaboration.Comments.Reopen on the mock backend", () => {
  it("makes a resolved thread active again and leaves an active one as it is", async () => {
    const { store, comments } = setup();
    const comment = store.createPageComment(INVOICE_DOC_ID, "usr-m-00001", "x");

    await comments.resolve(comment.id);
    expect(comment.resolvedAt).toBeTruthy();

    await comments.reopen(comment.id);
    expect(comment.resolvedAt).toBeNull();

    await comments.reopen(comment.id);
    expect(comment.resolvedAt).toBeNull();
  });

  it("answers not found for a thread that is not there", async () => {
    const { comments } = setup();
    await expect(comments.reopen("missing")).rejects.toMatchObject({
      kind: "not_found",
    });
  });
});
