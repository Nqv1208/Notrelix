import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type {
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import {
  FieldOptionWriteError,
  runAddFieldOption,
  runUpdateFieldOption,
  type FieldOptionWriter,
} from "../mutations/use-field-option";

const FIELD = "field-status";

function statusField(version: number | null = 3): FieldDefinition {
  return {
    id: FIELD,
    boardId: "b-1",
    name: "Trạng thái",
    fieldType: "select",
    semanticRole: "status",
    position: 1,
    isHidden: false,
    isSystemField: true,
    ...(version === null ? {} : { version }),
    options: [
      {
        id: "o-todo",
        label: "Cần làm",
        color: "#7A8391",
        position: "a0",
        category: "todo",
      },
      {
        id: "o-doing",
        label: "Đang làm",
        color: "#1E7BE0",
        position: "a1",
        category: "in_progress",
      },
    ],
  };
}

function fullBoard(field: FieldDefinition): FullBoardResponse {
  return {
    board: {
      id: "b-1",
      workspaceId: "ws-1",
      title: "Board",
      background: { type: "color", value: "#fff" },
      visibility: "workspace",
      isArchived: false,
      fieldDefinitions: [field],
      members: [],
      createdAt: "2026-10-01T00:00:00.000Z",
    },
    groups: [],
    fieldDefinitions: [field],
  };
}

function seeded(field = statusField()) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  const key = wmQueryKeys.fullBoard("ws-1", "b-1");
  queryClient.setQueryData(key, fullBoard(field));
  return { queryClient, key };
}

const target = { workspaceId: "ws-1", boardId: "b-1", fieldId: FIELD };
const draft = {
  name: "Chờ khách duyệt",
  color: "#C18A12",
  category: "in_progress" as const,
};

function options(queryClient: QueryClient, key: readonly unknown[]) {
  return queryClient
    .getQueryData<FullBoardResponse>(key)!
    .board.fieldDefinitions[0]!.options.map((option) => option.label);
}

describe("field option commands", () => {
  it("adds the option last at once and sends the field's version with an idempotency key", async () => {
    const { queryClient, key } = seeded();
    let during: string[] = [];
    const writer: FieldOptionWriter = {
      addOption: vi.fn(async () => {
        during = options(queryClient, key);
      }),
      updateOption: vi.fn(),
    };
    await runAddFieldOption(queryClient, writer, target, draft);
    expect(during).toEqual(["Cần làm", "Đang làm", "Chờ khách duyệt"]);
    expect(writer.addOption).toHaveBeenCalledWith(
      {
        boardId: "b-1",
        fieldId: FIELD,
        name: "Chờ khách duyệt",
        color: "#C18A12",
        category: "in_progress",
        expectedVersion: 3,
      },
      expect.objectContaining({
        idempotencyKey: expect.stringMatching(/^add-field-option:/),
      }),
    );
  });

  it("puts the field back and rethrows when the write fails", async () => {
    const { queryClient, key } = seeded();
    const writer: FieldOptionWriter = {
      addOption: vi.fn(async () => {
        throw new Error("412");
      }),
      updateOption: vi.fn(),
    };
    await expect(
      runAddFieldOption(queryClient, writer, target, draft),
    ).rejects.toThrow("412");
    expect(options(queryClient, key)).toEqual(["Cần làm", "Đang làm"]);
  });

  it("invalidates the board read after success and after failure", async () => {
    const { queryClient, key } = seeded();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    const failing: FieldOptionWriter = {
      addOption: vi.fn(async () => {
        throw new Error("x");
      }),
      updateOption: vi.fn(async () => undefined),
    };
    await runUpdateFieldOption(
      queryClient,
      failing,
      { ...target, optionId: "o-doing" },
      draft,
    );
    await runAddFieldOption(queryClient, failing, target, draft).catch(
      () => undefined,
    );
    expect(invalidate).toHaveBeenCalledTimes(2);
    expect(invalidate).toHaveBeenCalledWith({ queryKey: key });
  });

  it("renames and recolours one option at once and restores it on failure", async () => {
    const { queryClient, key } = seeded();
    let during: string[] = [];
    const writer: FieldOptionWriter = {
      addOption: vi.fn(),
      updateOption: vi.fn(async () => {
        during = options(queryClient, key);
        throw new Error("409");
      }),
    };
    await expect(
      runUpdateFieldOption(
        queryClient,
        writer,
        { ...target, optionId: "o-doing" },
        { name: "Đang triển khai", color: "#2E8B57", category: "in_progress" },
      ),
    ).rejects.toThrow("409");
    expect(during).toEqual(["Cần làm", "Đang triển khai"]);
    expect(options(queryClient, key)).toEqual(["Cần làm", "Đang làm"]);
  });

  it("fails closed without a field version or an unknown field and sends nothing", async () => {
    const writer: FieldOptionWriter = {
      addOption: vi.fn(),
      updateOption: vi.fn(),
    };
    const noVersion = seeded(statusField(null));
    await expect(
      runAddFieldOption(noVersion.queryClient, writer, target, draft),
    ).rejects.toMatchObject({ reason: "version-unknown" });
    const unknown = seeded();
    await expect(
      runAddFieldOption(
        unknown.queryClient,
        writer,
        { ...target, fieldId: "missing" },
        draft,
      ),
    ).rejects.toBeInstanceOf(FieldOptionWriteError);
    expect(writer.addOption).not.toHaveBeenCalled();
    expect(options(noVersion.queryClient, noVersion.key)).toEqual([
      "Cần làm",
      "Đang làm",
    ]);
  });
});
