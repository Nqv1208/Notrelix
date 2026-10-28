import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createColumnApi } from "../api/field.api";
import { createCardApi } from "../api/item.api";

describe("the item sheet's writes", () => {
  it("creates a field and answers its id, sending the domain's name for a multi select", async () => {
    const post = vi.fn(async () => "f-1");
    const id = await createColumnApi({
      api: { post },
    } as unknown as NotrelixClient).createColumn({
      boardId: "b-1",
      name: "Nhà cung cấp",
      fieldType: "multi_select",
    });

    expect(id).toBe("f-1");
    expect(post).toHaveBeenCalledWith(
      "/boards/b-1/fields",
      expect.objectContaining({ name: "Nhà cung cấp", type: "multiselect" }),
      expect.anything(),
    );
  });

  it("deletes a stored file through the attachment route", async () => {
    const del = vi.fn(async () => undefined);
    await createCardApi({
      api: { delete: del },
    } as unknown as NotrelixClient).deleteCardFile("i-1", "a-1");

    expect(del).toHaveBeenCalledWith("/board-items/i-1/attachments/a-1");
  });
});
