import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createFormApi, serializeFormQuestionConfig } from "../api/form.api";

function apiWith(methods: Record<string, unknown>) {
  return createFormApi({ api: methods } as unknown as NotrelixClient);
}

describe("createFormApi", () => {
  it("reads a form's questions in order, with the options a choice carries", async () => {
    const get = vi.fn(async () => ({
      id: "f-1",
      name: "Yêu cầu hỗ trợ IT",
      boardId: "b-1",
      status: "Published",
      version: 4,
      description: "Mô tả",
      questions: [
        {
          id: "q2",
          questionKey: "p",
          label: "B",
          questionType: "Select",
          position: "a1",
          options: [{ key: "k", label: "L" }],
        },
        {
          id: "q1",
          questionKey: "title",
          label: "A",
          questionType: "ShortText",
          isRequired: true,
          position: "a0",
          maxLength: 120,
        },
        { label: "no id" },
      ],
    }));
    const form = await apiWith({ get }).get("f-1");
    expect(get).toHaveBeenCalledWith("/forms/f-1");
    expect(form.status).toBe("published");
    expect(form.questions.map((q) => q.id)).toEqual(["q1", "q2"]);
    expect(form.questions[0]).toMatchObject({
      required: true,
      type: "text",
      maxLength: 120,
    });
    expect(form.questions[1]).toMatchObject({
      type: "select",
      options: [{ key: "k", label: "L" }],
    });
  });

  it("lists the forms of a board", async () => {
    const get = vi.fn(async () => [
      {
        id: "f-1",
        name: "A",
        status: "Draft",
        createdAt: "2026-10-05T00:00:00Z",
      },
      { name: "no id" },
    ]);
    const rows = await apiWith({ get }).list("b-1");
    expect(get).toHaveBeenCalledWith("/boards/b-1/forms");
    expect(rows).toEqual([
      {
        id: "f-1",
        boardId: "b-1",
        title: "A",
        status: "draft",
        createdAt: "2026-10-05T00:00:00Z",
      },
    ]);
  });

  it("sends a new question with the producer's enum index and its config", async () => {
    const post = vi.fn(async () => undefined);
    await apiWith({ post }).addQuestion(
      "f-1",
      {
        key: "q_1",
        type: "select",
        label: "Ưu tiên",
        required: true,
        boardFieldId: null,
        position: "a0",
        options: [{ key: "o_1", label: "Cao" }],
      },
      3,
    );
    expect(post).toHaveBeenCalledWith("/forms/f-1/questions", {
      questionKey: "q_1",
      questionType: 4,
      label: "Ưu tiên",
      isRequired: true,
      configJson: JSON.stringify({
        required: true,
        options: [{ key: "o_1", label: "Cao" }],
      }),
      position: "a0",
      boardFieldId: null,
      expectedVersion: 3,
    });
  });

  it("names a submit attempt so a retry replays it", async () => {
    const post = vi.fn(async () => "sub-1");
    await apiWith({ post }).submit("f-1", { title: "x" }, "attempt-1");
    expect(post).toHaveBeenCalledWith(
      "/forms/f-1/submissions",
      { answers: { title: "x" } },
      { idempotencyKey: "attempt-1" },
    );
  });

  it("sends a reject reason only when there is one, and counts a deleted response out", async () => {
    const post = vi.fn(async () => undefined);
    const api = apiWith({ post });
    await api.reject("s-1", "  ");
    await api.reject("s-2", " Trùng ");
    expect(post.mock.calls).toEqual([
      ["/form-submissions/s-1/reject", { reason: null }],
      ["/form-submissions/s-2/reject", { reason: "Trùng" }],
    ]);
    const get = vi.fn(async () => [
      {
        id: "a",
        status: "Accepted",
        payloadJson: '{"title":"x"}',
        submittedAt: "t",
      },
      { id: "b", status: "Accepted", createdItemId: "i", submittedAt: "t" },
      { id: "c", status: "Deleted", submittedAt: "t" },
    ]);
    const rows = await apiWith({ get }).submissions("f-1");
    expect(rows.map((r) => [r.id, r.status])).toEqual([
      ["a", "new"],
      ["b", "processed"],
    ]);
    expect(rows[0]?.answers).toEqual({ title: "x" });
  });

  it("writes only the settings a question has", () => {
    expect(serializeFormQuestionConfig(false, {})).toBe('{"required":false}');
    expect(
      serializeFormQuestionConfig(true, { maxLength: 120, placeholder: "p" }),
    ).toBe('{"required":true,"placeholder":"p","maxLength":120}');
  });
});
