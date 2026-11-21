import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { ERP_FORM_IDS } from "../state/mock-erp-forms";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const BOARD = mockIds.boards.erp;
const FORM = ERP_FORM_IDS.support;
const PASS = "captcha-test-pass";
const ANSWERS = {
  title: "Cấp lại mật khẩu",
  priority: "mock-erp-priority-high",
};

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  const actAs = (persona: "owner" | "member" | "commenter") => {
    (store as unknown as { config: { persona: string } }).config.persona =
      persona;
  };
  const linksOn = (on: boolean) => {
    store.getWorkspaceSettings(
      store.getBoard(BOARD)!.workspaceId,
    ).allowPublicSharing = on;
  };
  const access = (enabled: boolean) =>
    client.api.put<{ isPublic: boolean; publicKey: string | null }>(
      endpoints.forms.publicAccess(FORM),
      { enabled },
    );
  const read = (key: string) =>
    client.api.get<{
      name: string;
      description: string | null;
      captchaSiteKey: string | null;
      questions: { key: string; type: string; options: unknown }[];
    }>(endpoints.publicForms.detail(key), { skipAuthRefresh: true });
  const send = (
    key: string,
    body: { answers?: unknown; captchaToken?: string; website?: string },
  ) =>
    client.api.post<null>(endpoints.publicForms.submissions(key), body, {
      skipAuthRefresh: true,
    });
  return { store, client, actAs, linksOn, access, read, send };
}

describe("Public forms in the mock world (WorkManagement.PublicForms.*)", () => {
  it("is private by default, and a key that opens nothing is the same 404", async () => {
    const { client, read } = setup();
    expect(await client.api.get(endpoints.forms.publicAccess(FORM))).toEqual({
      isPublic: false,
      publicKey: null,
    });
    await expect(read("nothing-here")).rejects.toMatchObject({ status: 404 });
  });

  it("lets only a workspace Owner or Admin read or switch it", async () => {
    const { client, actAs, linksOn, access } = setup();
    linksOn(true);
    actAs("member");
    await expect(
      client.api.get(endpoints.forms.publicAccess(FORM)),
    ).rejects.toMatchObject({ status: 403 });
    await expect(access(true)).rejects.toMatchObject({ status: 403 });
    // A person who cannot even read the board gets the same refusal as an unknown form.
    actAs("commenter");
    await expect(access(true)).rejects.toMatchObject({ status: 404 });
    actAs("owner");
    expect((await access(true)).isPublic).toBe(true);
  });

  it("refuses to switch on while the workspace has public links off, and never blocks switching off", async () => {
    const { access, linksOn } = setup();
    await expect(access(true)).rejects.toMatchObject({
      ruleCode: "WorkManagement_Form_PublicSharingDisabled",
    });
    linksOn(true);
    await access(true);
    linksOn(false);
    expect(await access(false)).toEqual({ isPublic: false, publicKey: null });
  });

  it("mints a key on enable, keeps it on a second enable, clears it on disable and mints a fresh one next time", async () => {
    const { access, linksOn, read } = setup();
    linksOn(true);
    const first = await access(true);
    expect(first.publicKey).toBeTruthy();
    expect((await access(true)).publicKey).toBe(first.publicKey);
    const page = await read(first.publicKey!);
    expect(page.name).toBe("Yêu cầu hỗ trợ IT");
    expect(page.captchaSiteKey).toBeTruthy();
    expect(page.questions.map((q) => q.key)).toContain("priority");

    expect(await access(false)).toEqual({ isPublic: false, publicKey: null });
    await expect(read(first.publicKey!)).rejects.toMatchObject({ status: 404 });

    const second = await access(true);
    expect(second.publicKey).toBeTruthy();
    expect(second.publicKey).not.toBe(first.publicKey);
    await expect(read(first.publicKey!)).rejects.toMatchObject({ status: 404 });
  });

  it("opens only a published form on a workspace that still allows public links", async () => {
    const { store, linksOn, access, read } = setup();
    linksOn(true);
    const { publicKey } = await access(true);
    store.forms.find(FORM)!.status = "Closed";
    await expect(read(publicKey!)).rejects.toMatchObject({ status: 404 });
    store.forms.find(FORM)!.status = "Published";
    expect((await read(publicKey!)).name).toBeTruthy();
    linksOn(false);
    await expect(read(publicKey!)).rejects.toMatchObject({ status: 404 });
  });

  it("takes a response with a good captcha and stores it for the inbox with no submitter", async () => {
    const { store, linksOn, access, send } = setup();
    linksOn(true);
    const { publicKey } = await access(true);
    const before = store.forms.submissions(FORM).length;
    expect(
      await send(publicKey!, { answers: ANSWERS, captchaToken: PASS }),
    ).toBeNull();
    const rows = store.forms.submissions(FORM);
    expect(rows).toHaveLength(before + 1);
    expect(rows[0]).toMatchObject({
      submitterUserId: null,
      status: "Accepted",
    });
    expect(JSON.parse(rows[0]!.payloadJson)).toEqual(ANSWERS);
  });

  it("answers 400 on the captcha token for anything but the passing token, before it looks at the answers", async () => {
    const { linksOn, access, send } = setup();
    linksOn(true);
    const { publicKey } = await access(true);
    for (const captchaToken of ["", "wrong", undefined])
      await expect(
        send(publicKey!, { answers: {}, captchaToken }),
      ).rejects.toMatchObject({
        status: 400,
        validationErrors: { captchaToken: expect.anything() },
      });
  });

  it("refuses bad answers by question key with a 400", async () => {
    const { linksOn, access, send } = setup();
    linksOn(true);
    const { publicKey } = await access(true);
    await expect(
      send(publicKey!, { answers: { email: "no-at" }, captchaToken: PASS }),
    ).rejects.toMatchObject({
      status: 400,
      validationErrors: {
        title: expect.anything(),
        priority: expect.anything(),
        email: expect.anything(),
      },
    });
  });

  it("answers a filled honeypot as a success and stores nothing", async () => {
    const { store, linksOn, access, send } = setup();
    linksOn(true);
    const { publicKey } = await access(true);
    const before = store.forms.submissions(FORM).length;
    await send(publicKey!, {
      answers: ANSWERS,
      captchaToken: PASS,
      website: "http://spam.test",
    });
    expect(store.forms.submissions(FORM)).toHaveLength(before);
  });

  it("limits a form to 5 sends a minute, with a ready sentence, and opens again after the minute", async () => {
    const { store, linksOn, access, send } = setup();
    linksOn(true);
    const { publicKey } = await access(true);
    for (let n = 0; n < 5; n += 1)
      await send(publicKey!, { answers: ANSWERS, captchaToken: PASS });
    await expect(
      send(publicKey!, { answers: ANSWERS, captchaToken: PASS }),
    ).rejects.toMatchObject({
      status: 429,
      message: "Bạn đã gửi quá nhiều lần. Vui lòng thử lại sau ít phút.",
    });
    store.chat.advanceVirtualSeconds(61);
    await send(publicKey!, { answers: ANSWERS, captchaToken: PASS });
  });
});
