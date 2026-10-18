import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderPureUi, screen, waitFor } from "@notrelix/testing";

import { comment } from "../../../verification/collaboration-ui-fixtures";
import { ResourceComments } from "../resource-comments";

describe("collaboration web container", () => {
  it("names each delete action after its comment, never its id", async () => {
    const dtos = [
      comment({
        id: "comment-a",
        authorId: "current-user",
        authorName: "You",
        body: "First note",
      }),
      comment({
        id: "comment-b",
        authorId: "current-user",
        authorName: "You",
        body: "Second note",
      }),
    ];
    const api = {
      get: vi.fn().mockResolvedValue(dtos),
      post: vi.fn(),
      delete: vi.fn(),
    };

    renderPureUi(
      <QueryClientProvider
        client={
          new QueryClient({ defaultOptions: { queries: { retry: false } } })
        }
      >
        <ResourceComments
          resourceId="res-1"
          resourceType="page"
          currentUserId="current-user"
          api={api}
        />
      </QueryClientProvider>,
    );

    await waitFor(() =>
      expect(
        screen.getAllByRole("button", { name: /^Delete comment/ }),
      ).toHaveLength(2),
    );
    const names = screen
      .getAllByRole("button", { name: /^Delete comment/ })
      .map((button) => button.getAttribute("aria-label"));

    expect(names).toEqual([
      'Delete comment by You: "First note"',
      'Delete comment by You: "Second note"',
    ]);
    expect(names.join(" ")).not.toMatch(/comment-[ab]/);
  });
});
