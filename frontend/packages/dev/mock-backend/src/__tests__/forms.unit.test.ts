import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { ERP_FORM_IDS } from "../state/mock-erp-forms";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const BOARD = mockIds.boards.erp;
const FORM = ERP_FORM_IDS.support;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  const actAs = (persona: "owner" | "member" | "viewer" | "commenter") => {
    (store as unknown as { config: { persona: string } }).config.persona =
      persona;
  };
  return { client, actAs };
}

const key = () => ({ headers: { "Idempotency-Key": crypto.randomUUID() } });

describe("Forms in the mock world (WorkManagement.Forms.*)", () => {
  it("lists the board's forms and reads one with its questions in order", async () => {
    const { client } = setup();
    const rows = await client.api.get<{ id: string; status: string }[]>(
      endpoints.forms.forBoard(BOARD),
    );
    expect(rows.map((row) => row.status).sort()).toEqual([
      "Closed",
      "Draft",
      "Published",
    ]);
    const detail = await client.api.get<{
      questions: { questionKey: string }[];
    }>(endpoints.forms.detail(FORM));
    expect(detail.questions.map((q) => q.questionKey)).toEqual([
      "title",
      "description",
      "priority",
      "due",
      "email",
    ]);
  });

  it("takes a response only from a member who can create items, refuses bad answers by question key", async () => {
    const { client, actAs } = setup();
    actAs("viewer");
    await expect(
      client.api.post(
        endpoints.forms.submissions(FORM),
        { answers: { title: "x" } },
        key(),
      ),
    ).rejects.toMatchObject({ status: 403 });

    actAs("owner");
    await expect(
      client.api.post(
        endpoints.forms.submissions(FORM),
        { answers: { email: "no-at" } },
        key(),
      ),
    ).rejects.toMatchObject({
      validationErrors: {
        title: expect.anything(),
        priority: expect.anything(),
        email: expect.anything(),
      },
    });

    const attempt = crypto.randomUUID();
    const answers = { title: "Cấp quyền", priority: "mock-erp-priority-high" };
    const first = await client.api.post<string>(
      endpoints.forms.submissions(FORM),
      { answers },
      { headers: { "Idempotency-Key": attempt } },
    );
    const replay = await client.api.post<string>(
      endpoints.forms.submissions(FORM),
      { answers },
      { headers: { "Idempotency-Key": attempt } },
    );
    expect(replay).toBe(first);
    const list = await client.api.get<{ id: string }[]>(
      endpoints.forms.submissions(FORM),
    );
    expect(list.filter((row) => row.id === first)).toHaveLength(1);
  });

  it("refuses a response to a draft or closed form", async () => {
    const { client } = setup();
    for (const id of [ERP_FORM_IDS.purchase, ERP_FORM_IDS.bug])
      await expect(
        client.api.post(
          endpoints.forms.submissions(id),
          { answers: { title: "x" } },
          key(),
        ),
      ).rejects.toMatchObject({ status: 409 });
  });

  it("decides a response once: process, reject with an optional reason, spam, delete", async () => {
    const { client, actAs } = setup();
    const response = "mock-fsub-1";
    actAs("commenter");
    await expect(
      client.api.post(endpoints.forms.spam(response), {}, key()),
    ).rejects.toMatchObject({ status: 403 });
    actAs("owner");
    await client.api.post(
      endpoints.forms.reject("mock-fsub-2"),
      { reason: null },
      key(),
    );
    await client.api.post(
      endpoints.forms.process(response),
      { createdItemId: "mock-erp-item-151" },
      key(),
    );
    await expect(
      client.api.post(endpoints.forms.reject(response), {}, key()),
    ).rejects.toMatchObject({ status: 409 });
    await client.api.delete(endpoints.forms.submission("mock-fsub-3"));
    const list = await client.api.get<
      {
        id: string;
        status: string;
        reason: string | null;
        createdItemId: string | null;
      }[]
    >(endpoints.forms.submissions(FORM));
    expect(list.find((row) => row.id === "mock-fsub-2")).toMatchObject({
      status: "Rejected",
      reason: null,
    });
    expect(list.find((row) => row.id === response)).toMatchObject({
      createdItemId: "mock-erp-item-151",
    });
    expect(list.some((row) => row.id === "mock-fsub-3")).toBe(false);
  });

  it("builds a form: publish needs a question, deleting is soft and restorable", async () => {
    const { client } = setup();
    const id = await client.api.post<string>(
      endpoints.forms.forBoard(BOARD),
      { title: "Yêu cầu cấp quyền" },
      key(),
    );
    // Every form write names the version it read: 1 when new, one more after each change.
    await expect(
      client.api.post(
        `${endpoints.forms.publish(id)}?expectedVersion=1`,
        {},
        key(),
      ),
    ).rejects.toMatchObject({ status: 409 });
    await client.api.post(
      endpoints.forms.questions(id),
      {
        questionKey: "title",
        questionType: 0,
        label: "Cần gì?",
        isRequired: true,
        configJson: '{"required":true}',
        position: "a0",
        expectedVersion: 1,
      },
      key(),
    );
    await client.api.post(
      `${endpoints.forms.publish(id)}?expectedVersion=2`,
      {},
      key(),
    );
    await client.api.delete(`${endpoints.forms.detail(id)}?expectedVersion=3`);
    expect(
      (await client.api.get<unknown[]>(endpoints.forms.forBoard(BOARD))).length,
    ).toBe(3);
    await client.api.post(endpoints.forms.restore(id), {}, key());
    expect(
      (await client.api.get<unknown[]>(endpoints.forms.forBoard(BOARD))).length,
    ).toBe(4);
  });
});
